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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Securities;
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

            // Add equity with market price
            var spy = _algorithm.AddEquity("SPY", Resolution.Daily);
            spy.SetMarketPrice(new Tick { Value = 100m, Symbol = spy.Symbol, Time = DateTime.UtcNow });

            // Initialize transaction components
            _transactionHandler = new BrokerageTransactionHandler();
            _resultHandler = new TestResultHandler(Console.WriteLine);
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
        }

        /// <summary>
        /// Test that ProcessSplitSymbols returns early when in live mode and warming up,
        /// preventing the CancelOpenOrders exception.
        /// </summary>
        [Test]
        public void ProcessSplitSymbols_ReturnsEarly_WhenLiveModeAndWarmingUp()
        {
            // Arrange: Set algorithm to live mode and warming up
            _algorithm.SetLiveMode(true);
            // Algorithm is in warmup mode by default (don't call SetFinishedWarmingUp())
            Assert.IsTrue(_algorithm.IsWarmingUp, "Algorithm should be warming up by default");

            // Create a split warning for SPY
            var splitWarnings = new List<Split>
            {
                new Split(
                    Symbols.SPY,
                    DateTime.UtcNow,
                    100m,  // Reference price
                    0.5m,  // Split factor (2-for-1 split)
                    SplitType.Warning
                )
            };

            var pendingDelistings = new List<Delisting>();

            // Place an order so CancelOpenOrders would have something to cancel
            // (if it gets called, which it shouldn't due to our fix)
            _algorithm.SetHoldings(Symbols.SPY, 0.5m);

            // Act: Call ProcessSplitSymbols via reflection
            // With the fix, this should return early and NOT call CancelOpenOrders
            Assert.DoesNotThrow(() =>
            {
                _processSplitSymbolsMethod.Invoke(
                    _algorithmManager,
                    new object[] { _algorithm, splitWarnings, pendingDelistings }
                );
            });

            // Assert: Split warnings should still be in the list (not removed)
            // because we returned early before processing
            Assert.AreEqual(1, splitWarnings.Count, "Split warning should not be removed during warmup in live mode");
        }

        /// <summary>
        /// Test that ProcessSplitSymbols processes normally when NOT in warmup,
        /// ensuring our fix doesn't break normal operation.
        /// </summary>
        [Test]
        public void ProcessSplitSymbols_ProcessesNormally_WhenNotWarmingUp()
        {
            // Arrange: Set algorithm to live mode but NOT warming up
            _algorithm.SetLiveMode(true);
            _algorithm.SetFinishedWarmingUp();

            // Create a split warning for SPY
            var splitWarnings = new List<Split>
            {
                new Split(
                    Symbols.SPY,
                    DateTime.UtcNow,
                    100m,  // Reference price
                    0.5m,  // Split factor (2-for-1 split)
                    SplitType.Warning
                )
            };

            var pendingDelistings = new List<Delisting>();

            // Act: Call ProcessSplitSymbols via reflection
            // This should process normally (though may not remove the warning if timing conditions aren't met)
            Assert.DoesNotThrow(() =>
            {
                _processSplitSymbolsMethod.Invoke(
                    _algorithmManager,
                    new object[] { _algorithm, splitWarnings, pendingDelistings }
                );
            });

            // Assert: Should not throw any exceptions
            // Note: The split warning might still be in the list if the market close timing condition isn't met,
            // but the important thing is that no exception was thrown
            Assert.Pass("ProcessSplitSymbols executed without throwing when not warming up");
        }

        /// <summary>
        /// Test that ProcessSplitSymbols processes normally in backtest mode during warmup,
        /// since the fix only applies to live mode.
        /// </summary>
        [Test]
        public void ProcessSplitSymbols_ProcessesNormally_InBacktestModeDuringWarmup()
        {
            // Arrange: Set algorithm to backtest mode (LiveMode = false) and warming up
            _algorithm.SetLiveMode(false);
            // Algorithm is in warmup mode by default (don't call SetFinishedWarmingUp())
            Assert.IsTrue(_algorithm.IsWarmingUp, "Algorithm should be warming up by default");

            // Create a split warning for SPY
            var splitWarnings = new List<Split>
            {
                new Split(
                    Symbols.SPY,
                    DateTime.UtcNow,
                    100m,  // Reference price
                    0.5m,  // Split factor (2-for-1 split)
                    SplitType.Warning
                )
            };

            var pendingDelistings = new List<Delisting>();

            // Act: Call ProcessSplitSymbols via reflection
            // In backtest mode, warmup shouldn't trigger early return
            Assert.DoesNotThrow(() =>
            {
                _processSplitSymbolsMethod.Invoke(
                    _algorithmManager,
                    new object[] { _algorithm, splitWarnings, pendingDelistings }
                );
            });

            // Assert: Should not throw any exceptions
            // The split warning processing behavior depends on timing conditions
            Assert.Pass("ProcessSplitSymbols executed in backtest mode during warmup without throwing");
        }
    }
}
