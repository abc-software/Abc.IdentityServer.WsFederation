// ----------------------------------------------------------------------------
// <copyright file="WsFederationMessageExtensions.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Abc.IdentityServer4.Extensions
{
    internal static class WsFederationMessageExtensions
    {
        public static bool IsSignOutCleanupMessage(this WsFederationMessage message) => message.Wa == WsFederationConstants.WsFederationActions.SignOutCleanup;

        public static IDictionary<string, string[]> ToDictionary(this WsFederationMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var dictionary = new Dictionary<string, string[]>();
            foreach (var p in message.Parameters)
            {
                dictionary.Add(p.Key, new string[] { p.Value });
            }

            return dictionary;
        }

        public static Dictionary<string, string> ToScrubbedDictionary(this WsFederationMessage message, params string[] nameFilter)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var dictionary = new Dictionary<string, string>();
            foreach (var p in message.Parameters)
            {
                var key = p.Key;
                var value = p.Value;
                if (nameFilter.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    value = "***REDACTED***";
                }

                dictionary.Add(key, value);
            }

            return dictionary;
        }

        public static WsFederationMessage ToWsFederationMessage(this IDictionary<string, string[]> data)
        {
            return new WsFederationMessage(data);
        }

        public static NameValueCollection ToNameValueCollection(this WsFederationMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var nvc = new NameValueCollection();
            foreach (var p in message.Parameters)
            {
                nvc.Add(p.Key, p.Value);
            }

            return nvc;
        }

        public static WsFederationMessage ToWsFederationMessage(this NameValueCollection nameValueCollection)
        {
            var message = new WsFederationMessage();
            message.SetParameters(nameValueCollection);
            return message;
        }

        public static WsFederationMessage ToWsFederationMessage(this IEnumerable<KeyValuePair<string, StringValues>> data)
        {
            var message = new WsFederationMessage();
            foreach (var item in data)
            {
                message.SetParameter(item.Key, item.Value.ToString());
            }

            return message;
        }

        public static string ToQueryString(this WsFederationMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var strBuilder = new StringBuilder();
            bool first = true;
            foreach (var parameter in message.Parameters)
            {
                if (parameter.Value == null)
                {
                    continue;
                }

                if (first)
                {
                    first = false;
                }
                else
                {
                    strBuilder.Append('&');
                }

                strBuilder.Append(Uri.EscapeDataString(parameter.Key));
                strBuilder.Append('=');
                strBuilder.Append(Uri.EscapeDataString(parameter.Value));
            }

            return strBuilder.ToString();
        }

        public static IEnumerable<string> GetAcrValues(this WsFederationMessage message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var wauth = message.Wauth;
            if (wauth.IsPresent())
            {
                return new string[] { wauth };
            }

            return null;
        }
    }
}