// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using IdentityServer4.Extensions;
using System;

namespace Abc.IdentityServer4.WsFederation.Stores
{
    public class InMemoryRelyingPartyStore : IRelyingPartyStore
    {
        private IEnumerable<RelyingParty> _relyingParties;

        public InMemoryRelyingPartyStore(IEnumerable<RelyingParty> relyingParties)
        {
            _relyingParties = relyingParties ?? throw new ArgumentNullException(nameof(relyingParties));

            if (_relyingParties.HasDuplicates(m => m.Realm))
            {
                throw new ArgumentException("Relying parties must not contain duplicate entityIds", nameof(relyingParties));
            }
        }

        public Task<RelyingParty> FindRelyingPartyByRealmAsync(string realm)
        {
            return Task.FromResult(_relyingParties.FirstOrDefault(r => r.Realm == realm));
        }
    }
}