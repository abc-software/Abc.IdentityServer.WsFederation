// ----------------------------------------------------------------------------
// <copyright file="LogSerializer.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Abc.IdentityServer.WsFederation.Logging
{
    /// <summary>
    /// Helper to JSON serialize object data for logging.
    /// </summary>
    internal static class LogSerializer
    {
        private static readonly JsonSerializerOptions Options = Initialze();

        /// <summary>
        /// Serializes the specified object.
        /// </summary>
        /// <param name="logObject">The object.</param>
        /// <returns>The JSON string representing of object.</returns>
        public static string Serialize(object logObject)
        {
            return JsonSerializer.Serialize(logObject, Options);
        }

        private static JsonSerializerOptions Initialze()
        {
            var options = new JsonSerializerOptions
            {
#if NET6_0_OR_GREATER
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
#else
                IgnoreNullValues = true,
#endif
                WriteIndented = true,
            };

            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }
    }
}