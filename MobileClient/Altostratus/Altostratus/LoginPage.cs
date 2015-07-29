using Altostratus.Model;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Altostratus
{
    // Note that this page has not XAML because it's rendered at runtime using platform-specific
    // LoginPageRenderer implementations. Also note that until Xamarin.Auth supports Windows Phone,
    // the app runs unauthenticated on that platform and we won't use the login code at all.
    public class LoginPage : ContentPage
    {
        Configuration dataSource;
        public AuthProvider AuthProvider { get; set; }

        //This is needed by the page renderer
        public UserPreferences CurrentUser { get { return dataSource.CurrentUser; } }
        
        public LoginPage(AuthProvider selectedProvider)
        {            
            dataSource = App.DataModel.Configuration;
            AuthProvider = selectedProvider;

            // We could create some alternate UI here to indicate that a login failed.
            // Have to be careful, because the contents of this page will show briefly
            // even for a successful login.
        }

        // Completing the login means storing relevant information into the UserPreferences object.
        // We have to do a request to the backend to give us the authenticated user's userID (email)
        // because all the authentication process was otherwise transparent.
        public async Task CompleteLoginAsync(String token)
        {
            UserPreferences user = dataSource.CurrentUser;

            user.AccessToken = token;
            user.ProviderName = AuthProvider.Name;
            user.ProviderUri = AuthProvider.Url;
            // ActiveCategories is set by the configuration page.

            // Check if the user is already registered, and register if not. 
            UserInfoViewModel userInfo = await WebAPI.GetUserInfoAsync();

            if (userInfo != null)
            {
                if (!userInfo.HasRegistered)
                {
                    //This updates userInfo.HasRegistered if successful
                    await WebAPI.RegisterExternalAsync(userInfo, AuthProvider.Url);
                }

                // If the backend knows we exist, update the UserPreferences
                if (userInfo.HasRegistered)
                {
                    user.UserID = userInfo.Email;
                }
            }

            // Now that we're logged in, retrieve and apply the user preferences if they exist.
            // This also saves the preferences in the database (i.e. calls user.Save());
            await App.DataModel.Configuration.ApplyBackendConfiguration();            

            // Return to the configuration page
            await Navigation.PopAsync();
        }

        public async Task CancelLoginAsync()
        {
            // Canceling login should return to the Configuration page. Without this navigation,
            // a cancelation would just stay on the Login page, which is empty.
            await Navigation.PopAsync();
        }
    }
}
