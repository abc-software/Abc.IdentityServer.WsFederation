// ----------------------------------------------------------------------------
// <copyright file="WsFederationValidationResult.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using IdentityServer4.Validation;

namespace Abc.IdentityServer4.WsFederation.Validation
{
    /// <summary>
    /// Validation result for WS-Federation requests.
    /// </summary>
    /// <seealso cref="ValidationResult" />
    public class WsFederationValidationResult : ValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WsFederationValidationResult" /> class.
        /// </summary>
        /// <param name="validatedRequest">The validated WS-Federation request.</param>
        public WsFederationValidationResult(ValidatedWsFederationRequest validatedRequest)
        {
            IsError = false;
            ValidatedRequest = validatedRequest ?? throw new System.ArgumentNullException(nameof(validatedRequest));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WsFederationValidationResult" /> class with error and error description.
        /// </summary>
        /// <param name="validatedRequest">The validated WS-Federation request.</param>
        /// <param name="error">The error.</param>
        /// <param name="errorDescription">The error description.</param>
        public WsFederationValidationResult(ValidatedWsFederationRequest validatedRequest, string error, string errorDescription = null)
        {
            IsError = true;
            ValidatedRequest = validatedRequest ?? throw new System.ArgumentNullException(nameof(validatedRequest));
            Error = error;
            ErrorDescription = errorDescription;
        }

        /// <summary>
        /// Gets the validated WS-Federation request.
        /// </summary>
        /// <value>
        /// The validated WS-Federation request.
        /// </value>
        public ValidatedWsFederationRequest ValidatedRequest { get; }
    }
}