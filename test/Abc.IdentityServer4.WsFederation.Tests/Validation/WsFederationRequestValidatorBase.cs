using Abc.IdentityServer4.WsFederation.Stores;
using Abc.IdentityServer4.WsFederation.Validation;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using MockUserSession = IdentityServer4.Services.MockUserSession;

namespace Abc.IdentityServer4.WsFederation.Validation.UnitTests
{
    public abstract class WsFederationRequestValidatorBase
    {
        protected readonly WsFederationRequestValidator validator;
        protected readonly ISystemClock clock;

        public WsFederationRequestValidatorBase()
        {
            var options = TestIdentityServerOptions.Create();
            var relayingPartyStore = new InMemoryRelyingPartyStore(new []
            {
                new RelyingParty
                {
                    Realm = "urn:test",
                }
            });
            var clients = new InMemoryClientStore(new[]
            {
                  new Client
                    {
                        ClientId = "urn:test",
                        ClientName = "WS-Fed Client",
                        ProtocolType = IdentityServerConstants.ProtocolTypes.WsFederation,
                        Enabled = true,
                        RedirectUris = { "https://wsfed/callback"  },
                        PostLogoutRedirectUris = { "https://wsfed/postlogout" }
                    },
                    new Client
                    {
                        ClientName = "Code Client",
                        Enabled = true,
                        ClientId = "codeclient",
                    },
                });

                var uriValidator = new StrictRedirectUriValidator();

            //    wstrustRequestValidator = new WsTrustRequestValidator("https://identityserver", new LoggerFactory().CreateLogger<JwtRequestValidator>());
            //    wstrustRequestUriHttpClient = new DefaultWsTrustRequestUriHttpClient(new HttpClient(new NetworkHandler(new Exception("no jwt request uri response configured"))), options, new LoggerFactory());

            var userSession = new MockUserSession();

            clock = new StubClock();
            validator = new WsFederationRequestValidator(
                options,
                clients,
                relayingPartyStore,
                uriValidator,
                userSession,
                clock,
                //wstrustRequestValidator,
                //wstrustRequestUriHttpClient,
                TestLogger.Create<WsFederationRequestValidator>());
        }
     }
}
