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
            SetStartDate(2014, 10, 07);
            SetEndDate(2014, 10, 11);

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
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                if (data.Keys.Single().Value != "AAPL")
                {
                    throw new Exception($"Unexpected symbol was added to the universe: {data.Keys.Single()}");
                }
                SetHoldings(data.Keys.Single(), 1);
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
            {"Compounding Annual Return", "500.622%"},
            {"Drawdown", "0.300%"},
            {"Expectancy", "0"},
            {"Net Profit", "1.984%"},
            {"Sharpe Ratio", "14.933"},
            {"Probabilistic Sharpe Ratio", "90.520%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "2.211"},
            {"Beta", "-0.467"},
            {"Annual Standard Deviation", "0.166"},
            {"Annual Variance", "0.028"},
            {"Information Ratio", "7.778"},
            {"Tracking Error", "0.394"},
            {"Treynor Ratio", "-5.313"},
            {"Total Fees", "$5.40"},
            {"Fitness Score", "0.244"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "1748.254"},
            {"Portfolio Turnover", "0.244"},
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
            {"OrderListHash", "189eee984d524162b9676b83dab4d73c"}
        };
    }
}
