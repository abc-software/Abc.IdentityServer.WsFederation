#if IDS4 || IDS8

using Abc.IdentityServer.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

#if IDS4
namespace IdentityServer4.Extensions;
#else
namespace IdentityServer8.Extensions;
#endif

/// <summary>
/// Extension methods for <see cref="IServerUrls"/>.
/// </summary>
public static class ServerUrlExtensions
{
    /// <summary>
    /// Returns the origin in unicode, and not in punycode (if we have a unicode hostname).
    /// </summary>
    public static string GetUnicodeOrigin(this IServerUrls urls)
    {
        var split = urls.Origin.Split(new[] { "://" }, StringSplitOptions.RemoveEmptyEntries);
        var scheme = split.First();
        var host = HostString.FromUriComponent(split.Last()).Value;

        return scheme + "://" + host;
    }

    /// <summary>
    /// Returns an absolute URL for the URL or path.
    /// </summary>
    public static string GetAbsoluteUrl(this IServerUrls urls, string urlOrPath)
    {
        if (urlOrPath.IsLocalUrl())
        {
            if (urlOrPath.StartsWith("~/"))
            {
                urlOrPath = urlOrPath.Substring(1);
            }

            urlOrPath = urls.BaseUrl.EnsureTrailingSlash() + urlOrPath.RemoveLeadingSlash();
        }

        return urlOrPath;
    }

    /// <summary>
    /// Returns the URL into the server based on the relative path. The path parameter can start with "~/" or "/".
    /// </summary>
    public static string GetIdentityServerRelativeUrl(this IServerUrls urls, string path)
    {
        if (!path.IsLocalUrl())
        {
            return null;
        }

        if (path.StartsWith("~/"))
        {
            path = path.Substring(1);
        }

        path = urls.BaseUrl.EnsureTrailingSlash() + path.RemoveLeadingSlash();
        return path;
    }
}

#endif