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
using QuantConnect.Securities.Cfd;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Engine.DataFeeds;
using System;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmAddSecurityTests
    {
        private QCAlgorithm _algo;

        /// <summary>
        /// Instatiate a new algorithm before each test.
        /// Clear the <see cref="SymbolCache"/> so that no symbols and associated brokerage models are cached between test
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _algo = new QCAlgorithm();
            _algo.SubscriptionManager.SetDataManager(new DataManagerStub(_algo));
        }

        [Test, TestCaseSource(nameof(TestAddSecurityWithSymbol))]
        public void AddSecurityWithSymbol(Symbol symbol)
        {
            var security = _algo.AddSecurity(symbol);
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
                    case SecurityType.Crypto:
                        var crypto = (Crypto)security;
                        break;
                    default:
                        throw new Exception($"Invalid Security Type: {symbol.SecurityType}");
                }
            });

            if (symbol.IsCanonical())
            {
                // Throws NotImplementedException because we are using NullDataFeed
                // We need to call this to add the pending universe additions
                Assert.Throws<NotImplementedException>(() => _algo.OnEndOfTimeStep());

                Assert.IsTrue(_algo.UniverseManager.ContainsKey(symbol));
            }
        }

        private static TestCaseData[] TestAddSecurityWithSymbol
        {
            get
            {
                return new[]
                {
                    new TestCaseData(Symbols.SPY),
                    new TestCaseData(Symbols.EURUSD),
                    new TestCaseData(Symbols.DE30EUR),
                    new TestCaseData(Symbols.BTCUSD),
                    new TestCaseData(Symbols.ES_Future_Chain),
                    new TestCaseData(Symbols.Future_ESZ18_Dec2018),
                    new TestCaseData(Symbols.SPY_Option_Chain),
                    new TestCaseData(Symbols.SPY_C_192_Feb19_2016),
                    new TestCaseData(Symbols.SPY_P_192_Feb19_2016),
                };
            }
        }
    }
}