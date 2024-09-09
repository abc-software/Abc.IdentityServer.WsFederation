// ----------------------------------------------------------------------------
// <copyright file="RelyingPartyMappers.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Abc.IdentityServer.WsFederation.EntityFramework.Mappers;

/// <summary>
/// Extension methods to map to/from entity/model for relying party.
/// </summary>
public static class RelyingPartyMappers
{
    /// <summary>
    /// Maps an entity to a model.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The model.</returns>
    public static WsFederation.Stores.RelyingParty ToModel(this Entities.RelyingParty entity)
    {
        return entity == null ? null :
            new WsFederation.Stores.RelyingParty
            {
                Realm = entity.Realm,
                TokenType = entity.TokenType,
                DigestAlgorithm = entity.DigestAlgorithm,
                SignatureAlgorithm = entity.SignatureAlgorithm,
                EncryptionAlgorithm = entity.EncryptionAlgorithm,
                KeyWrapAlgorithm = entity.KeyWrapAlgorithm,
                WsTrustVersion = entity.WsTrustVersion,
                NameIdentifierFormat = entity.NameIdentifierFormat,

                ClaimMapping = entity.ClaimMappings.ToDictionary(m => m.FromClaimType, m => m.ToClaimType),
                EncryptionCertificate = entity.EncryptionCertificate != null ? new X509Certificate2(entity.EncryptionCertificate) : null,
            };
    }

    /// <summary>
    /// Maps a model to an entity.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns>The entity.</returns>
    public static Entities.RelyingParty ToEntity(this WsFederation.Stores.RelyingParty model)
    {
        return model == null ? null :
            new Entities.RelyingParty
            {
                Realm = model.Realm,
                TokenType = model.TokenType,
                DigestAlgorithm = model.DigestAlgorithm,
                SignatureAlgorithm = model.SignatureAlgorithm,
                EncryptionAlgorithm = model.EncryptionAlgorithm,
                KeyWrapAlgorithm = model.KeyWrapAlgorithm,
                WsTrustVersion = model.WsTrustVersion,
                NameIdentifierFormat = model.NameIdentifierFormat,

                ClaimMappings = model.ClaimMapping?.Select(c => new Entities.RelyingPartyClaimMapping
                {
                    FromClaimType = c.Key,
                    ToClaimType = c.Value,
                }).ToList() ?? new List<Entities.RelyingPartyClaimMapping>(),

                EncryptionCertificate = model.EncryptionCertificate?.GetPublicKey(),
            };
    }
}