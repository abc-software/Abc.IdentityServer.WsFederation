#if IDS4 || IDS8

using Abc.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

#if IDS4
namespace IdentityServer4.Services;
#else
namespace IdentityServer8.Services;
#endif

/// <summary>
/// Implements <see cref="IServerUrls"/>.
/// </summary>
public class DefaultServerUrls : IServerUrls
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultServerUrls"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public DefaultServerUrls(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public string Origin
    {
        get
        {
            var request = _httpContextAccessor.HttpContext.Request;
            return request.Scheme + "://" + request.Host.ToUriComponent();
        }

        set
        {
            var split = value.Split(new[] { "://" }, StringSplitOptions.RemoveEmptyEntries);

            var request = _httpContextAccessor.HttpContext.Request;
            request.Scheme = split.First();
            request.Host = new HostString(split.Last());
        }
    }

    /// <inheritdoc/>
    public string BasePath
    {
        get
        {
            return _httpContextAccessor.HttpContext.Items["idsvr:IdentityServerBasePath"] as string;
        }

        set
        {
            _httpContextAccessor.HttpContext.Items["idsvr:IdentityServerBasePath"] = value.RemoveTrailingSlash();
        }
    }
}

#endif