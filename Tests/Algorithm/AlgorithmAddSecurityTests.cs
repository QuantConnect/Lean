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

using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using QuantConnect.Securities.Cfd;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.IndexOption;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Engine.DataFeeds;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Index = QuantConnect.Securities.Index.Index;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmAddSecurityTests
    {
        private QCAlgorithm _algo;
        private NullDataFeed _dataFeed;

        /// <summary>
        /// Instatiate a new algorithm before each test.
        /// Clear the <see cref="SymbolCache"/> so that no symbols and associated brokerage models are cached between test
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _algo = new QCAlgorithm();
            _dataFeed = new NullDataFeed
            {
                ShouldThrow = false
            };
            _algo.SubscriptionManager.SetDataManager(new DataManagerStub(_dataFeed, _algo));
        }

        [Test, TestCaseSource(nameof(TestAddSecurityWithSymbol))]
        public void AddSecurityWithSymbol(Symbol symbol, Type type = null)
        {
            var security = type != null ? _algo.AddData(type, symbol.Underlying) : _algo.AddSecurity(symbol);
            Assert.AreEqual(security.Symbol, symbol);
            Assert.IsTrue(_algo.Securities.ContainsKey(symbol));

            Assert.DoesNotThrow(() =>
            {
                switch (symbol.SecurityType)
                {
                    case SecurityType.Equity:
                        var equity = (Equity)security;
                        break;
                    case SecurityType.Option:
                        var option = (Option)security;
                        break;
                    case SecurityType.Forex:
                        var forex = (Forex)security;
                        break;
                    case SecurityType.Future:
                        var future = (Future)security;
                        break;
                    case SecurityType.Cfd:
                        var cfd = (Cfd)security;
                        break;
                    case SecurityType.Index:
                        var index = (Index)security;
                        break;
                    case SecurityType.IndexOption:
                        var indexOption = (IndexOption)security;
                        break;
                    case SecurityType.Crypto:
                        var crypto = (Crypto)security;
                        break;
                    case SecurityType.CryptoFuture:
                        var cryptoFuture = (CryptoFuture)security;
                        break;
                    case SecurityType.Base:
                        break;
                    default:
                        throw new Exception($"Invalid Security Type: {symbol.SecurityType}");
                }
            });

            if (symbol.IsCanonical())
            {
                Assert.DoesNotThrow(() => _algo.OnEndOfTimeStep());

                Assert.IsTrue(_algo.UniverseManager.ContainsKey(symbol));
            }
        }

        [TestCaseSource(nameof(GetDataNormalizationModes))]
        public void AddsEquityWithExpectedDataNormalizationMode(DataNormalizationMode dataNormalizationMode)
        {
            var equity = _algo.AddEquity("AAPL", dataNormalizationMode: dataNormalizationMode);
            Assert.That(_algo.SubscriptionManager.Subscriptions.Where(x => x.Symbol == equity.Symbol).Select(x => x.DataNormalizationMode),
                Has.All.EqualTo(dataNormalizationMode));
        }

        [Test]
        public void ProperlyAddsFutureWithExtendedMarketHours(
            [Values(true, false)] bool extendedMarketHours,
            [ValueSource(nameof(FuturesTestCases))] Func<QCAlgorithm, Security> getFuture)
        {
            var future = _algo.AddFuture(Futures.Indices.VIX, Resolution.Minute, extendedMarketHours: extendedMarketHours);
            var subscriptions = _algo.SubscriptionManager.Subscriptions.Where(x => x.Symbol == future.Symbol).ToList();

            var universeSubscriptions = subscriptions.Where(x => x.Type == typeof(FutureUniverse)).ToList();
            Assert.AreEqual(1, universeSubscriptions.Count);
            // Universe does not support extended market hours
            Assert.IsFalse(universeSubscriptions[0].ExtendedMarketHours);

            var nonUniverseSubscriptions = subscriptions.Where(x => x.Type != typeof(FutureUniverse)).ToList();
            Assert.Greater(nonUniverseSubscriptions.Count, 0);
            Assert.That(nonUniverseSubscriptions.Select(x => x.ExtendedMarketHours),
                Has.All.EqualTo(extendedMarketHours));
        }

        [TestCaseSource(nameof(FuturesTestCases))]
        public void AddFutureWithExtendedMarketHours(Func<QCAlgorithm, Security> getFuture)
        {
            string file = Path.Combine("TestData", "SampleMarketHoursDatabase.json");
            var marketHoursDatabase = MarketHoursDatabase.FromFile(file);
            var securityService = new SecurityService(
                _algo.Portfolio.CashBook,
                marketHoursDatabase,
                SymbolPropertiesDatabase.FromDataFolder(),
                _algo,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCacheProvider(_algo.Portfolio),
                algorithm: _algo);
            _algo.Securities.SetSecurityService(securityService);

            var future = getFuture(_algo);

            var now = new DateTime(2022, 6, 26, 17, 0, 0);
            Assert.AreEqual(DayOfWeek.Sunday, now.DayOfWeek);
            var regularMarketStartTime = new TimeSpan(8, 30, 0);
            var regularMarketEndTime = new TimeSpan(15, 0, 0);
            var firstExtendedMarketStartTime = regularMarketEndTime;
            var firstExtendedMarketEndTime = new TimeSpan(16, 0, 0);
            var secondExtendedMarketStartTime = new TimeSpan(17, 0, 0);

            Action<DateTime> checkExtendedHours = (date) =>
            {
                Assert.IsFalse(future.Exchange.Hours.IsOpen(now, false));
                Assert.IsTrue(future.Exchange.Hours.IsOpen(now, true));
            };
            Action<DateTime> checkRegularHours = (date) =>
            {
                Assert.IsTrue(future.Exchange.Hours.IsOpen(now, false));
                Assert.IsTrue(future.Exchange.Hours.IsOpen(now, true));
            };
            Action<DateTime> checkClosed = (date) =>
            {
                Assert.IsFalse(future.Exchange.Hours.IsOpen(now, false));
                Assert.IsFalse(future.Exchange.Hours.IsOpen(now, true));
            };

            while (now.DayOfWeek < DayOfWeek.Saturday)
            {
                while (now.TimeOfDay < regularMarketStartTime)
                {
                    checkExtendedHours(now);
                    now = now.AddMinutes(1);
                }

                while (now.TimeOfDay < regularMarketEndTime)
                {
                    checkRegularHours(now);
                    now = now.AddMinutes(1);
                }

                while (now.TimeOfDay >= firstExtendedMarketStartTime && now.TimeOfDay < firstExtendedMarketEndTime)
                {
                    checkExtendedHours(now);
                    now = now.AddMinutes(1);
                }

                while (now.TimeOfDay < secondExtendedMarketStartTime)
                {
                    checkClosed(now);
                    now = now.AddMinutes(1);
                }

                var endOfDay = now.AddDays(1).Date;
                if (now.DayOfWeek < DayOfWeek.Friday)
                {
                    while (now < endOfDay)
                    {
                        checkExtendedHours(now);
                        now = now.AddMinutes(1);
                    }
                }
                else
                {
                    now = endOfDay;
                }
            }

            while (now.DayOfWeek < DayOfWeek.Sunday)
            {
                checkClosed(now);
                now = now.AddMinutes(1);
            }
        }

        // Reproduces https://github.com/QuantConnect/Lean/issues/7451
        [Test]
        public void DoesNotAddExtraIndexSubscriptionAfterAddingIndexOptionContract()
        {
            var spx = _algo.AddIndex("SPX", Resolution.Minute, fillForward: false);

            Assert.AreEqual(1, _algo.SubscriptionManager.Subscriptions.Count());
            Assert.AreEqual(spx.Symbol, _algo.SubscriptionManager.Subscriptions.Single().Symbol);

            var spxOption = Symbol.CreateOption(
                spx.Symbol,
                Market.USA,
                OptionStyle.European,
                OptionRight.Call,
                3200m,
                new DateTime(2021, 1, 15));
            _algo.AddIndexOptionContract(spxOption, Resolution.Minute);

            Assert.Greater(_algo.SubscriptionManager.Subscriptions.Count(), 1);
            Assert.AreEqual(1, _algo.SubscriptionManager.Subscriptions.Count(x => x.Symbol == spx.Symbol));
        }

        private static TestCaseData[] TestAddSecurityWithSymbol
        {
            get
            {
                var result = new List<TestCaseData>()
                {
                    new TestCaseData(Symbols.SPY, null),
                    new TestCaseData(Symbols.EURUSD, null),
                    new TestCaseData(Symbols.DE30EUR, null),
                    new TestCaseData(Symbols.BTCUSD, null),
                    new TestCaseData(Symbols.ES_Future_Chain, null),
                    new TestCaseData(Symbols.Future_ESZ18_Dec2018, null),
                    new TestCaseData(Symbols.SPY_Option_Chain, null),
                    new TestCaseData(Symbols.SPY_C_192_Feb19_2016, null),
                    new TestCaseData(Symbols.SPY_P_192_Feb19_2016, null),
                    new TestCaseData(Symbol.Create("CustomData", SecurityType.Base, Market.Binance), null),
                    new TestCaseData(Symbol.Create("CustomData2", SecurityType.Base, Market.COMEX), null)
                };

                foreach (var market in Market.SupportedMarkets())
                {
                    foreach (var kvp in SymbolPropertiesDatabase.FromDataFolder().GetSymbolPropertiesList(market))
                    {
                        var securityDatabaseKey = kvp.Key;
                        if (securityDatabaseKey.SecurityType != SecurityType.FutureOption)
                        {
                            result.Add(new TestCaseData(Symbol.Create(securityDatabaseKey.Symbol, securityDatabaseKey.SecurityType,
                                securityDatabaseKey.Market), null));
                        }
                    }
                }

                return result.ToArray();
            }
        }

        private static DataNormalizationMode[] GetDataNormalizationModes()
        {
            return ((DataNormalizationMode[])Enum.GetValues(typeof(DataNormalizationMode)))
                .Where(x => x != DataNormalizationMode.ScaledRaw).ToArray();
        }

        private static Func<QCAlgorithm, Security>[] FuturesTestCases
        {
            get
            {
                return new Func<QCAlgorithm, Security>[]
                {
                    (algo) => algo.AddFuture(Futures.Indices.VIX, Resolution.Minute, extendedMarketHours: true),
                    (algo) => algo.AddFutureContract(Symbol.CreateFuture(Futures.Indices.VIX, Market.CFE, new DateTime(2022, 8, 1)),
                        Resolution.Minute, extendedMarketHours: true)
                };
            }
        }
    }
}
