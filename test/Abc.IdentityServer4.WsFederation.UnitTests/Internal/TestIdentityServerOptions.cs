﻿#if DUENDE
namespace Duende.IdentityServer.Configuration
#else
namespace IdentityServer4.Configuration
#endif
{
    internal class TestIdentityServerOptions
    {
        public static IdentityServerOptions Create()
        {
            var options = new IdentityServerOptions
            {
                IssuerUri = "https://idsvr.com"
            };

            return options;
        }
    }
}
