using IdentityServer4.Models;
using IdentityServer4.Validation;
using System.Security.Claims;

namespace Abc.IdentityServer4.WsFederation.Validation
{
    public class WsFederationValidationResult : ValidationResult
    {
        public WsFederationValidationResult(ValidatedWsFederationRequest validatedRequest)
        {
            IsError = false;
            ValidatedRequest = validatedRequest ?? throw new System.ArgumentNullException(nameof(validatedRequest));
        }

        public WsFederationValidationResult(ValidatedWsFederationRequest validatedRequest, string error, string errorDescription = null)
        {
            IsError = true;
            ValidatedRequest = validatedRequest ?? throw new System.ArgumentNullException(nameof(validatedRequest));
            Error = error;
            ErrorDescription = errorDescription;
        }

        public ValidatedWsFederationRequest ValidatedRequest { get;}
    }
}