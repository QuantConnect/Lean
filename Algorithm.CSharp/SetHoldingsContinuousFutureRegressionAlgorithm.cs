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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities.Future;
using System.Collections.Generic;
using Futures = QuantConnect.Securities.Futures;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// End-to-end regression algorithm asserting that calling <see cref="QCAlgorithm.SetHoldings(Symbol, decimal, bool, string, IOrderProperties)"/>
    /// directly with the canonical (continuous) Future Symbol routes the order to the currently mapped contract,
    /// sizes the order using the mapped contract's price, and lets portfolio queries on the continuous symbol
    /// reflect the active position both before and after a contract roll.
    /// </summary>
    public class SetHoldingsContinuousFutureRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;
        private Symbol _filledContract;
        private DateTime _liquidateAfter;
        private bool _liquidated;

        public override void Initialize()
        {
            SetStartDate(2013, 7, 1);
            SetEndDate(2014, 1, 1);

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.LastTradingDay,
                contractDepthOffset: 0,
                extendedMarketHours: true);
            // A wide filter exercises the case where many future contracts are already in the chain universe across rolls
            _continuousContract.SetFilter(0, 90);
        }

        public override void OnData(Slice slice)
        {
            // The continuous Future is not tradable until a contract is mapped to it
            if (_liquidated || !_continuousContract.IsTradable)
            {
                return;
            }

            // Reading Invested off the canonical's Holdings relies on the universe linking it to the mapped contract
            var invested = _continuousContract.Holdings.Invested;

            if (!invested)
            {
                // Pass the canonical Symbol — the engine should route the order to the mapped contract
                SetHoldings(_continuousContract.Symbol, 0.5m);
                _liquidateAfter = Time.AddDays(30);
            }
            else if (Time >= _liquidateAfter)
            {
                // Set the flag before Liquidate so OnOrderEvent (called synchronously on fill) sees the post-liquidation state
                _liquidated = true;
                Liquidate(_continuousContract.Symbol);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status != OrderStatus.Filled)
            {
                return;
            }

            // The order must have been placed against the currently mapped contract, never the canonical
            if (orderEvent.Symbol.IsCanonical())
            {
                throw new RegressionTestException($"Order filled against canonical symbol {orderEvent.Symbol}; expected the mapped contract");
            }

            if (!_liquidated)
            {
                _filledContract = orderEvent.Symbol;

                // After the fill, querying the canonical should reflect the active position from the mapped contract
                if (!Portfolio[_continuousContract.Symbol].Invested)
                {
                    throw new RegressionTestException($"Portfolio[{_continuousContract.Symbol}].Invested is false after fill on {orderEvent.Symbol}");
                }
                if (Portfolio[_continuousContract.Symbol].Quantity != Portfolio[orderEvent.Symbol].Quantity)
                {
                    throw new RegressionTestException(
                        $"Continuous holdings quantity {Portfolio[_continuousContract.Symbol].Quantity} does not match mapped contract " +
                        $"{orderEvent.Symbol} quantity {Portfolio[orderEvent.Symbol].Quantity}");
                }
            }
            else
            {
                // After liquidation via the canonical symbol, both the canonical view and the mapped contract should be flat
                if (Portfolio[_continuousContract.Symbol].Invested)
                {
                    throw new RegressionTestException($"Portfolio[{_continuousContract.Symbol}].Invested is true after liquidating via the canonical symbol");
                }
                if (Portfolio[orderEvent.Symbol].Invested)
                {
                    throw new RegressionTestException($"Portfolio[{orderEvent.Symbol}].Invested is true after liquidation");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_filledContract == null)
            {
                throw new RegressionTestException("Expected at least one filled order during the backtest");
            }

            if (!_liquidated)
            {
                throw new RegressionTestException("Expected the canonical position to have been liquidated before end of algorithm");
            }

            if (Portfolio[_continuousContract.Symbol].Invested)
            {
                throw new RegressionTestException($"Portfolio[{_continuousContract.Symbol}].Invested is true at end of algorithm; expected flat after Liquidate");
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
        public long DataPoints => 727503;

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
            {"Total Orders", "2"},
            {"Average Win", "16.59%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "35.584%"},
            {"Drawdown", "28.400%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "116590.2"},
            {"Net Profit", "16.590%"},
            {"Sharpe Ratio", "1.068"},
            {"Sortino Ratio", "0.503"},
            {"Probabilistic Sharpe Ratio", "48.712%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.024"},
            {"Beta", "1.096"},
            {"Annual Standard Deviation", "0.245"},
            {"Annual Variance", "0.06"},
            {"Information Ratio", "0.194"},
            {"Tracking Error", "0.229"},
            {"Treynor Ratio", "0.239"},
            {"Total Fees", "$47.30"},
            {"Estimated Strategy Capacity", "$160000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "10.22%"},
            {"Drawdown Recovery", "2"},
            {"OrderListHash", "8c87879bcbeb3b139dc112bb0e4f199e"}
        };
    }
}
