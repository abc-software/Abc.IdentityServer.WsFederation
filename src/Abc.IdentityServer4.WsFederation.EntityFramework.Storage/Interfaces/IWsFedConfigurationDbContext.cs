// ----------------------------------------------------------------------------
// <copyright file="IWsFedConfigurationDbContext.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer.WsFederation.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace Abc.IdentityServer.WsFederation.EntityFramework.Interfaces;

/// <summary>
/// Abstraction for the configuration context.
/// </summary>
/// <seealso cref="IConfigurationDbContext" />
public interface IWsFedConfigurationDbContext : IConfigurationDbContext
{
    /// <summary>
    /// Gets or sets the relying parties.
    /// </summary>
    /// <value>
    /// The relying parties.
    /// </value>
    DbSet<RelyingParty> WsFedRelyingParties { get; set; }
}