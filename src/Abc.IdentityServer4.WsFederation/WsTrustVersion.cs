// ----------------------------------------------------------------------------
// <copyright file="WsTrustVersion.cs" company="ABC software Ltd">
//    Copyright © ABC SOFTWARE. All rights reserved.
//
//    Licensed under the Apache License, Version 2.0.
//    See LICENSE in the project root for license information.
// </copyright>
// ----------------------------------------------------------------------------

namespace Abc.IdentityServer4.WsFederation
{
    /// <summary>
    /// WS-Trust versions.
    /// </summary>
    public enum WsTrustVersion
    {
        /// <summary>
        /// The default version.
        /// </summary>
        Default,

        /// <summary>
        /// The February 2005 version of WS-Trust.
        /// </summary>
        WsTrust2005,

        /// <summary>
        /// The version 1.3 of WS-Trust.
        /// </summary>
        WsTrust13,
    }
}