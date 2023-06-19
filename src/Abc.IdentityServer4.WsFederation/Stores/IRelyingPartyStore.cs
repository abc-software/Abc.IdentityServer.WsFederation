// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Modified by ABC software Ltd.

using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.Stores
{
    /// <summary>
    /// Retrieval of relaying party configuration.
    /// </summary>
    public interface IRelyingPartyStore
    {
        /// <summary>
        /// Fins the realying party by realm.
        /// </summary>
        /// <param name="realm">The realying party realm.</param>
        /// <returns>The relaying party.</returns>
        Task<RelyingParty> FindRelyingPartyByRealmAsync(string realm);
    }
}