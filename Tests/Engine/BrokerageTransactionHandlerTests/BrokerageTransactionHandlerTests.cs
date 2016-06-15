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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;


namespace QuantConnect.Tests.Engine.BrokerageTransactionHandlerTests
{
    [TestFixture]
    class BrokerageTransactionHandlerTests
    {
        [Test]
        public void RoundOff_Long_Orders()
        {
            // Initializes the algorithm
            var algo = GetAlgorithm();
            // Sets the Security
            var security = algo.AddSecurity(SecurityType.Forex, "EURUSD");

            //Initializes the transaction handler
            var transactionHandler = new BrokerageTransactionHandler();
            transactionHandler.Initialize(algo, new BacktestingBrokerage(algo), new BacktestingResultHandler());

            // Creates the order
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 1600, 0, 0, DateTime.Now, "");
            var order = Order.CreateOrder(orderRequest);
            var actual = transactionHandler.RoundOffOrder(order, security);
            // 1600 after round off becomes 1000
            Assert.AreEqual(1000, actual);

        }

        [Test]
        public void RoundOff_Short_Orders()
        {
            // Initializes the algorithm
            var algo = GetAlgorithm();
            // Sets the Security
            var security = algo.AddSecurity(SecurityType.Forex, "EURUSD");

            //Initializes the transaction handler
            var transactionHandler = new BrokerageTransactionHandler();
            transactionHandler.Initialize(algo, new BacktestingBrokerage(algo), new BacktestingResultHandler());

            // Creates the order
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, -1600, 0, 0, DateTime.Now, "");
            var order = Order.CreateOrder(orderRequest);
            var actual = transactionHandler.RoundOffOrder(order, security);
            // -1600 after round off becomes -1000
            Assert.AreEqual(-1000, actual);
        }

        [Test]
        public void RoundOff_LessThanLotSize_Orders()
        {
            // Initializes the algorithm
            var algo = GetAlgorithm();
            // Sets the Security
            var security = algo.AddSecurity(SecurityType.Forex, "EURUSD");

            //Initializes the transaction handler
            var transactionHandler = new BrokerageTransactionHandler();
            transactionHandler.Initialize(algo, new BacktestingBrokerage(algo), new BacktestingResultHandler());

            // Creates the order
            var orderRequest = new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, 600, 0, 0, DateTime.Now, "");
            var order = Order.CreateOrder(orderRequest);
            var actual = transactionHandler.RoundOffOrder(order, security);
            // 600 after round off becomes 0
            Assert.AreEqual(0, actual);
        }

        private QCAlgorithm GetAlgorithm()
        {
            //Initialize algorithm
            var algo = new QCAlgorithm();
            algo.SetBrokerageModel(BrokerageName.FxcmBrokerage);
            algo.SetCash(100000);

            return algo;
        }
    }
}
