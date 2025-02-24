// ----------------------------------------------------------------------------
// <copyright file="WsFedConfigurationDbContext.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer.WsFederation.EntityFramework.Entities;
using Abc.IdentityServer.WsFederation.EntityFramework.Extensions;
using Abc.IdentityServer.WsFederation.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace Abc.IdentityServer.WsFederation.EntityFramework.DbContext;

/// <summary>
/// DbContext for the IdentityServer configuration data.
/// </summary>
/// <seealso cref="Microsoft.EntityFrameworkCore.DbContext" />
/// <seealso cref="WsFedConfigurationDbContext" />
public class WsFedConfigurationDbContext : WsFedConfigurationDbContext<WsFedConfigurationDbContext>
{
#if IDS4 || IDS8
    /// <summary>
    /// Initializes a new instance of the <see cref="WsFedConfigurationDbContext"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">storeOptions</exception>
    public WsFedConfigurationDbContext(DbContextOptions<WsFedConfigurationDbContext> options, WsFedConfigurationStoreOptions storeOptions)
        : base(options, storeOptions)
    {
    }
#endif
#if DUENDE
    /// <summary>
    /// Initializes a new instance of the <see cref="WsFedConfigurationDbContext"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">storeOptions</exception>
    public WsFedConfigurationDbContext(DbContextOptions<WsFedConfigurationDbContext> options)
        : base(options)
    {
    }
#endif
}

/// <summary>
/// DbContext for the IdentityServer configuration data.
/// </summary>
/// <seealso cref="Microsoft.EntityFrameworkCore.DbContext" />
/// <seealso cref="WsFedConfigurationDbContext
public class WsFedConfigurationDbContext<TContext> : ConfigurationDbContext<TContext>, Interfaces.IWsFedConfigurationDbContext
    where TContext : Microsoft.EntityFrameworkCore.DbContext, IConfigurationDbContext, Interfaces.IWsFedConfigurationDbContext
{
#if IDS4 || IDS8
    /// <summary>
    /// Initializes a new instance of the <see cref="WsFedConfigurationDbContext{TContext}"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">storeOptions</exception>
    public WsFedConfigurationDbContext(DbContextOptions<TContext> options, WsFedConfigurationStoreOptions storeOptions)
        : base(options, storeOptions)
    {
        this.StoreOptions = storeOptions;
    }

    /// <summary>
    /// Gets or sets the store options.
    /// </summary>
    public Options.WsFedConfigurationStoreOptions StoreOptions { get; }
#endif
#if DUENDE
    /// <summary>
    /// Initializes a new instance of the <see cref="WsFedConfigurationDbContext{TContext}"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">storeOptions</exception>
    public WsFedConfigurationDbContext(DbContextOptions<TContext> options) 
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the store options.
    /// </summary>
    public new Options.WsFedConfigurationStoreOptions StoreOptions { 
        get => (Options.WsFedConfigurationStoreOptions)base.StoreOptions; 
        set => base.StoreOptions = value; 
    }
#endif

    /// <summary>
    /// Gets or sets the relying parties.
    /// </summary>
    /// <value>
    /// The relying parties.
    /// </value>
    public DbSet<RelyingParty> WsFedRelyingParties { get; set; }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
#if DUENDE
        if (StoreOptions is null)
        {
            StoreOptions = this.GetService<WsFedConfigurationStoreOptions>();
            if (StoreOptions is null)
            {
                throw new ArgumentNullException(nameof(StoreOptions), "WsFedConfigurationStoreOptions must be configured in the DI system.");
            }
        }
#endif

        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureRelyingPartyContext(StoreOptions);
    }
}