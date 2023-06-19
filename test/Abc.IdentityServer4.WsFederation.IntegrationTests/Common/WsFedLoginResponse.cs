using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace Abc.IdentityServer.WsFederation.IntegrationTests.Common
{
    public class WsFedLoginResponse
    {
        private readonly string action;

        public IDictionary<string, string> Items { get; }

        public string Action => action;

        public WsFederationMessage Message { 
            get {
                return new WsFederationMessage
                {
                    Wresult = WebUtility.HtmlDecode(Items["wresult"]),
                };
            } 
        }

        public WsFedLoginResponse(string html)
        {
            Items = AnalysePost(html, out action);
        }

        private Dictionary<string, string> AnalysePost(string html, out string action)
        {
            action = null;
            var match = new Regex(@"action=""(?<action>.+?)""").Match(html);
            if (match.Success)
            {
                action = HttpUtility.HtmlDecode(match.Groups["action"].Value);
            }

            var collection = new Dictionary<string, string>();
            var mathces = new Regex(@"<input type=""hidden"" name=""(?<name>.+?)"" value=""(?<value>.+?)"" />").Matches(html);
            foreach (Match m in mathces)
            {
                collection.Add(m.Groups["name"].Value, HttpUtility.HtmlDecode(m.Groups["value"].Value));
            }

            return collection;
        }

        //private string ExtractInBetween(string source, string startStr, string endStr)
        //{
        //    var startIndex = source.IndexOf(startStr) + startStr.Length;
        //    var length = source.IndexOf(endStr, startIndex) - startIndex;
        //    var result = source.Substring(startIndex, length);
        //    return result;
        //}
    }
}
