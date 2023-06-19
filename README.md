# Abc.IdentityServer.WsFederation ![](https://github.com/abc-software/Abc.IdentityServer4.WsFederation/actions/workflows/dotnet.yml/badge.svg)


## Overview
Implementation WS-Federation IdP support for IdentityServer4 with .NET core.
This project is a continuation of the [IdentityServer4.WsFederation](https://github.com/IdentityServer/IdentityServer4.WsFederation) project

This is useful for connecting SharePoint or older ASP.NET relying parties to IdentityServer4.

## .NET Support
The underlying WS-Federation classes use .NET Core.

## WS-Federation endpoint
The WS-Federation endpoints is implemented via an `IdentityServer4.Hosting.IEndpointHanlder`.
Endpoint _~/wsfed/metadata_ returns WS-Federation metadata, _~/wsfed_ process WS-Federation sing-in and sign-out requests.
This endpoints handles the WS-Federation protocol requests and redirects the user to the login page if needed.

The login page will then use the normal return URL mechanism to redirect back to the WS-Federation endpoint
to create the protocol response.

## Response generation
The `SignInResponseGenerator` class does the heavy lifting of creating the contents of the WS-Federation response:

* it calls the IdentityServer profile service to retrieve the configured claims for the relying party
* it tries to map the standard claim types to WS-* style claim types
* it creates the SAML 1.1/2.0 token
* it creates the RSTR (request security token response)

The outcome of these operations is a `SignInResponseMessage` object which then gets turned into a WS-Federation response and sent back to the relying party.

## Configuration
For most parts, the WS-Federation endpoint can use the standard IdentityServer4 client configuration for relying parties.
But there are also options available for setting WS-Federation specific options.

### Defaults
You can configure global defaults in the `WsFederationOptions` class, e.g.:

* default token type (SAML 1.1 or SAML 2.0)
* default hashing and digest algorithms
* default SAML name identifier format
* default encryption and keywrap algorithms
* default WS-Trust version
* default mappings from "short" claim types to WS-* claim types
* specify SecurityTokenHandlers

### Relying party configuration
The following client settings are used by the WS-Federation endpoint:

```csharp
public static IEnumerable<Client> GetClients()
{
    return new[]
    {
        new Client
        {
            // realm identifier
            ClientId = "urn:owinrp",
            
            // must be set to WS-Federation
            ProtocolType = ProtocolTypes.WsFederation,

            // reply URL
            RedirectUris = { "http://localhost:10313/" },
            
            // signout cleanup url
            LogoutUri = "http://localhost:10313/home/signoutcleanup",
            
            // lifetime of SAML token
            AccessTokenLifetime = 36000,

            // identity scopes - the associated claims will be used to call the profile service
            AllowedScopes = { "openid", "profile" }
        }
    };
}
```

### WS-Federation specific relying party settings
If you want to deviate from the global defaults (e.g. set a different token type or claim mapping) for a specific
relying party, you can define a `RelyingParty` object that uses the same realm name as the client ID used above.

This sample contains an in-memory relying party store that you can use to make these relying party specific settings
available to the WS-Federation engine (using the `AddInMemoryRelyingParty` extension method).
Otherwise, if you want to use your own store, you will need an implementation of `IRelyingPartyStore`.

### Configuring IdentityServer
This repo contains an extension method for the IdentityServer builder object to register all the necessary services in DI, e.g.:

```csharp
services.AddIdentityServer()
    .AddSigningCredential(cert)
    .AddInMemoryIdentityResources(Config.GetIdentityResources())
    .AddInMemoryApiResources(Config.GetApiResources())
    .AddInMemoryClients(Config.GetClients())
    .AddTestUsers(TestUsers.Users)
    .AddWsFederation()
    .AddInMemoryRelyingParties(Config.GetRelyingParties());
```

### Enable encrypted SAML1.1/2.0 tokens
Add to project Abc.IdentityModel.Tokens.Saml via nuget and change SecurityTokenHandlers, e.g.:

```csharp
builder.AddWsFederation(options => {
    // Add encrypted SAML1.1 & SAML2.0 tokens support
    options.SecurityTokenHandlers = new Collection<SecurityTokenHandler>() {
            new Abc.IdentityModel.Tokens.Saml2.Saml2SecurityTokenHandler(),
            new Abc.IdentityModel.Tokens.Saml.SamlSecurityTokenHandler(),
        };
});
```

## Connecting a relying party to the WS-Federation endpoint

### Using .NET Core
Use the .NET Core WS-Federation middleware to point to the WS-Federation endpoint, e.g.:

```csharp
public void ConfigureServices(IServiceCollection services) 
{
    services.AddMvc();

    services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = WsFederationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "aspnetcorewsfed";
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
    })
    .AddWsFederation(options =>
    {
        options.MetadataAddress = "http://localhost:5000/wsfed/metadata";
        // SSL off for test
        options.RequireHttpsMetadata = false;

        options.Wtrealm = "urn:aspnetcorerp";
        options.SignOutWreply = "http://localhost:10314/";

        options.SkipUnrecognizedRequests = true;
    });
}
```

### Using Katana
Use the Katana WS-Federation middleware to point to the WS-Federation endpoint, e.g.:

```csharp
public void Configuration(IAppBuilder app)
{
    app.UseCookieAuthentication(new CookieAuthenticationOptions
    {
        AuthenticationType = "Cookies"
    });

    app.UseWsFederationAuthentication(new WsFederationAuthenticationOptions
    {
        MetadataAddress = "http://localhost:5000/wsfed/metadata",
        Wtrealm = "urn:owinrp",
        SignOutWreply = System.Web.VirtualPathUtility.ToAbsolute("~/"),

        SignInAsAuthenticationType = "Cookies"
    });
}
```

### Using ASP.NET WebForms
Use the WebForms WS-Federation module to point to the WS-Federation endpoint, e.g.:

```xml
<configuration>
    <system.webServer>
        <modules>
            <remove name="FormsAuthentication" />
            <add name="WSFederationAuthenticationModule" type="System.IdentityModel.Services.WSFederationAuthenticationModule, System.IdentityModel.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" preCondition="managedHandler" />
            <add name="SessionAuthenticationModule" type="System.IdentityModel.Services.SessionAuthenticationModule, System.IdentityModel.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" preCondition="managedHandler" />
        </modules>
    </system.webServer>
    <system.identityModel>
    <identityConfiguration>
        <issuerNameRegistry type="System.IdentityModel.Tokens.ConfigurationBasedIssuerNameRegistry, System.IdentityModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089">
        <trustedIssuers>
            <add thumbprint="6b 7a cc 52 03 05 bf db 4f 72 52 da eb 21 77 cc 09 1f aa e1" name="IDS4" />
        </trustedIssuers>
        </issuerNameRegistry>
        <audienceUris>
        <add value="urn:aspnetwebapprp" />
        </audienceUris>
        <!-- TODO remove for production -->
        <certificateValidation certificateValidationMode="None" />
    </identityConfiguration>    
    </system.identityModel>
    <system.identityModel.services>
        <federationConfiguration>
            <!-- TODO SSL off for test -->
            <cookieHandler requireSsl="false" />
            <wsFederation issuer="http://localhost:5000/wsfed" realm="urn:aspnetwebapprp" requireHttps="false" signOutQueryString="wtrealm=urn:aspnetwebapprp" />
        </federationConfiguration>
    </system.identityModel.services>
</configuration>
  ```

### SharePoint

see https://www.scottbrady91.com/Identity-Server/IdentityServer-4-SharePoint-Integration-using-WS-Federation
