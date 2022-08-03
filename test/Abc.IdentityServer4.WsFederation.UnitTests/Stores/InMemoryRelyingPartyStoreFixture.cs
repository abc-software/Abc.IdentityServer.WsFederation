using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer4.WsFederation.Stores.UnitTests
{
    public class InMemoryRelyingPartyStoreFixture
    {
        private InMemoryRelyingPartyStore _target;

        [Fact]
        public void InMemoryRelyingParty_ctor()
        {
            Action action = () =>
            {
                _target = new InMemoryRelyingPartyStore(null);
            };

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact()]
        public async Task FindRelyingPartyByRealmTest()
        {
            _target = new InMemoryRelyingPartyStore(
                new RelyingParty[] { 
                    new RelyingParty() { Realm = "urn:foo" },
                });

            {
                var relyingParty = await _target.FindRelyingPartyByRealm("foo");
                relyingParty.Should().BeNull();
            }

            {
                var relyingParty = await _target.FindRelyingPartyByRealm("urn:foo");
                relyingParty.Should().NotBeNull();
            }
        }
    }
}