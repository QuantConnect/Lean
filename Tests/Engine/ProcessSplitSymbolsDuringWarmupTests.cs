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
using System.Reflection;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Engine
{
    /// <summary>
    /// Tests the ProcessSplitSymbols method directly via reflection to ensure
    /// it properly skips processing during warmup in live mode.
    /// </summary>
    [TestFixture]
    public class ProcessSplitSymbolsDuringWarmupTests
    {
        private QCAlgorithm _algorithm;
        private AlgorithmManager _algorithmManager;
        private BrokerageTransactionHandler _transactionHandler;
        private TestResultHandler _resultHandler;
        private BacktestingBrokerage _brokerage;
        private MethodInfo _processSplitSymbolsMethod;

        [SetUp]
        public void SetUp()
        {
            // Create algorithm instance
            _algorithm = new QCAlgorithm();

            // Initialize data manager
            _algorithm.SubscriptionManager.SetDataManager(new DataManagerStub(_algorithm));
            _algorithm.SetDateTime(new DateTime(2025, 10, 10, 19, 0, 0));
            // Add equity with market price
            var spy = _algorithm.AddEquity("SPY", Resolution.Daily);
            spy.SetMarketPrice(new Tick { Value = 100m, Symbol = spy.Symbol, Time = _algorithm.Time });

            // Initialize transaction components
            _transactionHandler = new BrokerageTransactionHandler();
            _resultHandler = new TestResultHandler();
            _brokerage = new BacktestingBrokerage(_algorithm);

            _transactionHandler.Initialize(_algorithm, _brokerage, _resultHandler);
            _algorithm.Transactions.SetOrderProcessor(_transactionHandler);

            // Create AlgorithmManager and get ProcessSplitSymbols method via reflection
            _algorithmManager = new AlgorithmManager(false);
            _processSplitSymbolsMethod = typeof(AlgorithmManager).GetMethod(
                "ProcessSplitSymbols",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            Assert.IsNotNull(_processSplitSymbolsMethod, "ProcessSplitSymbols method should be found via reflection");
        }

        [TearDown]
        public void TearDown()
        {
            _transactionHandler?.Exit();
            _brokerage?.Dispose();
            _resultHandler?.Exit();
        }

        [TestCase(false, false, true)]
        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(true, true, true)]
        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, true, false)]
        public void ProcessSplitSymbolsDoesNotThrow(bool liveMode, bool isWarmingUp, bool hasHoldings)
        {
            _algorithm.SetLiveMode(liveMode);
            if (!isWarmingUp)
            {
                _algorithm.SetFinishedWarmingUp();
            }
            Assert.AreEqual(isWarmingUp, _algorithm.IsWarmingUp, $"Algorithm should be warming up: {isWarmingUp}");

            var splitWarnings = new List<Split>
            {
                new Split(
                    Symbols.SPY,
                    _algorithm.Time,
                    100m,
                    0.5m,
                    SplitType.Warning
                )
            };

            if (hasHoldings)
            {
                _algorithm.Securities[Symbols.SPY].Holdings.SetHoldings(350, 100);
            }

            Assert.DoesNotThrow(() =>
            {
                _processSplitSymbolsMethod.Invoke(
                    _algorithmManager,
                    new object[] { _algorithm, splitWarnings, new List<Delisting>() }
                );
            });

            Assert.AreEqual(0, splitWarnings.Count, "Split warning should be removed");
        }
    }
}
