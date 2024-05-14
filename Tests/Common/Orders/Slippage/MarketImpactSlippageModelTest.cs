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

using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Orders;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using QuantConnect.Tests.Common.Data.Fundamental;
using QuantConnect.Tests.Engine.DataFeeds;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common.Orders.Slippage
{
    [TestFixture]
    public class MarketImpactSlippageModelTests
    {
        private QCAlgorithm _algorithm;
        private MarketImpactSlippageModel _slippageModel;
        private List<Security> _securities;

        [SetUp]
        public void Initialize()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));

            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null,
                TestGlobals.DataProvider, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider,
                null, true, new DataPermissionManager(), _algorithm.ObjectStore, _algorithm.Settings));
            _algorithm.SetHistoryProvider(historyProvider);

            FundamentalService.Initialize(TestGlobals.DataProvider, new TestFundamentalDataProvider(), false);

            var optionContract = Symbol.CreateOption(Symbols.AAPL, Market.USA,
                OptionStyle.American, OptionRight.Call, 100, new DateTime(2016, 1, 15));

            _algorithm.SetDateTime(new DateTime(2015, 6, 10, 15, 0, 0));

            _securities = new List<Security>
            {
                _algorithm.AddEquity("SPY", Resolution.Daily),                      // liquid stock
                _algorithm.AddEquity("AIG", Resolution.Daily),                      // illquid stock
                _algorithm.AddCrypto("BTCUSD", Resolution.Daily, Market.Coinbase),      // crypto
                _algorithm.AddOptionContract(optionContract, Resolution.Minute)     // equity options
            };
            foreach (var security in _securities)
            {
                security.SetMarketPrice(new TradeBar(_algorithm.Time, security.Symbol, 100m, 100m, 100m, 100m, 1));
            }

            _algorithm.EnableAutomaticIndicatorWarmUp = true;

            _slippageModel = new MarketImpactSlippageModel(_algorithm);
        }

        // Test on buy & sell orders
        [TestCase(InsightDirection.Up)]
        [TestCase(InsightDirection.Down)]
        public void SizeSlippageComparisonTests(InsightDirection direction)
        {
            // Test on all liquid/illquid stocks/other asset classes
            foreach (var asset in _securities)
            {
                // A significantly large difference that noise cannot affect the result
                var smallBuyOrder = new MarketOrder(asset.Symbol, 10 * (int)direction, new DateTime(2015, 6, 10, 14, 00, 0));
                var largeBuyOrder = new MarketOrder(asset.Symbol, 10000000000 * (int)direction, new DateTime(2015, 6, 10, 14, 00, 0));

                var smallBuySlippage = _slippageModel.GetSlippageApproximation(asset, smallBuyOrder);
                var largeBuySlippage = _slippageModel.GetSlippageApproximation(asset, largeBuyOrder);

                // We expect small size order has less slippage than large size order on the same asset
                Assert.Less(smallBuySlippage, largeBuySlippage);
            }
        }

        // Order quantity large enough to create significant market impact
        // Test for buy & sell orders
        [TestCase(100000)]
        [TestCase(-100000)]
        public void VolatileSlippageComparisonTests(decimal orderQuantity)
        {
            var highVolAsset = _securities[0];
            var lowVolAsset = _securities[1];

            var highVolOrder = new MarketOrder(highVolAsset.Symbol, orderQuantity, new DateTime(2015, 6, 10, 14, 00, 0));
            var lowVolOrder = new MarketOrder(lowVolAsset.Symbol, orderQuantity, new DateTime(2015, 6, 10, 14, 00, 0));

            var highVolSlippage = _slippageModel.GetSlippageApproximation(highVolAsset, highVolOrder);
            var lowVolSlippage = _slippageModel.GetSlippageApproximation(lowVolAsset, lowVolOrder);

            // We expect same size order on volatile asset has greater slippage than less volatile asset
            Assert.Greater(highVolSlippage, lowVolSlippage);
        }

        // Test on buy & sell orders
        [TestCase(10000)]
        [TestCase(-10000)]
        [TestCase(10000000)]
        [TestCase(-10000000)]
        public void TimeSlippageComparisonTests(decimal orderQuantity)
        {
            // set up another slippage model with much longer execution time
            var slowSlippageModel = new MarketImpactSlippageModel(_algorithm, latency: 10);

            // Test on all liquid/illquid stocks/other asset classes
            foreach (var asset in _securities)
            {
                var order = new MarketOrder(asset.Symbol, orderQuantity, new DateTime(2015, 6, 10, 14, 00, 0));
                var fastFilledSlippage = _slippageModel.GetSlippageApproximation(asset, order);
                var slowFilledSlippage = slowSlippageModel.GetSlippageApproximation(asset, order);

                // We expect same size order on same asset has less slippage if filled slower since the market can digest slowly
                Assert.Less(slowFilledSlippage, fastFilledSlippage);
            }
        }

        // To test whether the slippage matches our expectation
        [TestCase(100, 0, 0.0)]
        [TestCase(100, 1, 0.0808)]
        [TestCase(1, 2, 15.5061)]
        [TestCase(1, 3, 38.7598)]
        [TestCase(-100, 0, 0.0)]
        [TestCase(-100, 1, 0.0808)]
        [TestCase(-1, 2, 15.5061)]
        [TestCase(-1, 3, 38.7598)]
        [TestCase(10000, 0, 0.5075)]
        [TestCase(10000, 1, 3.8421)]
        [TestCase(100, 2, 100.0)]
        [TestCase(100, 3, 100.0)]
        [TestCase(-10000, 0, 0.5075)]
        [TestCase(-10000, 1, 3.8421)]
        [TestCase(-100, 2, 100.0)]
        [TestCase(-100, 3, 100.0)]
        public void SlippageExpectationTests(decimal orderQuantity, int index, double expected)
        {
            var asset = _securities[index];
            
            var order = new MarketOrder(asset.Symbol, orderQuantity, new DateTime(2015, 6, 10, 14, 00, 0));
            var slippage = _slippageModel.GetSlippageApproximation(asset, order);

            Assert.AreEqual(expected, (double)slippage, 0.005d);
        }

        // Test on buy & sell orders
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(1000)]
        [TestCase(-1000)]
        [TestCase(1000000)]
        [TestCase(-1000000)]
        public void NonNegativeSlippageTests(decimal orderQuantity)
        {
            // Test on all liquid/illquid stocks/other asset classes
            foreach (var asset in _securities)
            {
                var order = new MarketOrder(asset.Symbol, orderQuantity, new DateTime(2015, 6, 10, 14, 00, 0));
                var slippage = _slippageModel.GetSlippageApproximation(asset, order);

                Assert.GreaterOrEqual(slippage, 0m);
            }
        }

        // Large order size to hit the threshold
        // Test on buy & sell orders
        [TestCase(10000)]
        [TestCase(-10000)]
        [TestCase(1000000000)]
        [TestCase(-1000000000)]
        public void MaxSlippageValueTests(decimal orderQuantity)
        {
            // Test on all liquid/illquid stocks/other asset classes
            foreach (var asset in _securities)
            {
                var order = new MarketOrder(asset.Symbol, orderQuantity, new DateTime(2015, 6, 10, 14, 00, 0));
                var slippage = _slippageModel.GetSlippageApproximation(asset, order);

                // Slippage is at max the asset's price, no limit on negative slippage
                Assert.LessOrEqual(slippage, asset.Price);
            }
        }

        [Test]
        public void CfdExceptionTests()
        {
            var cfd = _algorithm.AddCfd("XAUUSD", Resolution.Daily, Market.Oanda);
            var cfdOrder = new MarketOrder(cfd.Symbol, 10, new DateTime(2013, 10, 10, 14, 00, 0));

            Assert.Throws<Exception>(() => _slippageModel.GetSlippageApproximation(cfd, cfdOrder));
        }

        [Test]
        public void ForexExceptionTests()
        {
            var forex = _algorithm.AddForex("EURUSD", Resolution.Daily, Market.Oanda);
            var forexOrder = new MarketOrder(forex.Symbol, 10, new DateTime(2013, 10, 10, 14, 00, 0));

            Assert.Throws<Exception>(() => _slippageModel.GetSlippageApproximation(forex, forexOrder));
        }
    }
}
