using IdentityServer4.Validation;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Services
{
    internal class MockClaimsService : IClaimsService
    {
        public List<Claim> TokenClaims { get; set; } = new List<Claim>();

        public Task<IEnumerable<Claim>> GetClaimsAsync(ValidatedRequest validatedRequest, IEnumerable<string> requestedClaimTypes)
        {
            return Task.FromResult(TokenClaims.AsEnumerable());
        }

        public IEnumerable<Claim> MapClaims(IDictionary<string, string> claimsMapping, string tokenType, IEnumerable<Claim> claims)
        {
            return claims;
        }
    }
}
