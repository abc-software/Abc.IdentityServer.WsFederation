using FluentAssertions;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using Abc.IdentityServer4.WsFederation.Endpoints.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Abc.IdentityServer4.WsFederation.Endpoints.Results.UnitTests
{
    public class SignInResultFixture
    {
        private SignInResult _target;
        private IdentityServerOptions _options;
        private DefaultHttpContext _context;
        private WsFederationMessage _message;

        public SignInResultFixture()
        {
            _options = new IdentityServerOptions();

            _context = new DefaultHttpContext();
            _context.SetIdentityServerOrigin("https://server");
            _context.SetIdentityServerBasePath("/");
            _context.Response.Body = new MemoryStream();

            _message = new WsFederationMessage();
            _message.IssuerAddress = "http://client/callback";
            _message.Wres = "some_wresult";

            _target = new SignInResult(_message, _options);
        }

        [Fact]
        public void signin_ctor()
        {
            Action action = () =>
            {
                _target = new SignInResult(null, _options);
            };

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task form_post_mode_should_pass_results_in_body()
        {
            _target.Message.Should().NotBeNull();
            _target.Message.IssuerAddress.Should().Be("http://client/callback");

            await _target.ExecuteAsync(_context);

            _context.Response.StatusCode.Should().Be(200);
            _context.Response.ContentType.Should().StartWith("text/html");

            var cacheControl = _context.Response.Headers["Cache-Control"].First();
            cacheControl.Should().Contain("no-store");
            cacheControl.Should().Contain("max-age=0");

            var csp = _context.Response.Headers["Content-Security-Policy"].First();
            csp.Should().Contain("default-src 'none';");
            csp.Should().Contain("script-src 'sha256-veRHIN/XAFeehi7cRkeVBpkKTuAUMFxwA+NMPmu2Bec='");

            var xcsp = _context.Response.Headers["X-Content-Security-Policy"].First();
            xcsp.Should().Contain("default-src 'none';");
            xcsp.Should().Contain("script-src 'sha256-veRHIN/XAFeehi7cRkeVBpkKTuAUMFxwA+NMPmu2Bec='");

            _context.Response.Body.Seek(0, SeekOrigin.Begin);
            using (var rdr = new StreamReader(_context.Response.Body))
            {
                var html = rdr.ReadToEnd();
                html.Should().Contain(@"form method=""POST"" name=""hiddenform"" action=""http://client/callback"">");
                html.Should().Contain(@"<input type=""hidden"" name=""wres"" value=""some_wresult"" />");
            }
        }

        [Fact]
        public async Task form_post_mode_should_add_unsafe_inline_for_csp_level_1()
        {
            _options.Csp.Level = CspLevel.One;

            await _target.ExecuteAsync(_context);

            var csp = _context.Response.Headers["Content-Security-Policy"].First();
            csp.Should().Contain("script-src 'unsafe-inline' 'sha256-veRHIN/XAFeehi7cRkeVBpkKTuAUMFxwA+NMPmu2Bec='");

            var xcsp = _context.Response.Headers["X-Content-Security-Policy"].First();
            xcsp.Should().Contain("script-src 'unsafe-inline' 'sha256-veRHIN/XAFeehi7cRkeVBpkKTuAUMFxwA+NMPmu2Bec='");
        }

        [Fact]
        public async Task form_post_mode_should_not_add_deprecated_header_when_it_is_disabled()
        {
            _options.Csp.AddDeprecatedHeader = false;

            await _target.ExecuteAsync(_context);

            var csp = _context.Response.Headers["Content-Security-Policy"].FirstOrDefault();
            csp.Should().NotBeNull();

            var xcsp = _context.Response.Headers["X-Content-Security-Policy"].FirstOrDefault();
            xcsp.Should().BeNull();
        }
    }
}