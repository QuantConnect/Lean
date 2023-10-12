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
using QuantConnect.Data;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Orders;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities.Equity;
using QuantConnect.Tests.Engine.DataFeeds;
using System;

namespace QuantConnect.Tests.Common.Orders.Slippage
{
    [TestFixture]
    public class MarketImpactSlippageModelTests
    {
        private QCAlgorithm _algorithm;
        private MarketImpactSlippageModel _slippageModel;
        private Equity _liquidEquity;
        private Equity _illiquidEquity;

        [SetUp]
        public void Initialize()
        {
            _algorithm = new QCAlgorithm();
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));

            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            using var cacheProvider = new ZipDataCacheProvider(TestGlobals.DataProvider);
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null,
                TestGlobals.DataProvider, cacheProvider, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider,
                null, true, new DataPermissionManager(), _algorithm.ObjectStore));
            _algorithm.SetHistoryProvider(historyProvider);

            _algorithm.SetDateTime(new DateTime(2013, 10, 11, 15, 0, 0));
            _liquidEquity = _algorithm.AddEquity("SPY");
            _illiquidEquity = _algorithm.AddEquity("WM");

            _algorithm.EnableAutomaticIndicatorWarmUp = true;

            _slippageModel = new MarketImpactSlippageModel(_algorithm);
        }

        [Test]
        public void SizeSlippageComparisonTests()
        {
            // A significantly large difference that noise cannot affect the result
            var smallLiquidOrder = new MarketOrder(_liquidEquity.Symbol, 1, new DateTime(2013, 10, 11, 14, 50, 0));
            var largeLiquidOrder = new MarketOrder(_liquidEquity.Symbol, 10000000000, new DateTime(2013, 10, 11, 14, 50, 0));
            var smallIliquidOrder = new MarketOrder(_illiquidEquity.Symbol, 1, new DateTime(2013, 10, 11, 14, 50, 0));
            var largeIliquidOrder = new MarketOrder(_illiquidEquity.Symbol, 10000000000, new DateTime(2013, 10, 11, 14, 50, 0));

            var smallLiquidSlippage = _slippageModel.GetSlippageApproximation(_liquidEquity, smallLiquidOrder);
            var largeLiquidSlippage = _slippageModel.GetSlippageApproximation(_liquidEquity, largeLiquidOrder);
            var smallIliquidSlippage = _slippageModel.GetSlippageApproximation(_illiquidEquity, smallIliquidOrder);
            var largeIliquidSlippage = _slippageModel.GetSlippageApproximation(_illiquidEquity, largeIliquidOrder);

            // We expect small size order has less slippage than large size order on the same asset
            Assert.Less(smallLiquidSlippage, largeLiquidSlippage);
            Assert.Less(smallIliquidSlippage, largeIliquidSlippage);
        }

        [TestCase(100)]
        [TestCase(10000)]
        [TestCase(1000000000)]
        public void LiquiditySlippageComparisonTests(decimal orderQuantity)
        {
            var liquidOrder = new MarketOrder(_liquidEquity.Symbol, orderQuantity, new DateTime(2013, 10, 11, 14, 50, 0));
            var illquidOrder = new MarketOrder(_illiquidEquity.Symbol, orderQuantity, new DateTime(2013, 10, 11, 14, 50, 0));

            var liquidSlippage = _slippageModel.GetSlippageApproximation(_liquidEquity, liquidOrder);
            var illquidSlippage = _slippageModel.GetSlippageApproximation(_illiquidEquity, illquidOrder);

            // We expect same size order on liquid asset has less slippage than illquid asset
            Assert.Less(liquidSlippage, illquidSlippage);
        }
    }
}
