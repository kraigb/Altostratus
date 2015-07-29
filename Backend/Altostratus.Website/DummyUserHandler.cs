using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.Owin;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using Altostratus.DAL;
using System.Security.Claims;
using Microsoft.Owin.Security.OAuth;

namespace Altostratus.Website
{
    // Message handler that inserts a fake authenticated user - only for testing.
    class DummyUserHandler : DelegatingHandler
    {
        private readonly string DummyName = "galactus@example.com";

        public DummyUserHandler()
        {
        }


        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var context = request.GetRequestContext();
            var user = context.Principal.Identity;

            if (!user.IsAuthenticated)
            {
                IdentityResult result;
                ApplicationUserManager userManager = request.GetOwinContext().GetUserManager<ApplicationUserManager>();

                var appUser = await userManager.FindByNameAsync(DummyName);
                if (appUser == null)
                {
                    appUser = new ApplicationUser() { UserName = DummyName, Email = DummyName };

                    result = await userManager.CreateAsync(appUser);

                }
                ClaimsIdentity oAuthIdentity = await appUser.GenerateUserIdentityAsync(userManager, OAuthDefaults.AuthenticationType);

                context.Principal = new ClaimsPrincipal(oAuthIdentity);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
