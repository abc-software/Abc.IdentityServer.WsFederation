using Abc.IdentityServer.WsFederation.ResponseProcessing;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.Endpoints.UnitTests
{
    internal class StubMetadataResponseGenerator : IMetadataResponseGenerator
    {
        public WsFederationConfigurationEx WsFederationConfiguration { get; set; } = new WsFederationConfigurationEx();

        public Task<WsFederationConfigurationEx> GenerateAsync()
        {
            return Task.FromResult(WsFederationConfiguration);
        }
    }
}