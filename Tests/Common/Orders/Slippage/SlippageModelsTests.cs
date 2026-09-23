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
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Slippage;
using QuantConnect.Python;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using System;
using System.IO;

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
                new Cash("EUR", 0, 0),
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
        public void AlphaStreamsSlippageModel_ForexTest()
        {
            var model = new AlphaStreamsSlippageModel();

            var expected = 0;
            var actual = model.GetSlippageApproximation(_forex, _forexBuyOrder);
            Assert.AreEqual(expected, actual);
        }

        // Market on open orders fill at the bar open, so the slippage is referenced to it instead of the close.
        // Any other order type keeps using the last price. Ticks have no open so the price is used
        [TestCase(MarketDataType.TradeBar, OrderType.Market, 100)]
        [TestCase(MarketDataType.TradeBar, OrderType.MarketOnOpen, 90)]
        [TestCase(MarketDataType.TradeBar, OrderType.MarketOnClose, 100)]
        [TestCase(MarketDataType.TradeBar, OrderType.Limit, 100)]
        [TestCase(MarketDataType.QuoteBar, OrderType.Market, 100)]
        [TestCase(MarketDataType.QuoteBar, OrderType.MarketOnOpen, 90)]
        [TestCase(MarketDataType.Tick, OrderType.Market, 100)]
        [TestCase(MarketDataType.Tick, OrderType.MarketOnOpen, 100)]
        public void SlippageModelsReferenceTheOpenPriceForMarketOnOpenOrders(MarketDataType dataType, OrderType orderType, decimal expectedReferencePrice)
        {
            var security = CreateSecurityWithData(dataType);
            var order = CreateOrder(orderType, security.Symbol);

            Assert.AreEqual(expectedReferencePrice * 0.5m, new ConstantSlippageModel(0.5m).GetSlippageApproximation(security, order));

            if (security.Type == SecurityType.Equity)
            {
                Assert.AreEqual(expectedReferencePrice * 0.0001m, new AlphaStreamsSlippageModel().GetSlippageApproximation(security, order));
            }

            var volumeShareModel = new VolumeShareSlippageModel();
            if (dataType == MarketDataType.Tick)
            {
                Assert.Throws<InvalidOperationException>(() => volumeShareModel.GetSlippageApproximation(security, order));
            }
            else
            {
                // order quantity is 1 and the bar volume is 100, below the volume limit
                var volumeShare = 1m / 100m;
                Assert.AreEqual(expectedReferencePrice * volumeShare * volumeShare * 0.1m, volumeShareModel.GetSlippageApproximation(security, order));
            }
        }

        [TestCase(MarketDataType.TradeBar, OrderType.Market, 100)]
        [TestCase(MarketDataType.TradeBar, OrderType.MarketOnOpen, 90)]
        [TestCase(MarketDataType.QuoteBar, OrderType.Market, 100)]
        [TestCase(MarketDataType.QuoteBar, OrderType.MarketOnOpen, 90)]
        public void PythonVolumeShareSlippageModelReferencesTheOpenPriceForMarketOnOpenOrders(MarketDataType dataType, OrderType orderType, decimal expectedReferencePrice)
        {
            var security = CreateSecurityWithData(dataType);
            var order = CreateOrder(orderType, security.Symbol);

            ISlippageModel model;
            using (Py.GIL())
            {
                var module = PyModule.FromString("VolumeShareSlippageModelTest",
                    File.ReadAllText("../../../Common/Orders/Slippage/VolumeShareSlippageModel.py"));
                model = new SlippageModelPythonWrapper(module.GetAttr("VolumeShareSlippageModel").Invoke());
            }

            // order quantity is 1 and the bar volume is 100, below the volume limit
            var volumeShare = 1m / 100m;
            var expected = expectedReferencePrice * volumeShare * volumeShare * 0.1m;
            Assert.AreEqual((double)expected, (double)model.GetSlippageApproximation(security, order), 1e-12);
        }

        private static Order CreateOrder(OrderType orderType, Symbol symbol)
        {
            var time = new DateTime(2015, 6, 10, 9, 0, 0);
            return orderType switch
            {
                OrderType.Market => new MarketOrder(symbol, 1, time),
                OrderType.MarketOnOpen => new MarketOnOpenOrder(symbol, 1, time),
                OrderType.MarketOnClose => new MarketOnCloseOrder(symbol, 1, time),
                OrderType.Limit => new LimitOrder(symbol, 1, 100, time),
                _ => throw new ArgumentOutOfRangeException(nameof(orderType))
            };
        }

        /// <summary>
        /// Sets data whose open is 90 and close is 100 as the last data of a security and returns it.
        /// Quote bars are not the default data type for equities, so forex is used for them
        /// </summary>
        private Security CreateSecurityWithData(MarketDataType dataType)
        {
            var time = new DateTime(2015, 6, 10, 9, 30, 0);
            BaseData data;
            switch (dataType)
            {
                case MarketDataType.TradeBar:
                    data = new TradeBar(time, Symbols.SPY, 90m, 110m, 80m, 100m, 100);
                    break;
                case MarketDataType.QuoteBar:
                    data = new QuoteBar(time, Symbols.EURUSD, new Bar(89m, 109m, 79m, 99m), 100, new Bar(91m, 111m, 81m, 101m), 100);
                    break;
                case MarketDataType.Tick:
                    data = new Tick(time, Symbols.SPY, 100m, 100m) { TickType = TickType.Trade, Quantity = 100 };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType));
            }

            Security security = data.Symbol == Symbols.SPY ? _equity : _forex;
            security.SetMarketPrice(data);
            return security;
        }
    }
}
