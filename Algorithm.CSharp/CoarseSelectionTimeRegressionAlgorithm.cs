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

using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm that reproduces GH issues 3410 and 3409.
    /// Coarse universe selection should start from the algorithm start date.
    /// Data returned by history requests performed from the selection method should be up to date.
    /// </summary>
    public class CoarseSelectionTimeRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _spy;
        private decimal _spyPrice;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 03, 25);
            SetEndDate(2014, 04, 01);

            _spy = AddEquity("SPY", Resolution.Daily).Symbol;

            UniverseSettings.Resolution = Resolution.Daily;
            AddUniverse(CoarseSelectionFunction);
        }

        public IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            var sortedByDollarVolume = coarse.OrderByDescending(x => x.DollarVolume);
            var top = sortedByDollarVolume
                .Where(fundamental => fundamental.Symbol != _spy) // ignore spy
                .Take(1);

            var historyCoarseSpyPrice = History(_spy, 1).First().Close;
            if (_spyPrice != 0 && (historyCoarseSpyPrice == 0 ||  historyCoarseSpyPrice != _spyPrice))
            {
                throw new RegressionTestException($"Unexpected SPY price: {historyCoarseSpyPrice}");
            }
            _spyPrice = 0;

            return top.Select(x => x.Symbol);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (slice.Count != 2)
            {
                throw new RegressionTestException($"Unexpected data count: {slice.Count}");
            }
            if (ActiveSecurities.Count != 2)
            {
                throw new RegressionTestException($"Unexpected ActiveSecurities count: {ActiveSecurities.Count}");
            }

            // we get the data at 4PM, selection happening at midnight
            _spyPrice = Securities[_spy].Price;
            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 1);
                Debug("Purchased Stock");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 49660;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 6;

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
            {"Compounding Annual Return", "36.033%"},
            {"Drawdown", "1.300%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100676.75"},
            {"Net Profit", "0.677%"},
            {"Sharpe Ratio", "2.646"},
            {"Sortino Ratio", "2.77"},
            {"Probabilistic Sharpe Ratio", "58.013%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.264"},
            {"Beta", "1.183"},
            {"Annual Standard Deviation", "0.103"},
            {"Annual Variance", "0.011"},
            {"Information Ratio", "-8.158"},
            {"Tracking Error", "0.022"},
            {"Treynor Ratio", "0.231"},
            {"Total Fees", "$3.07"},
            {"Estimated Strategy Capacity", "$930000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "12.65%"},
            {"OrderListHash", "87438e51988f37757a2d7f97389483ea"}
        };
    }
}
