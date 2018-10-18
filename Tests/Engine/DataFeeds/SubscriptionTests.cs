/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Linq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class SubscriptionTests
    {
        private readonly DateTime _start = DateTime.MinValue;
        private readonly DateTime _end = DateTime.MinValue.AddDays(1);

        [Test]
        public void ConstructorNoUniverse()
        {
            var subscriptionRequest = GetSubscriptionRequest(false);
            var subscription = new Subscription(
                subscriptionRequest,
                null,
                new TimeZoneOffsetProvider(DateTimeZone.Utc, _start, _end));

            Assert.IsTrue(subscription.HasSubscriptionRequests);
            Assert.IsFalse(subscription.Universe.Any());
            Assert.IsFalse(subscription.EndOfStream);
            Assert.AreEqual(_start, subscription.UtcStartTime);
            Assert.AreEqual(_end, subscription.UtcEndTime);
            Assert.AreEqual(subscription.Configuration, subscriptionRequest.Configuration);
            Assert.IsFalse(subscription.IsUniverseSelectionSubscription);
        }

        [Test]
        public void Constructor()
        {
            var subscriptionRequest = GetSubscriptionRequest();
            var subscription = new Subscription(
                subscriptionRequest,
                null,
                new TimeZoneOffsetProvider(DateTimeZone.Utc, _start, _end));

            Assert.IsTrue(subscription.HasSubscriptionRequests);
            Assert.AreEqual(1, subscription.Universe.Count());
            Assert.AreEqual(_start, subscription.UtcStartTime);
            Assert.AreEqual(_end, subscription.UtcEndTime);
            Assert.AreEqual(subscriptionRequest.Universe, subscription.Universe.First());
            Assert.IsFalse(subscription.EndOfStream);
            Assert.AreEqual(subscription.Configuration, subscriptionRequest.Configuration);
            Assert.IsFalse(subscription.IsUniverseSelectionSubscription);
        }

        [Test]
        public void AddSubscriptionRequestOncePerUniverse()
        {
            var subscriptionRequest = GetSubscriptionRequest();
            var subscription = new Subscription(
                subscriptionRequest,
                null,
                new TimeZoneOffsetProvider(DateTimeZone.Utc, _start, _end));

            Assert.AreEqual(1, subscription.Universe.Count());
            Assert.AreEqual(subscriptionRequest.Universe, subscription.Universe.First());

            subscription.AddSubscriptionRequest(subscriptionRequest);

            Assert.AreEqual(1, subscription.Universe.Count());
            Assert.AreEqual(subscriptionRequest.Universe, subscription.Universe.First());
        }

        [Test]
        public void SubscriptionOnlyAcceptsOneUniverseSelection()
        {
            var subscriptionRequest = GetSubscriptionRequest(true, true);
            var subscriptionRequest2 = GetSubscriptionRequest();
            var subscription = new Subscription(
                subscriptionRequest,
                null,
                new TimeZoneOffsetProvider(DateTimeZone.Utc, _start, _end));

            try
            {
                subscription.AddSubscriptionRequest(subscriptionRequest2);
                Assert.Fail("Subscription should only accept one universe selection" +
                    " subscription request.");
            }
            catch (Exception)
            {
                Assert.Pass();
            }
        }

        [Test]
        public void SubscriptionOnlyAcceptsSameConfiguration()
        {
            var subscriptionRequest = GetSubscriptionRequest();
            var subscriptionRequest2 = GetSubscriptionRequest(resolution: Resolution.Second);
            var subscription = new Subscription(
                subscriptionRequest,
                null,
                new TimeZoneOffsetProvider(DateTimeZone.Utc, _start, _end));

            try
            {
                subscription.AddSubscriptionRequest(subscriptionRequest2);
                Assert.Fail("Subscription should only accept the same configuration");
            }
            catch (Exception)
            {
                Assert.Pass();
            }
        }

        [Test]
        public void RemoveSubscriptionRequest()
        {
            var subscriptionRequest = GetSubscriptionRequest();
            var subscription = new Subscription(
                subscriptionRequest,
                null,
                new TimeZoneOffsetProvider(DateTimeZone.Utc, _start, _end));

            var removedUniverse = subscription.RemoveSubscriptionRequest(subscriptionRequest.Universe);
            Assert.IsTrue(removedUniverse.Contains(subscriptionRequest.Universe));

            Assert.IsFalse(subscription.Universe.Any());
            Assert.IsFalse(subscription.HasSubscriptionRequests);
        }

        [Test]
        public void RemoveAllSubscriptionRequest()
        {
            var subscriptionRequest = GetSubscriptionRequest();
            var subscriptionRequest2 = GetSubscriptionRequest();
            var subscription = new Subscription(
                subscriptionRequest,
                null,
                new TimeZoneOffsetProvider(DateTimeZone.Utc, _start, _end));

            subscription.AddSubscriptionRequest(subscriptionRequest2);
            Assert.AreEqual(2, subscription.Universe.Count());
            var removedUniverses = subscription.RemoveSubscriptionRequest().ToList();

            Assert.IsTrue(removedUniverses.Contains(subscriptionRequest.Universe));
            Assert.IsTrue(removedUniverses.Contains(subscriptionRequest2.Universe));

            Assert.IsFalse(subscription.Universe.Any());
            Assert.IsFalse(subscription.HasSubscriptionRequests);
        }

        [Test]
        public void RemoveAllSubscriptionRequestNoUniverse()
        {
            var subscriptionRequest = GetSubscriptionRequest(false);
            var subscription = new Subscription(
                subscriptionRequest,
                null,
                new TimeZoneOffsetProvider(DateTimeZone.Utc, _start, _end));

            var removedUniverses = subscription.RemoveSubscriptionRequest().ToList();

            Assert.IsFalse(removedUniverses.Any());
            Assert.IsFalse(subscription.Universe.Any());
            Assert.IsFalse(subscription.HasSubscriptionRequests);
        }

        private SubscriptionRequest GetSubscriptionRequest(
            bool useUniverse = true,
            bool isUniverseSelection = false,
            Resolution resolution = Resolution.Minute)
        {
            var security = SecurityTests.GetSecurity();
            var config = SecurityTests.CreateTradeBarConfig(resolution);
            var universe = new ManualUniverse(
                config,
                new UniverseSettings(Resolution.Daily, 1, true, true, TimeSpan.FromDays(1)),
                new[] {security.Symbol}
            );
            return new SubscriptionRequest(isUniverseSelection, useUniverse ? universe : null, security, config, _start, _end);
        }
    }
}
