using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Altostratus.Model
{
    #region DataModel
    // DataModel is the roll-up class that is instantiated once for the app and obtained through
    // App.DataModel. Its public properties contain what is needed by the view models.
    //
    // Note that all data synchronization methods are in sync.cs, hence the partial class
        
    public partial class DataModel : BindableBase
    {        
        Task<SyncResult> syncTask = null;
        CancellationTokenSource syncTokenSource;
        CancellationToken syncToken;

        public GroupList GroupedItems { get; private set; }
        public Configuration Configuration { get; private set; }
        public DataAccessLayer db { get; private set; }

        
        public DataModel(DataAccessLayer dbInit = null)
        {
            // The optional dbInit argument is allows the DBInitialize console application to bypass
            // the Xamarin.Forms DependencyService code by creating the DB directly and pass that in here.
            // The mobile client, on the other hand, lets the Database constructor do that work (see database.cs).
            if (dbInit == null)
            {
                //Constructor is responsible for instantiating the data model.
                db = new DataAccessLayer();                
            }
            else
            {
                db = dbInit;
            }
        }

        public async Task InitAsync()
        {
            Configuration = new Configuration();
            await Configuration.InitAsync();

            // The WebAPI needs a token provider, which is the UserPreferences object we now have.
            WebAPI.Initialize(Configuration.CurrentUser);

            // Build the ListView data source from the database. This always uses the existing
            // database as sync with then backend happens later and the data source is updated then.             
            GroupedItems = new GroupList();
            await PopulateGroupedItemsFromDB();
            
            // Synchronization calls are made by users of the data model.            
        }


        public async Task PopulateGroupedItemsFromDB()
        {
            // Generates the grouped list of active categories as needed by the Xamarin.Forms ListView, 
            // limited by the user's conversation limit setting because there could be more items in the
            // database above the limit that we haven't cleaned up yet.            
            GroupedItems.Clear();

            var categories = Configuration.GetCategories(CategorySet.Active);
            IEnumerable<Item> list;

            foreach (String c in categories)
            {
                list = await db.GetItemListForCategoryAsync(c, Configuration.CurrentUser.ConversationLimit);
                Group group = new Group(0, c, list);
                GroupedItems.Add(group);
            }
        }
    }
    #endregion

    #region Items
    // Item is what we store in our database; FeedItem is aligned with what
    // comes from web requests and is just an intermediary.
    
    public class Item
    {
        [PrimaryKey]
        public String Url { get; set; }
        public DateTime LastUpdated {get; set;} 
        [MaxLength(512)]
        public String Title { get; set; }
        [MaxLength(128)]
        public String Description { get; set; }

        public String Body { get; set; }
        [MaxLength(100)]
        public String Provider { get; set; }
        [MaxLength(100)]
        public String Category { get; set; }
    }
    #endregion

    #region Timestamp
    //Timestamp is used to mark when we last synced.
    public class Timestamp
    {
        [PrimaryKey]
        public String Stamp { get; set; }
    }

    #endregion

    #region GroupsAndCategories
    // A group is a collection of items with a name; the ListView in the UI
    // works with a collection of Groups (a GroupList).
    public class Group : ObservableCollection<Item>
    {
        public Int32 ID { get; set; }
        public String Name { get; set; }        

        public Group()
        {
        }

        //Override to handle converting an IEnumerable<Item> to a Group
        public Group(Int32 ID, String Name, IEnumerable<Item> list) : base(list)
        {
            this.ID = ID;
            this.Name = Name;
        }
    }
    
    public class GroupList : ObservableCollection<Group> { }

    public enum CategorySet
    {
        All = 0,
        Active
    }

    public enum CategoryKeepActive
    {
        Keep = 0,
        Force
    }

    public class Category : BindableBase
    {
        String name;
        Boolean isActive;
        
        public Category()
        {
        }        

        [PrimaryKey]
        public String Name        
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public Boolean IsActive
        {
            get { return isActive; }
            set { SetProperty(ref isActive, value);  }
        }
    }

    public class CategoryList : ObservableCollection<Category> { }
    #endregion

    #region UserPreferences
    public class UserPreferences : BindableBase, ITokenProvider
    {        
        public static Int32 ConversationMin = 20;
        public static Int32 ConversationMax = 100;

        // Database requires non-null for the primary key UserID (though it's ignored unless UserToken is non-null);
        public static String DefaultUserID = "unknown";

        Int32 conversationLimit = ConversationMax;

        [PrimaryKey]
        public String UserID { get; set; }  

        // The existence of an access token means the user has authenticated with the backend.
        // V2: access token should be stored in secure storage and not the database.
        public String AccessToken { get; set; }
        public String ProviderName { get; set; }
        public String ProviderUri { get; set; }
        public Int32 ConversationLimit {
            get { return conversationLimit; }
            set
            {
                SetProperty(ref conversationLimit, value);
            }
        }

        // This field is just used with IsEqual; real values are in the IsActive fields in Configuration.Categories.
        public String ActiveCategories { get; set; }

        public UserPreferences()
        {
            UserID = DefaultUserID;
            AccessToken = null;
            ProviderName = null;
            ProviderUri = null;
            ConversationLimit = UserPreferences.ConversationMax;
            ActiveCategories = null;
        }
        
        public async Task Save()
        {
            await DataAccessLayer.Current.SetUserPreferencesAsync(this);
        }

        //Clone takes a snapshot for later comparison with IsEqual
        public UserPreferences Clone()
        {
            // Note that we don't need to call InitAsync on this UserPreference instance
            // because we're initializing with known values.
            return new UserPreferences()
            {
                UserID = this.UserID,
                AccessToken = this.AccessToken,
                ProviderName = this.ProviderName,
                ProviderUri = this.ProviderUri,
                ConversationLimit = this.ConversationLimit,
                ActiveCategories = this.ActiveCategories
            };
        }

        public Boolean IsEqual(UserPreferences other)
        {
            if (other == null) { return false; }

            return (this.UserID == other.UserID 
                && this.AccessToken == other.AccessToken
                && this.ProviderName == other.ProviderName
                && this.ProviderUri == other.ProviderUri
                && this.ConversationLimit == other.ConversationLimit
                && this.ActiveCategories == other.ActiveCategories);                
        }
    }
    #endregion

    #region AuthProvider
    //Describes an authentication provider supported by the backend
    public class AuthProvider
    {
        [PrimaryKey]
        public String Name { get; set; }
        public String Url { get; set; }        
    }
    #endregion

    #region Configuration
    // Argument type for CheckChanges method
    [Flags]
    public enum ChangeCheckType : int
    {
        ConversationLimit,
        CategorySelection,        
    }

    // Configuration contains the data for the configuration page, which includes the
    // list of all categories and the current user preferences.
    public class Configuration : BindableBase
    {
        CategoryList categories;
        List<AuthProvider> providers;
        Boolean hasChanged;

        public Configuration()
        {
        }
        
        public async Task InitAsync()
        {
            categories = new CategoryList();
            await PopulateCategoriesFromDB();

            providers = new List<AuthProvider>();
            await PopulateAuthProvidersFromDB();
            
            // The access token in the user preferences determines authentication status, as that token
            // is used automatically in the HTTP request message hander. Nothing else is needed to
            // authenticate on startup.

            CurrentUser = await DataAccessLayer.Current.GetUserPreferencesAsync();
            
            if (CurrentUser == null)
            {
                CurrentUser = new UserPreferences();
                CurrentUser.ActiveCategories = GetCategoryList(CategorySet.Active);
            }

            // We could call ApplyBackendConfiguration here, but we will let the consumer of
            // this data model do that when it wants. (See HomeViewModel.cs.)            
        }

        public UserPreferences CurrentUser { get; private set; }                
        public List<AuthProvider> Providers { get { return providers; } }
        public CategoryList Categories { get { return categories; } }

        public Boolean HasChanged
        {
            get { return hasChanged; }
            set { SetProperty(ref hasChanged, value);  }
        }

        public async Task PopulateCategoriesFromDB()
        {
            Categories.Clear();
            var dbCategories = await DataAccessLayer.Current.GetCategoriesAsync(CategorySet.All);

            foreach (Category c in dbCategories)
            {
                Categories.Add(c);
            }
        }

        public async Task PopulateAuthProvidersFromDB()
        {
            Providers.Clear();
            IEnumerable<AuthProvider> providers = null;
            providers = await DataAccessLayer.Current.GetAuthProvidersAsync();
            Providers.AddRange(providers);
        }


        public List<String> GetCategories(CategorySet set)
        {
            IEnumerable<String> temp;

            if (set == CategorySet.Active)
            {
                temp = from c in Categories where c.IsActive == true select c.Name;
            } 
            else
            {
                temp = from c in Categories select c.Name;
            }
            
            return temp.ToList();
        }

        public String GetCategoryList(CategorySet set = CategorySet.All)
        {
            String list = "";

            foreach (String c in GetCategories(set))
            {
                list += c + ",";
            }

            //Remove the trailing , if there is one
            char[] trims = { ',' };
            return list.TrimEnd(trims);
        }

        public Task ApplyConversationLimit()
        {
            return DataAccessLayer.Current.ApplyConversationLimitAsync(CurrentUser.ConversationLimit);
        }         

        public void ConfigurationApplied()
        {            
            HasChanged = false;
        }

        public async Task<Boolean> CheckChanges(UserPreferences previous, ChangeCheckType type)
        {
            if ((type & ChangeCheckType.CategorySelection) == ChangeCheckType.CategorySelection)
            {
                // Update category status in the database
                foreach (Category c in categories)
                {
                    await DataAccessLayer.Current.UpdateOrAddCategoryAsync(c, CategoryKeepActive.Force);
                }

                // Update the UserPreferences
                CurrentUser.ActiveCategories = GetCategoryList(CategorySet.Active);
                await CurrentUser.Save();
            }

            HasChanged = !CurrentUser.IsEqual(previous);
            return HasChanged;
        }

        public async Task<Boolean> ApplyBackendConfiguration()
        {
            // Ignore if we're not authenticated.
            if (CurrentUser.AccessToken == null || CurrentUser.AccessToken == "")
            {
                return false;
            }

            Boolean result = false;

            // Retrieve preferences from the backend and apply them here.
            UserPreferenceDTO prefs = await WebAPI.GetUserPrefsAsync();

            if (prefs != null)            
            {
                result = true;
                CurrentUser.ConversationLimit = prefs.ConversationLimit;

                // Go through all categories and change IsActive flags depending on how they're
                // set in the preferences we just retrieved.
                foreach (Category c in categories)
                {
                    c.IsActive = prefs.Categories.Contains(c.Name);
                    await DataAccessLayer.Current.UpdateOrAddCategoryAsync(c, CategoryKeepActive.Force);
                }

                CurrentUser.ActiveCategories = GetCategoryList(CategorySet.Active);
            }

            await CurrentUser.Save();
            return result;
        }
    }
    #endregion      
    }
