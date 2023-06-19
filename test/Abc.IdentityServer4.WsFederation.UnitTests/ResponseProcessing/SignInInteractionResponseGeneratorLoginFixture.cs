using Abc.IdentityServer.WsFederation.Validation;
using FluentAssertions;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer.WsFederation.ResponseProcessing.UnitTests
{
    public class SignInInteractionResponseGeneratorLoginFixture
    {
        private MockProfileService _profile;
        private SignInInteractionResponseGenerator _subject;
        private StubClock _clock = new StubClock();

        public SignInInteractionResponseGeneratorLoginFixture()
        {
            _profile = new MockProfileService();

            _subject = new SignInInteractionResponseGenerator(
               _profile,
               _clock,
               TestLogger.Create<SignInInteractionResponseGenerator>());
        }

        [Fact]
        public async Task Anonymous_User_must_SignIn()
        {
            var request = new ValidatedWsFederationRequest
            {
                ClientId = "foo",
                Subject = Principal.Anonymous
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task Authenticated_User_must_not_SignIn()
        {
            var request = new ValidatedWsFederationRequest
            {
                ClientId = "foo",
                Client = new Client(),
                ValidatedResources = new ResourceValidationResult(),
                Subject = new Ids.IdentityServerUser("123")
                {
                    IdentityProvider = Ids.IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessInteractionAsync(request);

            result.IsLogin.Should().BeFalse();
        }

        [Fact]
        public async Task Authenticated_User_with_allowed_current_Idp_must_not_SignIn()
        {
            var request = new ValidatedWsFederationRequest
            {
                ClientId = "foo",
                Subject = new Ids.IdentityServerUser("123")
                {
                    IdentityProvider = Ids.IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal(),
                Client = new Client
                {
                    IdentityProviderRestrictions = new List<string>
                    {
                        Ids.IdentityServerConstants.LocalIdentityProvider
                    }
                }
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeFalse();
        }

        [Fact]
        public async Task Authenticated_User_with_restricted_current_Idp_must_SignIn()
        {
            var request = new ValidatedWsFederationRequest
            {
                ClientId = "foo",
                Subject = new Ids.IdentityServerUser("123")
                {
                    IdentityProvider = "idp"
                }.CreatePrincipal(),
                Client = new Client
                {
                    IdentityProviderRestrictions = new List<string>
                    {
                        "some_idp"
                    }
                }
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }


        [Fact]
        public async Task Authenticated_User_with_allowed_requested_Idp_must_not_SignIn()
        {
            var request = new ValidatedWsFederationRequest
            {
                ClientId = "foo",
                Client = new Client(),
                HomeRealm = Ids.IdentityServerConstants.LocalIdentityProvider,
                Subject = new Ids.IdentityServerUser("123")
                {
                    IdentityProvider = Ids.IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeFalse();
        }

        [Fact]
        public async Task Authenticated_User_with_different_requested_Idp_must_SignIn()
        {
            var request = new ValidatedWsFederationRequest
            {
                ClientId = "foo",
                Client = new Client(),
                HomeRealm = "some_id",
                Subject = new Ids.IdentityServerUser("123")
                {
                    IdentityProvider = Ids.IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task Authenticated_User_within_client_user_sso_lifetime_should_not_signin()
        {
            var request = new ValidatedWsFederationRequest
            {
                ClientId = "foo",
                Client = new Client()
                {
                    UserSsoLifetime = 3600 // 1h
                },
                Subject = new Ids.IdentityServerUser("123")
                {
                    IdentityProvider = "local",
                    AuthenticationTime = _clock.UtcNow.UtcDateTime.Subtract(TimeSpan.FromSeconds(10))
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeFalse();
        }

        [Fact]
        public async Task Authenticated_User_beyond_client_user_sso_lifetime_should_signin()
        {
            var request = new ValidatedWsFederationRequest
            {
                ClientId = "foo",
                Client = new Client()
                {
                    UserSsoLifetime = 3600 // 1h
                },
                Subject = new Ids.IdentityServerUser("123")
                {
                    IdentityProvider = "local",
                    AuthenticationTime = _clock.UtcNow.UtcDateTime.Subtract(TimeSpan.FromSeconds(3700))
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task locally_authenticated_user_but_client_does_not_allow_local_should_sign_in()
        {
            var request = new ValidatedWsFederationRequest
            {
                ClientId = "foo",
                Client = new Client()
                {
                    EnableLocalLogin = false
                },
                Subject = new Ids.IdentityServerUser("123")
                {
                    IdentityProvider = Ids.IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task Authenticated_User_with_wfresh_zero_should_sign_in()
        {
            var request = new ValidatedWsFederationRequest
            {
                ClientId = "foo",
                Subject = new Ids.IdentityServerUser("123")
                {
                    IdentityProvider = Ids.IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal(),
                Freshness = 0,
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task Authenticated_User_beyoung_wfresh_time_should_sign_in()
        {
            var request = new ValidatedWsFederationRequest
            {
                ClientId = "foo",
                Client = new Client(),
                Subject = new Ids.IdentityServerUser("123")
                {
                    IdentityProvider = Ids.IdentityServerConstants.LocalIdentityProvider,
                    AuthenticationTime = _clock.UtcNow.UtcDateTime.Subtract(TimeSpan.FromSeconds(3700)),
                }.CreatePrincipal(),
                Freshness = 60, // 1 hour
            };

            var result = await _subject.ProcessLoginAsync(request);

            result.IsLogin.Should().BeTrue();
        }

        [Fact]
        public async Task Inactive_User_should_sign_in()
        {
            _profile.IsActive = false;

            var request = new ValidatedWsFederationRequest
            {
                ClientId = "foo",
                Client = new Client(),
                ValidatedResources = new ResourceValidationResult(),
                Subject = new Ids.IdentityServerUser("123")
                {
                    IdentityProvider = Ids.IdentityServerConstants.LocalIdentityProvider
                }.CreatePrincipal()
            };

            var result = await _subject.ProcessInteractionAsync(request);

            result.IsLogin.Should().BeTrue();
        }
    }
}
