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
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Brokerages;

namespace QuantConnect.Tests.Common.Brokerages
{
    [TestFixture]
    public class ContingentOrdersBrokerageModelTests
    {
        private static readonly DateTime Time = new DateTime(2024, 1, 3, 15, 0, 0);
        private static readonly OrderFactory Factory = new QCAlgorithm().OrderFactory;

        private static IEnumerable<IBrokerageModel> NotSupportedBrokerageModels()
        {
            foreach (var type in typeof(DefaultBrokerageModel).Assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(DefaultBrokerageModel).IsAssignableFrom(type) || SupportedBrokerageModels.Contains(type))
                {
                    continue;
                }

                var constructor = type.GetConstructors().FirstOrDefault(c => c.GetParameters().All(p => p.IsOptional));
                if (constructor != null)
                {
                    yield return (IBrokerageModel)constructor.Invoke(constructor.GetParameters().Select(p => p.DefaultValue).ToArray());
                }
            }
        }

        private static readonly HashSet<Type> SupportedBrokerageModels = new()
        {
            typeof(DefaultBrokerageModel),
            typeof(AlphaStreamsBrokerageModel),
            typeof(InteractiveBrokersBrokerageModel),
            typeof(CharlesSchwabBrokerageModel),
            typeof(TradeStationBrokerageModel),
            typeof(AlpacaBrokerageModel),
            typeof(BinanceBrokerageModel),
            typeof(BinanceFuturesBrokerageModel),
            typeof(BinanceCoinFuturesBrokerageModel)
        };

        [TestCaseSource(nameof(NotSupportedBrokerageModels))]
        public void BrokerageModelsReject(IBrokerageModel model)
        {
            var bracket = CreateBracket(Symbols.SPY);
            foreach (var order in bracket)
            {
                Assert.IsFalse(model.CanSubmitOrder(GetSecurity(order.Symbol), order, out var message), model.GetType().Name);
                StringAssert.Contains("does not support contingent orders", message.Message);
            }
        }

        [Test]
        public void DefaultBrokerageModelSupportsEverything()
        {
            var model = new DefaultBrokerageModel();
            foreach (var order in CreateBracket(Symbols.SPY, ContingencyType.OneUpdatesOther).Concat(CreateChain()).Concat(CreateComboOneCancelsOther()))
            {
                Assert.IsTrue(model.CanSubmitOrder(GetSecurity(order.Symbol), order, out _));
            }
        }

        [Test]
        public void InteractiveBrokersSupportsEverything()
        {
            var model = new InteractiveBrokersBrokerageModel();
            foreach (var order in CreateBracket(Symbols.SPY, ContingencyType.OneUpdatesOther).Concat(CreateChain()).Concat(CreateOneCancelsOther(Symbols.SPY, Symbols.AAPL)))
            {
                Assert.IsTrue(model.CanSubmitOrder(GetSecurity(order.Symbol), order, out var message), message?.Message);
            }

            // but not through FIX
            var fixModel = new InteractiveBrokersFixModel();
            Assert.IsFalse(fixModel.CanSubmitOrder(GetSecurity(Symbols.SPY), CreateBracket(Symbols.SPY)[0], out _));
        }

        [TestCase(OrderType.Limit, true)]
        [TestCase(OrderType.StopLimit, true)]
        [TestCase(OrderType.Market, false)]
        [TestCase(OrderType.StopMarket, false)]
        public void InteractiveBrokersTrailingStopHasToBeTriggeredByALimitOrder(OrderType parentType, bool expected)
        {
            var parent = parentType switch
            {
                OrderType.Limit => Factory.LimitOrder(Symbols.SPY, 1, 100),
                OrderType.StopLimit => Factory.StopLimitOrder(Symbols.SPY, 1, 100, 101),
                OrderType.Market => Factory.MarketOrder(Symbols.SPY, 1),
                _ => Factory.StopMarketOrder(Symbols.SPY, 1, 100)
            };
            parent.Triggers(Factory.OneCancelsOther(Factory.LimitOrder(Symbols.SPY, -1, 110), Factory.TrailingStopOrder(Symbols.SPY, -1, 0.02m, true)));

            AssertCanSubmit(new InteractiveBrokersBrokerageModel(), ToOrders(parent), expected, "a trailing stop order can only be triggered by a limit or stop limit order");
        }

