﻿using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if DUENDE
namespace Duende.IdentityServer.Services
#elif IDS8
namespace IdentityServer8.Services
#else
namespace IdentityServer4.Services
#endif
{
    public class TestEventService : IEventService
    {
        private Dictionary<Type, object> _events = new Dictionary<Type, object>();

        public Task RaiseAsync(Event evt)
        {
            _events.Add(evt.GetType(), evt);
            return Task.CompletedTask;
        }

        public T AssertEventWasRaised<T>()
            where T : class
        {
            _events.ContainsKey(typeof(T)).Should().BeTrue();
            return (T)_events.Where(x => x.Key == typeof(T)).Select(x => x.Value).First();
        }

        public bool CanRaiseEventType(EventTypes evtType)
        {
            return true;
        }
    }
}
