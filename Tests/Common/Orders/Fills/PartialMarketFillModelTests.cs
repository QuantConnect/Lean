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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine;

namespace QuantConnect.Tests.Common.Orders.Fills
{
    [TestFixture, Ignore]
    public class PartialMarketFillModelTests
    {
        [Test]
        public void CreatesSpecificNumberOfFills()
        {
            Security security;
            MarketOrder order;
            OrderTicket ticket;
            PartialMarketFillModel model;
            BasicTemplateAlgorithm algorithm;
            var referenceTimeUtc = InitializeTest(out algorithm, out security, out model, out order, out ticket);

            algorithm.SetDateTime(referenceTimeUtc.AddSeconds(1));

            var fill1 = model.MarketFill(security, order);
            ticket.AddOrderEvent(fill1);
            Assert.AreEqual(order.Quantity / 2, fill1.FillQuantity);
            Assert.AreEqual(OrderStatus.PartiallyFilled, fill1.Status);

            algorithm.SetDateTime(referenceTimeUtc.AddSeconds(2));

            var fill2 = model.MarketFill(security, order);
            ticket.AddOrderEvent(fill2);
            Assert.AreEqual(order.Quantity / 2, fill2.FillQuantity);
            Assert.AreEqual(OrderStatus.Filled, fill2.Status);
        }

        [Test]
        public void RequiresAdvancingTime()
        {
            Security security;
            MarketOrder order;
            OrderTicket ticket;
            PartialMarketFillModel model;
            BasicTemplateAlgorithm algorithm;
            var referenceTimeUtc = InitializeTest(out algorithm, out security, out model, out order, out ticket);

            var fill1 = model.MarketFill(security, order);
            ticket.AddOrderEvent(fill1);
            Assert.AreEqual(order.Quantity / 2, fill1.FillQuantity);
            Assert.AreEqual(OrderStatus.PartiallyFilled, fill1.Status);

            var fill2 = model.MarketFill(security, order);
            ticket.AddOrderEvent(fill2);
            Assert.AreEqual(0, fill2.FillQuantity);
            Assert.AreEqual(OrderStatus.None, fill2.Status);

            algorithm.SetDateTime(referenceTimeUtc.AddSeconds(1));

            var fill3 = model.MarketFill(security, order);
            ticket.AddOrderEvent(fill3);
            Assert.AreEqual(order.Quantity / 2, fill3.FillQuantity);
            Assert.AreEqual(OrderStatus.Filled, fill3.Status);
        }

        private static DateTime InitializeTest(out BasicTemplateAlgorithm algorithm, out Security security, out PartialMarketFillModel model, out MarketOrder order, out OrderTicket ticket)
        {
            var referenceTimeNY = new DateTime(2015, 12, 21, 13, 0, 0);
            var referenceTimeUtc = referenceTimeNY.ConvertToUtc(TimeZones.NewYork);
            algorithm = new BasicTemplateAlgorithm();
            algorithm.SetDateTime(referenceTimeUtc);

            var transactionHandler = new BacktestingTransactionHandler();
            transactionHandler.Initialize(algorithm, new BacktestingBrokerage(algorithm), new TestResultHandler(Console.WriteLine));

            algorithm.Transactions.SetOrderProcessor(transactionHandler);

            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Second, TimeZones.NewYork, TimeZones.NewYork, false, false, false);
            security = new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );

            model = new PartialMarketFillModel(algorithm.Transactions, 2);

            algorithm.Securities.Add(security);
            algorithm.Securities[Symbols.SPY].FillModel = model;
            security.SetMarketPrice(new Tick { Symbol = Symbols.SPY, Value = 100 });
            algorithm.SetFinishedWarmingUp();

            order = new MarketOrder(Symbols.SPY, 100, referenceTimeUtc) { Id = 1 };

            var request = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, order.Quantity, 0, 0, algorithm.UtcTime, null);
            ticket = algorithm.Transactions.ProcessRequest(request);
            return referenceTimeUtc;
        }

        // Provides a test implementation that forces partial market order fills


        /// <summary>
        /// Provides an implementation of <see cref="IFillModel"/> that creates a specific
        /// number of partial fills for marke orders only. All other order types reuse the
        /// <see cref="ImmediateFillModel"/> behavior. This model will emit one partial fill
        /// per time step.
        /// NOTE: If the desired number of fills is very large, then a few more fills may be issued
        /// due to rounding errors. This model does not hold internal state regarding orders/previous
        /// fills.
        /// </summary>
        public class PartialMarketFillModel : ImmediateFillModel
        {
            private readonly decimal _percent;
            private readonly IOrderProvider _orderProvider;

            /// <summary>
            /// Initializes a new instance of the <see cref="PartialMarketFillModel"/> class
            /// </summary>
            /// <code>
            /// // split market orders into two fills
            /// Securities["SPY"].FillModel = new PartialMarketFillModel(Transactions, 2);
            /// </code>
            /// <param name="orderProvider">The order provider used for getting order tickets</param>
            /// <param name="numberOfFills"></param>
            public PartialMarketFillModel(IOrderProvider orderProvider, int numberOfFills = 1)
            {
                _orderProvider = orderProvider;
                _percent = 1m / numberOfFills;
            }

            /// <summary>
            /// Performs partial market fills once per time step
            /// </summary>
            /// <param name="asset">The security being ordered</param>
            /// <param name="order">The order</param>
            /// <returns>The order fill</returns>
            public override OrderEvent MarketFill(Security asset, MarketOrder order)
            {
                var currentUtcTime = asset.LocalTime.ConvertToUtc(asset.Exchange.TimeZone);

                var ticket = _orderProvider.GetOrderTickets(x => x.OrderId == order.Id).FirstOrDefault();
                if (ticket == null)
                {
                    // if we can't find the ticket issue empty fills
                    return new OrderEvent(order, currentUtcTime, OrderFee.Zero);
                }

                // make sure some time has passed
                var lastOrderEvent = ticket.OrderEvents.LastOrDefault();
                var increment = TimeSpan.FromTicks(Math.Max(asset.Resolution.ToTimeSpan().Ticks, 1));
                if (lastOrderEvent != null && currentUtcTime - lastOrderEvent.UtcTime < increment)
                {
                    // wait a minute between fills
                    return new OrderEvent(order, currentUtcTime, OrderFee.Zero);
                }

                var remaining = (int)(ticket.Quantity - ticket.QuantityFilled);
                var fill = base.MarketFill(asset, order);
                var filledThisTime = Math.Min(remaining, (int)(_percent * order.Quantity));
                fill.FillQuantity = filledThisTime;

                // only mark it as filled if there is zero quantity remaining
                fill.Status = remaining == filledThisTime
                    ? OrderStatus.Filled
                    : OrderStatus.PartiallyFilled;

                return fill;
            }
        }
    }
}