        [Test]
        public void CharlesSchwab()
        {
            var model = new CharlesSchwabBrokerageModel();
            AssertCanSubmit(model, CreateBracket(Symbols.SPY), true);
            AssertCanSubmit(model, CreateChain(), true);
            AssertCanSubmit(model, CreateOneCancelsOther(Symbols.SPY, Symbols.AAPL), true);
            AssertCanSubmit(model, CreateBracket(Symbols.SPY, ContingencyType.OneUpdatesOther), false, "OneUpdatesOther");

            // can't be updated
            var order = CreateBracket(Symbols.SPY)[1];
            Assert.IsFalse(model.CanUpdateOrder(GetSecurity(order.Symbol), order, new UpdateOrderRequest(Time, order.Id, new UpdateOrderFields { LimitPrice = 1 }), out var message));
            StringAssert.Contains("does not support updating contingent orders", message.Message);
            var plainOrder = new LimitOrder(Symbols.SPY, 1, 1, Time);
            Assert.IsTrue(model.CanUpdateOrder(GetSecurity(order.Symbol), plainOrder, new UpdateOrderRequest(Time, order.Id, new UpdateOrderFields { LimitPrice = 1 }), out _));
        }

        [Test]
        public void TradeStation()
        {
            var model = new TradeStationBrokerageModel();
            AssertCanSubmit(model, CreateBracket(Symbols.SPY), true);
            AssertCanSubmit(model, CreateBracket(Symbols.SPY, ContingencyType.OneUpdatesOther), true);
            AssertCanSubmit(model, CreateOneCancelsOther(Symbols.SPY, Symbols.AAPL), true);
            AssertCanSubmit(model, CreateOneCancelsOther(Symbols.SPY, Symbols.AAPL, ContingencyType.OneUpdatesOther), false, "same symbol");
            AssertCanSubmit(model, ToOrders(Factory.OneUpdatesOther(Factory.LimitOrder(Symbols.SPY, -1, 110), Factory.LimitOrder(Symbols.SPY, -1, 111))[0]),
                false, "require a stop order");
            AssertCanSubmit(model, CreateChain(), false, "can not trigger other orders in turn");
        }

        [Test]
        public void Alpaca()
        {
            var model = new AlpacaBrokerageModel();
            // bracket, oto & oco
            AssertCanSubmit(model, CreateBracket(Symbols.SPY), true);
            AssertCanSubmit(model, CreateOneTriggersOther(Symbols.SPY), true);
            AssertCanSubmit(model, CreateOneCancelsOther(Symbols.SPY, Symbols.SPY), true);

            AssertCanSubmit(model, CreateBracket(Symbols.SPY, ContingencyType.OneUpdatesOther), false, "OneUpdatesOther");
            AssertCanSubmit(model, CreateOneCancelsOther(Symbols.SPY, Symbols.AAPL), false, "same symbol");
            AssertCanSubmit(model, CreateChain(), false, "can not trigger other orders in turn");
            AssertCanSubmit(model, CreateBracket(Symbols.BTCUSD), false, "only equities");

            // more than 3 orders
            var entry = Factory.LimitOrder(Symbols.SPY, 1, 100).Bracket(110, 90).Triggers(Factory.LimitOrder(Symbols.SPY, -1, 120));
            AssertCanSubmit(model, ToOrders(entry), false, "maximum number of orders");

            // 3 members, not a bracket
            var members = Factory.OneCancelsOther(Factory.LimitOrder(Symbols.SPY, -1, 110), Factory.StopMarketOrder(Symbols.SPY, -1, 90), Factory.LimitOrder(Symbols.SPY, -1, 120));
            AssertCanSubmit(model, ToOrders(members[0]), false, "only supported as a bracket");

            // two limits
            members = Factory.OneCancelsOther(Factory.LimitOrder(Symbols.SPY, -1, 110), Factory.LimitOrder(Symbols.SPY, -1, 120));
            AssertCanSubmit(model, ToOrders(members[0]), false, "requires a limit order (take profit) and a stop");

            // different sides
            members = Factory.OneCancelsOther(Factory.LimitOrder(Symbols.SPY, -1, 110), Factory.StopMarketOrder(Symbols.SPY, 1, 90));
            AssertCanSubmit(model, ToOrders(members[0]), false, "same side");

            // market exit
            entry = Factory.LimitOrder(Symbols.SPY, 1, 100).Triggers(Factory.MarketOrder(Symbols.SPY, -1));
            AssertCanSubmit(model, ToOrders(entry), false, "exit orders have to be");

            // quantity can't be updated
            var exit = CreateBracket(Symbols.SPY)[1];
            Assert.IsTrue(model.CanUpdateOrder(GetSecurity(Symbols.SPY), exit, new UpdateOrderRequest(Time, exit.Id, new UpdateOrderFields { LimitPrice = 1 }), out _));
            Assert.IsFalse(model.CanUpdateOrder(GetSecurity(Symbols.SPY), exit, new UpdateOrderRequest(Time, exit.Id, new UpdateOrderFields { Quantity = -5 }), out var message));
            StringAssert.Contains("updating the quantity of contingent orders", message.Message);
        }

