﻿// ----------------------------------------------------------------------------
// <copyright file="SignInTokenIssuedSuccessEvent.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Abc.IdentityServer.Extensions;
using Abc.IdentityServer.WsFederation.Validation;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Collections.Generic;

namespace Abc.IdentityServer.WsFederation.Events
{
    public class SignInTokenIssuedSuccessEvent : TokenIssuedSuccessEvent
    {
        public SignInTokenIssuedSuccessEvent(WsFederationMessage responseMessage, ValidatedWsFederationRequest request)
            : base()
        {
            if (request != null)
            {
                ClientId = request.ClientId;
                ClientName = request.Client?.ClientName;
                SubjectId = request.Subject?.GetSubjectId();
                Scopes = request.ValidatedResources?.RawScopeValues.ToSpaceSeparatedString();
            }

            Endpoint = WsFederationConstants.EndpointNames.WsFederation;

            var tokens = new List<Token>();
            if (responseMessage != null)
            {
                tokens.Add(new Token("SecurityToken", responseMessage.Wresult));
            }

            Tokens = tokens;
        }
    }
}