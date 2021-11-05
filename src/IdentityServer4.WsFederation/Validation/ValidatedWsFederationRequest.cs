using IdentityServer4.Validation;
using IdentityServer4.WsFederation.Stores;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Collections.Generic;

namespace IdentityServer4.WsFederation.Validation
{
    public class ValidatedWsFederationRequest : ValidatedRequest
    {
        public WsFederationMessage WsFederationMessage { get; set; }

        public RelyingParty RelyingParty { get; set; }
        
        public string ReplyUrl { get; set; }

        public IEnumerable<string> ClientIds { get; set; }

        public string AdditionalContext { get; set; }
    }
}