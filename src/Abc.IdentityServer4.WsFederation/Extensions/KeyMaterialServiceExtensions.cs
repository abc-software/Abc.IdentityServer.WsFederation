// ----------------------------------------------------------------------------
// <copyright file="KeyMaterialServiceExtensions.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Microsoft.IdentityModel.Tokens;
using System;
using System.Threading.Tasks;

namespace Abc.IdentityServer.Extensions
{
    internal static class KeyMaterialServiceExtensions
    {
        public static async Task<X509SecurityKey> GetX509SigningKeyAsync(this IKeyMaterialService keyMaterialService)
        {
            var signingKey = (await keyMaterialService.GetSigningCredentialsAsync()).Key as X509SecurityKey;
            if (signingKey == null)
            {
                throw new InvalidOperationException($"No X509 signing credential registered.");
            }

            return signingKey;
        }
    }
}