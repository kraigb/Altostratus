using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.OAuth;
using Owin;
using Altostratus.Website.Providers;
using Altostratus.Website.Models;
using Altostratus.DAL;
using Microsoft.Owin.Security.Facebook;
using System.Configuration;

namespace Altostratus.Website
{
    public partial class Startup
    {
        public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }

        public static string PublicClientId { get; private set; }

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Configure the db context and user manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Configure the application for OAuth based flow
            PublicClientId = "self";
            OAuthOptions = new OAuthAuthorizationServerOptions
            {
                TokenEndpointPath = new PathString("/Token"),
                Provider = new ApplicationOAuthProvider(PublicClientId),
                AuthorizeEndpointPath = new PathString("/api/Account/ExternalLogin"),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(14),
                AllowInsecureHttp = true
            };

            // Enable the application to use bearer tokens to authenticate users
            app.UseOAuthBearerTokens(OAuthOptions);

            var fbOpts = new FacebookAuthenticationOptions
            {
               AppId = ConfigurationManager.AppSettings["FB_AppId"],
               AppSecret = ConfigurationManager.AppSettings["FB_AppSecret"]
            };
            fbOpts.Scope.Add("email");
            app.UseFacebookAuthentication(fbOpts);

            var googleOptions = new GoogleOAuth2AuthenticationOptions()
            {
               ClientId = ConfigurationManager.AppSettings["GoogClientID"],
               ClientSecret = ConfigurationManager.AppSettings["GoogClientSecret"]
            };
            app.UseGoogleAuthentication(googleOptions);
        }
    }
}
