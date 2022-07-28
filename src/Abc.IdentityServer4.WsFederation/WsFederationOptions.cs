using IdentityModel;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Claims;

namespace Abc.IdentityServer4.WsFederation
{
    /// <summary>
    /// The default options for the relying party's behavior.
    /// </summary>
    public class WsFederationOptions
    {
        /// <summary>
        /// Gets or sets the default token type. Defaults to SAML2.0.
        /// </summary>
        /// <value>
        /// The default token type.
        /// </value>
        public string DefaultTokenType { get; set; } = WsFederationConstants.TokenTypes.Saml2TokenProfile11;

        /// <summary>
        /// Gets or sets the default digest algorithm. Defaults to SHA256.
        /// </summary>
        /// <value>
        /// The default digest algorithm.
        /// </value>
        public string DefaultDigestAlgorithm { get; set; } = SecurityAlgorithms.Sha256Digest;

        /// <summary>
        /// Gets or sets the default signature algorithm. Defaults to SHA256.
        /// </summary>
        /// <value>
        /// The default signature algorithm.
        /// </value>        
        public string DefaultSignatureAlgorithm { get; set; } = SecurityAlgorithms.RsaSha256Signature;

        /// <summary>
        /// Gets or sets the name identifier format. Defaults to Unspecified.
        /// </summary>
        /// <value>
        /// The name identifier format.
        /// </value>
        public string DefaultNameIdentifierFormat { get; set; } = WsFederationConstants.SamlNameIdentifierFormats.UnspecifiedString;

        /// <summary>
        /// Gets or sets the default encryption algorithm. Defaults to AES256-CBC.
        /// </summary>
        /// <value>
        /// The default encryption algorithm.
        /// </value>
        public string DefaultEncryptionAlgorithm { get; set; } = SecurityAlgorithms.Aes256Encryption;

        /// <summary>
        /// Gets or sets the default key wrap algorithm. Defaults to RSA-OAEP.
        /// </summary>
        /// <value>
        /// The default key wrap algorithm.
        /// </value>
        public string DefaultKeyWrapAlgorithm { get; set; } = SecurityAlgorithms.RsaOaepKeyWrap;

        /// <summary>
        /// Gets or sets the default WS-Trust version. Defaults to WS-Trust v1.3.
        /// </summary>
        /// <value>
        /// The default WS-Trust version.
        /// </value>
        public WsTrustVersion DefaultWsTrustVersion { get; set; } = WsTrustVersion.WsTrust13;

        /// <summary>
        /// Gets or sets the security token handlers used to write <see cref="SecurityToken"/>.
        /// </summary>
        /// <value>
        /// The security token handlers.
        /// </value>
        public ICollection<SecurityTokenHandler> SecurityTokenHandlers { get; set; } = new Collection<SecurityTokenHandler>()
        {
            new Microsoft.IdentityModel.Tokens.Saml2.Saml2SecurityTokenHandler(),
            new Microsoft.IdentityModel.Tokens.Saml.SamlSecurityTokenHandler(),
            new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
        };

        /// <summary>
        /// Gets or sets the default claim mapping.
        /// </summary>
        /// <value>
        /// The default claim mapping.
        /// </value>
        public IDictionary<string, string> DefaultClaimMapping { get; set; } = new Dictionary<string, string>
        {
            { JwtClaimTypes.Name, ClaimTypes.Name },
            { JwtClaimTypes.Subject, ClaimTypes.NameIdentifier },
            { JwtClaimTypes.Email, ClaimTypes.Email },
            { JwtClaimTypes.GivenName, ClaimTypes.GivenName },
            { JwtClaimTypes.FamilyName, ClaimTypes.Surname },
            { JwtClaimTypes.BirthDate, ClaimTypes.DateOfBirth },
            { JwtClaimTypes.WebSite, ClaimTypes.Webpage },
            { JwtClaimTypes.Gender, ClaimTypes.Gender },
            { JwtClaimTypes.Role, ClaimTypes.Role }
        };
    }
}