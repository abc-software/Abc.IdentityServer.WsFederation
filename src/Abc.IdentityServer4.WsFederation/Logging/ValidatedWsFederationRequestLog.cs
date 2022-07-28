using Abc.IdentityServer4.Extensions;
using Abc.IdentityServer4.WsFederation.Validation;
using IdentityModel;
using IdentityServer4.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Abc.IdentityServer4.WsFederation.Logging
{
    internal class ValidatedWsFederationRequestLog
    {
        public ValidatedWsFederationRequestLog(ValidatedWsFederationRequest request, IEnumerable<string> sensitiveValuesFilter)
        {
            Raw = request.WsFederationMessage.ToScrubbedDictionary(sensitiveValuesFilter.ToArray());
            ClientId = request.ClientId;
            ReplyUrl = request.ReplyUrl;
            Freshness = request.Freshness;
            HomeRealm = request.HomeRealm;

            if (request.Client != null)
            {
                ClientName = request.Client.ClientName;
                AllowedRedirectUris = request.Client.RedirectUris;
                AllowedPostLogoutRedirectUris = request.Client.PostLogoutRedirectUris;
            }

            if (request.Subject != null)
            {
                var subjectClaim = request.Subject.FindFirst(JwtClaimTypes.Subject);
                SubjectId = subjectClaim != null ? subjectClaim.Value : "anonymous";
            }
        }

        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string ReplyUrl { get; set; }
        public IEnumerable<string> AllowedRedirectUris { get; set; }
        public IEnumerable<string> AllowedPostLogoutRedirectUris { get; set; }
        public string SubjectId { get; set; }
        public Dictionary<string, string> Raw { get; set; }
        public int? Freshness { get; set; }
        public string HomeRealm { get; set; }

        public override string ToString()
        {
            return LogSerializer.Serialize(this);
        }
    }
}
