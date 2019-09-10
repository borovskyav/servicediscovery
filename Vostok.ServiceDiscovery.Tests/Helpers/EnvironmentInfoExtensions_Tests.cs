﻿using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Vostok.ServiceDiscovery.Helpers;
using Vostok.ServiceDiscovery.Models;

namespace Vostok.ServiceDiscovery.Tests.Helpers
{
    [TestFixture]
    internal class EnvironmentInfoExtensions_Tests
    {
        private Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
        }

        [Test]
        public void SkipIfEmpty_should_be_false()
        {
            var someName = fixture.Create<string>();
            ((EnvironmentInfo)null).SkipIfEmpty().Should().BeFalse();
            new EnvironmentInfo(someName, null, null).SkipIfEmpty().Should().BeFalse();
            new EnvironmentInfo(someName, null, new Dictionary<string, string> {{"key", "value"}}).SkipIfEmpty().Should().BeFalse();
            new EnvironmentInfo(someName, null, new Dictionary<string, string> {{EnvironmentInfoKeys.SkipIfEmpty, null}}).SkipIfEmpty().Should().BeFalse();
            new EnvironmentInfo(someName, null, new Dictionary<string, string> {{EnvironmentInfoKeys.SkipIfEmpty, ""}}).SkipIfEmpty().Should().BeFalse();
            new EnvironmentInfo(someName, null, new Dictionary<string, string> {{EnvironmentInfoKeys.SkipIfEmpty, "false"}}).SkipIfEmpty().Should().BeFalse();
            new EnvironmentInfo(someName, null, new Dictionary<string, string> {{EnvironmentInfoKeys.SkipIfEmpty, "False"}}).SkipIfEmpty().Should().BeFalse();
            new EnvironmentInfo(someName, null, new Dictionary<string, string> {{EnvironmentInfoKeys.SkipIfEmpty, "xxx"}}).SkipIfEmpty().Should().BeFalse();
        }

        [Test]
        public void SkipIfEmpty_should_be_true()
        {
            var someName = fixture.Create<string>();
            new EnvironmentInfo(someName, null, new Dictionary<string, string> {{EnvironmentInfoKeys.SkipIfEmpty, "true"}}).SkipIfEmpty().Should().BeTrue();
            new EnvironmentInfo(someName, null, new Dictionary<string, string> {{EnvironmentInfoKeys.SkipIfEmpty, "True"}}).SkipIfEmpty().Should().BeTrue();
        }
    }
}