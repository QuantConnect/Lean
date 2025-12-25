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
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.TimeInForces;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class TradovateBrokerageModelTests
    {
        private TradovateBrokerageModel _brokerageModel;

        [SetUp]
        public void SetUp()
        {
            _brokerageModel = new TradovateBrokerageModel();
        }

        #region Supported Order Types Tests

        [TestCase(OrderType.Market, true)]
        [TestCase(OrderType.Limit, true)]
        [TestCase(OrderType.StopMarket, true)]
        [TestCase(OrderType.StopLimit, true)]
        [TestCase(OrderType.TrailingStop, true)]
        [TestCase(OrderType.MarketOnOpen, false)]
        [TestCase(OrderType.MarketOnClose, false)]
        [TestCase(OrderType.LimitIfTouched, false)]
        public void CanSubmitOrder_ValidatesOrderType(OrderType orderType, bool shouldBeAllowed)
        {
            var security = CreateFutureSecurity();
            var order = CreateOrder(security.Symbol, orderType);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.AreEqual(shouldBeAllowed, canSubmit);
            if (!shouldBeAllowed)
            {
                Assert.IsNotNull(message);
                Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
            }
        }

        #endregion

        #region Supported Security Types Tests

        [TestCase(SecurityType.Future, true)]
        [TestCase(SecurityType.FutureOption, true)]
        [TestCase(SecurityType.Equity, false)]
        [TestCase(SecurityType.Option, false)]
        [TestCase(SecurityType.Forex, false)]
        [TestCase(SecurityType.Crypto, false)]
        public void CanSubmitOrder_ValidatesSecurityType(SecurityType securityType, bool shouldBeAllowed)
        {
            var security = CreateSecurity(securityType);
            var order = new MarketOrder(security.Symbol, 1, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.AreEqual(shouldBeAllowed, canSubmit);
            if (!shouldBeAllowed)
            {
                Assert.IsNotNull(message);
                Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
                StringAssert.Contains("security type", message.Message.ToLower());
            }
        }

        #endregion

        #region Time In Force Tests

        [Test]
        public void CanSubmitOrder_AllowsDayTimeInForce()
        {
            var security = CreateFutureSecurity();
            var order = new LimitOrder(security.Symbol, 1, 100m, DateTime.UtcNow)
            {
                Properties = { TimeInForce = TimeInForce.Day }
            };

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.IsTrue(canSubmit);
            Assert.IsNull(message);
        }

        [Test]
        public void CanSubmitOrder_AllowsGoodTilCanceledTimeInForce()
        {
            var security = CreateFutureSecurity();
            var order = new LimitOrder(security.Symbol, 1, 100m, DateTime.UtcNow)
            {
                Properties = { TimeInForce = TimeInForce.GoodTilCanceled }
            };

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.IsTrue(canSubmit);
            Assert.IsNull(message);
        }

        [Test]
        public void CanSubmitOrder_RejectsGoodTilDateTimeInForce()
        {
            var security = CreateFutureSecurity();
            var order = new LimitOrder(security.Symbol, 1, 100m, DateTime.UtcNow)
            {
                Properties = { TimeInForce = TimeInForce.GoodTilDate(DateTime.UtcNow.AddDays(7)) }
            };

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.IsFalse(canSubmit);
            Assert.IsNotNull(message);
            Assert.AreEqual(BrokerageMessageType.Warning, message.Type);
        }

        #endregion

        #region Order Update Tests

        [Test]
        public void CanUpdateOrder_AllowsUpdateForLimitOrder()
        {
            var security = CreateFutureSecurity();
            var order = new LimitOrder(security.Symbol, 1, 100m, DateTime.UtcNow);
            var request = new UpdateOrderRequest(DateTime.UtcNow, 1, new UpdateOrderFields { LimitPrice = 101m });

            var canUpdate = _brokerageModel.CanUpdateOrder(security, order, request, out var message);

            Assert.IsTrue(canUpdate);
            Assert.IsNull(message);
        }

        [Test]
        public void CanUpdateOrder_AllowsUpdateForStopOrder()
        {
            var security = CreateFutureSecurity();
            var order = new StopMarketOrder(security.Symbol, 1, 100m, DateTime.UtcNow);
            var request = new UpdateOrderRequest(DateTime.UtcNow, 1, new UpdateOrderFields { StopPrice = 99m });

            var canUpdate = _brokerageModel.CanUpdateOrder(security, order, request, out var message);

            Assert.IsTrue(canUpdate);
            Assert.IsNull(message);
        }

        [Test]
        public void CanUpdateOrder_AllowsQuantityUpdate()
        {
            var security = CreateFutureSecurity();
            var order = new LimitOrder(security.Symbol, 1, 100m, DateTime.UtcNow);
            var request = new UpdateOrderRequest(DateTime.UtcNow, 1, new UpdateOrderFields { Quantity = 2 });

            var canUpdate = _brokerageModel.CanUpdateOrder(security, order, request, out var message);

            Assert.IsTrue(canUpdate);
            Assert.IsNull(message);
        }

        #endregion

        #region Fee Model Tests

        [Test]
        public void GetFeeModel_ReturnsTradovateFeeModel()
        {
            var security = CreateFutureSecurity();

            var feeModel = _brokerageModel.GetFeeModel(security);

            Assert.IsInstanceOf<TradovateFeeModel>(feeModel);
        }

        #endregion

        #region Default Markets Tests

        [Test]
        public void DefaultMarkets_ContainsFutureWithCME()
        {
            var defaultMarkets = _brokerageModel.DefaultMarkets;

            Assert.IsTrue(defaultMarkets.ContainsKey(SecurityType.Future));
            Assert.AreEqual(Market.CME, defaultMarkets[SecurityType.Future]);
        }

        [Test]
        public void DefaultMarkets_ContainsFutureOptionWithCME()
        {
            var defaultMarkets = _brokerageModel.DefaultMarkets;

            Assert.IsTrue(defaultMarkets.ContainsKey(SecurityType.FutureOption));
            Assert.AreEqual(Market.CME, defaultMarkets[SecurityType.FutureOption]);
        }

        #endregion

        #region Account Type Tests

        [Test]
        public void Constructor_DefaultsToMarginAccount()
        {
            var model = new TradovateBrokerageModel();

            Assert.AreEqual(AccountType.Margin, model.AccountType);
        }

        [Test]
        public void Constructor_AcceptsAccountTypeParameter()
        {
            var model = new TradovateBrokerageModel(AccountType.Cash);

            Assert.AreEqual(AccountType.Cash, model.AccountType);
        }

        #endregion

        #region Trailing Stop Order Tests

        [Test]
        public void CanSubmitOrder_AllowsTrailingStopOrder()
        {
            var security = CreateFutureSecurity();
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100m, 100m));

            var order = new TrailingStopOrder(security.Symbol, 1, 95m, 5m, false, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.IsTrue(canSubmit);
            Assert.IsNull(message);
        }

        #endregion

        #region Helper Methods

        private static Future CreateFutureSecurity()
        {
            var symbol = Symbols.CreateFutureSymbol(Futures.Indices.Dow30EMini, new DateTime(2025, 3, 21));
            return new Future(
                symbol,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash("USD", 0, 1),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        private static Security CreateSecurity(SecurityType securityType)
        {
            Symbol symbol;
            switch (securityType)
            {
                case SecurityType.Future:
                    symbol = Symbols.CreateFutureSymbol(Futures.Indices.Dow30EMini, new DateTime(2025, 3, 21));
                    return new Future(
                        symbol,
                        SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                        new Cash("USD", 0, 1),
                        SymbolProperties.GetDefault("USD"),
                        ErrorCurrencyConverter.Instance,
                        RegisteredSecurityDataTypesProvider.Null,
                        new SecurityCache()
                    );

                case SecurityType.FutureOption:
                    var futureSymbol = Symbols.CreateFutureSymbol(Futures.Indices.Dow30EMini, new DateTime(2025, 3, 21));
                    symbol = Symbols.CreateFutureOptionSymbol(futureSymbol, OptionRight.Call, 40000m, new DateTime(2025, 3, 21));
                    var future = new Future(
                        futureSymbol,
                        SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                        new Cash("USD", 0, 1),
                        SymbolProperties.GetDefault("USD"),
                        ErrorCurrencyConverter.Instance,
                        RegisteredSecurityDataTypesProvider.Null,
                        new SecurityCache()
                    );
                    return new QuantConnect.Securities.FutureOption.FutureOption(
                        symbol,
                        SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                        new Cash("USD", 0, 1),
                        new OptionSymbolProperties(SymbolProperties.GetDefault("USD")),
                        ErrorCurrencyConverter.Instance,
                        RegisteredSecurityDataTypesProvider.Null,
                        new SecurityCache(),
                        future
                    );

                case SecurityType.Equity:
                    symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
                    break;

                case SecurityType.Option:
                    symbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 400m, new DateTime(2025, 3, 21));
                    break;

                case SecurityType.Forex:
                    symbol = Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda);
                    break;

                case SecurityType.Crypto:
                    symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase);
                    break;

                default:
                    symbol = Symbol.Create("TEST", securityType, Market.USA);
                    break;
            }

            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, false, true, false),
                new Cash("USD", 0, 1),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
        }

        private static Order CreateOrder(Symbol symbol, OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Market:
                    return new MarketOrder(symbol, 1, DateTime.UtcNow);
                case OrderType.Limit:
                    return new LimitOrder(symbol, 1, 100m, DateTime.UtcNow);
                case OrderType.StopMarket:
                    return new StopMarketOrder(symbol, 1, 100m, DateTime.UtcNow);
                case OrderType.StopLimit:
                    return new StopLimitOrder(symbol, 1, 100m, 99m, DateTime.UtcNow);
                case OrderType.TrailingStop:
                    return new TrailingStopOrder(symbol, 1, 95m, 5m, false, DateTime.UtcNow);
                case OrderType.MarketOnOpen:
                    return new MarketOnOpenOrder(symbol, 1, DateTime.UtcNow);
                case OrderType.MarketOnClose:
                    return new MarketOnCloseOrder(symbol, 1, DateTime.UtcNow);
                case OrderType.LimitIfTouched:
                    return new LimitIfTouchedOrder(symbol, 1, 100m, 99m, DateTime.UtcNow);
                default:
                    throw new ArgumentException($"Unsupported order type: {orderType}");
            }
        }

        #endregion
    }
}
