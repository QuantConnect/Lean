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
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Queues;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class InternalSubscriptionManagerTests
    {
        private Synchronizer _synchronizer;
        private DataManager _dataManager;
        private QCAlgorithm _algorithm;
        private IDataFeed _dataFeed;

        [SetUp]
        public void Setup()
        {
            _dataFeed = new TestableLiveTradingDataFeed(new FakeDataQueue());
            _algorithm = new AlgorithmStub(createDataManager: false);
            _synchronizer = new LiveSynchronizer();
            var registeredTypesProvider = new RegisteredSecurityDataTypesProvider();
            var securityService = new SecurityService(_algorithm.Portfolio.CashBook,
                MarketHoursDatabase.FromDataFolder(),
                SymbolPropertiesDatabase.FromDataFolder(),
                _algorithm,
                registeredTypesProvider,
                new SecurityCacheProvider(_algorithm.Portfolio));
            var universeSelection = new UniverseSelection(
                _algorithm,
                securityService,
                new DataPermissionManager(), 
                new DefaultDataProvider(),
                Resolution.Second);
            _dataManager = new DataManager(_dataFeed, universeSelection, _algorithm, new TimeKeeper(DateTime.UtcNow, TimeZones.NewYork),
                MarketHoursDatabase.FromDataFolder(),
                true,
                new RegisteredSecurityDataTypesProvider(), 
                new DataPermissionManager());
            _synchronizer.Initialize(_algorithm, _dataManager);
            _dataFeed.Initialize(_algorithm,
                new LiveNodePacket(),
                new TestResultHandler(),
                new LocalDiskMapFileProvider(),
                new LocalDiskFactorFileProvider(),
                new DefaultDataProvider(),
                _dataManager,
                _synchronizer,
                new DataChannelProvider());
            _algorithm.SubscriptionManager.SetDataManager(_dataManager);
            _algorithm.Securities.SetSecurityService(securityService);
            _algorithm.SetFinishedWarmingUp();
            _algorithm.Transactions.SetOrderProcessor(new FakeOrderProcessor());
        }

        [TearDown]
        public void TearDown()
        {
            _dataFeed.Exit();
            _dataManager.RemoveAllSubscriptions();
        }

        [TestCaseSource(nameof(DataTypeTestCases))]
        public void CreatesSubscriptions(SubscriptionRequest subscriptionRequest, bool liveMode, bool expectNewSubscription)
        {
            _algorithm.SetLiveMode(liveMode);

            var added = false;
            var start = DateTime.UtcNow;
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            foreach (var timeSlice in _synchronizer.StreamData(tokenSource.Token))
            {
                if (!added)
                {
                    added = true;
                    _algorithm.AddSecurity(subscriptionRequest.Security.Symbol, subscriptionRequest.Configuration.Resolution);
                }
                else if (!timeSlice.IsTimePulse)
                {
                    Assert.AreEqual(
                        expectNewSubscription,
                        _algorithm.SubscriptionManager.SubscriptionDataConfigService
                            .GetSubscriptionDataConfigs(Symbols.BTCUSD, includeInternalConfigs: true).Any(config => config.IsInternalFeed)
                    );

                    if (expectNewSubscription)
                    {
                        // let's wait for a data point
                        if (timeSlice.DataPointCount > 0)
                        {
                            break;
                        }

                        if (DateTime.UtcNow - start > TimeSpan.FromSeconds(5))
                        {
                            Assert.Fail("Timeout waiting for data point");
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                // give time for the base exchange to pick up the data point that will trigger the universe selection
                // so next step we assert the internal config is there
                Thread.Sleep(100);
                _algorithm.OnEndOfTimeStep();
            }
            Assert.IsFalse(tokenSource.IsCancellationRequested);
        }

        [TestCaseSource(nameof(DataTypeTestCases))]
        public void RemoveSubscriptions(SubscriptionRequest subscriptionRequest, bool liveMode, bool expectNewSubscription)
        {
            _algorithm.SetLiveMode(liveMode);
            if (!expectNewSubscription)
            {
                // we only test cases where we expect an internal subscription
                return;
            }
            var added = false;
            var shouldRemoved = false;
            var count = 0;
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            foreach (var timeSlice in _synchronizer.StreamData(tokenSource.Token))
            {
                if (!added)
                {
                    added = true;
                    _algorithm.AddSecurity(subscriptionRequest.Security.Symbol, subscriptionRequest.Configuration.Resolution);
                }
                else if (!timeSlice.IsTimePulse && !shouldRemoved)
                {
                    Assert.IsTrue(_algorithm.SubscriptionManager.SubscriptionDataConfigService
                            .GetSubscriptionDataConfigs(Symbols.BTCUSD, includeInternalConfigs: true).Any());

                    _algorithm.RemoveSecurity(subscriptionRequest.Security.Symbol);
                    shouldRemoved = true;
                }
                else if (!timeSlice.IsTimePulse && shouldRemoved)
                {
                    var result = _algorithm.SubscriptionManager.SubscriptionDataConfigService
                        .GetSubscriptionDataConfigs(Symbols.BTCUSD, includeInternalConfigs: true).Any(config => config.IsInternalFeed);
                    // can take some extra loop till the base exchange thread picks up the data point that will trigger the universe selection
                    if (!result || count++ > 5)
                    {
                        Assert.IsFalse(result);
                    }
                    break;
                }
                _algorithm.OnEndOfTimeStep();
                // give time for the base exchange to pick up the data point that will trigger the universe selection
                // so next step we assert the internal config is there
                Thread.Sleep(100);
            }
            Assert.IsFalse(tokenSource.IsCancellationRequested);
        }

        [Test]
        public void UniverseSelectionAddAndRemove()
        {
            _algorithm.SetLiveMode(true);
            _algorithm.UniverseSettings.Resolution = Resolution.Hour;
            _algorithm.UniverseSettings.MinimumTimeInUniverse = TimeSpan.Zero;
            var added = false;
            var first = true;
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            foreach (var timeSlice in _synchronizer.StreamData(tokenSource.Token))
            {
                if (!added)
                {
                    added = true;
                    _algorithm.AddUniverse(SecurityType.Equity,
                            "AUniverse",
                            Resolution.Second,
                            Market.USA,
                            _algorithm.UniverseSettings,
                            time =>
                            {
                                return first ? new[] { "SPY" } : new[] { "AAPL" };
                            }
                    );
                }
                else if (!timeSlice.IsTimePulse)
                {
                    if (first)
                    {
                        Assert.IsTrue(_algorithm.SubscriptionManager.SubscriptionDataConfigService
                            .GetSubscriptionDataConfigs(Symbols.SPY, includeInternalConfigs: true).Any(config => config.Resolution == Resolution.Second));
                        Assert.IsFalse(_algorithm.SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(Symbols.AAPL, includeInternalConfigs: true).Any());
                        first = false;
                    }
                    else
                    {
                        Assert.IsTrue(_algorithm.SubscriptionManager.SubscriptionDataConfigService
                            .GetSubscriptionDataConfigs(Symbols.AAPL, includeInternalConfigs: true).Any(config => config.Resolution == Resolution.Second));
                        Assert.IsFalse(_algorithm.SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(Symbols.SPY, includeInternalConfigs: true).Any());
                        break;
                    }
                }
                _algorithm.OnEndOfTimeStep();
            }
            Assert.IsFalse(tokenSource.IsCancellationRequested);
        }

        private static TestCaseData[] DataTypeTestCases
        {
            get
            {
                var result = new List<TestCaseData>();
                var config = GetConfig(Symbols.BTCUSD, Resolution.Second);
                result.Add(new TestCaseData(new SubscriptionRequest(false, null, CreateSecurity(config), config, DateTime.UtcNow, DateTime.UtcNow), true, false));

                config = GetConfig(Symbols.BTCUSD, Resolution.Minute);
                result.Add(new TestCaseData(new SubscriptionRequest(false, null, CreateSecurity(config), config, DateTime.UtcNow, DateTime.UtcNow), true, false));

                config = GetConfig(Symbols.BTCUSD, Resolution.Hour);
                result.Add(new TestCaseData(new SubscriptionRequest(false, null, CreateSecurity(config), config, DateTime.UtcNow, DateTime.UtcNow), true, true));

                config = GetConfig(Symbols.BTCUSD, Resolution.Daily);
                result.Add(new TestCaseData(new SubscriptionRequest(false, null, CreateSecurity(config), config, DateTime.UtcNow, DateTime.UtcNow), true, true));

                result.Add(new TestCaseData(new SubscriptionRequest(false, null, CreateSecurity(config), config, DateTime.UtcNow, DateTime.UtcNow), false, false));

                return result.ToArray();
            }
        }

        private static Security CreateSecurity(SubscriptionDataConfig config)
        {
            return new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        private static SubscriptionDataConfig GetConfig(Symbol symbol, Resolution resolution)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol, resolution, TimeZones.Utc, TimeZones.Utc, false, false, false);
        }
    }
}
