
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Net.Http.Headers;

public static class HttRequestMessageExtensions
{
    // <summary>
    // extract cookies from response and set them to request message
    // </summary>
    public static HttpRequestMessage SetCookiesFromResponse(this HttpRequestMessage request, HttpResponseMessage response)
    {
        IEnumerable<string> values;
        if (response.Headers.TryGetValues("Set-Cookie", out values))
        {
            var setCookieHeaderValues = SetCookieHeaderValue.ParseList(values.ToList());
            var cookiesValues = setCookieHeaderValues.Select(c => new CookieHeaderValue(c.Name, c.Value).ToString());
            var cookieHeaderValue = string.Join("; ", cookiesValues);
            request.Headers.Add("Cookie", cookieHeaderValue);
        }
        return request;
    }
}