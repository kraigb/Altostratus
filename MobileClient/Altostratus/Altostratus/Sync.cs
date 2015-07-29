using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Altostratus.Model
{
    public enum SyncExtent
    {
        None = 0,
        Items,
        SettingsAndItems, 
        All
    }

    public enum SyncResult
    {
        Success = 0,
        NoContent,
        AlreadyRunning,
        NotRun,
        Failed
    }


    public partial class DataModel : BindableBase
    {
        public async Task<SyncResult> SyncSettings()
        {
            Boolean success = await this.Configuration.ApplyBackendConfiguration();
            return success ? SyncResult.Success : SyncResult.Failed;
        }


        public async Task<SyncResult> SyncCategories()
        {
            // Current set of categories in the database so we can check if any have been removed.
            List<String> currentList = (await DataAccessLayer.Current.GetCategoryNamesAsync()).ToList();
            SyncResult result = SyncResult.Failed;
            HttpResponseMessage response;

            try
            {                
                response = await WebAPI.GetCategories();

                if (response.IsSuccessStatusCode)
                {
                    String[] Categories = await response.Content.ReadAsAsync<String[]>();

                    foreach (String s in Categories)
                    {
                        // Add the category to the database
                        await DataAccessLayer.Current.UpdateOrAddCategoryAsync(new Category()
                            { Name = s, IsActive = true }, CategoryKeepActive.Keep);

                        //Remove from the current list.
                        currentList.Remove(s);
                    }

                    // If there are any categories remaining in currentList, then they've been removed
                    // from the backend and we need to delete those records from our database.
                    foreach (String s in currentList)
                    {
                        await DataAccessLayer.Current.DeleteCategoryAsync(s);
                    }

                    // Refresh the Configuration object
                    await Configuration.PopulateCategoriesFromDB();                        
                    result = SyncResult.Success;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SyncCategories failed: " + ex.Message);                
            }
            
            return result;
        }


        public async Task<SyncResult> SyncAuthProviders()
        {
            // Current set of providers in the database so we can check if any have been removed.
            List<String> currentList = (await DataAccessLayer.Current.GetAuthProvidersAsync()).Select(a => a.Name).ToList();
            SyncResult result = SyncResult.Failed;
            HttpResponseMessage response;

            try
            {                
                response = await WebAPI.GetAuthProviders();

                if (response.IsSuccessStatusCode)
                {
                    var providers = await response.Content.ReadAsAsync<IEnumerable<AuthProvider>>();

                    foreach (AuthProvider ap in providers)
                    {
                        // Add provider to the database and remove from the current list
                        await DataAccessLayer.Current.UpdateOrAddAuthProviderAsync(ap);
                        currentList.Remove(ap.Name);
                    }

                    // If there are any providers remaining in currentList, then they've been removed
                    // from the backend and we need to delete those records from our database.
                    foreach (String s in currentList)
                    {
                        await DataAccessLayer.Current.DeleteAuthProviderAsync(s);
                    }

                    await Configuration.PopulateAuthProvidersFromDB();
                    result = SyncResult.Success;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SyncAuthProviders failed: " + ex.Message);                
            }

            return result;
        }


        public void SyncCancel()
        {
            if (syncTask != null)
            {
                syncTokenSource.Cancel();
                syncToken.WaitHandle.WaitOne();
            }
        }

        public async Task<SyncResult> SyncItems()
        {
            SyncResult result;

            // Avoid starting a redundant task
            if (syncTask != null)
            {
                return SyncResult.AlreadyRunning;
            }

            try
            {                
                syncTokenSource = new CancellationTokenSource();
                syncToken = syncTokenSource.Token;

                //Save the task object so we can guard against reentrancy.
                syncTask = SyncItemsCore();
                result = await syncTask;
            }
            catch (Exception ex)
            {
                // If callers aren't awaiting on us, it's important to catch exceptions here.
                Debug.WriteLine("SyncItems failed: " + ex.Message);                
                result = SyncResult.Failed;
            }

            // Clear the task so we can run an item sync again.
            syncTask = null;

            // Run a background cleanup to remove items in excess of our max conversation limit. We could also
            // remove those in excess of our current limit, but if the user increases the limit we'd have to
            // retrieve those items again. Because the storage overhead for 100 items isn't much, and because we
            // know that our backend supports only a handful of categories and not hundreds or even dozens, we can 
            // just set the limit to the maximum. Note that the PopulateGroupedItemsFromDB method we just called 
            // automatically applies the current user limit to the query results. In short, this call is just about
            // what we maintain in our database cache and has no impact on what's displayed in the UI.            
            try
            {
                // No need to await this at all, but be sure to try/catch in case of exceptions. 
                var ignoreTask = DataAccessLayer.Current.ApplyConversationLimitAsync(UserPreferences.ConversationMax);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in Database.ApplyConversationLimit: " + ex.Message);
            }
            
            return result;
        }

        private async Task<SyncResult> SyncItemsCore()
        {
            SyncResult result = SyncResult.Success;
            HttpResponseMessage response;
            
            // Retrieve previous request timestamp to pass to the next request (limiting the size of response).
            Timestamp t = await DataAccessLayer.Current.GetTimestampAsync();

            // Remember when we made this new request. Worst case is that this timestamp is slightly earlier
            // than when the backend actually does the processing, meaning that we might get back a few 
            // redundant items. In those cases. the items will just be updated in the database.
            String newRequestTimestamp = DateTime.UtcNow.ToString(WebAPIConstants.ItemsFeedTimestampFormat);
            response = await WebAPI.GetItems(t, syncToken);             
            
            if (!response.IsSuccessStatusCode)
            {
                return SyncResult.Failed;
            }

            // We got a response, so remember the timestamp in the database for the next sync.            
            t = new Timestamp() { Stamp = newRequestTimestamp };
            await DataAccessLayer.Current.SetTimestampAsync(t);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return SyncResult.NoContent;
            }
            
            var items = await response.Content.ReadAsAsync<IEnumerable<FeedItem>>();            
            await ProcessItems(items);

            // Sync is done, refresh the ListView data source.
            await PopulateGroupedItemsFromDB();

            return result;
        }

        // Convert each FeedItem from the HTTP request into an Item and store in the Database.
        private async Task ProcessItems(IEnumerable<FeedItem> items) 
        {
            //Check if there's anything to do
            if (items.Count() == 0)
            {
                Debug.WriteLine("ProcessItems returning--no items contained in response.");
                return;
            }

            // Note: the backend automatically limits items per category either to its default of 100 for
            // a non-authenticated user, or to the user preference if authenticated. For this reason we
            // can just add the items to the database knowing that they're also the most recent. 
            String description; 

            foreach (FeedItem fi in items)
            {
                // The description is just the first 100 Body chars stripped of HTML markup (and \n), with an added "..." added. 
                // We could, of course, have the backend create this description for us, which would mean trading a little processing
                // time for a larger data set. Depending on the data, it might be the right choice.
                description = StripHTML(fi.Body.Substring(0, Math.Min(fi.Body.Length, 100))) + "...";
                await DataAccessLayer.Current.UpdateOrAddItemAsync(new Item()
                {
                    LastUpdated = fi.LastUpdated,
                    Title = CleanEscapes(fi.Title),
                    Description = description,
                    Body = fi.Body,
                    Url = fi.Url,
                    Provider = fi.ProviderName,
                    Category = fi.CategoryName
                });
            }
        }

        // We can get &#39; and &quot; for quotes in title strings, so this fixes those up. Could also do 
        // this on the backend. This function could be extended for other cleanup instances if encountered.
        // Note that this work could be done on the backend, but there could be scenarios where platform-specific
        // work is desired, in which case we want this operation to take place on the client that ultimately
        // owns the UI.
        private String CleanEscapes(String source)
        {            
            return source.Replace("&#39;", "\"").Replace("&quot;", "\"");
        }


        // Code from http://stackoverflow.com/questions/286813/how-do-you-convert-html-to-plain-text for a 
        // quick-and-dirty way to clean up HTML into plainish text by stripping out <>'s and newlines (\n).
        private String StripHTML(string source)
        {
            return Regex.Replace(source, "<[^>]*>", string.Empty).Replace("\n", " ");
        }
    }
}
