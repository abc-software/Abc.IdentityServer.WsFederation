﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#if DUENDE
namespace Duende.IdentityServer.Stores
#elif IDS8
namespace IdentityServer8.Services
#else
namespace IdentityServer4.Stores
#endif
{
    public class MockMessageStore<TModel> : IMessageStore<TModel>
    {
        public Dictionary<string, Message<TModel>> Messages { get; set; } = new Dictionary<string, Message<TModel>>();

        public Task<Message<TModel>> ReadAsync(string id)
        {
            Message<TModel> val = null;
            if (id != null)
            {
                Messages.TryGetValue(id, out val);
            }

            return Task.FromResult(val);
        }

        public Task<string> WriteAsync(Message<TModel> message)
        {
            var id = Guid.NewGuid().ToString();
            Messages[id] = message;
            return Task.FromResult(id);
        }
    }

    public class AuthorizationParametersMessageStoreMock : MockMessageStore<Dictionary<string, string[]>>, IAuthorizationParametersMessageStore
    {
        public Task DeleteAsync(string id)
        {
            if (this.Messages.ContainsKey(id))
            {
                this.Messages.Remove(id);
            }

            return Task.CompletedTask;
        }

        Task<string> IAuthorizationParametersMessageStore.WriteAsync(Message<IDictionary<string, string[]>> message)
        {
            return base.WriteAsync(new Message<Dictionary<string, string[]>>(new Dictionary<string, string[]>(message.Data), DateTime.UtcNow));
        }

        async Task<Message<IDictionary<string, string[]>>> IAuthorizationParametersMessageStore.ReadAsync(string id)
        {
            var message = await base.ReadAsync(id);
            return new Message<IDictionary<string, string[]>>(message?.Data, DateTime.UtcNow);
        }
    }
}