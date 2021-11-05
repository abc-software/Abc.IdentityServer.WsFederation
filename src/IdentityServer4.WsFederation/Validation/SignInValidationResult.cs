// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using IdentityServer4.Validation;
using System.Security.Claims;

namespace IdentityServer4.WsFederation.Validation
{
    public class SignInValidationResult : ValidationResult
    {
        public SignInValidationResult(ValidatedWsFederationRequest validatedRequest, bool signInRequired = false)
        {
            IsError = false;
            ValidatedRequest = validatedRequest;
            SignInRequired = signInRequired;
        }

        public SignInValidationResult(ValidatedWsFederationRequest message, string error, string errorDescription = null)
        {
            IsError = true;
            ValidatedRequest = message;
            Error = error;
            ErrorDescription = errorDescription;
        }

        public ValidatedWsFederationRequest ValidatedRequest { get;}
        
        public bool SignInRequired { get; }
    }
}