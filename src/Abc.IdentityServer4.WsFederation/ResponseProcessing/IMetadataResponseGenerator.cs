using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.ResponseProcessing
{
    public interface IMetadataResponseGenerator
    {
        Task<WsFederationConfigurationEx> GenerateAsync();
    }
}