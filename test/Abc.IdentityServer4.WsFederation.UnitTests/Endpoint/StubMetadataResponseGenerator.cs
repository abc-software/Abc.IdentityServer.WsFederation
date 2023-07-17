using Abc.IdentityModel.Metadata;
using Abc.IdentityServer.WsFederation.ResponseProcessing;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.Endpoints.UnitTests
{
    internal class StubMetadataResponseGenerator : IMetadataResponseGenerator
    {
        public DescriptorBase Descriptor { get; set; } = new SecurityTokenServiceDescriptor();

        public Task<DescriptorBase> GenerateAsync()
        {
            return Task.FromResult(Descriptor);
        }
    }
}