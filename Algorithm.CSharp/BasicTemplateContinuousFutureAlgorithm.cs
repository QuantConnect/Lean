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
    /// Basic Continuous Futures Template Algorithm
    /// </summary>
    public class BasicTemplateContinuousFutureAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;
        private Security _currentContract;
        private SimpleMovingAverage _fast;
        private SimpleMovingAverage _slow;

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
                contractDepthOffset: 0
            );

            _fast = SMA(_continuousContract.Symbol, 3, Resolution.Daily);
            _slow = SMA(_continuousContract.Symbol, 10, Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            foreach (var changedEvent in data.SymbolChangedEvents.Values)
            {
                Debug($"{Time} - SymbolChanged event: {changedEvent}");
                if (Time.TimeOfDay != TimeSpan.Zero)
                {
                    throw new Exception($"{Time} unexpected symbol changed event {changedEvent}!");
                }
            }

            if (!Portfolio.Invested)
            {
                if(_fast > _slow)
                {
                    _currentContract = Securities[_continuousContract.Mapped];
                    Buy(_currentContract.Symbol, 1);
                }
            }
            else if(_fast < _slow)
            {
                Liquidate();
            }

            // We check exchange hours because the contract mapping can call OnData outside of regular hours.
            if (_currentContract != null && _currentContract.Symbol != _continuousContract.Mapped && _continuousContract.Exchange.ExchangeOpen)
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
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 709638;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-0.033%"},
            {"Drawdown", "0.000%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.017%"},
            {"Sharpe Ratio", "-1.173"},
            {"Probabilistic Sharpe Ratio", "0.011%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0"},
            {"Beta", "-0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-2.752"},
            {"Tracking Error", "0.082"},
            {"Treynor Ratio", "1.883"},
            {"Total Fees", "$4.30"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Return Over Maximum Drawdown", "-1.996"},
            {"Portfolio Turnover", "0.01"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"OrderListHash", "1fd4b49e9450800981c6dead2bbca995"}
        };
    }
}
