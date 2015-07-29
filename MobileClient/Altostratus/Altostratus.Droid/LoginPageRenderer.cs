using Android.App;
using Android.Webkit;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net;
using Xamarin.Auth;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Altostratus.LoginPage), typeof(Altostratus.Android.LoginPageRenderer))]

namespace Altostratus.Android
{
    class LoginPageRenderer : PageRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);
            var activity = this.Context as Activity;
            var loginPage = e.NewElement as LoginPage;            
            
            var authUrl = new Uri(WebAPI.BaseURI, loginPage.AuthProvider.Url);
            var auth = new SocialLoginAuthenticator(authUrl, WebAPI.BaseURI);

            auth.Completed += async (sender, eventArgs) =>
            {
                if (eventArgs.IsAuthenticated)
                {                                        
                    await loginPage.CompleteLoginAsync(eventArgs.Account.Properties["access_token"]);
                }
                else
                {                    
                    System.Diagnostics.Debug.WriteLine("LoginPage failed to authenticate or was canceled.");                   
                    await loginPage.CancelLoginAsync();
                }

            };

            activity.StartActivity(auth.GetUI(activity));
        }
    }

    class SocialLoginAuthenticator : WebRedirectAuthenticator
    {
        static char[] nameValuePairSeparator = new char[] { '=' };

        public SocialLoginAuthenticator(Uri initialUrl, Uri redirectUrl)
            : base(initialUrl, redirectUrl)
        {
            ClearCookiesBeforeLogin = false;
        }

        protected override void OnRedirectPageLoaded(Uri url, IDictionary<string, string> query, IDictionary<string, string> fragment)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("OnRedirectPageLoaded: {0}", url));            

            if (fragment.ContainsKey("access_token"))
            {                
                // Cookies come back in a string of name-value pairs separated by semicolons.
                var cookies = CookieManager.Instance.GetCookie(url.AbsoluteUri);                
                String[] segments = cookies.Split(';');                
                
                foreach (String segment in segments)
                {
                    var pair = segment.Split(nameValuePairSeparator, 2);
                    Cookie netCookie = new Cookie(pair[0].Trim(), pair[1].Trim());
                    WebAPI.CookieContainer.Add(WebAPI.BaseURI, netCookie);
                }
                      
                var account = new Account("", fragment);
                OnSucceeded(account);
            }
        }
    }
}