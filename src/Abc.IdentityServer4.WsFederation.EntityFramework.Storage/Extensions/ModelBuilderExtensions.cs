﻿// ----------------------------------------------------------------------------
// <copyright file="ModelBuilderExtensions.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Abc.IdentityServer.WsFederation.EntityFramework.Extensions;

/// <summary>
/// Extension methods to define the database schema for the configuration data store.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures the relying party context.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="storeOptions">The store options.</param>
    public static void ConfigureRelyingPartyContext(this ModelBuilder modelBuilder, Options.WsFedConfigurationStoreOptions storeOptions)
    {
        modelBuilder.Entity<Entities.RelyingParty>(rp =>
        {
            rp.ToTable(storeOptions.RelyingParty);

            rp.HasKey(x => x.ClientId);
            rp.Ignore(x => x.Realm);

            rp.Property(x => x.TokenType).HasMaxLength(200).IsRequired();
            rp.Property(x => x.DigestAlgorithm).HasMaxLength(200).IsRequired();
            rp.Property(x => x.SignatureAlgorithm).HasMaxLength(200).IsRequired();
            rp.Property(x => x.NameIdentifierFormat).HasMaxLength(200);
            rp.Property(x => x.EncryptionCertificate);
            rp.Property(x => x.EncryptionAlgorithm).HasMaxLength(200);
            rp.Property(x => x.KeyWrapAlgorithm).HasMaxLength(200);
            rp.Property(x => x.WsTrustVersion).HasMaxLength(200);

            rp.HasMany(x => x.ClaimMappings).WithOne(x => x.RelyingParty).HasForeignKey(x => x.ClientId).IsRequired().OnDelete(DeleteBehavior.Cascade);

            rp.HasOne(e => e.Client).WithOne().HasForeignKey<Entities.RelyingParty>(e => e.ClientId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Entities.RelyingPartyClaimMapping>(claimMapping =>
        {
            claimMapping.ToTable(storeOptions.RelyingPartyClaimMapping);

            claimMapping.HasKey(x => x.Id);

            claimMapping.Property(x => x.FromClaimType).HasMaxLength(150).IsRequired();
            claimMapping.Property(x => x.ToClaimType).HasMaxLength(150).IsRequired();
        });
    }

    private static EntityTypeBuilder<TEntity> ToTable<TEntity>(this EntityTypeBuilder<TEntity> entityTypeBuilder, TableConfiguration configuration)
        where TEntity : class
    {
        if (!string.IsNullOrWhiteSpace(configuration.Schema))
        {
            return entityTypeBuilder.ToTable(configuration.Name, configuration.Schema);
        }

        return entityTypeBuilder.ToTable(configuration.Name);
    }
}