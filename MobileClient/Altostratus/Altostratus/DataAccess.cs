using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Altostratus.Model
{    
    // The singleton Database class provides methods to abstract queries to the SQLite database.
    // It doesn't concern itself with data binding as that's the job of the DataModel class.
    //
    // Note: this implementation is derived from Xamarin's To Do PCL example but is rewritten to use
    // the asynchronous SQLite API. The primary changes from the sychronous API to the async API are:
    //   1. No need to use a lock around SQLite calls.
    //   2. Database initialization code went into InitAsync, which has to be called from another
    //      async function, e.g. the App's OnStart.
    //   3. Functions that end in a single async call are implemented to return Task<T>. These do not
    //      use await and are not therefore marked as async.
    //   4. Functions that involve multiple async calls, do processing after an await, or have no
    //      return value are marked as async and return Task<T>.    

    public class DataAccessLayer
    {
        static object locker = new object();
        static SQLiteAsyncConnection database;        
        public static DataAccessLayer Current { get { return _current; } }
        public static DataAccessLayer _current = null;

        #region Constructors
        // The optional argument (and InitAsync) are used by the DBInitialize console program to bypass the
        // Xamarin.Forms DependencyService.         
        public DataAccessLayer(SQLiteAsyncConnection db = null)
        {
            if (db == null)
            {
                String path = DependencyService.Get<ISQLite>().GetDatabasePath();
                db = new SQLiteAsyncConnection(path);                

                // Alternate use to use the synchronous SQLite API:
                // database = SQLiteConnection(path);                
            }

            database = db;
            _current = this;
        }

        public async Task InitAsync()
        {
            // Note that the data types used in CreateTableAsync need to be simple classes without any properties
            // of complex classes like System.Uri. If you try to use such classes, CreateTableasync will throw an
            // exception without any decent information as to why.

            await database.CreateTableAsync<UserPreferences>();
            await database.CreateTableAsync<AuthProvider>();
            await database.CreateTableAsync<Timestamp>();
            await database.CreateTableAsync<Category>();
            await database.CreateTableAsync<Item>();
        }

        #endregion

        #region UserPreferences Table
        // UserPreferences is a single-row table.
        public Task<UserPreferences> GetUserPreferencesAsync()
        {
            return database.Table<UserPreferences>().FirstOrDefaultAsync();
        }

        public async Task SetUserPreferencesAsync(UserPreferences prefs)
        {
            UserPreferences current = await GetUserPreferencesAsync();

            if (current == null)
            {                
                await database.InsertAsync(prefs);
            }
            else
            {
                String query = "update UserPreferences set UserID='" + prefs.UserID + "' ";
                query += ", AccessToken='" + prefs.AccessToken + "' ";
                query += ", ProviderName='" + prefs.ProviderName + "' ";
                query += ", ProviderUri='" + prefs.ProviderUri + "' ";
                query += ", ConversationLimit='" + prefs.ConversationLimit + "' ";
                query += ", ActiveCategories='" + prefs.ActiveCategories + "'";                                    
                await database.QueryAsync<UserPreferences>(query);
            }        
        }
        #endregion

        #region AuthProviders Table
        public Task<List<AuthProvider>> GetAuthProvidersAsync()
        {            
            return database.Table<AuthProvider>().ToListAsync();                
        }

        public async Task UpdateOrAddAuthProviderAsync(AuthProvider ap)
        {            
            List<AuthProvider> results = await database.QueryAsync<AuthProvider>("select * from AuthProvider where Name='" + ap.Name + "'");
            AuthProvider current = results.FirstOrDefault<AuthProvider>();            

            if (current != null)
            {                
                await database.UpdateAsync(ap);
            }
            else
            {                
                await database.InsertAsync(ap);
            }
        }

        public Task<int> DeleteAuthProviderAsync(String name)
        {
            return database.DeleteAsync(new AuthProvider() { Name = name });
        }
        #endregion

        #region Timestamp Table
        // Timestamp is a single-row table
        public Task<Timestamp> GetTimestampAsync()
        {
            return database.Table<Timestamp>().FirstOrDefaultAsync();
        }

        public async Task SetTimestampAsync(Timestamp t)
        {
            Timestamp current = await GetTimestampAsync();

            if (current == null)
            {                    
                await database.InsertAsync(t);
            }
            else
            {
                await database.QueryAsync<Timestamp>("update Timestamp set Stamp='" + t.Stamp + "'");
            }
        }
        #endregion

        #region Category Table
        public Task<List<Category>> GetCategoriesAsync(CategorySet set = CategorySet.All)
        {
            String query = "select * from Category";

            switch (set)
            {
                case CategorySet.All:
                default:
                    break;

                case CategorySet.Active:
                    query += " where IsActive = 1";
                    break;
            }

            return database.QueryAsync<Category>(query);                
        }

        public async Task<IEnumerable<String>> GetCategoryNamesAsync(CategorySet set = CategorySet.All)
        {
            IEnumerable<Category> categories = await GetCategoriesAsync(set);            
            return categories.Select(c => c.Name);            
        }

        public async Task UpdateOrAddCategoryAsync(Category s, CategoryKeepActive keepIsActive)
        {            
            List<Category> results = await database.QueryAsync<Category>("select * from Category where Name='" + s.Name + "'");
            Category current = results.FirstOrDefault<Category>();

            if (current != null)
            {
                // If we want to retain the current IsActive flag, copy it from the current record
                // before doing an update.
                if (keepIsActive == CategoryKeepActive.Keep)
                {
                    s.IsActive = current.IsActive;
                }

                await database.UpdateAsync(s);
            }
            else
            {
                await database.InsertAsync(s);
            }
        }

        public Task<int> DeleteCategoryAsync(String name)
        {
            return database.DeleteAsync(new Category() { Name = name });
        }
        #endregion

        #region Item Table        
        public Task<List<Item>> GetItemsAsync()
        {
            return database.Table<Item>().ToListAsync();
        }
        
        // GetItemListForCategoryAsync queries for item data appropriate for UI display, which is
        // only the title, description, provider, and uri (the primary key). 
        public async Task<IEnumerable<Item>> GetItemListForCategoryAsync(String category, Int32 itemLimit)
        {
            // Note: Using LINQ with SQLite.Net retrieves the full item data regardless of what's indicated
            // in the select clause, so we use the Query method instead to retrieve only partial data.                
            String query = "select Title, Description, Provider, Url from Item where Category='" 
                + category + "' order by LastUpdated DESC"
                + " limit " + itemLimit;

            IEnumerable<Item> list = (await database.QueryAsync<Item>(query)).ToList();
            return list;
        }

        public Task<Item> GetItemAsync(String uri)
        {
            var query = database.Table<Item>().Where(x => x.Url == uri);
            return query.FirstOrDefaultAsync();                
        }
       
        public async Task<String> UpdateOrAddItemAsync(Item item)
        {
            Item current = await GetItemAsync(item.Url);

            if (current != null)
            {
                await database.UpdateAsync(item);
                return item.Url;
            }
            else
            {
                await database.InsertAsync(item);
                return item.Url;
            }
        }

        public Task<int> DeleteItemAsync(String url)
        {
            return database.DeleteAsync(new Item() { Url = url } );
        }

        // Enforces keeping only a limited number of items per category in the table.
        public async Task ApplyConversationLimitAsync(Int32 limit)
        {
            var categories = await GetCategoryNamesAsync();

            foreach (String c in categories)
            {
                AsyncTableQuery<Item> catItems;

                catItems = database.Table<Item>().Where(item => item.Category == c);
                    
                Int32 itemsToDelete = (await catItems.CountAsync() - limit);

                if (itemsToDelete > 0)
                {
                    // We need to delete some, so sort by descending age and then knock off whatever is
                    // at the position of limit until we get the count down.
                    List<Item> sorted = await catItems.OrderByDescending<DateTime>(item => item.LastUpdated).ToListAsync();

                    for (var i = 0; i < itemsToDelete; i++)
                    {
                        await database.DeleteAsync(sorted.ElementAt(limit).Url);
                    }
                }
            }               
        }
        #endregion
    }
}
