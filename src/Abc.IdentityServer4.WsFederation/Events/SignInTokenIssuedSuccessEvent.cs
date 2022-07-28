using Abc.IdentityServer4.Extensions;
using Abc.IdentityServer4.WsFederation.Validation;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Collections.Generic;

namespace Abc.IdentityServer4.WsFederation.Events
{
    public class SignInTokenIssuedSuccessEvent : TokenIssuedSuccessEvent
    {
        public SignInTokenIssuedSuccessEvent(WsFederationMessage responseMessage, WsFederationValidationResult request)
            : base()
        {
            ClientId = request.ValidatedRequest.Client.ClientId;
            ClientName = request.ValidatedRequest.Client.ClientName;
            Endpoint = WsFederationConstants.EndpointNames.WsFederation;
            SubjectId = request.ValidatedRequest.Subject?.GetSubjectId();
            Scopes = request.ValidatedRequest.ValidatedResources?.RawScopeValues.ToSpaceSeparatedString();

            var tokens = new List<Token>();
            tokens.Add(new Token("SecurityToken", responseMessage.GetToken()));
            Tokens = tokens;
        }
    }
}
