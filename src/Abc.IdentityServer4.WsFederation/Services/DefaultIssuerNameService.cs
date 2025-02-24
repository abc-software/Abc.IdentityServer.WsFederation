#if IDS4 || IDS8

using Abc.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

#if IDS4
namespace IdentityServer4.Services;
#else
namespace IdentityServer8.Services;
#endif

/// <summary>
/// Abstracts issuer name access.
/// </summary>
public class DefaultIssuerNameService : IIssuerNameService
{
    private readonly IdentityServerOptions _options;
    private readonly IServerUrls _urls;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultIssuerNameService"/> class.
    /// </summary>
    /// <param name="options">The identity server options.</param>
    /// <param name="urls">The server uris.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public DefaultIssuerNameService(IdentityServerOptions options, IServerUrls urls, IHttpContextAccessor httpContextAccessor)
    {
        _options = options;
        _urls = urls;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public Task<string> GetCurrentAsync()
    {
        // if they've explicitly configured a URI then use it,
        // otherwise dynamically calculate it
        var issuer = _options.IssuerUri;
        if (issuer.IsMissing())
        {
            string origin = null;

            if (_options.MutualTls.Enabled && _options.MutualTls.DomainName.IsPresent() 
                && !_options.MutualTls.DomainName.Contains("."))
            {
                var request = _httpContextAccessor.HttpContext.Request;
                if (request.Host.Value.StartsWith(_options.MutualTls.DomainName, StringComparison.OrdinalIgnoreCase))
                {
                    // if MTLS is configured with domain like "foo", then the request will be for "foo.acme.com", 
                    // so the issuer we use is from the parent domain (e.g. "acme.com")
                    // 
                    // Host.Value is used to get unicode hostname, instead of ToUriComponent (aka punycode)
                    origin = request.Scheme + "://" + request.Host.Value.Substring(_options.MutualTls.DomainName.Length + 1);
                }
            }

            if (origin == null)
            {
                // no MTLS, so use the current origin for the issuer
                // this also means we emit the issuer value in unicode
                origin = _urls.GetUnicodeOrigin();
            }

            issuer = origin + _urls.BasePath;

            if (_options.LowerCaseIssuerUri)
            {
                issuer = issuer.ToLowerInvariant();
            }
        }

        return Task.FromResult(issuer);
    }
}

#endif