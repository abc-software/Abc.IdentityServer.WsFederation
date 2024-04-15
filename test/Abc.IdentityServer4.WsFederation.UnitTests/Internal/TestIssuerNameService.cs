using System.Threading.Tasks;

namespace Abc.IdentityServer
{
    internal class TestIssuerNameService : IIssuerNameService
    {
        private readonly string _value;

        public TestIssuerNameService(string value = null)
        {
            _value = value ?? "https://identityserver";
        }

        public Task<string> GetCurrentAsync()
        {
            return Task.FromResult(_value);
        }
    }
}
