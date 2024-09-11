
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
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing github issue #5191 where the symbol was removed from the cache
    /// even if a subscription is still present
    /// </summary>
    public class UniverseSelectionSymbolCacheRemovalRegressionTest : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private bool _optionWasRemoved;
        private Symbol _optionContract;
        private Symbol _equitySymbol;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 23);

            AddEquity("AAPL", Resolution.Daily);
            _equitySymbol = AddEquity("TWX", Resolution.Minute).Symbol;

            var contracts = OptionChain(_equitySymbol).ToList();

            var callOptionSymbol = contracts
                .Where(c => c.ID.OptionRight == OptionRight.Call)
                .OrderBy(c => c.ID.Date)
                .First();
            _optionContract = AddOptionContract(callOptionSymbol).Symbol;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            var symbol = SymbolCache.GetSymbol("TWX");
            if (symbol == null)
            {
                throw new RegressionTestException("Unexpected removal of symbol from cache!");
            }

            foreach (var dataDelisting in slice.Delistings.Where(pair => pair.Value.Type == DelistingType.Delisted))
            {
                if (dataDelisting.Key != _optionContract)
                {
                    throw new RegressionTestException("Unexpected delisting event!");
                }
                _optionWasRemoved = true;
            }

            if (!Portfolio.Invested)
            {
                SetHoldings("AAPL", 0.1);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_optionWasRemoved)
            {
                throw new RegressionTestException("Option contract was not removed!");
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
        public long DataPoints => 24288;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 1;

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
            {"Compounding Annual Return", "-4.228%"},
            {"Drawdown", "0.400%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "99779.30"},
            {"Net Profit", "-0.221%"},
            {"Sharpe Ratio", "-3.185"},
            {"Sortino Ratio", "-4.277"},
            {"Probabilistic Sharpe Ratio", "17.836%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.047"},
            {"Beta", "0.053"},
            {"Annual Standard Deviation", "0.012"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-4.592"},
            {"Tracking Error", "0.047"},
            {"Treynor Ratio", "-0.714"},
            {"Total Fees", "$2.39"},
            {"Estimated Strategy Capacity", "$2900000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "0.53%"},
            {"OrderListHash", "ff4e9e05d7a60c96ccc6e7541d200168"}
        };
    }
}
