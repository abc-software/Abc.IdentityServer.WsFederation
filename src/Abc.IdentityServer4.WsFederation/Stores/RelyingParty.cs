// ----------------------------------------------------------------------------
// <copyright file="RelyingParty.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Abc.IdentityServer4.WsFederation.Stores
{
    /// <summary>
    /// The relying party.
    /// </summary>
    public class RelyingParty
    {
        /// <summary>
        /// Gets or sets the realm.
        /// </summary>
        /// <value>
        /// The realm.
        /// </value>
        public string Realm { get; set; }

        /// <summary>
        /// Gets or sets the type of the token.
        /// </summary>
        /// <value>
        /// The type of the token.
        /// </value>
        public string TokenType { get; set; }

        /// <summary>
        /// Gets or sets the signature digest.
        /// </summary>
        /// <value>
        /// The signature digest.
        /// </value>
        public string DigestAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets the signature algorithm.
        /// </summary>
        /// <value>
        /// The signature algorithm.
        /// </value>
        public string SignatureAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets the name identifier format.
        /// </summary>
        /// <value>
        /// The name identifier format.
        /// </value>
        public string NameIdentifierFormat { get; set; }

        /// <summary>
        /// Gets or sets the encryption certificate.
        /// </summary>
        /// <value>
        /// The encryption certificate.
        /// </value>
        public X509Certificate2 EncryptionCertificate { get; set; }

        /// <summary>
        /// Gets or sets the encryption algorithm.
        /// </summary>
        /// <value>
        /// The encryption algorithm.
        /// </value>
        public string EncryptionAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets the key wrap algorithm.
        /// </summary>
        /// <value>
        /// The key wrap algorithm.
        /// </value>
        public string KeyWrapAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets the WS-Trust version.
        /// </summary>
        /// <value>
        /// The WS-Trust version.
        /// </value>
        public WsTrustVersion WsTrustVersion { get; set; }

        /// <summary>
        /// Gets or sets the claim mapping.
        /// </summary>
        /// <value>
        /// The claim mapping.
        /// </value>
        public IDictionary<string, string> ClaimMapping { get; set; }
    }
}