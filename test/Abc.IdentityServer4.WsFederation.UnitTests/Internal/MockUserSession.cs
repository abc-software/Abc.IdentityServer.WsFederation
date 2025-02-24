using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

#if DUENDE
namespace Duende.IdentityServer.Services
#elif IDS8
namespace IdentityServer8.Services
#else
namespace IdentityServer4.Services
#endif
{
    public class MockUserSession : IUserSession
    {
        public List<string> Clients = new List<string>();

        public bool EnsureSessionIdCookieWasCalled { get; set; }
        public bool RemoveSessionIdCookieWasCalled { get; set; }
        public bool CreateSessionIdWasCalled { get; set; }

        public ClaimsPrincipal User { get; set; }
        public string SessionId { get; set; }
        public AuthenticationProperties Properties { get; set; }


        public Task<string> CreateSessionIdAsync(ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            CreateSessionIdWasCalled = true;
            User = principal;
            SessionId = Guid.NewGuid().ToString();
            return Task.FromResult(SessionId);
        }

        public Task<ClaimsPrincipal> GetUserAsync()
        {
            return Task.FromResult(User);
        }

        Task<string> IUserSession.GetSessionIdAsync()
        {
            return Task.FromResult(SessionId);
        }

        public Task EnsureSessionIdCookieAsync()
        {
            EnsureSessionIdCookieWasCalled = true;
            return Task.CompletedTask;
        }

        public Task RemoveSessionIdCookieAsync()
        {
            RemoveSessionIdCookieWasCalled = true;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<string>> GetClientListAsync()
        {
            return Task.FromResult<IEnumerable<string>>(Clients);
        }

        public Task AddClientIdAsync(string clientId)
        {
            Clients.Add(clientId);
            return Task.CompletedTask;
        }
    }
}