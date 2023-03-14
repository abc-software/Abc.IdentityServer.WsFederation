using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if DUENDE
namespace Duende.IdentityServer.Stores
#else
namespace IdentityServer4.Stores
#endif
{
    internal class MockResourceStore : IResourceStore
    {
        public List<IdentityResource> IdentityResources { get; set; } = new List<IdentityResource>();
        public List<ApiResource> ApiResources { get; set; } = new List<ApiResource>();
        public List<ApiScope> ApiScopes { get; set; } = new List<ApiScope>();

        public Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> names)
        {
            var apis = from a in ApiResources
                       where names.Contains(a.Name)
                       select a;
            return Task.FromResult(apis);
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));

            var api = from a in ApiResources
                      where a.Scopes.Any(x => names.Contains(x))
                      select a;

            return Task.FromResult(api);
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));

            var identity = from i in IdentityResources
                           where names.Contains(i.Name)
                           select i;

            return Task.FromResult(identity);
        }

        public Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
        {
            var q = from x in ApiScopes
                    where scopeNames.Contains(x.Name)
                    select x;
            return Task.FromResult(q);
        }

        public Task<Resources> GetAllResourcesAsync()
        {
            var result = new Resources(IdentityResources, ApiResources, ApiScopes);
            return Task.FromResult(result);
        }
    }
}
