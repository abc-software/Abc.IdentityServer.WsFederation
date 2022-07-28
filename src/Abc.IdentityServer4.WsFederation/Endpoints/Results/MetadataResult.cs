using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Abc.IdentityServer4.WsFederation.Endpoints.Results
{
    public class MetadataResult : IEndpointResult
    {
        private readonly WsFederationConfigurationEx _configuration;

        public MetadataResult(WsFederationConfigurationEx configuration)
        {
            _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
        }

        public Task ExecuteAsync(HttpContext context)
        {
            var ser = new WsFederationMetadataSerializer();
            using (var ms = new MemoryStream())
            using (var writer = XmlDictionaryWriter.CreateTextWriter(ms, Encoding.UTF8, false))
            {
                ser.WriteMetadataEx(writer, _configuration);
                writer.Flush();
                context.Response.ContentType = "application/xml";
                var metaAsString = Encoding.UTF8.GetString(ms.ToArray());
                return context.Response.WriteAsync(metaAsString);
            }
        }
    }
}