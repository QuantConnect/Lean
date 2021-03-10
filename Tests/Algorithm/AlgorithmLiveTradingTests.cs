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
using System.Threading;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class AlgorithmLiveTradingTests
    {
        [Test]
        public void SetHoldingsTakesIntoAccountPendingMarketOrders()
        {
            var algorithm = new QCAlgorithm();
            algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(algorithm));
            var security = algorithm.AddEquity("SPY");
            security.Exchange = new SecurityExchange(SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork));
            security.SetMarketPrice(new Tick { Value = 270m });
            algorithm.SetFinishedWarmingUp();

            var brokerage = new NullBrokerage();
            var transactionHandler = new BrokerageTransactionHandler();

            transactionHandler.Initialize(algorithm, brokerage, new LiveTradingResultHandler());
            Thread.Sleep(250);
            algorithm.Transactions.SetOrderProcessor(transactionHandler);

            var symbol = security.Symbol;

            // this order should timeout (no fills received within 5 seconds)
            algorithm.SetHoldings(symbol, 1m);
            Thread.Sleep(2000);

            var openOrders = algorithm.Transactions.GetOpenOrders();
            Assert.AreEqual(1, openOrders.Count);

            // this order should never be submitted because of the pending order
            algorithm.SetHoldings(symbol, 1m);
            Thread.Sleep(2000);

            openOrders = algorithm.Transactions.GetOpenOrders();
            Assert.AreEqual(1, openOrders.Count);

            transactionHandler.Exit();
        }

        private class NullBrokerage : IBrokerage
        {
            public void Dispose() {}
            public event EventHandler<OrderEvent> OrderStatusChanged;
            public event EventHandler<OrderEvent> OptionPositionAssigned;
            public event EventHandler<AccountEvent> AccountChanged;
            public event EventHandler<BrokerageMessageEvent> Message;
            public string Name => "NullBrokerage";
            public bool IsConnected { get; } = true;
            public List<Order> GetOpenOrders() { return new List<Order>(); }
            public List<Holding> GetAccountHoldings() { return new List<Holding>(); }
            public List<CashAmount> GetCashBalance() { return new List<CashAmount>(); }
            public bool PlaceOrder(Order order) { return true; }
            public bool UpdateOrder(Order order) { return true; }
            public bool CancelOrder(Order order) { return true; }
            public void Connect() {}
            public void Disconnect() {}
            public bool AccountInstantlyUpdated { get; } = true;
            public string AccountBaseCurrency => Currencies.USD;
            public IEnumerable<BaseData> GetHistory(HistoryRequest request) { return Enumerable.Empty<BaseData>(); }
            public DateTime LastSyncDateTimeUtc { get; } = DateTime.UtcNow;
            public bool ShouldPerformCashSync(DateTime currentTimeUtc) { return false; }
            public bool PerformCashSync(IAlgorithm algorithm, DateTime currentTimeUtc, Func<TimeSpan> getTimeSinceLastFill) { return true; }
        }
    }
}
