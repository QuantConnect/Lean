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
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests a custom filter function when creating an ETF constituents universe for SPY
    /// </summary>
    public class ETFConstituentUniverseFilterFunctionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _aapl;
        private Symbol _spy;
        private bool _filtered;
        private bool _securitiesChanged;
        private bool _receivedData;
        
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2020, 12, 1);
            SetEndDate(2021, 1, 31);
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Hour;
            
            _spy = AddEquity("SPY", Resolution.Hour).Symbol;
            _aapl = QuantConnect.Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            
            AddUniverse(new ETFConstituentsUniverse(_spy, UniverseSettings, FilterETFs));
        }

        /// <summary>
        /// Filters ETFs, performing some sanity checks
        /// </summary>
        /// <param name="constituents">Constituents of the ETF universe added above</param>
        /// <returns>Constituent Symbols to add to algorithm</returns>
        /// <exception cref="ArgumentException">Constituents collection was not structured as expected</exception>
        private IEnumerable<Symbol> FilterETFs(IEnumerable<ETFConstituentData> constituents)
        {
            var constituentsData = constituents.ToHashSet();
            var constituentsSymbols = constituentsData.Select(x => x.Symbol).ToList();
            if (constituentsData.Count == 0)
            {
                throw new ArgumentException($"Constituents collection is empty on {UtcTime:yyyy-MM-dd HH:mm:ss.fff}");
            }
            if (!constituentsSymbols.Contains(_aapl))
            {
                throw new ArgumentException("AAPL is not in the constituents data provided to the algorithm");
            }

            var aaplData = constituentsData.Single(x => x.Symbol == _aapl);
            if (aaplData.Weight == 0m)
            {
                throw new ArgumentException("AAPL weight is expected to be a non-zero value");
            }

            _filtered = true;
            return constituentsSymbols;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!_filtered && data.Bars.Count != 0 && data.Bars.ContainsKey(_aapl))
            {
                throw new Exception("AAPL TradeBar data added to algorithm before constituent universe selection took place");
            }

            if (data.Bars.Count == 1 && data.Bars.ContainsKey(_spy))
            {
                return;
            }
            
            if (data.Bars.Count != 0 && !data.Bars.ContainsKey(_aapl))
            {
                throw new Exception($"Expected AAPL TradeBar data in OnData on {UtcTime:yyyy-MM-dd HH:mm:ss}");
            }

            if (!Portfolio.Invested)
            {
                SetHoldings(_aapl, 0.5m);
            }

            _receivedData = true;
        }

        /// <summary>
        /// Checks if new securities have been added to the algorithm after universe selection has occurred
        /// </summary>
        /// <param name="changes">Security changes</param>
        /// <exception cref="ArgumentException">Expected number of stocks were not added to the algorithm</exception>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            if (_filtered && !_securitiesChanged && changes.AddedSecurities.Count < 500)
            {
                throw new ArgumentException($"Added SPY S&P 500 ETF to algorithm, but less than 500 equities were loaded (added {changes.AddedSecurities.Count} securities)");
            }

            _securitiesChanged = true;
        }

        /// <summary>
        /// Ensures that all expected events were triggered by the end of the algorithm
        /// </summary>
        /// <exception cref="Exception">An expected event didn't happen</exception>
        public override void OnEndOfAlgorithm()
        {
            if (!_filtered)
            {
                throw new Exception("Universe selection was never triggered");
            }
            if (!_securitiesChanged)
            {
                throw new Exception("Security changes never propagated to the algorithm");
            }
            if (!_receivedData)
            {
                throw new Exception("Data was never loaded for the S&P 500 constituent AAPL");
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "27.575%"},
            {"Drawdown", "5.400%"},
            {"Expectancy", "0"},
            {"Net Profit", "4.061%"},
            {"Sharpe Ratio", "1.788"},
            {"Probabilistic Sharpe Ratio", "58.837%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.226"},
            {"Beta", "0.698"},
            {"Annual Standard Deviation", "0.17"},
            {"Annual Variance", "0.029"},
            {"Information Ratio", "1.297"},
            {"Tracking Error", "0.149"},
            {"Treynor Ratio", "0.434"},
            {"Total Fees", "$2.05"},
            {"Estimated Strategy Capacity", "$180000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Fitness Score", "0.011"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "5.042"},
            {"Return Over Maximum Drawdown", "10.294"},
            {"Portfolio Turnover", "0.012"},
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
            {"OrderListHash", "da16cdc1a6a1260a20db0f10607fd913"}
        };
    }
}
