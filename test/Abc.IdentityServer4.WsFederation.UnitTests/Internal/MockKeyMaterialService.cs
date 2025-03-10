﻿using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if DUENDE
namespace Duende.IdentityServer.Services
#elif IDS8
namespace IdentityServer8.Services
#else
namespace IdentityServer4.Services
#endif
{
    internal class MockKeyMaterialService : IKeyMaterialService
    {
        public List<SigningCredentials> SigningCredentials = new List<SigningCredentials>();
        public List<SecurityKeyInfo> ValidationKeys = new List<SecurityKeyInfo>();

        public Task<IEnumerable<SigningCredentials>> GetAllSigningCredentialsAsync()
        {
            return Task.FromResult(SigningCredentials.AsEnumerable());
        }

        public Task<SigningCredentials> GetSigningCredentialsAsync(IEnumerable<string> allowedAlgorithms = null)
        {
            return Task.FromResult(SigningCredentials.FirstOrDefault());
        }

        public Task<IEnumerable<SecurityKeyInfo>> GetValidationKeysAsync()
        {
            return Task.FromResult(ValidationKeys.AsEnumerable());
        }
    }
}
