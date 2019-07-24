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
 *
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine.Setup
{
    [TestFixture]
    public class BrokerageSetupHandlerTests
    {
        private IAlgorithm _algorithm;
        private ITransactionHandler _transactionHandler;
        private NonDequeingTestResultsHandler _resultHandler;
        private IBrokerage _brokerage;
        private DataManager _dataManager;

        private TestableBrokerageSetupHandler _brokerageSetupHandler;

        [TestFixtureSetUp]
        public void Setup()
        {
            _algorithm = new QCAlgorithm();
            _dataManager = new DataManagerStub(_algorithm);
            _algorithm.SubscriptionManager.SetDataManager(_dataManager);
            _transactionHandler = new BrokerageTransactionHandler();
            _resultHandler = new NonDequeingTestResultsHandler();
            _brokerage = new TestBrokerage();

            _brokerageSetupHandler = new TestableBrokerageSetupHandler();
        }

        [Test]
        public void CanGetOpenOrders()
        {
            _brokerageSetupHandler.PublicGetOpenOrders(_algorithm, _resultHandler, _transactionHandler, _brokerage);

            Assert.AreEqual(_transactionHandler.Orders.Count, 4);

            Assert.AreEqual(_transactionHandler.OrderTickets.Count, 4);

            // Warn the user about each open order
            Assert.AreEqual(_resultHandler.PersistentMessages.Count, 4);

            // Market order
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.Market).Value.Quantity, 100);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.Market).Value.SubmitRequest.LimitPrice, 1.2345m);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.Market).Value.SubmitRequest.StopPrice, 1.2345m);

            // Limit Order
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.Limit).Value.Quantity, -100);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.Limit).Value.SubmitRequest.LimitPrice, 2.2345m);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.Limit).Value.SubmitRequest.StopPrice, 0m);

            // Stop market order
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.StopMarket).Value.Quantity, 100);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.StopMarket).Value.SubmitRequest.LimitPrice, 0m);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.StopMarket).Value.SubmitRequest.StopPrice, 2.2345m);

            // Stop Limit order
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.StopLimit).Value.Quantity, 100);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.StopLimit).Value.SubmitRequest.LimitPrice, 0.2345m);
            Assert.AreEqual(_transactionHandler.OrderTickets.First(x => x.Value.OrderType == OrderType.StopLimit).Value.SubmitRequest.StopPrice, 2.2345m);

            // SPY security should be added to the algorithm
            Assert.Contains(Symbols.SPY, _algorithm.Securities.Select(x => x.Key).ToList());
        }

        [Test, TestCaseSource(nameof(GetExistingHoldingsAndOrdersTestCaseData))]
        public void LoadsExistingHoldingsAndOrders(Func<List<Holding>> getHoldings, Func<List<Order>> getOrders, bool expected)
        {
            var algorithm = new TestAlgorithm();
            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            var job = new LiveNodePacket
            {
                UserId = 1,
                ProjectId = 1,
                DeployId = "1",
                Brokerage = "PaperBrokerage",
                DataQueueHandler = "none"
            };
            // Increasing RAM limit, else the tests fail. This is happening in master, when running all the tests together, locally (not travis).
            job.Controls.RamAllocation = 1024 * 1024 * 1024;

            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var brokerage = new Mock<IBrokerage>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount>());
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(getHoldings);
            brokerage.Setup(x => x.GetOpenOrders()).Returns(getOrders);

            var setupHandler = new BrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            var result = setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object));

            Assert.AreEqual(expected, result);

            foreach (var security in algorithm.Securities.Values)
            {
                if (security.Symbol.SecurityType == SecurityType.Option)
                {
                    Assert.AreEqual(DataNormalizationMode.Raw, security.DataNormalizationMode);

                    var underlyingSecurity = algorithm.Securities[security.Symbol.Underlying];
                    Assert.AreEqual(DataNormalizationMode.Raw, underlyingSecurity.DataNormalizationMode);
                }
            }
        }

        [Test]
        public void LoadsHoldingsForExpectedMarket()
        {
            var symbol = Symbol.Create("AUDUSD", SecurityType.Forex, Market.Oanda);

            var algorithm = new TestAlgorithm();
            algorithm.SetBrokerageModel(BrokerageName.InteractiveBrokersBrokerage);

            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            var job = new LiveNodePacket
            {
                UserId = 1,
                ProjectId = 1,
                DeployId = "1",
                Brokerage = "PaperBrokerage",
                DataQueueHandler = "none"
            };
            // Increasing RAM limit, else the tests fail. This is happening in master, when running all the tests together, locally (not travis).
            job.Controls.RamAllocation = 1024 * 1024 * 1024;

            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var brokerage = new Mock<IBrokerage>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount>());
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(new List<Holding>
            {
                new Holding { Symbol = symbol, Type = symbol.SecurityType, Quantity = 100 }
            });
            brokerage.Setup(x => x.GetOpenOrders()).Returns(new List<Order>());

            var setupHandler = new BrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            Assert.IsTrue(setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object)));

            Security security;
            Assert.IsTrue(algorithm.Portfolio.Securities.TryGetValue(symbol, out security));
            Assert.AreEqual(symbol, security.Symbol);
        }

        [Test]
        public void AlgorithmTimeIsSetToUtcNowBeforePostInitialize()
        {
            var time = DateTime.UtcNow;
            TestAlgorithm algorithm = null;

            algorithm = new TestAlgorithm(() =>
            {
                Assert.That(algorithm.UtcTime > time);
            });

            Assert.AreEqual(new DateTime(1998, 1, 1), algorithm.UtcTime);

            algorithm.SetHistoryProvider(new BrokerageTransactionHandlerTests.BrokerageTransactionHandlerTests.EmptyHistoryProvider());
            var job = new LiveNodePacket
            {
                UserId = 1,
                ProjectId = 1,
                DeployId = "1",
                Brokerage = "PaperBrokerage",
                DataQueueHandler = "none"
            };

            var resultHandler = new Mock<IResultHandler>();
            var transactionHandler = new Mock<ITransactionHandler>();
            var realTimeHandler = new Mock<IRealTimeHandler>();
            var brokerage = new Mock<IBrokerage>();

            brokerage.Setup(x => x.IsConnected).Returns(true);
            brokerage.Setup(x => x.GetCashBalance()).Returns(new List<CashAmount>());
            brokerage.Setup(x => x.GetAccountHoldings()).Returns(new List<Holding>());
            brokerage.Setup(x => x.GetOpenOrders()).Returns(new List<Order>());

            var setupHandler = new BrokerageSetupHandler();

            IBrokerageFactory factory;
            setupHandler.CreateBrokerage(job, algorithm, out factory);

            Assert.IsTrue(setupHandler.Setup(new SetupHandlerParameters(_dataManager.UniverseSelection, algorithm, brokerage.Object, job, resultHandler.Object,
                transactionHandler.Object, realTimeHandler.Object)));

            Assert.Greater(algorithm.UtcTime, time);
        }

        public TestCaseData[] GetExistingHoldingsAndOrdersTestCaseData()
        {
            return new[]
            {
                new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>()),
                        new Func<List<Order>>(() => new List<Order>()),
                        true)
                    .SetName("None"),

                new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Type = SecurityType.Equity, Symbol = Symbols.SPY, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.SPY, 1, 1, DateTime.UtcNow)
                        }),
                        true)
                    .SetName("Equity"),

                new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Type = SecurityType.Option, Symbol = Symbols.SPY_C_192_Feb19_2016, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.SPY_C_192_Feb19_2016, 1, 1, DateTime.UtcNow)
                        }),
                        true)
                    .SetName("Option"),

                new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Type = SecurityType.Equity, Symbol = Symbols.SPY, Quantity = 1 },
                            new Holding { Type = SecurityType.Option, Symbol = Symbols.SPY_C_192_Feb19_2016, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.SPY, 1, 1, DateTime.UtcNow),
                            new LimitOrder(Symbols.SPY_C_192_Feb19_2016, 1, 1, DateTime.UtcNow)
                        }),
                        true)
                    .SetName("Equity + Option"),

                new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Type = SecurityType.Option, Symbol = Symbols.SPY_C_192_Feb19_2016, Quantity = 1 },
                            new Holding { Type = SecurityType.Equity, Symbol = Symbols.SPY, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.SPY_C_192_Feb19_2016, 1, 1, DateTime.UtcNow),
                            new LimitOrder(Symbols.SPY, 1, 1, DateTime.UtcNow)
                        }),
                        true)
                    .SetName("Option + Equity"),

                new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Type = SecurityType.Option, Symbol = Symbols.SPY_C_192_Feb19_2016, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.SPY, 1, 1, DateTime.UtcNow),
                        }),
                        true)
                    .SetName("Equity open order + Option holding"),

                new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Type = SecurityType.Forex, Symbol = Symbols.EURUSD, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.EURUSD, 1, 1, DateTime.UtcNow)
                        }),
                        true)
                    .SetName("Forex"),

                new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Type = SecurityType.Crypto, Symbol = Symbols.BTCUSD, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.BTCUSD, 1, 1, DateTime.UtcNow)
                        }),
                        true)
                    .SetName("Crypto"),

                new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Type = SecurityType.Future, Symbol = Symbols.Fut_SPY_Feb19_2016, Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder(Symbols.Fut_SPY_Feb19_2016, 1, 1, DateTime.UtcNow)
                        }),
                        true)
                    .SetName("Future"),

                new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>
                        {
                            new Holding { Type = SecurityType.Base, Symbol = Symbol.Create("XYZ", SecurityType.Base, Market.USA), Quantity = 1 }
                        }),
                        new Func<List<Order>>(() => new List<Order>
                        {
                            new LimitOrder("XYZ", 1, 1, DateTime.UtcNow)
                        }),
                        false)
                    .SetName("Base"),

                new TestCaseData(
                        new Func<List<Holding>>(() => { throw new Exception(); }),
                        new Func<List<Order>>(() => new List<Order>()),
                        false)
                    .SetName("Invalid Holdings"),

                new TestCaseData(
                        new Func<List<Holding>>(() => new List<Holding>()),
                        new Func<List<Order>>(() => { throw new Exception(); }),
                        false)
                    .SetName("Invalid Orders"),
            };
        }

        private class TestAlgorithm : QCAlgorithm
        {
            private readonly Action _beforePostInitializeAction;

            public TestAlgorithm(Action beforePostInitializeAction = null)
            {
                _beforePostInitializeAction = beforePostInitializeAction;
                SubscriptionManager.SetDataManager(new DataManagerStub(this, new MockDataFeed()));
            }

            public override void Initialize() { }

            public override void PostInitialize()
            {
                _beforePostInitializeAction?.Invoke();
                base.PostInitialize();
            }
        }

        private class NonDequeingTestResultsHandler : TestResultHandler
        {
            private readonly AlgorithmNodePacket _job = new BacktestNodePacket();
            public readonly ConcurrentQueue<Packet> PersistentMessages = new ConcurrentQueue<Packet>();

            public override void DebugMessage(string message)
            {
                PersistentMessages.Enqueue(new DebugPacket(_job.ProjectId, _job.AlgorithmId, _job.CompileId, message));
            }
        }

        private class TestableBrokerageSetupHandler : BrokerageSetupHandler
        {
            private readonly HashSet<SecurityType> _supportedSecurityTypes = new HashSet<SecurityType>
            {
                SecurityType.Equity, SecurityType.Forex, SecurityType.Cfd, SecurityType.Option, SecurityType.Future, SecurityType.Crypto
            };

            public void PublicGetOpenOrders(IAlgorithm algorithm, IResultHandler resultHandler, ITransactionHandler transactionHandler, IBrokerage brokerage)
            {
                GetOpenOrders(algorithm, resultHandler, transactionHandler, brokerage, _supportedSecurityTypes, Resolution.Second);
            }
        }
    }

    internal class TestBrokerage : Brokerage
    {
        public override bool IsConnected { get; } = true;
        public int GetCashBalanceCallCount;

        public TestBrokerage() : base("Test")
        {
        }

        public TestBrokerage(string name) : base(name)
        {
        }

        public override List<Order> GetOpenOrders()
        {
            const decimal delta = 1m;
            const decimal price = 1.2345m;
            const int quantity = 100;
            const decimal pricePlusDelta = price + delta;
            const decimal priceMinusDelta = price - delta;
            var tz = TimeZones.NewYork;

            var time = new DateTime(2016, 2, 4, 16, 0, 0).ConvertToUtc(tz);
            var marketOrderWithPrice = new MarketOrder(Symbols.SPY, quantity, time)
            {
                Price = price
            };

            return new List<Order>
            {
                marketOrderWithPrice,
                new LimitOrder(Symbols.SPY, -quantity, pricePlusDelta, time),
                new StopMarketOrder(Symbols.SPY, quantity, pricePlusDelta, time),
                new StopLimitOrder(Symbols.SPY, quantity, pricePlusDelta, priceMinusDelta, time)
            };
        }

        public override List<CashAmount> GetCashBalance()
        {
            GetCashBalanceCallCount++;

            return new List<CashAmount> { new CashAmount(10, Currencies.USD) };
        }

        #region UnusedMethods

        public override List<Holding> GetAccountHoldings()
        {
            throw new NotImplementedException();
        }

        public override bool PlaceOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool CancelOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override void Connect()
        {
            throw new NotImplementedException();
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
