// ----------------------------------------------------------------------------
// <copyright file="RequestSecurityTokenResponse.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Xml;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace Abc.IdentityServer4.WsFederation.ResponseProcessing
{
    internal class RequestSecurityTokenResponse
    {
#pragma warning disable SA1516 // Elements should be separated by blank line
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string AppliesTo { get; set; }
        public string Context { get; set; }
        public string TokenType { get; set; }
        public SecurityToken RequestedSecurityToken { get; set; }
#pragma warning restore SA1516 // Elements should be separated by blank line

        public string Serialize(SecurityTokenHandler securityTokenHandler, WsTrustVersion trustVersion)
        {
            if (securityTokenHandler is null)
            {
                throw new ArgumentNullException(nameof(securityTokenHandler));
            }

            using (var ms = new MemoryStream())
            {
                bool flag;
                string prefix;
                string ns;
                if (trustVersion == WsTrustVersion.WsTrust2005)
                {
                    flag = false;
                    prefix = WsTrustConstants_2005.PreferredPrefix;
                    ns = WsTrustConstants.Namespaces.WsTrust2005;
                }
                else
                {
                    flag = true;
                    prefix = WsTrustConstants_1_3.PreferredPrefix;
                    ns = WsTrustConstants.Namespaces.WsTrust1_3;
                }

                using (var writer = XmlDictionaryWriter.CreateTextWriter(ms, Encoding.UTF8, false))
                {
                    // <t:RequestSecurityTokenResponseCollection>
                    if (flag)
                    {
                        writer.WriteStartElement(prefix, WsFederationConstants.Elements.RequestSecurityTokenResponseCollection, ns);
                    }

                    // <t:RequestSecurityTokenResponse>
                    writer.WriteStartElement(prefix, WsTrustConstants.Elements.RequestSecurityTokenResponse, ns);
                    
                    // @Context
                    writer.WriteAttributeString(WsFederationConstants.Attributes.Context, Context);

                    // <t:Lifetime>
                    writer.WriteStartElement(prefix, WsTrustConstants.Elements.Lifetime, ns);

                    // <wsu:Created></wsu:Created>
                    writer.WriteElementString(WsUtility.PreferredPrefix, WsUtility.Elements.Created, WsFederationConstants.WsUtility.Namespace, CreatedAt.ToString(SamlConstants.GeneratedDateTimeFormat, DateTimeFormatInfo.InvariantInfo));
                    
                    // <wsu:Expires></wsu:Expires>
                    writer.WriteElementString(WsUtility.PreferredPrefix, WsUtility.Elements.Expires, WsFederationConstants.WsUtility.Namespace, ExpiresAt.ToString(SamlConstants.GeneratedDateTimeFormat, DateTimeFormatInfo.InvariantInfo));

                    // </t:Lifetime>
                    writer.WriteEndElement();

                    // <wsp:AppliesTo>
                    writer.WriteStartElement(WsPolicy.PreferredPrefix, WsPolicy.Elements.AppliesTo, WsPolicy.Namespace);

                    // <wsa:EndpointReference>
                    writer.WriteStartElement(WsAddressing.PreferredPrefix, WsAddressing.Elements.EndpointReference, WsAddressing.Namespace);

                    // <wsa:Address></wsa:Address>
                    writer.WriteElementString(WsAddressing.PreferredPrefix, WsAddressing.Elements.Address, WsAddressing.Namespace, AppliesTo);

                    // </wsa:EndpointReference>
                    writer.WriteEndElement();

                    // </wsp:AppliesTo>
                    writer.WriteEndElement();

                    // <t:RequestedSecurityToken>
                    writer.WriteStartElement(prefix, WsTrustConstants.Elements.RequestedSecurityToken, ns);

                    // write assertion
                    securityTokenHandler.WriteToken(writer, RequestedSecurityToken);

                    // </t:RequestedSecurityToken>
                    writer.WriteEndElement();

                    // <t:TokenType>
                    if (!string.IsNullOrEmpty(TokenType))
                    {
                        writer.WriteElementString(prefix, WsTrustConstants.Elements.TokenType, ns, TokenType);
                    }

                    // </t:RequestSecurityTokenResponse>
                    writer.WriteEndElement();

                    // <t:RequestSecurityTokenResponseCollection>
                    if (flag)
                    {
                        writer.WriteEndElement();
                    }
                }

                var result = Encoding.UTF8.GetString(ms.ToArray());
                return result;
            }
        }
    }
}