// ----------------------------------------------------------------------------
// <copyright file="IMetadataResponseGenerator.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityModel.Metadata;
using System.Threading.Tasks;

namespace Abc.IdentityServer.WsFederation.ResponseProcessing
{
    /// <summary>
    /// Interface for metadata endpoint response generator.
    /// </summary>
    public interface IMetadataResponseGenerator
    {
        /// <summary>
        ///  Creates the metadata document.
        /// </summary>
        /// <returns>The metadata document.</returns>
        Task<DescriptorBase> GenerateAsync();
    }
}