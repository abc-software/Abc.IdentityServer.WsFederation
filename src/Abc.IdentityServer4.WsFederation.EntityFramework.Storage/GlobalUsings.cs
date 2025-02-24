#if DUENDE
global using Duende.IdentityServer.EntityFramework.DbContexts;
global using Duende.IdentityServer.EntityFramework.Interfaces;
global using Duende.IdentityServer.EntityFramework.Options;
global using Duende.IdentityServer.EntityFramework.Storage;
global using Duende.IdentityServer.EntityFramework.Stores;
global using Duende.IdentityServer.Services;
global using IdsEntities = Duende.IdentityServer.EntityFramework.Entities;
#elif IDS8
global using IdentityServer8.EntityFramework.DbContexts;
global using IdentityServer8.EntityFramework.Interfaces;
global using IdentityServer8.EntityFramework.Options;
global using IdentityServer8.EntityFramework.Storage;
global using IdentityServer8.EntityFramework.Stores;
global using IdsEntities = IdentityServer8.EntityFramework.Entities;
#else
global using IdentityServer4.EntityFramework.DbContexts;
global using IdentityServer4.EntityFramework.Interfaces;
global using IdentityServer4.EntityFramework.Options;
global using IdentityServer4.EntityFramework.Storage;
global using IdentityServer4.EntityFramework.Stores;
global using IdsEntities = IdentityServer4.EntityFramework.Entities;
#endif