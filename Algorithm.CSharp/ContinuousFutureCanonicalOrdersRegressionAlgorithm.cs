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
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Future;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the canonical continuous future symbol cannot be traded directly and fails loudly,
    /// while its currently mapped contract can: <see cref="QCAlgorithm.CalculateOrderQuantity(Symbol, decimal)"/> returns
    /// zero with an instructive error, <see cref="QCAlgorithm.SetHoldings(Symbol, double, bool, bool, string, Orders.IOrderProperties)"/>
    /// submits no orders and direct orders produce an invalid ticket pointing to <see cref="Future.Mapped"/>.
    /// Also asserts <see cref="Future.Canonical"/> and that <see cref="Future.Mapped"/> is null until the continuous
    /// contract universe makes its first selection, after Initialize.
    /// </summary>
    public class ContinuousFutureCanonicalOrdersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;
        private bool _canonicalChecksDone;
        private bool _traded;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);
            SetEndDate(2013, 10, 10);

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.OpenInterest,
                contractDepthOffset: 0
            );

            if (_continuousContract.Mapped != null)
            {
                throw new RegressionTestException("Expected Future.Mapped to be null during Initialize: " +
                    "the continuous contract universe does not make its first selection until after Initialize");
            }

            if (_continuousContract.Canonical != _continuousContract.Symbol)
            {
                throw new RegressionTestException("Expected Future.Canonical to be the continuous contract symbol itself");
            }
        }

        public override void OnData(Slice slice)
        {
            if (_continuousContract.Mapped == null || !slice.Bars.ContainsKey(_continuousContract.Symbol))
            {
                return;
            }

            if (!_canonicalChecksDone)
            {
                _canonicalChecksDone = true;
                var canonical = _continuousContract.Symbol;

                // Continuous contract data is keyed by the canonical symbol
                if (slice.Bars[canonical].Symbol != canonical)
                {
                    throw new RegressionTestException("Expected the continuous contract bar to be keyed by the canonical symbol");
                }

                // The canonical symbol is not tradable: no order quantity can be computed for it
                if (CalculateOrderQuantity(canonical, 1m) != 0)
                {
                    throw new RegressionTestException("Expected CalculateOrderQuantity to return 0 for the canonical symbol");
                }

                // SetHoldings must not submit orders for the canonical symbol
                if (SetHoldings(canonical, 0.5).Count != 0 || Portfolio.Invested)
                {
                    throw new RegressionTestException("Expected SetHoldings to not submit orders for the canonical symbol");
                }

                // Direct orders on the canonical symbol are rejected with an instructive message
                var ticket = MarketOrder(canonical, 1);
                if (ticket.Status != OrderStatus.Invalid)
                {
                    throw new RegressionTestException("Expected a market order on the canonical symbol to be invalid");
                }
                if (!ticket.SubmitRequest.Response.ErrorMessage.Contains("canonical"))
                {
                    throw new RegressionTestException("Expected the invalid canonical order error message to explain " +
                        $"the symbol is canonical, but was: '{ticket.SubmitRequest.Response.ErrorMessage}'");
                }
            }

            if (!_traded)
            {
                _traded = true;

                // The currently mapped contract is the tradable one
                var ticket = MarketOrder(_continuousContract.Mapped, 1);
                if (ticket.Status == OrderStatus.Invalid)
                {
                    throw new RegressionTestException("Expected a market order on the mapped contract to be valid");
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_canonicalChecksDone)
            {
                throw new RegressionTestException("No data was received so the canonical symbol checks were not performed");
            }

            if (!Portfolio.Invested)
            {
                throw new RegressionTestException("Expected to hold a position in the mapped contract");
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
        public long DataPoints => 10881;

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
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "79.914%"},
            {"Drawdown", "1.900%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100645.7"},
            {"Net Profit", "0.646%"},
            {"Sharpe Ratio", "3.958"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.372"},
            {"Beta", "0.815"},
            {"Annual Standard Deviation", "0.222"},
            {"Annual Variance", "0.049"},
            {"Information Ratio", "-12.526"},
            {"Tracking Error", "0.052"},
            {"Treynor Ratio", "1.077"},
            {"Total Fees", "$2.15"},
            {"Estimated Strategy Capacity", "$2800000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "20.89%"},
            {"Drawdown Recovery", "3"},
            {"OrderListHash", "2338180a2a964389525a9f1221f97a06"}
        };
    }
}
