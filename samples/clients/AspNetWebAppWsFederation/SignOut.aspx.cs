using System;
using System.Collections.Generic;
using System.IdentityModel.Services;
using System.IdentityModel.Services.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace AspNetWebAppWsFederation
{
    public partial class SignOut : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            if (User.Identity.IsAuthenticated) {
                string returnUrl = ResolveClientUrl(FormsAuthentication.DefaultUrl);
                returnUrl = Request.Url.GetLeftPart(UriPartial.Authority) + returnUrl;

                // BUG: WIF45 Method FederatedSignOut method don't send 'signOutQueryString' parameter from configuration
                // In WSFederationAuthenticationModule class must be overridden method 'GetSessionTokenContext'
                // protected override string GetSessionTokenContext() {
                //   return "(" + typeof(WSFederationAuthenticationModule).Name + ")%%" + GetFederationPassiveSignOutUrl(Issuer, SignOutReply, "wtrealm=" + Realm);
                // }
                //WSFederationAuthenticationModule.FederatedSignOut(null, new Uri(returnUrl));

                var realm = SystemIdentityModelServicesSection.DefaultFederationConfigurationElement.WsFederation.Realm;
                var issuer = SystemIdentityModelServicesSection.DefaultFederationConfigurationElement.WsFederation.Issuer;
                var federationPassiveSignOutUrl = WSFederationAuthenticationModule.GetFederationPassiveSignOutUrl(issuer, returnUrl, "wtrealm=" + realm);
                FederatedAuthentication.SessionAuthenticationModule?.DeleteSessionTokenCookie();
                Response.Redirect(federationPassiveSignOutUrl);
            }
            else {
                string returnUrl = FormsAuthentication.DefaultUrl;
                Response.Redirect(returnUrl);
            }
        }
    }
}