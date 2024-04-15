#if NET8_0_OR_GREATER

using System;

namespace Abc.IdentityServer;

internal class DefaultClock : IClock
{
    private readonly TimeProvider _timeProvider;

    public DefaultClock()
    {
        _timeProvider = TimeProvider.System;
    }

    public DefaultClock(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DateTimeOffset UtcNow { get => _timeProvider.GetUtcNow(); }
}

#endif