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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Securities.Future;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm for testing limit orders are filled after hours for futures.
    /// </summary>
    public class LimitOrdersAreFilledAfterHoursForFuturesRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Future _continuousContract;
        private Future _futureContract;
        private int _onDataCallCount;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 6);
            SetEndDate(2013, 10, 10);

            _continuousContract = AddFuture(Futures.Indices.SP500EMini,
                dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                dataMappingMode: DataMappingMode.LastTradingDay,
                contractDepthOffset: 0
            );

            _futureContract = AddFutureContract(FutureChainProvider.GetFutureContractList(_continuousContract.Symbol, Time).First());
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (_onDataCallCount > 2)
            {
                throw new Exception($"Algorithm should of ended!");
            }

            var orders = Transactions.GetOrders().ToList();
            if (++_onDataCallCount > 1)
            {
                if (orders.Count != 2 || orders.Any(order => order.Status != OrderStatus.Filled))
                {
                    throw new Exception($"There should have been 2 orders by the next time slice after placing the limit orders and they should have been filled");
                }

                RemoveSecurity(_futureContract.Symbol);
                RemoveSecurity(_continuousContract.Symbol);

                return;
            }

            if (data.Keys.Count > 2)
            {
                throw new Exception($"Getting data for more than 2 symbols! {string.Join(",", data.Keys.Select(symbol => symbol))}");
            }
            if (UniverseManager.Count != 3)
            {
                throw new Exception($"Expecting 3 universes (chain, continuous and user defined) but have {UniverseManager.Count}");
            }

            if (!Portfolio.Invested)
            {
                // Use a very high limit price so the limit orders get filled immediately
                LimitOrder(_futureContract.Symbol, 1, _futureContract.Price * 2m);
                LimitOrder(_continuousContract.Mapped, 1, _continuousContract.Price * 2m);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                Log($"{orderEvent}");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            Debug($"{Time}-{changes}");

            if (changes.AddedSecurities.Any(security => security.Symbol != _continuousContract.Symbol && security.Symbol != _futureContract.Symbol)
                || changes.RemovedSecurities.Any(security => security.Symbol != _continuousContract.Symbol && security.Symbol != _futureContract.Symbol))
            {
                throw new Exception($"We got an unexpected security changes {changes}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 73;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.16%"},
            {"Compounding Annual Return", "-11.592%"},
            {"Drawdown", "0.200%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.157%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.678"},
            {"Tracking Error", "0.243"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$7.40"},
            {"Estimated Strategy Capacity", "$820000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Fitness Score", "0.419"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-77.549"},
            {"Portfolio Turnover", "0.838"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "18e402ff4f371652800c8c4bf76056eb"}
        };
    }
}
