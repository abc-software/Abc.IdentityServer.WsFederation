using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Microsoft.IdentityModel.Protocols.WsFederation
{
    public class WsFederationConfigurationEx : WsFederationConfiguration
    {
        public ICollection<string> TokenTypesOffered { get; } = new Collection<string>();
        
        public ICollection<string> ClaimTypesOffered { get; } = new Collection<string>();
    }
}
