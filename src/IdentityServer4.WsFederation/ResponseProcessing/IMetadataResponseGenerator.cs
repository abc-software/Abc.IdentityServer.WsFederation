
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Threading.Tasks;

namespace IdentityServer4.WsFederation
{
    public interface IMetadataResponseGenerator
    {
        Task<WsFederationConfigurationEx> GenerateAsync();
    }
}