using Altostratus.Model;
using Altostratus.Properties;
using Connectivity.Plugin;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Altostratus
{
    // An empty task definition is a useful default for working with tasks that might not
    // need to be run; null doesn't work for functions like Task.WhenAll or WhenAny.
    
    public static class Empty<T>
    {
        public static Task<T> Task { get { return _task; } }
        private static readonly Task<T> _task = System.Threading.Tasks.Task.FromResult(default(T));
    }


    class HomeViewModel : BindableBase
    {
        DataModel dataModel;
        Boolean isConfigured;
        Boolean isOffline = false;
        Boolean isSyncing = false;
        Boolean syncWhenReconnected = false;
        SyncExtent syncWhenReconnectedExtent = SyncExtent.Items;
        Boolean applyConfiguration = false;        
        DateTime lastSync = DateTime.Now;

        public Command Command_Refresh { protected set; get; }
        public Command Command_Configure { protected set; get; }
        
        public HomeViewModel()
        {
            dataModel = App.DataModel;

            // Create commands for the UI
            this.Command_Refresh = new Command(async () =>
            {
                TimeSpan elapsed = DateTime.Now.Subtract(lastSync);
                await CheckTimeAndSync(elapsed, true);
            }, () =>
            {
                //This determines the return value of CanExecute
                return !isOffline && !isSyncing;
            });

            this.Command_Configure = new Command(() =>
            {
                // Navigate to configuration page. Configuration page will
                // accept changes and refresh the data model if needed.
                this.Navigation.PushAsync(new ConfigurationPage());
            });


            // Initialize Xamarin connectivity plugin. If we're offline when we first initialize this
            // view model, remember that in syncWhenReconnected so we can sync when online again (see
            // ConnectivityChanged handler).
            IsOffline = !CrossConnectivity.Current.IsConnected;
            syncWhenReconnected = isOffline;            
            CrossConnectivity.Current.ConnectivityChanged += ConnectivityChanged;
            
            // We fire and forget our initial synchronization, so we can skip the await.
            Task ignoreTask = Sync(SyncExtent.All);
        }

        // Sharing the Navigation object from the view to the ViewModel is a suggestion
        // from the Xamarin forums to allow navigation from within a command handler here.
        // See https://forums.xamarin.com/discussion/21822/call-navigation-pushasync-from-viewmodel        
        public INavigation Navigation { get; set; }

        // Note: for simplicity, resource lookups are part of the viewmodel instead of using a markup
        // extension as described on http://developer.xamarin.com/guides/cross-platform/xamarin-forms/localization/.        
        public String PageTitle { get { return Resources.Home_PageTitle; } }
        public String NoCategoriesMessage1 { get { return Resources.Home_NoCategoriesMessage1;  } }
        public String NoCategoriesMessage2 { get { return Resources.Home_NoCategoriesMessage2; } }
        public String OfflineMessage { get { return Resources.Home_OfflineMessage; } }
        public String ToolbarItemText_Configure { get { return Resources.Home_ToolbarItem_Configure;  } }
        public String ToolbarItemText_Refresh { get { return Resources.Home_ToolbarItem_Refresh; } }

        public Boolean NoCategoriesMessageVisible
        {
            get { return dataModel.GroupedItems.Count == 0; }        
        }

        private async void ConnectivityChanged(object sender, global::Connectivity.Plugin.Abstractions.ConnectivityChangedEventArgs e)
        {
            Debug.WriteLine("Connectivity changed: IsConnected = " + e.IsConnected);            

            if (e.IsConnected)
            {
                IsOffline = false;

                if (syncWhenReconnected)
                {
                    syncWhenReconnected = false;
                    await Sync(syncWhenReconnectedExtent);
                }                
            } 
            else
            {
                IsOffline = true;
                dataModel.SyncCancel();
            }
        }


        public Boolean IsSyncing
        {
            get { return isSyncing; }
            set
            {
                SetProperty(ref isSyncing, value);
                Command_Refresh.ChangeCanExecute();
            }
        }

        public Boolean IsOffline
        {
            get { return isOffline; }
            set
            {                
                SetProperty(ref isOffline, value);                
                Command_Refresh.ChangeCanExecute();
            }
        }

        public GroupList GroupedItems
        {
            get { return dataModel.GroupedItems; }
        }

        public Boolean IsConfigured
        {
            get { return isConfigured; }
            set { SetProperty(ref isConfigured, value); }
        }


        public async Task CheckTimeAndSync(TimeSpan elapsed, Boolean alwaysDoItems)
        {            
            SyncExtent extent = alwaysDoItems ? SyncExtent.Items : SyncExtent.None;

            if (elapsed.TotalMinutes >= 30)
            {
                extent = SyncExtent.SettingsAndItems;
            }

            if (elapsed.TotalDays >= 1)
            {
                extent = SyncExtent.All;
            }

            await Sync(extent);
        }

        public async Task Sync(SyncExtent extent)
        {
            lastSync = DateTime.Now;

            if (extent == SyncExtent.None)
            {
                return;
            }

            // Force an update to the connection status, because it's possible to turn off the network on
            // a device and hit the sync button before the Connectivity plugin has received a connection change
            // event to update its status. Fortunately, its IsConnected property changes quickly, so make sure
            // IsOffline is updated from that before we check it.
            IsOffline = !CrossConnectivity.Current.IsConnected;

            if (IsOffline)
            {
                // Remember to sync when we go back online
                syncWhenReconnected = true;
                syncWhenReconnectedExtent = extent;
                return;
            }

            IsSyncing = true;            

            Task<SyncResult> taskCategories = Empty<SyncResult>.Task;            
            Task<SyncResult> taskSettings = Empty<SyncResult>.Task;

            if (extent == SyncExtent.All)
            {                
                Task<SyncResult> taskAP = dataModel.SyncAuthProviders();  // Fire and forget
                taskCategories = dataModel.SyncCategories();
            }

            if (extent == SyncExtent.All || extent == SyncExtent.SettingsAndItems)
            {
                taskSettings = dataModel.SyncSettings();
            }
            
            // Do both categories and settings before syncing items
            await Task.WhenAll(taskCategories, taskSettings);            
            
            await dataModel.SyncItems();

            IsSyncing = false;            

            // This makes sure the Configuration knows it can reset its changed flag.
            if (applyConfiguration)
            {
                dataModel.Configuration.ConfigurationApplied();
            }
        }

        public async Task CheckRefresh()
        {
            // The view knows when it's appropriate to do a data refresh according to UI navigations e.g. on 
            // on startup, or returning from the congfiguration page, so the viewmodel carries out the operation
            // by checking if there's been any changes to apply and asking the model to refresh accordingly.
            // Refreshing the model will trigger a change to its data which triggers a change to this viewmodel
            // and a corresponding change in the view.

            Debug.WriteLine("HomeViewModel.CheckRefresh, Configuration.HasChanged = " + dataModel.Configuration.HasChanged);

            if (dataModel.Configuration.HasChanged)
            {
                // Rebuild the ListView data source immediately to reflect changes in the configuration. This way,
                // if the user turns some categories off, they disappear quickly, and other categories that get turned
                // on will appear if there is data in the database already. Without doing this, you'd end up staring
                // at inappropriate data for a time until the sync was complete.
                await dataModel.PopulateGroupedItemsFromDB();

                // Trigger an update for the message label visibility as it might have changed with a refresh.
                OnPropertyChanged("NoCategoriesMessageVisible");

                applyConfiguration = true;
                
                //If we have at least one category selected, sync the items.
                if (!NoCategoriesMessageVisible)
                {
                    await Sync(SyncExtent.Items);
                }                
            }
        }       
    }
}
