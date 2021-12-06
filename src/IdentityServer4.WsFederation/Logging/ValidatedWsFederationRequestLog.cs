using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.WsFederation.Validation;
using System.Collections.Generic;
using System.Linq;

namespace IdentityServer4.WsFederation.Logging
{
    internal class ValidatedWsFederationRequestLog
    {
        public ValidatedWsFederationRequestLog(ValidatedWsFederationRequest request, IEnumerable<string> sensitiveValuesFilter)
        {
            Raw = request.WsFederationMessage.ToScrubbedDictionary(sensitiveValuesFilter.ToArray());

            if (request.Client != null)
            {
                ClientId = request.Client.ClientId;
                ClientName = request.Client.ClientName;
                AllowedRedirectUris = request.Client.RedirectUris;
            }

            if (request.Subject != null)
            {
                var subjectClaim = request.Subject.FindFirst(JwtClaimTypes.Subject);
                if (subjectClaim != null)
                {
                    SubjectId = subjectClaim.Value;
                }
                else
                {
                    SubjectId = "anonymous";
                }
            }

            ReplyUrl = request.ReplyUrl;
        }

        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string ReplyUrl { get; set; }
        public IEnumerable<string> AllowedRedirectUris { get; set; }
        public string SubjectId { get; set; }

        public Dictionary<string, string> Raw { get; set; }

        public override string ToString()
        {
            return LogSerializer.Serialize(this);
        }
    }
}
