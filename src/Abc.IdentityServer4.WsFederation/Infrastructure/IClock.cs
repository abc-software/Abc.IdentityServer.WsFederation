using System;

namespace Abc.IdentityServer;

/// <summary>
/// Abstraction for the date/time.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC date/time.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}