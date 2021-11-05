using IdentityServer4.Models;
using IdentityServer4.Validation;
using IdentityServer4.WsFederation.Stores;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityServer4.WsFederation.Validation
{
    public class SignOutValidationResult : ValidationResult
    {
        public SignOutValidationResult(ValidatedWsFederationRequest validatedRequest)
        {
            IsError = false;
            ValidatedRequest = validatedRequest;
        }

        public SignOutValidationResult(ValidatedWsFederationRequest message, string error, string errorDescription = null)
        {
            IsError = true;
            ValidatedRequest = message;
            Error = error;
            ErrorDescription = errorDescription;
        }

        public ValidatedWsFederationRequest ValidatedRequest { get; }
    }
}