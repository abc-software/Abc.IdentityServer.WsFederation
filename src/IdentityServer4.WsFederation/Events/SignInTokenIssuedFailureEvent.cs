using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.WsFederation.Validation;

namespace IdentityServer4.WsFederation.Events
{
    public class SignInTokenIssuedFailureEvent : TokenIssuedFailureEvent
    {
        public SignInTokenIssuedFailureEvent(ValidatedWsFederationRequest request, string error, string description)
            : base()
        {
            if (request != null)
            {
                ClientId = request.Client?.ClientId;
                ClientName = request.Client?.ClientName;

                if (request.Subject != null && request.Subject.Identity.IsAuthenticated)
                {
                    SubjectId = request.Subject.GetSubjectId();
                }
            }

            Endpoint = WsFederationConstants.EndpointNames.WsFederation;
            Error = error;
            ErrorDescription = description;
        }
    }
}
