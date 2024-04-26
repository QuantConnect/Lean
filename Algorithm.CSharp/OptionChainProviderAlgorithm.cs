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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of the Option Chain Provider -- a much faster mechanism for manually specifying the option contracts you'd like to receive
    /// data for and manually subscribing to them.
    /// </summary>
    /// <meta name="tag" content="strategy example" />
    /// <meta name="tag" content="options" />
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="selecting options" />
    /// <meta name="tag" content="manual selection" />
    public class OptionChainProviderAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _equitySymbol;
        private Symbol _optionContract = string.Empty;
        private readonly HashSet<Symbol> _contractsAdded = new HashSet<Symbol>();

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);
            SetCash(100000);
            var equity = AddEquity("GOOG", Resolution.Minute);
            equity.SetDataNormalizationMode(DataNormalizationMode.Raw);
            _equitySymbol = equity.Symbol;
        }

        public override void OnData(Slice data)
        {
            if (!Portfolio[_equitySymbol].Invested)
            {
                MarketOrder(_equitySymbol, 100);
            }

            if (!(Securities.ContainsKey(_optionContract) && Portfolio[_optionContract].Invested))
            {
                var contracts = OptionChainProvider.GetOptionContractList(_equitySymbol, data.Time);
                var underlyingPrice = Securities[_equitySymbol].Price;
                // filter the out-of-money call options from the contract list which expire in 10 to 30 days from now on
                var otmCalls = (from symbol in contracts
                                where symbol.ID.OptionRight == OptionRight.Call
                                where symbol.ID.StrikePrice - underlyingPrice > 0
                                where ((symbol.ID.Date - data.Time).TotalDays < 30 && (symbol.ID.Date - data.Time).TotalDays > 10)
                                select symbol);

                if (otmCalls.Count() != 0)
                {
                    _optionContract = otmCalls.OrderBy(x => x.ID.Date)
                                          .ThenBy(x => (x.ID.StrikePrice - underlyingPrice))
                                          .FirstOrDefault();
                    if (_contractsAdded.Add(_optionContract))
                    {
                        // use AddOptionContract() to subscribe the data for specified contract
                        AddOptionContract(_optionContract, Resolution.Minute);
                    }
                }
                else _optionContract = string.Empty;
            }
            if (Securities.ContainsKey(_optionContract) && !Portfolio[_optionContract].Invested)
            {
                MarketOrder(_optionContract, -1);
            }
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
        public long DataPoints => 881;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99890"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$2.00"},
            {"Estimated Strategy Capacity", "$6300000.00"},
            {"Lowest Capacity Asset", "GOOCV W723A0UB7HTY|GOOCV VP83T1ZUHROL"},
            {"Portfolio Turnover", "76.04%"},
            {"OrderListHash", "d7290944d7fee84f232b47d658010730"}
        };
    }
}
