using Abc.IdentityServer.WsFederation.Stores;
using Microsoft.AspNetCore.Authentication;

namespace Abc.IdentityServer.WsFederation.Validation.UnitTests
{
    public abstract class WsFederationRequestValidatorBase
    {
        protected readonly WsFederationRequestValidator validator;
        protected readonly IClock clock;

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

            var client = new Client
            {
                ClientId = "urn:test",
                ClientName = "WS-Fed Client",
                ProtocolType = Ids.IdentityServerConstants.ProtocolTypes.WsFederation,
                Enabled = true,
                RedirectUris = { "https://wsfed/callback" },
                PostLogoutRedirectUris = { "https://wsfed/postlogout" },
            };

            client.IdentityProviderRestrictions.Add("test");

            var clients = new InMemoryClientStore(new[]
            {
                client,
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
