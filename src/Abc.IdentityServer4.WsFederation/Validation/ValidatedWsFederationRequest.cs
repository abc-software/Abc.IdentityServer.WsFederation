// ----------------------------------------------------------------------------
// <copyright file="ValidatedWsFederationRequest.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer4.WsFederation.Stores;
using IdentityServer4.Validation;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Collections.Generic;

namespace Abc.IdentityServer4.WsFederation.Validation
{
    /// <summary>
    /// Models a validated request to the sign in endpoint.
    /// </summary>
    public class ValidatedWsFederationRequest : ValidatedRequest
    {
        public WsFederationMessage WsFederationMessage { get; set; } = new WsFederationMessage();

        public RelyingParty RelyingParty { get; set; }

        /// <summary>
        /// Gets or sets the reply URL.
        /// </summary>
        /// <value>
        /// The reply URL.
        /// </value>
        public string ReplyUrl { get; set; }

        public IEnumerable<string> ClientIds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether consent was shown.
        /// </summary>
        /// <value>
        /// <c>true</c> if consent was shown; otherwise, <c>false</c>.
        /// </value>
        public bool WasConsentShown { get; set; }

        /// <summary>
        /// Gets or sets desired maximum age of authentication requests, in minutes.
        /// </summary>
        /// <value>
        /// The desired maximum age of authentication requests, in minutes.
        /// </value>
        public int? Freshness { get; set; }

        /// <summary>
        /// Gets or sets the identity provider.
        /// </summary>
        /// <value>
        /// The identity provider.
        /// </value>
        public string HomeRealm { get; set; }
    }
}