// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.IdentityModel.Protocols.WsFederation;
using IdentityServer4.Hosting;

namespace IdentityServer4.WsFederation.Endpoints.Results
{
    public class MetadataResult : IEndpointResult
    {
        private readonly WsFederationConfiguration _config;

        public MetadataResult(WsFederationConfiguration config)
        {
            _config = config;
        }

        public Task ExecuteAsync(HttpContext context)
        {
            var ser = new WsFederationMetadataSerializer();
            using (var ms = new MemoryStream())
            using (XmlWriter writer = XmlDictionaryWriter.CreateTextWriter(ms, Encoding.UTF8, false))
            {
                WsFederationMetadataSerializerExtensions.WriteMetadata(ser, writer, _config);
                // ser.WriteMetadata(writer, _config);
                writer.Flush();
                context.Response.ContentType = "application/xml";
                var metaAsString = Encoding.UTF8.GetString(ms.ToArray());
                return context.Response.WriteAsync(metaAsString);
            }
        }
    }
}