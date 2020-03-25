using IdentityServer4.Models;
using IdentityServer4.WsFederation.Stores;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityServer4.WsFederation.Validation
{
    public class SignOutValidationResult
    {
        public bool IsError => !string.IsNullOrWhiteSpace(Error);
        public string Error { get; set; }
        public string ErrorMessage { get; set; }

        public WsFederationMessage WsFederationMessage { get; set; }
        
        public ClaimsPrincipal User { get; set; }
        public bool SignOutRequired { get; set; }

        public Client Client { get; set; }
        public RelyingParty RelyingParty { get; set; }

        public string ReplyUrl { get; set; }
        public string SessionId { get; set; }
        public IEnumerable<string> ClientIds { get; set; }
    }
}