using System.IdentityModel.Services;

namespace AspNetWebAppWsFederation {
    public class CustomWSFederationAuthenticationModule : WSFederationAuthenticationModule {
        protected override string GetSessionTokenContext() {
            return "(" + typeof(WSFederationAuthenticationModule).Name + ")%%" + GetFederationPassiveSignOutUrl(Issuer, SignOutReply, SignOutQueryString + "&wtrealm=" + Realm);
         }
    }
}