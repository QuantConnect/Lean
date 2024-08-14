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
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm which tests a fine fundamental filtered universe, related to GH issue 4127
    /// </summary>
    public class FineFundamentalFilteredUniverseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2014, 10, 08);
            SetEndDate(2014, 10, 13);

            UniverseSettings.Resolution = Resolution.Daily;

            var customUniverseSymbol = new Symbol(SecurityIdentifier.GenerateConstituentIdentifier(
                    "constituents-universe-qctest",
                    SecurityType.Equity,
                    Market.USA),
                "constituents-universe-qctest");

            // we use test ConstituentsUniverse
            AddUniverse(new ConstituentsUniverse(customUniverseSymbol, UniverseSettings), FineSelectionFunction);
        }

        private IEnumerable<Symbol> FineSelectionFunction(IEnumerable<FineFundamental> data)
        {
            return data.Where(fundamental => fundamental.CompanyProfile.HeadquarterCity.Equals("Cupertino"))
                .Select(fundamental => fundamental.Symbol);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="slice">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                if (slice.Keys.Single().Value != "AAPL")
                {
                    throw new RegressionTestException($"Unexpected symbol was added to the universe: {slice.Keys.Single()}");
                }
                SetHoldings(slice.Keys.Single(), 1);
            }
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
        public long DataPoints => 41;

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
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "-66.721%"},
            {"Drawdown", "1.700%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "98306.39"},
            {"Net Profit", "-1.694%"},
            {"Sharpe Ratio", "-9.567"},
            {"Sortino Ratio", "-11.484"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.261"},
            {"Beta", "0.353"},
            {"Annual Standard Deviation", "0.061"},
            {"Annual Variance", "0.004"},
            {"Information Ratio", "3.33"},
            {"Tracking Error", "0.1"},
            {"Treynor Ratio", "-1.655"},
            {"Total Fees", "$21.85"},
            {"Estimated Strategy Capacity", "$360000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "16.82%"},
            {"OrderListHash", "6f46dbb94071af805eee55f78adf3a23"}
        };
    }
}
