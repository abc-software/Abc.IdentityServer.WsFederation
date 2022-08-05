﻿using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Abc.IdentityServer4.WsFederation.Stores.UnitTests
{
    public class InMemoryRelyingPartyStoreFixture
    {
        private InMemoryRelyingPartyStore _target;

        [Fact]
        public void InMemoryRelyingPartyStore_ctor()
        {
            Action action = () =>
            {
                _target = new InMemoryRelyingPartyStore(null);
            };

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void InMemoryRelyingPartyStore_should_throw_if_contain_duplicate_relying_parties()
        {
            var relyingParties = new RelyingParty[] {
                    new RelyingParty() { Realm = "urn:foo" },
                    new RelyingParty() { Realm = "urn:foo" },
                    new RelyingParty() { Realm = "urn:baz" },
                };

            Action act = () => _target = new InMemoryRelyingPartyStore(relyingParties);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void InMemoryRelyingPartyStore_should_not_throw_if_does_not_contain_duplicate_relying_parties()
        {
            var relyingParties = new RelyingParty[] {
                    new RelyingParty() { Realm = "urn:foo" },
                    new RelyingParty() { Realm = "urn:far" },
                    new RelyingParty() { Realm = "urn:baz" },
                };

            Action act = () => _target = new InMemoryRelyingPartyStore(relyingParties);
            act.Should().NotThrow();
        }

        [Fact()]
        public async Task InMemoryRelyingPartyStore_should_filter()
        {
            var relyingParties = new RelyingParty[] {
                    new RelyingParty() { Realm = "urn:foo" },
                    new RelyingParty() { Realm = "urn:far" },
                    new RelyingParty() { Realm = "urn:baz" },
                };
            _target = new InMemoryRelyingPartyStore(relyingParties);

            {
                var relyingParty = await _target.FindRelyingPartyByRealmAsync("foo");
                relyingParty.Should().BeNull();
            }

            {
                var relyingParty = await _target.FindRelyingPartyByRealmAsync("urn:foo");
                relyingParty.Should().NotBeNull();
                relyingParty.Realm.Should().Be("urn:foo");
            }
        }
    }
}