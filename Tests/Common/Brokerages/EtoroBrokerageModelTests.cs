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
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class EtoroBrokerageModelTests
    {
        private readonly EtoroBrokerageModel _brokerageModel = new();

        [TestCase(SecurityType.Equity)]
        [TestCase(SecurityType.Crypto)]
        [TestCase(SecurityType.Forex)]
        public void CanSubmitOrderMarketOrderReturnsTrue(SecurityType securityType)
        {
            var security = GetSecurityForType(securityType);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.True);
            Assert.That(message, Is.Null);
        }

        [TestCase(SecurityType.Cfd)]
        [TestCase(SecurityType.Option)]
        [TestCase(SecurityType.Future)]
        [TestCase(SecurityType.Index)]
        public void CanSubmitOrderUnsupportedSecurityTypeReturnsFalse(SecurityType securityType)
        {
            var security = GetSecurityForType(securityType);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        [TestCase(OrderType.Limit)]
        [TestCase(OrderType.StopMarket)]
        [TestCase(OrderType.StopLimit)]
        [TestCase(OrderType.MarketOnOpen)]
        [TestCase(OrderType.MarketOnClose)]
        [TestCase(OrderType.TrailingStop)]
        public void CanSubmitOrderUnsupportedOrderTypeReturnsFalse(OrderType orderType)
        {
            var security = GetSecurityForType(SecurityType.Equity);
            var order = CreateOrder(orderType, security.Symbol);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        // A sell with no holdings opens a short position, which eToro accepts only with a stop loss: a stop market order it triggers.
        [TestCase(false, false)]
        [TestCase(true, true)]
        public void CanSubmitOrderShortSaleRequiresStopLoss(bool triggersStopLoss, bool expectedCanSubmit)
        {
            var security = GetSecurityForType(SecurityType.Equity);
            var order = new MarketOrder(security.Symbol, -1m, DateTime.UtcNow);
            if (triggersStopLoss)
            {
                OrderContingency.Trigger([order], [new StopMarketOrder(security.Symbol, 1m, 110m, DateTime.UtcNow)]);
            }

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.EqualTo(expectedCanSubmit));
            if (expectedCanSubmit)
            {
                Assert.That(message, Is.Null);
            }
            else
            {
                Assert.That(message.Message, Does.Contain("stop market order"));
            }
        }

        // A sell within the long holdings closes units and needs no stop loss rate; a sell beyond them crosses zero and is refused.
        [TestCase(2, -1, true)]
        [TestCase(2, -2, true)]
        [TestCase(1, -2, false)]
        public void CanSubmitOrderSellAgainstLongHoldingsRefusesCrossZero(decimal holdingQuantity, decimal orderQuantity, bool expectedCanSubmit)
        {
            var equity = Symbols.AAPL;
            var security = TestsHelpers.InitializeSecurity(SecurityType.Equity, (equity, 209m, holdingQuantity))[equity];
            var order = new MarketOrder(equity, orderQuantity, DateTime.UtcNow);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.EqualTo(expectedCanSubmit));
            Assert.That(message, expectedCanSubmit ? Is.Null : Is.Not.Null);
        }

        [Test]
        public void CanUpdateOrderReturnsFalse()
        {
            var security = GetSecurityForType(SecurityType.Equity);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);
            var request = new UpdateOrderRequest(DateTime.UtcNow, order.Id, new UpdateOrderFields { Quantity = 2m });

            var canUpdate = _brokerageModel.CanUpdateOrder(security, order, request, out var message);

            Assert.That(canUpdate, Is.False);
            Assert.That(message, Is.Not.Null);
        }

        // An exit order moves the stop loss or the take profit of the position; its quantity is the position's
        [TestCase(true, false, true, Description = "A new stop price")]
        [TestCase(true, true, false, Description = "A new quantity")]
        [TestCase(false, false, true, Description = "A new stop price on an exit read from the position after a restart, without its entry")]
        public void CanUpdateOrderExitOrderAllowsThePriceOnly(bool hasEntry, bool changesQuantity, bool expectedCanUpdate)
        {
            var security = GetSecurityForType(SecurityType.Equity);
            var stopLoss = new StopMarketOrder(security.Symbol, -1m, 90m, DateTime.UtcNow);
            if (hasEntry)
            {
                OrderContingency.Trigger([new MarketOrder(security.Symbol, 1m, DateTime.UtcNow)], [stopLoss]);
            }
            var fields = changesQuantity ? new UpdateOrderFields { Quantity = -2m } : new UpdateOrderFields { StopPrice = 95m };
            var request = new UpdateOrderRequest(DateTime.UtcNow, stopLoss.Id, fields);

            var canUpdate = _brokerageModel.CanUpdateOrder(security, stopLoss, request, out var message);

            Assert.That(canUpdate, Is.EqualTo(expectedCanUpdate));
            Assert.That(message, expectedCanUpdate ? Is.Null : Is.Not.Null);
        }

        [TestCase(0.05, false, Description = "A crypto stop loss 5% under the price is inside the 10% eToro keeps")]
        [TestCase(0.1, true, Description = "A crypto stop loss 10% under the price")]
        [TestCase(0.2, true, Description = "A crypto stop loss 20% under the price")]
        public void CanSubmitOrderCryptoStopLossKeepsTheMinimumDistance(decimal distance, bool expectedCanSubmit)
        {
            var security = GetSecurityForType(SecurityType.Crypto);
            security.SetMarketPrice(new Tick { Symbol = security.Symbol, Value = 100m });
            var entry = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);
            var stopLoss = new StopMarketOrder(security.Symbol, -1m, 100m * (1 - distance), DateTime.UtcNow);
            OrderContingency.Trigger([entry], [stopLoss]);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, stopLoss, out var message);

            Assert.That(canSubmit, Is.EqualTo(expectedCanSubmit));
            if (!expectedCanSubmit)
            {
                Assert.That(message.Message, Does.Contain("10%"));
            }
        }

        [TestCase(0.05, false, Description = "A crypto stop loss moved to 5% under the price is inside the 10% eToro keeps")]
        [TestCase(0.2, true, Description = "A crypto stop loss moved to 20% under the price")]
        public void CanUpdateOrderCryptoStopLossKeepsTheMinimumDistance(decimal distance, bool expectedCanUpdate)
        {
            var security = GetSecurityForType(SecurityType.Crypto);
            security.SetMarketPrice(new Tick { Symbol = security.Symbol, Value = 100m });
            var entry = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);
            var stopLoss = new StopMarketOrder(security.Symbol, -1m, 70m, DateTime.UtcNow);
            OrderContingency.Trigger([entry], [stopLoss]);
            var request = new UpdateOrderRequest(DateTime.UtcNow, stopLoss.Id, new UpdateOrderFields { StopPrice = 100m * (1 - distance) });

            var canUpdate = _brokerageModel.CanUpdateOrder(security, stopLoss, request, out var message);

            Assert.That(canUpdate, Is.EqualTo(expectedCanUpdate));
            Assert.That(message, expectedCanUpdate ? Is.Null : Is.Not.Null);
        }

        // eToro refuses a rate on the wrong side of the price, where Lean's fill model would fill the exit at once
        [TestCase(OrderType.StopMarket, -1, 90, true, Description = "The stop loss of a long position below the price")]
        [TestCase(OrderType.StopMarket, -1, 120, false, Description = "The stop loss of a long position above the price")]
        [TestCase(OrderType.StopMarket, 1, 110, true, Description = "The stop loss of a short position above the price")]
        [TestCase(OrderType.StopMarket, 1, 80, false, Description = "The stop loss of a short position below the price")]
        [TestCase(OrderType.Limit, -1, 110, true, Description = "The take profit of a long position above the price")]
        [TestCase(OrderType.Limit, -1, 90, false, Description = "The take profit of a long position below the price")]
        [TestCase(OrderType.Limit, 1, 90, true, Description = "The take profit of a short position below the price")]
        [TestCase(OrderType.Limit, 1, 110, false, Description = "The take profit of a short position above the price")]
        public void CanSubmitOrderExitPriceHasToBeOnItsSideOfThePrice(OrderType exitType, decimal exitQuantity, decimal exitPrice, bool expectedCanSubmit)
        {
            var security = GetSecurityForType(SecurityType.Equity);
            security.SetMarketPrice(new Tick { Symbol = security.Symbol, Value = 100m });
            var exit = CreateExitOrder(exitType, security.Symbol, exitQuantity, exitPrice);
            OrderContingency.Trigger([new MarketOrder(security.Symbol, -exitQuantity, DateTime.UtcNow)], [exit]);

            var canSubmit = _brokerageModel.CanSubmitOrder(security, exit, out var message);

            Assert.That(canSubmit, Is.EqualTo(expectedCanSubmit));
            if (!expectedCanSubmit)
            {
                Assert.That(message.Message, Does.Contain("wrong side"));
            }
        }

        // The exit comes without its entry, as read from the position after a restart; the side of its new price is checked all the same
        [TestCase(OrderType.StopMarket, -1, 90, true, Description = "The stop loss of a long position moved below the price")]
        [TestCase(OrderType.StopMarket, -1, 120, false, Description = "The stop loss of a long position moved above the price")]
        [TestCase(OrderType.StopMarket, 1, 110, true, Description = "The stop loss of a short position moved above the price")]
        [TestCase(OrderType.StopMarket, 1, 80, false, Description = "The stop loss of a short position moved below the price")]
        [TestCase(OrderType.Limit, -1, 110, true, Description = "The take profit of a long position moved above the price")]
        [TestCase(OrderType.Limit, -1, 90, false, Description = "The take profit of a long position moved below the price")]
        [TestCase(OrderType.Limit, 1, 90, true, Description = "The take profit of a short position moved below the price")]
        [TestCase(OrderType.Limit, 1, 110, false, Description = "The take profit of a short position moved above the price")]
        public void CanUpdateOrderExitPriceHasToBeOnItsSideOfThePrice(OrderType exitType, decimal exitQuantity, decimal newPrice, bool expectedCanUpdate)
        {
            var security = GetSecurityForType(SecurityType.Equity);
            security.SetMarketPrice(new Tick { Symbol = security.Symbol, Value = 100m });
            var exit = CreateExitOrder(exitType, security.Symbol, exitQuantity, 100m);
            var fields = exitType == OrderType.StopMarket ? new UpdateOrderFields { StopPrice = newPrice } : new UpdateOrderFields { LimitPrice = newPrice };
            var request = new UpdateOrderRequest(DateTime.UtcNow, exit.Id, fields);

            var canUpdate = _brokerageModel.CanUpdateOrder(security, exit, request, out var message);

            Assert.That(canUpdate, Is.EqualTo(expectedCanUpdate));
            if (!expectedCanUpdate)
            {
                Assert.That(message.Message, Does.Contain("wrong side"));
            }
        }

        [Test]
        public void GetFeeModelReturnsEtoroFeeModel()
        {
            var security = GetSecurityForType(SecurityType.Equity);

            Assert.That(_brokerageModel.GetFeeModel(security), Is.InstanceOf<EtoroFeeModel>());
        }

        [TestCase(1, false, true)]
        [TestCase(2, false, false)]
        [TestCase(2, true, true)]
        public void CanSubmitOrderLeverageAboveOneRequiresStopLoss(decimal leverage, bool triggersStopLoss, bool expectedCanSubmit)
        {
            var security = GetSecurityForType(SecurityType.Equity);
            security.SetLeverage(leverage);
            var order = new MarketOrder(security.Symbol, 1m, DateTime.UtcNow);
            if (triggersStopLoss)
            {
                OrderContingency.Trigger([order], [new StopMarketOrder(security.Symbol, -1m, 90m, DateTime.UtcNow)]);
            }

            var canSubmit = _brokerageModel.CanSubmitOrder(security, order, out var message);

            Assert.That(canSubmit, Is.EqualTo(expectedCanSubmit));
            if (expectedCanSubmit)
            {
                Assert.That(message, Is.Null);
            }
            else
            {
                Assert.That(message.Message, Does.Contain("stop market order"));
            }
        }

        [TestCase(AccountType.Cash, SecurityType.Equity, 1)]
        [TestCase(AccountType.Cash, SecurityType.Crypto, 1)]
        [TestCase(AccountType.Cash, SecurityType.Forex, 1)]
        [TestCase(AccountType.Margin, SecurityType.Equity, 2)]
        [TestCase(AccountType.Margin, SecurityType.Crypto, 1)]
        [TestCase(AccountType.Margin, SecurityType.Forex, 30, Description = "The highest leverage eToro offers on a currency pair, under Lean's default of 50")]
        public void GetLeverageReturnsTheDefaultOfTheAccountType(AccountType accountType, SecurityType securityType, decimal expectedLeverage)
        {
            var brokerageModel = new EtoroBrokerageModel(accountType);
            var security = GetSecurityForType(securityType);

            Assert.That(brokerageModel.GetLeverage(security), Is.EqualTo(expectedLeverage));
        }

        [Test]
        public void ConstructorWithoutAccountTypeUsesCash()
        {
            Assert.That(new EtoroBrokerageModel().AccountType, Is.EqualTo(AccountType.Cash));
        }

        [TestCase("stop loss", 1, true, Description = "A market entry which triggers a stop market stop loss")]
        [TestCase("take profit", 1, true, Description = "A market entry which triggers a limit take profit")]
        [TestCase("bracket", 1, true, Description = "A market entry which triggers a take profit and a stop loss, one cancels the other")]
        [TestCase("short stop loss", 1, true, Description = "A short sale which triggers a stop loss")]
        [TestCase("short take profit", 1, false, Description = "A short sale needs a stop loss")]
        [TestCase("stop loss", 2, true, Description = "A leveraged entry which triggers a stop loss")]
        [TestCase("take profit", 2, false, Description = "A leveraged entry needs a stop loss")]
        [TestCase("exits only", 1, false, Description = "The exits have to be triggered by an entry")]
        [TestCase("same side exit", 1, false, Description = "The exits are on the opposite side of the entry")]
        [TestCase("stop loss on the entry side", 1, false, Description = "A bracket whose stop loss buys with the entry: eToro's close at the stop would fill a buy")]
        [TestCase("take profit on the entry side", 1, false, Description = "A bracket whose take profit buys with the entry")]
        [TestCase("short stop loss on the entry side", 1, false, Description = "A short bracket whose stop loss sells with the entry")]
        [TestCase("half exit", 1, false, Description = "The exits are for the quantity of the entry, the whole position closes at one of them")]
        [TestCase("limit entry", 1, false, Description = "The entry has to be a market order")]
        [TestCase("market exit", 1, false, Description = "An exit has to be a stop market or a limit order")]
        [TestCase("two stop losses", 1, false, Description = "3 orders are only supported as a bracket")]
        public void CanSubmitOrderContingentOrders(string shape, decimal leverage, bool expectedCanSubmit)
        {
            var security = GetSecurityForType(SecurityType.Equity);
            security.SetLeverage(leverage);
            var orders = CreateContingentOrders(shape, security.Symbol);

            // the transaction handler submits the set only when every order of it can be submitted
            var canSubmit = orders.All(order => _brokerageModel.CanSubmitOrder(security, order, out _));

            Assert.That(canSubmit, Is.EqualTo(expectedCanSubmit));
        }

        // The exits close the position the entry opens, so the 2 shares held now do not make a 5 share exit cross zero
        [Test]
        public void CanSubmitOrderContingentOrdersExitsBeyondTheHoldings()
        {
            var equity = Symbols.AAPL;
            var security = TestsHelpers.InitializeSecurity(SecurityType.Equity, (equity, 209m, 2m))[equity];
            var entry = new MarketOrder(equity, 5m, DateTime.UtcNow);
            var exits = new List<Order> { new LimitOrder(equity, -5m, 230m, DateTime.UtcNow), new StopMarketOrder(equity, -5m, 190m, DateTime.UtcNow) };
            OrderContingency.Trigger([entry], exits);
            OrderContingency.Relate(ContingencyType.OneCancelsOther, exits);

            var canSubmit = exits.Prepend(entry).All(order => _brokerageModel.CanSubmitOrder(security, order, out _));

            Assert.That(canSubmit, Is.True);
        }

        private static List<Order> CreateContingentOrders(string shape, Symbol symbol)
        {
            var time = DateTime.UtcNow;
            var entryQuantity = shape.StartsWith("short") ? -2m : 2m;
            var exitQuantity = shape == "half exit" ? -entryQuantity / 2 : -entryQuantity;
            var stopLossQuantity = shape is "same side exit" or "stop loss on the entry side" or "short stop loss on the entry side" ? entryQuantity : exitQuantity;
            var takeProfitQuantity = shape is "same side exit" or "take profit on the entry side" ? entryQuantity : exitQuantity;
            Order entry = shape == "limit entry" ? new LimitOrder(symbol, entryQuantity, 100m, time) : new MarketOrder(symbol, entryQuantity, time);
            var stopLoss = new StopMarketOrder(symbol, stopLossQuantity, 90m, time);
            var takeProfit = new LimitOrder(symbol, takeProfitQuantity, 110m, time);

            List<Order> exits = shape switch
            {
                "take profit" or "short take profit" => [takeProfit],
                "bracket" or "exits only" or "stop loss on the entry side" or "take profit on the entry side" or "short stop loss on the entry side" => [takeProfit, stopLoss],
                "market exit" => [new MarketOrder(symbol, exitQuantity, time)],
                "two stop losses" => [stopLoss, new StopMarketOrder(symbol, exitQuantity, 80m, time)],
                _ => [stopLoss]
            };

            if (shape != "exits only")
            {
                OrderContingency.Trigger([entry], exits);
            }
            if (exits.Count == 2)
            {
                OrderContingency.Relate(ContingencyType.OneCancelsOther, exits);
            }
            return shape == "exits only" ? exits : [entry, .. exits];
        }

        private static Order CreateExitOrder(OrderType exitType, Symbol symbol, decimal quantity, decimal price)
        {
            switch (exitType)
            {
                case OrderType.StopMarket:
                    return new StopMarketOrder(symbol, quantity, price, DateTime.UtcNow);
                case OrderType.Limit:
                    return new LimitOrder(symbol, quantity, price, DateTime.UtcNow);
                default:
                    throw new ArgumentOutOfRangeException(nameof(exitType), exitType, null);
            }
        }

        private static Security GetSecurityForType(SecurityType securityType)
        {
            switch (securityType)
            {
                case SecurityType.Future:
                    return TestsHelpers.GetSecurity(securityType: SecurityType.Future,
                        symbol: Futures.Indices.SP500EMini, market: Market.CME);
                case SecurityType.Crypto:
                    return TestsHelpers.GetSecurity(securityType: SecurityType.Crypto,
                        symbol: "BTCUSD", market: Market.Coinbase);
                case SecurityType.Forex:
                case SecurityType.Cfd:
                    return TestsHelpers.GetSecurity(securityType: securityType,
                        symbol: "EURUSD", market: Market.Oanda);
                case SecurityType.Index:
                    return TestsHelpers.GetSecurity(securityType: SecurityType.Index,
                        symbol: "SPX", market: Market.USA);
                default:
                    return TestsHelpers.GetSecurity(securityType: securityType,
                        symbol: "AAPL", market: Market.USA);
            }
        }

        private static Order CreateOrder(OrderType orderType, Symbol symbol)
        {
            switch (orderType)
            {
                case OrderType.Limit:
                    return new LimitOrder(symbol, 1m, 100m, DateTime.UtcNow);
                case OrderType.StopMarket:
                    return new StopMarketOrder(symbol, 1m, 100m, DateTime.UtcNow);
                case OrderType.StopLimit:
                    return new StopLimitOrder(symbol, 1m, 105m, 100m, DateTime.UtcNow);
                case OrderType.MarketOnClose:
                    return new MarketOnCloseOrder(symbol, 1m, DateTime.UtcNow);
                case OrderType.MarketOnOpen:
                    return new MarketOnOpenOrder(symbol, 1m, DateTime.UtcNow);
                case OrderType.TrailingStop:
                    return new TrailingStopOrder(symbol, 1m, 100m, 1m, false, DateTime.UtcNow);
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderType), orderType, null);
            }
        }
    }
}
