using Foundation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Xamarin.Auth;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Altostratus.LoginPage), typeof(Altostratus.iOS.LoginPageRenderer))]

namespace Altostratus.iOS
{
    class LoginPageRenderer : PageRenderer
    {
        LoginPage loginPage;
        Boolean isShown;

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);            
            loginPage = e.NewElement as LoginPage;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
                         
            if (!isShown)
            {
                isShown = true;

                var authUrl = new Uri(WebAPI.BaseURI, loginPage.AuthProvider.Url);
                var auth = new SocialLoginAuthenticator(authUrl, WebAPI.BaseURI); 

                auth.Completed += async (sender, eventArgs) =>
                {
                    DismissViewController(true, null);
                    isShown = false;

                    if (eventArgs.IsAuthenticated)
                    {
                        await loginPage.CompleteLoginAsync(eventArgs.Account.Properties["access_token"]);
                    }
                    else
                    {
                        Debug.WriteLine("LoginPage failed to authenticate or was canceled.");                        
                        await loginPage.CancelLoginAsync();
                    }
                };

                PresentViewController(auth.GetUI(), true, null);

            }
        }
    }

    class SocialLoginAuthenticator : WebRedirectAuthenticator
    {
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
                var cookieUrl = new Foundation.NSUrl(url.AbsoluteUri);
                var cookies = NSHttpCookieStorage.SharedStorage.CookiesForUrl(cookieUrl);
                
                foreach (NSHttpCookie c in cookies)
                {
                    WebAPI.CookieContainer.Add(WebAPI.BaseURI, new Cookie(c.Name, c.Value));
                }

                var account = new Account("", fragment);                
                OnSucceeded(account);
            }
        }
    }
}