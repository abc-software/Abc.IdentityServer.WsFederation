// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// Modified by ABC software Ltd.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.Stores
{
    /// <summary>
    /// In-memory relying party store.
    /// </summary>
    public class InMemoryRelyingPartyStore : IRelyingPartyStore
    {
        private readonly IEnumerable<RelyingParty> _relyingParties;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryRelyingPartyStore"/> class.
        /// </summary>
        /// <param name="relyingParties">The relaying parties.</param>
        public InMemoryRelyingPartyStore(IEnumerable<RelyingParty> relyingParties)
        {
            _relyingParties = relyingParties ?? throw new ArgumentNullException(nameof(relyingParties));

            if (_relyingParties.HasDuplicates(m => m.Realm))
            {
                throw new ArgumentException("Relying parties must not contain duplicate entityIds", nameof(relyingParties));
            }
        }

        /// <inheritdoc/>
        public Task<RelyingParty> FindRelyingPartyByRealmAsync(string realm)
        {
            return Task.FromResult(_relyingParties.FirstOrDefault(r => r.Realm == realm));
        }
    }
}