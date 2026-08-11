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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm exercising the engine-guaranteed OCO semantics of <see cref="QCAlgorithm.BracketOrder(Symbol, decimal, decimal?, decimal?, decimal?, string, Interfaces.IOrderProperties)"/>:
    /// the entry fill places the protective legs, a leg fill cancels its sibling, an unrelated order
    /// closing the position cancels the remaining legs and a new bracket is refused while one is active.
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="placing orders" />
    /// <meta name="tag" content="bracket order"/>
    public class BracketOrderRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _symbol;
        private BracketOrderTicket _bracket1;
        private BracketOrderTicket _bracket2;
        private bool _legsVerified;
        private bool _refusalVerified;
        private bool _phase1Verified;
        private DateTime _manualCloseTime;
        private bool _manualCloseDone;
        private bool _phase2Verified;
        private bool _takeProfitFilled;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100000);

            _symbol = AddEquity("SPY", Resolution.Minute).Symbol;
        }

        public override void OnData(Slice slice)
        {
            if (Math.Abs(Portfolio[_symbol].Quantity) > 10)
            {
                throw new RegressionTestException("The position must never exceed the bracket entry quantity.");
            }

            var price = Securities[_symbol].Price;

            // Phase 1: entry fill places the legs, then the take profit fill cancels the stop loss
            if (_bracket1 == null)
            {
                _bracket1 = BracketOrder(_symbol, 10,
                    stopLossPrice: Math.Round(price * 0.975m, 2),
                    takeProfitPrice: Math.Round(price * 1.008m, 2));
                return;
            }

            if (!_legsVerified && _bracket1.StopLossTicket != null)
            {
                if (_bracket1.EntryTicket.Status != OrderStatus.Filled)
                {
                    throw new RegressionTestException("The exit legs must not be placed before the entry order fills.");
                }
                if (_bracket1.StopLossTicket.OrderType != OrderType.StopMarket || _bracket1.StopLossTicket.Quantity != -10)
                {
                    throw new RegressionTestException("Expected a stop market leg for -10 units.");
                }
                if (_bracket1.TakeProfitTicket == null ||
                    _bracket1.TakeProfitTicket.OrderType != OrderType.Limit || _bracket1.TakeProfitTicket.Quantity != -10)
                {
                    throw new RegressionTestException("Expected a limit take profit leg for -10 units.");
                }

                // a new bracket must be refused while this one is live instead of silently
                // overwriting it and stranding its legs
                try
                {
                    BracketOrder(_symbol, 10, stopLossPrice: 100m, takeProfitPrice: 200m);
                    throw new RegressionTestException("A second bracket order for the same symbol should have been refused.");
                }
                catch (InvalidOperationException)
                {
                    _refusalVerified = true;
                }
                _legsVerified = true;
                return;
            }

            // Phase 2: with a fresh bracket in place, manually closing the position cancels both legs
            if (_bracket2 == null)
            {
                if (_legsVerified && !_bracket1.IsActive)
                {
                    if (_bracket1.TakeProfitTicket.Status != OrderStatus.Filled)
                    {
                        throw new RegressionTestException("Expected the take profit leg of the first bracket to fill.");
                    }
                    if (_bracket1.StopLossTicket.Status != OrderStatus.Canceled)
                    {
                        throw new RegressionTestException("Expected the stop loss leg to be canceled when its sibling filled.");
                    }
                    if (Portfolio.Invested)
                    {
                        throw new RegressionTestException("Expected a flat position after the take profit filled.");
                    }
                    if (Transactions.GetBracketOrderTicket(_symbol) != null)
                    {
                        throw new RegressionTestException("Expected no active bracket after the first one completed.");
                    }
                    _phase1Verified = true;

                    // legs far away from the market so only the manual close can end this bracket
                    _bracket2 = BracketOrder(_symbol, 10,
                        stopLossPrice: Math.Round(price * 0.93m, 2),
                        takeProfitPrice: Math.Round(price * 1.07m, 2));
                }
                return;
            }

            if (_manualCloseTime == default && _bracket2.StopLossTicket != null)
            {
                _manualCloseTime = Time.AddMinutes(30);
                return;
            }

            if (!_manualCloseDone && _manualCloseTime != default && Time >= _manualCloseTime)
            {
                MarketOrder(_symbol, -10);
                _manualCloseDone = true;
                return;
            }

            if (_manualCloseDone && !_phase2Verified)
            {
                if (_bracket2.StopLossTicket.Status != OrderStatus.Canceled ||
                    _bracket2.TakeProfitTicket.Status != OrderStatus.Canceled)
                {
                    throw new RegressionTestException("Expected both legs to be canceled after the position was closed manually.");
                }
                if (Portfolio.Invested || _bracket2.IsActive || Transactions.GetBracketOrderTicket(_symbol) != null)
                {
                    throw new RegressionTestException("Expected a flat position and no active bracket after the manual close.");
                }
                _phase2Verified = true;
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (_bracket1 != null && _bracket1.TakeProfitTicket != null &&
                orderEvent.OrderId == _bracket1.TakeProfitTicket.OrderId && orderEvent.Status == OrderStatus.Filled)
            {
                _takeProfitFilled = true;
            }
            if (_bracket1 != null && _bracket1.StopLossTicket != null &&
                orderEvent.OrderId == _bracket1.StopLossTicket.OrderId && orderEvent.Status == OrderStatus.Canceled &&
                !_takeProfitFilled)
            {
                throw new RegressionTestException("The stop loss must only be canceled after its sibling take profit filled.");
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_legsVerified || !_refusalVerified || !_phase1Verified || !_manualCloseDone || !_phase2Verified)
            {
                throw new RegressionTestException($"Not every phase completed: legs placed {_legsVerified}, " +
                    $"re-entry refused {_refusalVerified}, sibling canceled on fill {_phase1Verified}, " +
                    $"manual close {_manualCloseDone}, legs canceled on position close {_phase2Verified}");
            }
            // entry, stop loss and take profit per bracket, plus the manual close
            if (Transactions.OrdersCount != 7)
            {
                throw new RegressionTestException($"Expected 7 orders, found {Transactions.OrdersCount}");
            }
            if (Transactions.GetOpenOrders().Count != 0)
            {
                throw new RegressionTestException("Expected no dangling open orders at the end of the algorithm.");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "7"},
            {"Average Win", "0.01%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "0.648%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "3.381"},
            {"Start Equity", "100000"},
            {"End Equity", "100008.26"},
            {"Net Profit", "0.008%"},
            {"Sharpe Ratio", "-0.536"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "43.394%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "7.76"},
            {"Alpha", "-0.026"},
            {"Beta", "0.012"},
            {"Annual Standard Deviation", "0.003"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-8.993"},
            {"Tracking Error", "0.22"},
            {"Treynor Ratio", "-0.121"},
            {"Total Fees", "$4.00"},
            {"Estimated Strategy Capacity", "$19000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "1.17%"},
            {"Drawdown Recovery", "3"},
            {"OrderListHash", "86d5cef4794178bd4f6eb46202b54fd5"}
        };
    }
}
