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
    public interface IMetadataResponseGenerator
    {
        Task<DescriptorBase> GenerateAsync();
    }
}