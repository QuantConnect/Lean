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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using Futures = QuantConnect.Securities.Futures;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic Continuous Futures Template Algorithm with extended market hours
    /// </summary>
    public class BasicTemplateContinuousFutureWithExtendedMarketAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;
        private Security _currentContract;
        private SimpleMovingAverage _fast;
        private SimpleMovingAverage _slow;

        // Minimum SMA gap required before acting on a cross; see the workaround note in OnData.
        private const decimal CrossThreshold = 0.001m;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 7, 1);
            SetEndDate(2014, 1, 1);

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.LastTradingDay,
                contractDepthOffset: 0,
                extendedMarketHours: true
            );

            _fast = SMA(_continuousContract.Symbol, 4, Resolution.Daily);
            _slow = SMA(_continuousContract.Symbol, 10, Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            foreach (var changedEvent in slice.SymbolChangedEvents.Values)
            {
                Debug($"{Time} - SymbolChanged event: {changedEvent}");
                if (Time.TimeOfDay != TimeSpan.Zero)
                {
                    throw new RegressionTestException($"{Time} unexpected symbol changed event {changedEvent}!");
                }
            }

            if (!IsMarketOpen(_continuousContract.Symbol))
            {
                return;
            }

            // This is just to limit the amount of orders done in this regression test, since data in the repo is limited.
            // Also limit it to 3 orders so that the continuous contract rolls happens with an open position.
            if (Time < new DateTime(2013, 11, 12) && Transactions.OrdersCount < 3)
            {
                // Workaround so the C# and Python versions take the exact same trades on the limited
                // sample data in the repository (decimal vs double rounding can disagree at a cross).
                if (!Portfolio.Invested)
                {
                    if (_fast.Current.Value - _slow.Current.Value > CrossThreshold)
                    {
                        _currentContract = Securities[_continuousContract.Mapped];
                        Buy(_currentContract.Symbol, 1);
                    }
                }
                else if (_slow.Current.Value - _fast.Current.Value > CrossThreshold)
                {
                    Liquidate();
                }
            }

            if (_currentContract != null && _currentContract.Symbol != _continuousContract.Mapped)
            {
                Log($"{Time} - rolling position from {_currentContract.Symbol} to {_continuousContract.Mapped}");

                var currentPositionSize = _currentContract.Holdings.Quantity;
                Liquidate(_currentContract.Symbol);
                Buy(_continuousContract.Mapped, currentPositionSize);
                _currentContract = Securities[_continuousContract.Mapped];
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{orderEvent}");
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            Debug($"{Time}-{changes}");
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
        public long DataPoints => 504530;

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
            {"Total Orders", "3"},
            {"Average Win", "6.15%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "13.813%"},
            {"Drawdown", "1.400%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "106741.4"},
            {"Net Profit", "6.741%"},
            {"Sharpe Ratio", "2.003"},
            {"Sortino Ratio", "2.845"},
            {"Probabilistic Sharpe Ratio", "87.787%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "100%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.069"},
            {"Beta", "0.086"},
            {"Annual Standard Deviation", "0.044"},
            {"Annual Variance", "0.002"},
            {"Information Ratio", "-1.506"},
            {"Tracking Error", "0.086"},
            {"Treynor Ratio", "1.023"},
            {"Total Fees", "$6.45"},
            {"Estimated Strategy Capacity", "$3700000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Portfolio Turnover", "1.37%"},
            {"Drawdown Recovery", "18"},
            {"OrderListHash", "764ab9f6ea662a60e41daedb9613b246"}
        };
    }
}
