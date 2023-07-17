// ----------------------------------------------------------------------------
// <copyright file="MetadataResult.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityModel.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Abc.IdentityServer.WsFederation.Endpoints.Results
{
    public class MetadataResult : IEndpointResult
    {
        private readonly DescriptorBase _metadata;
        private MetadataSerializer _serializer;

        public MetadataResult(DescriptorBase metadata)
        {
            _metadata = metadata ?? throw new System.ArgumentNullException(nameof(metadata));
        }

        public Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            using var stream = new MemoryStream();
            using (var writer = XmlWriter.Create(stream))
            {
                _serializer.WriteMetadata(writer, _metadata);
            }

            context.Response.ContentType = "application/xml";
            var metaAsString = Encoding.UTF8.GetString(stream.ToArray());
            return context.Response.WriteAsync(metaAsString);
        }

        private void Init(HttpContext context)
        {
            _serializer ??= context.RequestServices.GetService<MetadataSerializer>() ?? new MetadataSerializer();
        }
    }
}