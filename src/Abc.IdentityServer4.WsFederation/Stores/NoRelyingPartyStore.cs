// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Modified by ABC software Ltd.

using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.Stores
{
    /// <summary>
    /// Nop implementation of IRelyingPartyStore that does not return relaying party.
    /// </summary>
    public class NoRelyingPartyStore : IRelyingPartyStore
    {
        /// <inheritdoc/>
        public Task<RelyingParty> FindRelyingPartyByRealmAsync(string realm)
        {
            return Task.FromResult<RelyingParty>(null);
        }
    }
}