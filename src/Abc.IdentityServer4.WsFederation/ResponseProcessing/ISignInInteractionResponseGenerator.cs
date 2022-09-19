// ----------------------------------------------------------------------------
// <copyright file="ISignInInteractionResponseGenerator.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer4.WsFederation.Validation;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.ResponseProcessing
{
    /// <summary>
    /// Interface for determining if user must login when making requests to the WS-Federation endpoint.
    /// </summary>
    public interface ISignInInteractionResponseGenerator
    {
        /// <summary>
        /// Processes the interaction logic.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="consent">The consent.</param>
        /// <returns>The interaction response.</returns>
        Task<InteractionResponse> ProcessInteractionAsync(ValidatedWsFederationRequest request, ConsentResponse consent = null);
    }
}