        [Test]
        public void Binance()
        {
            var model = new BinanceBrokerageModel();
            var symbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Binance);
            var future = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.Binance);

            // OTOCO, OTO & OCO. Stop market is not supported by binance spot
            AssertCanSubmit(model, CreateBracket(symbol, stopLimit: true), true);
            AssertCanSubmit(model, CreateOneTriggersOther(symbol), true);
            AssertCanSubmit(model, CreateOneCancelsOther(symbol, symbol, stopLimit: true), true);

            AssertCanSubmit(model, CreateBracket(symbol, ContingencyType.OneUpdatesOther, stopLimit: true), false, "OneUpdatesOther");
            AssertCanSubmit(model, CreateBracket(future, stopLimit: true), false, "only spot crypto");

            // the working order has to be a limit order
            var parent = Factory.MarketOrder(symbol, 1).Triggers(Factory.LimitOrder(symbol, -1, 110000));
            Assert.IsFalse(model.CanSubmitOrder(GetSecurity(symbol), ToOrders(parent)[0], out var message));
            StringAssert.Contains("has to be a single limit order", message.Message);
        }

        private static void AssertCanSubmit(IBrokerageModel model, List<Order> orders, bool expected, string expectedMessage = null)
        {
            var results = orders.Select(order =>
            {
                var result = model.CanSubmitOrder(GetSecurity(order.Symbol), order, out var message);
                return (result, message);
            }).ToList();

            if (expected)
            {
                Assert.IsTrue(results.All(x => x.result), $"{model.GetType().Name}: {results.FirstOrDefault(x => !x.result).message?.Message}");
            }
            else
            {
                var failed = results.Where(x => !x.result).ToList();
                Assert.IsNotEmpty(failed, model.GetType().Name);
                Assert.IsTrue(failed.Any(x => x.message.Message.Contains(expectedMessage, StringComparison.InvariantCulture)), failed[0].message.Message);
            }
        }

        /// <summary>
        /// The orders of the whole set of contingent orders the request belongs to, as the brokerage model gets them
        /// </summary>
        private static List<Order> ToOrders(SubmitOrderRequest request)
        {
            var orders = new List<Order>();
            foreach (var member in request.Contingency.Requests)
            {
                member.SetOrderId(orders.Count + 1);
                orders.Add(Order.CreateOrder(member));
            }
            return orders;
        }

        private static List<Order> CreateBracket(Symbol symbol, ContingencyType exitsContingencyType = ContingencyType.OneCancelsOther, bool stopLimit = false)
        {
            var entry = Factory.LimitOrder(symbol, 1, stopLimit ? 100000 : 100)
                .Bracket(stopLimit ? 110000 : 110, stopLimit ? 90000 : 90, stopLimit ? 89000 : null, exitsContingencyType);
            return ToOrders(entry);
        }

        private static List<Order> CreateOneTriggersOther(Symbol symbol)
        {
            return ToOrders(Factory.LimitOrder(symbol, 1, 100).Triggers(Factory.LimitOrder(symbol, -1, 110)));
        }

        private static List<Order> CreateOneCancelsOther(Symbol first, Symbol second, ContingencyType type = ContingencyType.OneCancelsOther, bool stopLimit = false)
        {
            var takeProfit = Factory.LimitOrder(first, -1, stopLimit ? 110000 : 110);
            var stopLoss = stopLimit ? Factory.StopLimitOrder(second, -1, 90000, 89000) : Factory.StopMarketOrder(second, -1, 90);
            var members = type == ContingencyType.OneUpdatesOther ? Factory.OneUpdatesOther(takeProfit, stopLoss) : Factory.OneCancelsOther(takeProfit, stopLoss);
            return ToOrders(members[0]);
        }

        /// <summary>
        /// An order which triggers another which triggers another in turn
        /// </summary>
        private static List<Order> CreateChain()
        {
            var last = Factory.LimitOrder(Symbols.SPY, 1, 100);
            var middle = Factory.LimitOrder(Symbols.SPY, -1, 110).Triggers(last);
            return ToOrders(Factory.LimitOrder(Symbols.SPY, 1, 100).Triggers(middle));
        }

        private static List<Order> CreateComboOneCancelsOther()
        {
            var combo = Factory.ComboMarketOrder(new List<Leg> { Leg.Create(Symbols.SPY, 1) }, 1);
            return ToOrders(Factory.OneCancelsOther(combo.Concat(new[] { Factory.LimitOrder(Symbols.SPY, 1, 100) }))[0]);
        }

        private static Security GetSecurity(Symbol symbol)
        {
            var isCrypto = symbol.SecurityType == SecurityType.Crypto || symbol.SecurityType == SecurityType.CryptoFuture;
            var security = TestsHelpers.GetSecurity(symbol: symbol.Value, securityType: symbol.SecurityType, market: symbol.ID.Market,
                quoteCurrency: symbol.Value.EndsWith("USDT", StringComparison.InvariantCulture) ? "USDT" : "USD");
            var price = isCrypto ? 100000 : 100;
            security.SetMarketPrice(new Tick(Time, symbol, price, price));
            return security;
        }
    }
}
