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
        private Dictionary<Symbol, ETFConstituentUniverse> _etfConstituentData = new Dictionary<Symbol, ETFConstituentUniverse>();
        
        private Symbol _aapl;
        private Symbol _spy;
        private bool _filtered;
        private bool _securitiesChanged;
        private bool _receivedData;
        private bool _etfRebalanced;
        private int _rebalanceCount;
        private int _rebalanceAssetCount;
        
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
            
            AddUniverse(Universe.ETF(_spy, universeFilterFunc: FilterETFs));
        }

        /// <summary>
        /// Filters ETFs, performing some sanity checks
        /// </summary>
        /// <param name="constituents">Constituents of the ETF universe added above</param>
        /// <returns>Constituent Symbols to add to algorithm</returns>
        /// <exception cref="ArgumentException">Constituents collection was not structured as expected</exception>
        private IEnumerable<Symbol> FilterETFs(IEnumerable<ETFConstituentUniverse> constituents)
        {
            var constituentsData = constituents.ToList();
            _etfConstituentData = constituentsData.ToDictionary(x => x.Symbol, x => x);
            
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
            _etfRebalanced = true;
            
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

            _receivedData = true;
            // If the ETF hasn't changed its weights, then let's not update our holdings
            if (!_etfRebalanced)
            {
                return;
            }

            foreach (var bar in data.Bars.Values)
            {
                if (_etfConstituentData.TryGetValue(bar.Symbol, out var constituentData) && 
                    constituentData.Weight != null && 
                    constituentData.Weight >= 0.0001m)
                {
                    // If the weight of the constituent is less than 1%, then it will be set to 1%
                    // If the weight of the constituent exceeds more than 5%, then it will be capped to 5%
                    // Otherwise, if the weight falls in between, then we use that value.
                    var boundedWeight = Math.Max(0.01m, Math.Min(constituentData.Weight.Value, 0.05m));
                    SetHoldings(bar.Symbol, boundedWeight);
                    
                    if (_etfRebalanced)
                    {
                        _rebalanceCount++;
                    }
                    _etfRebalanced = false;
                    _rebalanceAssetCount++;
                }
            }
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
            if (_rebalanceCount != 2)
            {
                throw new Exception($"Expected 2 rebalances, instead rebalanced: {_rebalanceCount}");
            }
            if (_rebalanceAssetCount != 8)
            {
                throw new Exception($"Invested in {_rebalanceAssetCount} assets (expected 8)");
            }
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 2722;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "4"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "1.989%"},
            {"Drawdown", "0.600%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100322.52"},
            {"Net Profit", "0.323%"},
            {"Sharpe Ratio", "0.838"},
            {"Sortino Ratio", "1.122"},
            {"Probabilistic Sharpe Ratio", "50.081%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.005"},
            {"Beta", "0.098"},
            {"Annual Standard Deviation", "0.014"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-0.614"},
            {"Tracking Error", "0.096"},
            {"Treynor Ratio", "0.123"},
            {"Total Fees", "$4.00"},
            {"Estimated Strategy Capacity", "$130000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.13%"},
            {"OrderListHash", "0c0cb7214d49cee63fc08115f62fe357"}
        };
    }
}
