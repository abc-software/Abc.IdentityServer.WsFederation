// ----------------------------------------------------------------------------
// <copyright file="ISignInResponseGenerator.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer4.WsFederation.Validation;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.ResponseProcessing
{
    public interface ISignInResponseGenerator
    {
        Task<WsFederationMessage> GenerateResponseAsync(WsFederationValidationResult validationResult);
    }
}