// ----------------------------------------------------------------------------
// <copyright file="ValidatedWsFederationRequestLog.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer.Extensions;
using Abc.IdentityServer.WsFederation.Validation;
using IdentityModel;
using System.Collections.Generic;
using System.Linq;

namespace Abc.IdentityServer.WsFederation.Logging
{
    internal class ValidatedWsFederationRequestLog
    {
        public ValidatedWsFederationRequestLog(ValidatedWsFederationRequest request, IEnumerable<string> sensitiveValuesFilter)
        {
            Raw = request.WsFederationMessage.ToScrubbedDictionary(sensitiveValuesFilter.ToArray());
            ClientId = request.ClientId;
            ReplyUrl = request.ReplyUrl;
            Freshness = request.Freshness;
            HomeRealm = request.HomeRealm;

            if (request.Client != null)
            {
                ClientName = request.Client.ClientName;
                AllowedRedirectUris = request.Client.RedirectUris;
                AllowedPostLogoutRedirectUris = request.Client.PostLogoutRedirectUris;
            }

            if (request.Subject != null)
            {
                var subjectClaim = request.Subject.FindFirst(JwtClaimTypes.Subject);
                SubjectId = subjectClaim != null ? subjectClaim.Value : "anonymous";
            }
        }

#pragma warning disable SA1516 // Elements should be separated by blank line
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string ReplyUrl { get; set; }
        public IEnumerable<string> AllowedRedirectUris { get; set; }
        public IEnumerable<string> AllowedPostLogoutRedirectUris { get; set; }
        public string SubjectId { get; set; }
        public Dictionary<string, string> Raw { get; set; }
        public int? Freshness { get; set; }
        public string HomeRealm { get; set; }
#pragma warning restore SA1516 // Elements should be separated by blank line

        public override string ToString()
        {
            return LogSerializer.Serialize(this);
        }
    }
}