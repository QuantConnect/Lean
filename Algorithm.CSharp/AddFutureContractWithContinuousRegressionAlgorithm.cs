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
    /// Continuous Futures Regression algorithm. Asserting and showcasing the behavior of adding a continuous future
    /// and a future contract at the same time
    /// </summary>
    public class AddFutureContractWithContinuousRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _currentMappedSymbol;
        private Future _continuousContract;
        private Future _futureContract;
        private bool _ended;

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
                contractDepthOffset: 0,
                extendedMarketHours: true
            );

            _futureContract = AddFutureContract(FutureChainProvider.GetFutureContractList(_continuousContract.Symbol, Time).First(),
                extendedMarketHours: true);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (_ended)
            {
                throw new Exception($"Algorithm should of ended!");
            }

            var orders = Transactions.GetOrders().ToList();
            if (orders.Any())
            {
                if (orders.Count != 2)
                {
                    throw new Exception($"Expected 2 orders but got {orders.Count}");
                }

                if (orders.All(x => x.Status == OrderStatus.Filled) && _futureContract.Exchange.ExchangeOpen && _futureContract.Exchange.ExchangeOpen)
                {
                    RemoveSecurity(_futureContract.Symbol);
                    RemoveSecurity(_continuousContract.Symbol);
                    _ended = true;
                }

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
                // Very high limit price so the order is filled in the next time slice where market is open
                LimitOrder(_futureContract.Symbol, 1, Securities[_futureContract.Symbol].Price * 2m);
                LimitOrder(_continuousContract.Mapped, 1, Securities[_continuousContract.Mapped].Price * 2m);
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
        public long DataPoints => 13066;

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
            {"Average Loss", "-0.68%"},
            {"Compounding Annual Return", "-41.466%"},
            {"Drawdown", "1.100%"},
            {"Expectancy", "-1"},
            {"Net Profit", "-0.682%"},
            {"Sharpe Ratio", "-6.861"},
            {"Probabilistic Sharpe Ratio", "1.216%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.27"},
            {"Beta", "0.063"},
            {"Annual Standard Deviation", "0.038"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "-1.844"},
            {"Tracking Error", "0.23"},
            {"Treynor Ratio", "-4.097"},
            {"Total Fees", "$7.40"},
            {"Estimated Strategy Capacity", "$13000000.00"},
            {"Lowest Capacity Asset", "ES VMKLFZIH2MTD"},
            {"Fitness Score", "0.419"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-57.657"},
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
            {"OrderListHash", "05e434fa1937f69b2e0ab3440d5a39a9"}
        };
    }
}
