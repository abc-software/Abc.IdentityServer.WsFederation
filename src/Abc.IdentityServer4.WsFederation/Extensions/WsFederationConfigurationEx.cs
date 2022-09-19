// ----------------------------------------------------------------------------
// <copyright file="WsFederationConfigurationEx.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.IdentityModel.Protocols.WsFederation
{
    public class WsFederationConfigurationEx : WsFederationConfiguration
    {
        public ICollection<string> TokenTypesOffered { get; } = new Collection<string>();
        
        public ICollection<string> ClaimTypesOffered { get; } = new Collection<string>();
    }
}