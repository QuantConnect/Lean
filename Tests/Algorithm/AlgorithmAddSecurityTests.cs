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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Cfd;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Securities.IndexOption;
using QuantConnect.Data.Custom.AlphaStreams;
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
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_dataFeed, algorithm));
            var equity = algorithm.AddEquity("AAPL", dataNormalizationMode: dataNormalizationMode);
            Assert.That(algorithm.SubscriptionManager.Subscriptions.Where(x => x.Symbol == equity.Symbol).Select(x => x.DataNormalizationMode),
                Has.All.EqualTo(dataNormalizationMode));
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
                    new TestCaseData(Symbol.CreateBase(typeof(AlphaStreamsPortfolioState), Symbols.SPY, Market.USA), typeof(AlphaStreamsPortfolioState)),
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
            return (DataNormalizationMode[])Enum.GetValues(typeof(DataNormalizationMode));
        }
    }
}
