// ----------------------------------------------------------------------------
// <copyright file="RelyingParty.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Abc.IdentityServer.WsFederation.EntityFramework.Entities;

/// <summary>
/// 
/// </summary>
public class RelyingParty
{
    public int ClientId { get; set; }
    public virtual IdsEntities.Client Client { get; set; }

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
    [Required]
    public string TokenType { get; set; }

    /// <summary>
    /// Gets or sets the digest algorithm.
    /// </summary>
    /// <value>
    /// The digest algorithm.
    /// </value>
    [Required]
    public string DigestAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the signature algorithm.
    /// </summary>
    /// <value>
    /// The signature algorithm.
    /// </value>
    [Required]
    public string SignatureAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the saml name identifier format.
    /// </summary>
    /// <value>
    /// The saml name identifier format.
    /// </value>
    public string NameIdentifierFormat { get; set; }

    /// <summary>
    /// Gets or sets the encryption certificate.
    /// </summary>
    /// <value>
    /// The encryption certificate.
    /// </value>
    public byte[] EncryptionCertificate { get; set; }

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
    /// Gets or sets the claim mappings.
    /// </summary>
    /// <value>
    /// The claim mappings.
    /// </value>
    public virtual ICollection<RelyingPartyClaimMapping> ClaimMappings { get; set; }
}