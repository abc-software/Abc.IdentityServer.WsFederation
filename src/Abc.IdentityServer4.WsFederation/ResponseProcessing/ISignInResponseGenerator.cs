// ----------------------------------------------------------------------------
// <copyright file="ISignInResponseGenerator.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer.WsFederation.Validation;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.ResponseProcessing
{
    /// <summary>
    /// Interface for the sign-in response generator.
    /// </summary>
    public interface ISignInResponseGenerator
    {
        /// <summary>
        /// Creates the WS-Federation response message.
        /// </summary>
        /// <param name="validationResult">The WS-Federation request validation result.</param>
        /// <returns>The WS-Federation response message.</returns>
        Task<WsFederationMessage> GenerateResponseAsync(WsFederationValidationResult validationResult);
    }
}