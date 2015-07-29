using Altostratus.Model;
using Altostratus.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Altostratus
{
    class ConfigurationViewModel : BindableBase
    {
        Configuration dataSource;
        Boolean providerListVisible;
        String authenticationMessage = Resources.Configuration_NotLoggedIn;
        String loginButtonLabel = Resources.Configuration_Login;
        String limitHeader = null;
        Boolean doingCheck = false;

        public ConfigurationViewModel()
        {
            dataSource = App.DataModel.Configuration;
            Refresh();
        }
        
        public String PageTitle { get { return Resources.Configuration_PageTitle; } }        
        public String CategoriesHeader { get { return Resources.Configuration_CategoriesHeader; } }

        public String LimitHeader
        {
            get {return limitHeader; } 
            set { SetProperty(ref limitHeader, value); }
        }

        public String ProviderListLabel { get { return Resources.Configuration_ProvidersLabel; } }

        public Boolean ProviderListVisible
        { 
            get { return providerListVisible; }
            set { SetProperty(ref providerListVisible, value); }
        }

        public String AuthenticationMessage
        {
            get { return authenticationMessage; }
            set { SetProperty(ref authenticationMessage, value); }
        }

        public String LoginButtonLabel
        {
            get {return loginButtonLabel; } 
            set { SetProperty(ref loginButtonLabel, value); }
        }

        public Boolean IsAuthenticated
        {
            //The existence of a token is what we use to determine authentication.
            get { return !String.IsNullOrEmpty(CurrentUser.AccessToken); } 
        }
        public UserPreferences CurrentUser { get; private set; }
        public UserPreferences GetState() { return CurrentUser.Clone(); }
        public CategoryList Categories { get { return dataSource.Categories; } }
        public List<AuthProvider> Providers { get { return dataSource.Providers; } }
           

        public void Refresh()
        {
            CurrentUser = dataSource.CurrentUser;            
            ProviderListVisible = !IsAuthenticated;
            
            AuthenticationMessage = IsAuthenticated
                ? String.Format(Resources.Configuration_LoggedInto, CurrentUser.ProviderName, CurrentUser.UserID)
                : Resources.Configuration_NotLoggedIn;

            LoginButtonLabel = IsAuthenticated ? Resources.Configuration_Logoff : Resources.Configuration_Login;
            SetLimitHeader();
        }

        public async Task Logout()
        {            
            // Tell the backend we're no longer logged in
            try
            {
                await WebAPI.LogoutAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConfigurationViewModel.Logout, WebAPI.Logout failed, message = " + ex.Message);
            }
            
            // Clear out the AccessToken from the current user perferences (and the ID, though
            // it's ignored when the access token is null, but it's good to clean up fully).
            CurrentUser.UserID = UserPreferences.DefaultUserID;
            CurrentUser.AccessToken = null;
            await CurrentUser.Save();

            Refresh();
        }

        public async Task CheckChanges(UserPreferences previous, ChangeCheckType type, Boolean updateBackend)
        {           
            // Because this is an async function, need to debounce repeated calls which
            // can come from the UI controls.
            if (doingCheck)
            {
                return;
            }

            doingCheck = true;
            Boolean changed = await dataSource.CheckChanges(previous, type);

            if (changed)
            {
                SetLimitHeader();

                if (updateBackend)
                {
                    try
                    {
                        await WebAPI.SetUserPrefsAsync(CurrentUser, dataSource.GetCategories(CategorySet.Active));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ConfigurationViewModel.CheckChanges, WebAPI.SetUserPrefs failed: " + ex.Message);
                    }
                }
            }

            doingCheck = false;
        }

        private void SetLimitHeader()
        {
            String str = Altostratus.Properties.Resources.Configuration_ConversationLimitHeader;
            LimitHeader = String.Format(str, CurrentUser.ConversationLimit); 
        }        
    }
}
