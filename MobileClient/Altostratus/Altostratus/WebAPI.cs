using Altostratus.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Altostratus
{
    #region Web API Wrappers

    #region Constants
    //Class to centralize strings used in the web API.
    public static class WebAPIConstants
    {
        public static readonly Int32 Timeout = 10;

        public static readonly String BaseURI = "https://alto54.azurewebsites.net/";
        public static readonly String Categories = "api/categories";
        public static readonly String ExternalLogins = "api/account/externalLogins?returnUrl=/";
        public static readonly String ItemsFeed = "api/conversations";
        public static readonly String ItemsFeedQuery = "?from=";
        public static readonly String ItemsFeedTimestampFormat = "yyyy-MM-ddTHH:mm:ssZ";
        public static readonly String GetUserInfo = "api/account/userinfo";
        public static readonly String RegisterExternal = "api/account/RegisterExternal";
        public static readonly String GetSetUserPrefs = "api/userpreferences";
        public static readonly String Logout = "api/account/Logout";
    }
    #endregion

    #region AuthenticationMessageHandler and ITokenProvider
    public interface ITokenProvider
    {
        string AccessToken { get; }
    }

    class AuthenticationMessageHandler : DelegatingHandler
    {
        ITokenProvider _provider;

        public AuthenticationMessageHandler(ITokenProvider provider)
        {
            _provider = provider;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var token = _provider.AccessToken;

            if (!String.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
    #endregion

    #region WebAPI Class
    public static class WebAPI
    {
        static HttpClient _httpClient = null;
        public static Uri BaseURI { get; private set; }
        public static HttpClient HttpClient { get { return _httpClient; } }
        public static CookieContainer CookieContainer { get; private set; }

        // A constant to use for a variety of return values.
        static Task<HttpResponseMessage> NoContentResponseTask = System.Threading.Tasks.Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));

        public static void Initialize(ITokenProvider provider)
        {
            // Create the HttpClient with a cookie container for authentication
            CookieContainer = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = CookieContainer;

            // The AuthenticationMessageHandler (see Auth/AuthenticationMessageHandler.cs) provides for transparent
            // authentication with our backend using the token from the ITokenProvider given to its constructor.
            // If the token is non-null, the backend applies that user's preferences to requests. In the implementation
            // here, the provider is the Configuration.UserPreferences class, which must be initialized before now.            
            _httpClient = HttpClientFactory.Create(handler, new AuthenticationMessageHandler(provider));
            BaseURI = new System.Uri(WebAPIConstants.BaseURI);
            _httpClient.BaseAddress = BaseURI;
            _httpClient.Timeout = TimeSpan.FromSeconds(WebAPIConstants.Timeout); 
        }

        public static async Task<UserInfoViewModel> GetUserInfoAsync()
        {
            UserInfoViewModel userInfo = null;            

            try
            {
                var response = await _httpClient.GetAsync(WebAPIConstants.GetUserInfo);

                if (response.IsSuccessStatusCode)
                {
                    userInfo = await response.Content.ReadAsAsync<UserInfoViewModel>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WebAPI.GetUserInfo failed: " + ex.Message);
            }

            return userInfo;
        }

        public static async Task RegisterExternalAsync(UserInfoViewModel userInfo, String providerUrl)
        {
            var model = new RegisterExternalModel
            {
                Email = userInfo.Email
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(WebAPIConstants.RegisterExternal, model);

                if (response.IsSuccessStatusCode)
                {                
                    response = await _httpClient.GetAsync(new Uri(_httpClient.BaseAddress, providerUrl));

                    if (response.IsSuccessStatusCode)
                    {
                        // Registered and logged in to local account
                        userInfo.HasRegistered = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WebAPI.RegisterExternal failed: " + ex.Message);
            }
        }

        public static async Task<UserPreferenceDTO> GetUserPrefsAsync()
        {
            UserPreferenceDTO prefs = null;

            try
            {
                var response = await _httpClient.GetAsync(WebAPIConstants.GetSetUserPrefs);

                if (response.IsSuccessStatusCode)
                {
                    prefs = await response.Content.ReadAsAsync<UserPreferenceDTO>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WebAPI.GetUserPrefs failed: " + ex.Message);
            }

            return prefs;
        }

        public static async Task<Boolean> SetUserPrefsAsync(UserPreferences current, List<String> ActiveCategories)
        {
            UserPreferenceDTO prefs = new UserPreferenceDTO() 
            {
                ConversationLimit = current.ConversationLimit,
                SortOrder = 0,
                Categories = ActiveCategories
            };
                                   
            try
            {
                var response = await _httpClient.PutAsJsonAsync(WebAPIConstants.GetSetUserPrefs, prefs);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WebAPI.SetUserPrefs failed: " + ex.Message);
                return false;
            }
        }


        public static Task<HttpResponseMessage> LogoutAsync(StringContent content = null)
        {
            if (content == null)
            {
                content = new StringContent("");
            }
            
            return _httpClient.PostAsync(WebAPIConstants.Logout, content);
        }

        public static Task<HttpResponseMessage> GetCategories()
        {
            try
            {
                return  HttpClient.GetAsync(WebAPIConstants.Categories);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WebAPI.GetCategories failed: " + ex.Message);
                return NoContentResponseTask;
            }            
        }

        public static Task<HttpResponseMessage> GetAuthProviders()
        {
            try
            {
                return HttpClient.GetAsync(WebAPIConstants.ExternalLogins);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WebAPI.GetAuthProviders failed: " + ex.Message);
                return NoContentResponseTask;
            }
        }

        public static Task<HttpResponseMessage> GetItems(Timestamp t, CancellationToken cancelToken)
        {
            try
            { 
                String queryParams = (t != null) ? (WebAPIConstants.ItemsFeedQuery + t.Stamp) : "";
                Uri relative = new Uri(HttpClient.BaseAddress, WebAPIConstants.ItemsFeed + queryParams);
                return WebAPI.HttpClient.GetAsync(relative.ToString(), cancelToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WebAPI.GetItems failed: " + ex.Message);
                return NoContentResponseTask;
            }
        }
    }
    #endregion

    #endregion
}
