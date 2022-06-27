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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests we can add future option contracts from contracts in the future chain
    /// </summary>
    public class AddFutureOptionContractFromFutureChainRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _addedOptions;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 4);
            SetEndDate(2020, 1, 6);

            var es = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute, Market.CME);
            es.SetFilter((futureFilter) =>
            {
                return futureFilter.Expiration(0, 365).ExpirationCycle(new[] { 3, 6 });
            });
        }

        public override void OnData(Slice data)
        {
            if (!_addedOptions)
            {
                _addedOptions = true;
                foreach (var futuresContracts in data.FutureChains.Values)
                {
                    foreach (var contract in futuresContracts)
                    {
                        var option_contract_symbols = OptionChainProvider.GetOptionContractList(contract.Symbol, Time).ToList();
                        if(option_contract_symbols.Count == 0)
                        {
                            continue;
                        }

                        foreach (var option_contract_symbol in option_contract_symbols.OrderBy(x => x.ID.Date)
                            .ThenBy(x => x.ID.StrikePrice)
                            .ThenBy(x => x.ID.OptionRight).Take(5))
                        {
                            AddOptionContract(option_contract_symbol);
                        }
                    }
                }
            }

            if (Portfolio.Invested)
            {
                return;
            }

            foreach (var chain in data.OptionChains.Values)
            {
                foreach (var option in chain.Contracts.Keys)
                {
                    MarketOrder(option, 1);
                    MarketOrder(option.Underlying, 1);
                }
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
        public long DataPoints => 46583;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "20"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-47.647%"},
            {"Drawdown", "3.200%"},
            {"Expectancy", "0"},
            {"Net Profit", "-0.530%"},
            {"Sharpe Ratio", "-8.194"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-1.345"},
            {"Beta", "1.391"},
            {"Annual Standard Deviation", "0.06"},
            {"Annual Variance", "0.004"},
            {"Information Ratio", "-66.031"},
            {"Tracking Error", "0.017"},
            {"Treynor Ratio", "-0.351"},
            {"Total Fees", "$37.00"},
            {"Estimated Strategy Capacity", "$3400000.00"},
            {"Lowest Capacity Asset", "ES 31C3JQS9D84PW|ES XCZJLC9NOB29"},
            {"Fitness Score", "0.5"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "-94.467"},
            {"Portfolio Turnover", "5.578"},
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
            {"OrderListHash", "7fbb8c0a1f5eee780f0b37efafbbdc4b"}
        };
    }
}
