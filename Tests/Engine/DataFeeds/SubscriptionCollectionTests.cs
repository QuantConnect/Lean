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
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class SubscriptionCollectionTests
    {
        [Test]
        public void EnumerationWhileUpdatingDoesNotThrow()
        {
            using var cts = new CancellationTokenSource();
            var subscriptions = new SubscriptionCollection();
            var start = DateTime.UtcNow;
            var end = start.AddSeconds(10);
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, DateTimeZone.Utc, DateTimeZone.Utc, true, false, false);
            var security = new Equity(
                Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(DateTimeZone.Utc, start, end);
            using var enumerator = new EnqueueableEnumerator<BaseData>();
            using var subscriptionDataEnumerator = new SubscriptionDataEnumerator(config, security.Exchange.Hours, timeZoneOffsetProvider, enumerator, false, false);
            var subscriptionRequest = new SubscriptionRequest(false, null, security, config, start, end);
            var subscription = new Subscription(subscriptionRequest, subscriptionDataEnumerator, timeZoneOffsetProvider);

            var addTask = Task.Factory.StartNew(() =>
            {
                Log.Trace("Add task started");

                while (DateTime.UtcNow < end)
                {
                    if (!subscriptions.Contains(config))
                    {
                        subscriptions.TryAdd(subscription);
                    }

                    Thread.Sleep(1);
                }

                Log.Trace("Add task ended");
            }, cts.Token);

            var removeTask = Task.Factory.StartNew(() =>
            {
                Log.Trace("Remove task started");

                while (DateTime.UtcNow < end)
                {
                    subscriptions.TryRemove(config, out _);

                    Thread.Sleep(1);
                }

                Log.Trace("Remove task ended");
            }, cts.Token);

            var readTask = Task.Factory.StartNew(() =>
            {
                Log.Trace("Read task started");

                while (DateTime.UtcNow < end)
                {
                    foreach (var _ in subscriptions) { }

                    Thread.Sleep(1);
                }

                Log.Trace("Read task ended");
            }, cts.Token);

            Task.WaitAll(addTask, removeTask, readTask);
            subscription.Dispose();
        }
        [Test]
        public void DefaultFillForwardResolution()
        {
            var subscriptionCollection = new SubscriptionCollection();
            var defaultFillForwardResolution = subscriptionCollection.UpdateAndGetFillForwardResolution();
            Assert.AreEqual(defaultFillForwardResolution.Value, new TimeSpan(0, 1, 0));
        }

        [Test]
        public void UpdatesFillForwardResolutionOverridesDefaultWhenNotAdding()
        {
            var subscriptionCollection = new SubscriptionCollection();
            var subscription = CreateSubscription(Resolution.Daily);

            var fillForwardResolution = subscriptionCollection.UpdateAndGetFillForwardResolution(subscription.Configuration);
            Assert.AreEqual(fillForwardResolution.Value, new TimeSpan(1, 0, 0, 0));
            subscription.Dispose();
        }

        [Test]
        public void UpdatesFillForwardResolutionSuccessfullyWhenNotAdding()
        {
            var subscriptionCollection = new SubscriptionCollection();
            var subscription = CreateSubscription(Resolution.Second);

            var fillForwardResolution = subscriptionCollection.UpdateAndGetFillForwardResolution(subscription.Configuration);
            Assert.AreEqual(fillForwardResolution.Value, new TimeSpan(0, 0, 1));
            subscription.Dispose();
        }

        [Test]
        public void UpdatesFillForwardResolutionSuccessfullyWhenAdding()
        {
            var subscriptionCollection = new SubscriptionCollection();
            using var subscription = CreateSubscription(Resolution.Second);

            subscriptionCollection.TryAdd(subscription);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 0, 1));
        }

        [Test]
        public void UpdatesFillForwardResolutionSuccessfullyOverridesDefaultWhenAdding()
        {
            var subscriptionCollection = new SubscriptionCollection();
            var subscription = CreateSubscription(Resolution.Daily);

            subscriptionCollection.TryAdd(subscription);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(1, 0, 0, 0));
            subscription.Dispose();
        }

        [Test]
        public void DoesNotUpdateFillForwardResolutionWhenAddingBiggerResolution()
        {
            var subscriptionCollection = new SubscriptionCollection();
            var subscription = CreateSubscription(Resolution.Second);
            var subscription2 = CreateSubscription(Resolution.Minute);

            subscriptionCollection.TryAdd(subscription);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 0, 1));
            subscriptionCollection.TryAdd(subscription2);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 0, 1));
            subscription.Dispose();
            subscription2.Dispose();
        }

        [Test]
        public void UpdatesFillForwardResolutionWhenRemoving()
        {
            var subscriptionCollection = new SubscriptionCollection();
            using var subscription = CreateSubscription(Resolution.Second);
            using var subscription2 = CreateSubscription(Resolution.Daily);

            subscriptionCollection.TryAdd(subscription);
            subscriptionCollection.TryAdd(subscription2);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 0, 1));
            subscriptionCollection.TryRemove(subscription.Configuration, out var outSubscription);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(1, 0, 0, 0));
            subscriptionCollection.TryRemove(subscription2.Configuration, out var outSubscription2);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 1, 0));
            outSubscription.Dispose();
            outSubscription2.Dispose();
        }

        [Test]
        public void FillForwardResolutionIgnoresTick()
        {
            var subscriptionCollection = new SubscriptionCollection();
            using var subscription = CreateSubscription(Resolution.Tick);

            subscriptionCollection.TryAdd(subscription);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 1, 0));
            subscriptionCollection.TryRemove(subscription.Configuration, out var outSubscription);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 1, 0));
            outSubscription.Dispose();
        }

        [Test]
        public void FillForwardResolutionIgnoresInternalFeed()
        {
            var subscriptionCollection = new SubscriptionCollection();
            using var subscription = CreateSubscription(Resolution.Second, "AAPL", true);

            subscriptionCollection.TryAdd(subscription);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 1, 0));
            subscriptionCollection.TryRemove(subscription.Configuration, out var outSubscription);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 1, 0));
            outSubscription.Dispose();
        }

        [Test]
        public void DoesNotUpdateFillForwardResolutionWhenRemovingDuplicateResolution()
        {
            var subscriptionCollection = new SubscriptionCollection();
            using var subscription = CreateSubscription(Resolution.Second);
            using var subscription2 = CreateSubscription(Resolution.Second, "SPY");

            subscriptionCollection.TryAdd(subscription);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 0, 1));
            subscriptionCollection.TryAdd(subscription2);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 0, 1));
            subscriptionCollection.TryRemove(subscription.Configuration, out var outSubscription);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 0, 1));
            subscriptionCollection.TryRemove(subscription2.Configuration, out var outSubscription2);
            Assert.AreEqual(subscriptionCollection.UpdateAndGetFillForwardResolution().Value, new TimeSpan(0, 1, 0));
            outSubscription.Dispose();
            outSubscription2.Dispose();
        }

        [Test]
        public void SubscriptionsAreSortedWhenAdding()
        {
            var subscriptionCollection = new SubscriptionCollection();
            var subscription = CreateSubscription(Resolution.Second, Futures.Metals.Gold, false, SecurityType.Future);
            var subscription2 = CreateSubscription(Resolution.Second, "SPY");
            var subscription3 = CreateSubscription(Resolution.Second, "AAPL", false, SecurityType.Option);
            var subscription4 = CreateSubscription(Resolution.Second, "EURGBP");
            var subscription5 = CreateSubscription(Resolution.Second, "AAPL", false, SecurityType.Option, TickType.OpenInterest);
            var subscription6 = CreateSubscription(Resolution.Second, "AAPL", false, SecurityType.Option, TickType.Quote);

            subscriptionCollection.TryAdd(subscription);
            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription });
            subscriptionCollection.TryAdd(subscription2);
            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription2, subscription });
            subscriptionCollection.TryAdd(subscription3);
            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription2, subscription3, subscription });
            subscriptionCollection.TryAdd(subscription4);
            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription4, subscription2, subscription3, subscription });
            subscriptionCollection.TryAdd(subscription5);
            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription4, subscription2, subscription3, subscription5, subscription });
            subscriptionCollection.TryAdd(subscription6);
            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription4, subscription2, subscription3, subscription6, subscription5, subscription });


            Assert.AreEqual(subscriptionCollection.Select(x => x.Configuration.SecurityType).ToList(), new[] { SecurityType.Equity, SecurityType.Equity, SecurityType.Option,
                SecurityType.Option, SecurityType.Option, SecurityType.Future });

            subscription.Dispose();
            subscription2.Dispose();
            subscription3.Dispose();
            subscription4.Dispose();
            subscription5.Dispose();
            subscription6.Dispose();
        }

        [Test]
        public void SubscriptionsAreSortedWhenAdding2()
        {
            var subscriptionCollection = new SubscriptionCollection();
            var subscription = CreateSubscription(Resolution.Second, Futures.Metals.Gold, false, SecurityType.Future);
            var subscription2 = CreateSubscription(Resolution.Second, "SPY");
            var subscription3 = CreateSubscription(Resolution.Second, "AAPL", false, SecurityType.Option);
            var subscription4 = CreateSubscription(Resolution.Second, "EURGBP");

            subscriptionCollection.TryAdd(subscription);
            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription });
            subscriptionCollection.TryAdd(subscription2);
            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription2, subscription });
            subscriptionCollection.TryAdd(subscription3);
            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription2, subscription3, subscription });
            subscriptionCollection.TryAdd(subscription4);

            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription4, subscription2, subscription3, subscription });
            Assert.AreEqual(subscriptionCollection.Select(x => x.Configuration.SecurityType).ToList(), new[] { SecurityType.Equity, SecurityType.Equity, SecurityType.Option, SecurityType.Future });

            subscription.Dispose();
            subscription2.Dispose();
            subscription3.Dispose();
            subscription4.Dispose();
        }

        [Test]
        public void SubscriptionsAreSortedWhenRemoving()
        {
            var subscriptionCollection = new SubscriptionCollection();
            var subscription = CreateSubscription(Resolution.Second, Futures.Metals.Gold, false, SecurityType.Future);
            var subscription2 = CreateSubscription(Resolution.Second, "SPY");
            var subscription3 = CreateSubscription(Resolution.Second, "AAPL", false, SecurityType.Option);
            var subscription4 = CreateSubscription(Resolution.Second, "EURGBP");
            var subscription5 = CreateSubscription(Resolution.Second, "AAPL", false, SecurityType.Option, TickType.OpenInterest);
            var subscription6 = CreateSubscription(Resolution.Second, "AAPL", false, SecurityType.Option, TickType.Quote);

            subscriptionCollection.TryAdd(subscription);
            subscriptionCollection.TryAdd(subscription2);
            subscriptionCollection.TryAdd(subscription3);
            subscriptionCollection.TryAdd(subscription4);
            subscriptionCollection.TryAdd(subscription5);
            subscriptionCollection.TryAdd(subscription6);
            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription4, subscription2, subscription3, subscription6, subscription5, subscription });

            subscriptionCollection.TryRemove(subscription2.Configuration, out subscription2);
            Assert.AreEqual(subscriptionCollection.Select(x => x.Configuration.SecurityType).ToList(), new[] { SecurityType.Equity, SecurityType.Option,
                            SecurityType.Option, SecurityType.Option, SecurityType.Future });

            subscriptionCollection.TryRemove(subscription3.Configuration, out subscription3);
            Assert.AreEqual(subscriptionCollection.Select(x => x.Configuration.SecurityType).ToList(), new[] { SecurityType.Equity, SecurityType.Option, SecurityType.Option, SecurityType.Future });

            subscriptionCollection.TryRemove(subscription.Configuration, out subscription);
            Assert.AreEqual(subscriptionCollection.Select(x => x.Configuration.SecurityType).ToList(), new[] { SecurityType.Equity, SecurityType.Option, SecurityType.Option });
            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription4, subscription6, subscription5 });

            subscriptionCollection.TryRemove(subscription6.Configuration, out subscription6);
            Assert.AreEqual(subscriptionCollection.Select(x => x.Configuration.SecurityType).ToList(), new[] { SecurityType.Equity, SecurityType.Option });
            Assert.AreEqual(subscriptionCollection.ToList(), new[] { subscription4, subscription5 });

            subscriptionCollection.TryRemove(subscription5.Configuration, out subscription5);
            Assert.AreEqual(subscriptionCollection.Select(x => x.Configuration.SecurityType).ToList(), new[] {SecurityType.Equity});

            subscriptionCollection.TryRemove(subscription4.Configuration, out subscription4);
            Assert.IsTrue(subscriptionCollection.Select(x => x.Configuration.SecurityType).ToList().IsNullOrEmpty());

            subscription.Dispose();
            subscription2.Dispose();
            subscription3.Dispose();
            subscription4.Dispose();
            subscription5.Dispose();
            subscription6.Dispose();
        }

        private Subscription CreateSubscription(Resolution resolution, string symbol = "AAPL", bool isInternalFeed = false,
                                                SecurityType type = SecurityType.Equity, TickType tickType = TickType.Trade)
        {
            var start = DateTime.UtcNow;
            var end = start.AddSeconds(10);
            Security security;
            Symbol _symbol;
            if (type == SecurityType.Equity)
            {
                _symbol = new Symbol(SecurityIdentifier.GenerateEquity(DateTime.Now, symbol, Market.USA), symbol);
                security = new Equity(
                    _symbol,
                    SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                    new Cash(Currencies.USD, 0, 1),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                );
            }
            else if (type == SecurityType.Option)
            {
                _symbol = Symbol.CreateOption(
                    new Symbol(SecurityIdentifier.GenerateEquity(DateTime.Now, symbol, Market.USA), symbol),
                    Market.USA,
                    OptionStyle.American,
                    OptionRight.Call,
                    0m,
                    DateTime.Now
                );
                security = new Option(
                    _symbol,
                    SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                    new Cash(Currencies.USD, 0, 1),
                    new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache(),
                    null
                );
            }
            else if (type == SecurityType.Future)
            {
                _symbol = new Symbol(SecurityIdentifier.GenerateFuture(DateTime.Now, symbol, Market.COMEX), symbol);
                security = new Future(
                    _symbol,
                    SecurityExchangeHours.AlwaysOpen(DateTimeZone.Utc),
                    new Cash(Currencies.USD, 0, 1),
                    SymbolProperties.GetDefault(Currencies.USD),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache()
                );
            }
            else
            {
                throw new RegressionTestException("SecurityType not implemented");
            }
            var config = new SubscriptionDataConfig(typeof(TradeBar), _symbol, resolution, DateTimeZone.Utc, DateTimeZone.Utc, true, false, isInternalFeed, false, tickType);
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(DateTimeZone.Utc, start, end);
            # pragma warning disable CA2000
            var enumerator = new EnqueueableEnumerator<BaseData>();
            var subscriptionDataEnumerator = new SubscriptionDataEnumerator(config, security.Exchange.Hours, timeZoneOffsetProvider, enumerator, false, false);
                # pragma warning restore CA2000
            var subscriptionRequest = new SubscriptionRequest(false, null, security, config, start, end);
            return new Subscription(subscriptionRequest, subscriptionDataEnumerator, timeZoneOffsetProvider);
        }
    }
}
