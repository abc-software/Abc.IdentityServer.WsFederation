﻿// ----------------------------------------------------------------------------
// <copyright file="MetadataResult.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Abc.IdentityServer.WsFederation.Endpoints.Results
{
    public class MetadataResult : IEndpointResult
    {
        private readonly WsFederationConfigurationEx _configuration;

        public MetadataResult(WsFederationConfigurationEx configuration)
        {
            _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
        }

        public async Task ExecuteAsync(HttpContext context)
        {
            var ser = new WsFederationMetadataSerializer();
            using (var ms = new MemoryStream())
            using (var writer = XmlDictionaryWriter.CreateTextWriter(ms, Encoding.UTF8, false))
            {
                ser.WriteMetadataEx(writer, _configuration);
                await writer.FlushAsync();
                context.Response.ContentType = "application/xml";
                var metaAsString = Encoding.UTF8.GetString(ms.ToArray());
                await context.Response.WriteAsync(metaAsString);
            }
        }
    }
}