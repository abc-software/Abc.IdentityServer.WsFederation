using IdentityModel;
using IdentityServer4.Models;
using System.Collections.Generic;
using static IdentityServer4.IdentityServerConstants;
using IdentityServer4.WsFederation.Stores;
using System.Security.Claims;
using IdentityServer4.Test;

namespace IdentityServer4.WsFederation
{
    public static class Config
    {
        public static List<TestUser> GetTestUsers()
        {
            return new List<TestUser> { 
                new TestUser
                {
                    SubjectId = "user1",
                    Username = "testName",
                    Password = "testPassword",
                    Claims = {
                        new Claim(JwtClaimTypes.Subject, "user1"),
                        new Claim(JwtClaimTypes.Name, "testName"),
                        new Claim(JwtClaimTypes.Email, "testUser1@email.com"),
                    }
                }
            };
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new[]
            {
                new IdentityResources.OpenId(),
                new IdentityResource("profile", new[] { JwtClaimTypes.Name, JwtClaimTypes.Email })
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new[]
            {
                new ApiResource("api1", "Some API 1"),
                new ApiResource("api2", "Some API 2")
            };
        }

        public static IEnumerable<ApiScope> GetApiScopes()
        {
            return new[]
            {
                new ApiScope("api1", "Some API 1"),
                new ApiScope("api2", "Some API 2")
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new[]
            {
                new Client
                {
                    ClientId = "urn:owinrp",
                    ProtocolType = ProtocolTypes.WsFederation,

                    RedirectUris = { "http://localhost:10313/" },
                    FrontChannelLogoutUri = "http://localhost:10313/home/signoutcleanup",
                    IdentityTokenLifetime = 36000,

                    AllowedScopes = { "openid", "profile" }
                }
            };
        }

        public static IEnumerable<RelyingParty> GetRelyingParties()
        {
            return new[]
            {
                new RelyingParty
                {
                    Realm = "urn:owinrp",
                    TokenType = WsFederationConstants.TokenTypes.Saml11TokenProfile11,
                    ClaimMapping = new Dictionary<string, string> {
                        { JwtClaimTypes.Subject , ClaimTypes.NameIdentifier },
                        { JwtClaimTypes.Name , ClaimTypes.Name },
                        { JwtClaimTypes.Email , ClaimTypes.Email },
                        { JwtClaimTypes.AuthenticationTime, ClaimTypes.AuthenticationInstant }
                    },
                },
            };
        }
    }
}