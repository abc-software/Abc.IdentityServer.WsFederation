// ----------------------------------------------------------------------------
// <copyright file="RelyingPartyStore.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer.WsFederation.EntityFramework.Interfaces;
using Abc.IdentityServer.WsFederation.EntityFramework.Mappers;
using Abc.IdentityServer.WsFederation.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.EntityFramework.Stores;

/// <summary>
/// Implementation of IClientStore and IRelyingPartyStore thats uses EF.
/// </summary>
/// <seealso cref="IClientStore" />
/// <seealso cref="IRelyingPartyStore" />
public class RelyingPartyStore : ClientStore, IRelyingPartyStore
{
#if IDS4
    /// <summary>
    /// Initializes a new instance of the <see cref="RelyingPartyStore"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">if context is null.</exception>
    public RelyingPartyStore(IWsFedConfigurationDbContext context, ILogger<RelyingPartyStore> logger) 
        : base(context, logger)
    {
    }
#endif
#if DUENDE
    /// <summary>
    /// Initializes a new instance of the <see cref="RelyingPartyStore"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationTokenProvider">The cancelation token provider.</param>
    /// <exception cref="ArgumentNullException">if context is null.</exception>
    public RelyingPartyStore(IWsFedConfigurationDbContext context, ILogger<RelyingPartyStore> logger, ICancellationTokenProvider cancellationTokenProvider) 
        : base(context, logger, cancellationTokenProvider)
    {
    }
#endif

    /// <summary>
    /// Gets the DbContext.
    /// </summary>
    protected new IWsFedConfigurationDbContext Context { get => (IWsFedConfigurationDbContext)base.Context; }

    /// <inheritdoc/>
    public async Task<RelyingParty> FindRelyingPartyByRealmAsync(string realm)
    {
        var query = Context.Clients
            .Where(x => x.ClientId == realm)
            .Join(Context.WsFedRelyingParties.Include(c => c.EncryptionCertificate), x => x.Id, y => y.ClientId, (c, rp) => 
            new Entities.RelyingParty
            {
                Realm = c.ClientId,
                TokenType = rp.TokenType,
                DigestAlgorithm = rp.DigestAlgorithm,
                SignatureAlgorithm = rp.SignatureAlgorithm,
                EncryptionAlgorithm = rp.EncryptionAlgorithm,
                KeyWrapAlgorithm = rp.KeyWrapAlgorithm,
                WsTrustVersion = rp.WsTrustVersion,
                NameIdentifierFormat = rp.NameIdentifierFormat,

                ClaimMappings = rp.ClaimMappings,
                EncryptionCertificate = rp.EncryptionCertificate,
            })
            .AsNoTracking()
#if NET5_0_OR_GREATER
            .AsSplitQuery()
#endif
            ;

        var relyingParty = (await query.ToArrayAsync(
#if DUENDE
            CancellationTokenProvider.CancellationToken
#endif
            ))
            .SingleOrDefault(x => x.Realm == realm);
        if (relyingParty == null)
        {
            return null;
        }

        return relyingParty.ToModel();
    }
}