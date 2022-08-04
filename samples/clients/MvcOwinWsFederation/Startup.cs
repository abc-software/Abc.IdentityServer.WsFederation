using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.WsFederation;
using Owin;
using System.Diagnostics;

[assembly: OwinStartup(typeof(MvcOwinWsFederation.Startup))]

namespace MvcOwinWsFederation
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            bool isExpress =
              string.Compare(Process.GetCurrentProcess().ProcessName, "iisexpress") == 0;

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Cookies"
            });

            app.UseWsFederationAuthentication(new WsFederationAuthenticationOptions
            {
                MetadataAddress = "http://localhost:5000/wsfed/metadata",
                Wtrealm = "urn:owinrp",
                SignOutWreply = isExpress ?
                    "http://localhost:10313/"
                    : System.Web.VirtualPathUtility.ToAbsolute("~/"),

                SignInAsAuthenticationType = "Cookies"
            });
        }
    }
}
