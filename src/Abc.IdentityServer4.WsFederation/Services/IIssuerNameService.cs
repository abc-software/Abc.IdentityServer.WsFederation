#if IDS4 || IDS8

using System.Threading.Tasks;

#if IDS4
namespace IdentityServer4.Services;
#else
namespace IdentityServer8.Services;
#endif

/// <summary>
/// Abstract access to the current issuer name.
/// </summary>
public interface IIssuerNameService
{
    /// <summary>
    /// Returns the issuer name for the current request.
    /// </summary>
    /// <returns>The issuer name.</returns>
    Task<string> GetCurrentAsync();
}

#endif