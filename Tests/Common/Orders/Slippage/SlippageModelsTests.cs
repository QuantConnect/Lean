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
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using System;

namespace QuantConnect.Tests.Common.Orders.Slippage
{
    [TestFixture]
    public class SlippageModelsTests
    {
        private Order _equityBuyOrder;
        private Equity _equity;
        private Order _forexBuyOrder;
        private Forex _forex;

        [SetUp]
        public void Initialize()
        {
            _equity = new Equity(
                Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            _equity.SetMarketPrice(new TradeBar(DateTime.Now, Symbols.SPY, 100m, 100m, 100m, 100m, 1));

            _equityBuyOrder = new MarketOrder(Symbols.SPY, 1, DateTime.Now);


            _forex = new Forex(
                Symbols.EURUSD,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            _forex.SetMarketPrice(new TradeBar(DateTime.Now, Symbols.EURUSD, 100m, 100m, 100m, 100m, 0));

            _forexBuyOrder = new MarketOrder(Symbols.EURUSD, 1000, DateTime.Now);
        }

        [Test]
        public void ConstantSlippageModelTests()
        {
            var slippagePercent = 1m;
            var model = new ConstantSlippageModel(slippagePercent);

            var expected = _equity.Price * slippagePercent;
            var actual = model.GetSlippageApproximation(_equity, _equityBuyOrder);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VolumeShareSlippageModelInitializationTests()
        {
            // These are low volume tests, since the order quantity and the volume are the same

            // These are the default values for the VolumeShareSlippageModel
            var priceImpact = 0.1m;
            var volumeLimit = 0.025m;
            var model = new VolumeShareSlippageModel();

            var expected = _equity.Price * priceImpact * volumeLimit * volumeLimit;
            var actual = model.GetSlippageApproximation(_equity, _equityBuyOrder);
            Assert.AreEqual(expected, actual);

            // Double the values
            priceImpact *= 2;
            volumeLimit *= 2;
            model = new VolumeShareSlippageModel(volumeLimit, priceImpact);

            expected = _equity.Price * priceImpact * volumeLimit * volumeLimit;
            actual = model.GetSlippageApproximation(_equity, _equityBuyOrder);
            Assert.AreEqual(expected, actual);

            // Half the values
            priceImpact /= 4;
            volumeLimit /= 4;
            model = new VolumeShareSlippageModel(volumeLimit, priceImpact);

            expected = _equity.Price * priceImpact * volumeLimit * volumeLimit;
            actual = model.GetSlippageApproximation(_equity, _equityBuyOrder);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VolumeShareSlippageModel_HighVolumeTest()
        {
            // These are the default values for the VolumeShareSlippageModel
            var priceImpact = 0.1m;
            var volumeLimit = 0.025m;
            var model = new VolumeShareSlippageModel();

            // High volume: volume > volumeLimit x order.Quantity
            var volume = 100;
            var volumeShare = _equityBuyOrder.Quantity / (decimal)volume;
            Assert.Greater(volume, volumeLimit * _equityBuyOrder.Quantity);
            _equity.SetMarketPrice(new TradeBar(DateTime.Now, Symbols.SPY, 100m, 100m, 100m, 100m, volume));

            var expected = _equity.Price * priceImpact * volumeShare * volumeShare;
            var actual = model.GetSlippageApproximation(_equity, _equityBuyOrder);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VolumeShareSlippageModel_ForexTest()
        {
            var model = new VolumeShareSlippageModel();

            // Since FX/CFD often have zero volume, the model returns zero slippage
            var expected = 0;
            var actual = model.GetSlippageApproximation(_forex, _forexBuyOrder);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void AlphaStreamsSlippageModel_EquityTest()
        {
            decimal slippagePercent = 0.0001m;

            var model = new AlphaStreamsSlippageModel();

            var expected = _equity.Price * slippagePercent;
            var actual = model.GetSlippageApproximation(_equity, _equityBuyOrder);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void AlphaStreamsSlippageModel_HardCodedEquityTest()
        {
            Symbol symbol = Symbol.Create("DGAZ", SecurityType.Equity, Market.USA);
            Equity equity = new Equity(
                symbol,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            equity.SetMarketPrice(new TradeBar(DateTime.Now, Symbols.SPY, 100m, 100m, 100m, 100m, 1));
            Order equityBuyOrder = new MarketOrder(symbol, 1, DateTime.Now);

            var model = new AlphaStreamsSlippageModel();

            var actual = model.GetSlippageApproximation(equity, equityBuyOrder);
            Assert.AreEqual(0.135m, actual);
        }

        [Test]
        public void AlphaStreamsSlippageModel_ForexTest()
        {
            var model = new AlphaStreamsSlippageModel();

            var expected = 0;
            var actual = model.GetSlippageApproximation(_forex, _forexBuyOrder);
            Assert.AreEqual(expected, actual);
        }
    }
}