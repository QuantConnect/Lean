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

using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example algorithm defines its own custom coarse/fine fundamental selection model
    /// with sector weighted portfolio
    /// </summary>
    public class SectorWeightingFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            // Set requested data resolution
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 25);
            SetEndDate(2014, 04, 07);
            SetCash(100000);

            SetUniverseSelection(new FineFundamentalUniverseSelectionModel(SelectCoarse, SelectFine));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, QuantConnect.Time.OneDay));
            SetPortfolioConstruction(new SectorWeightingPortfolioConstructionModel());
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status.IsFill())
            {
                Debug($"Order event: {orderEvent}. Holding value: {Securities[orderEvent.Symbol].Holdings.AbsoluteHoldingsValue}");
            }
        }

        private IEnumerable<Symbol> SelectCoarse(IEnumerable<CoarseFundamental> coarse)
        {
            var tickers = Time.Date < new DateTime(2014, 4, 1)
                // IndustryTemplateCode of AAPL and IBM is N and AIG is I 
                ? new[] { "AAPL", "AIG", "IBM" }
                // IndustryTemplateCode of GOOG is N and BAC is B. SPY have no fundamentals
                : new[] { "GOOG", "BAC", "SPY" };

            return tickers.Select(x => QuantConnect.Symbol.Create(x, SecurityType.Equity, Market.USA));
        }

        private IEnumerable<Symbol> SelectFine(IEnumerable<FineFundamental> fine) => fine.Select(f => f.Symbol);

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
            {"Total Trades", "19"},
            {"Average Win", "0.29%"},
            {"Average Loss", "-0.05%"},
            {"Compounding Annual Return", "-78.953%"},
            {"Drawdown", "6.600%"},
            {"Expectancy", "0.310"},
            {"Net Profit", "-5.802%"},
            {"Sharpe Ratio", "-5.76"},
            {"Loss Rate", "82%"},
            {"Win Rate", "18%"},
            {"Profit-Loss Ratio", "6.21"},
            {"Alpha", "-1.233"},
            {"Beta", "-0.008"},
            {"Annual Standard Deviation", "0.214"},
            {"Annual Variance", "0.046"},
            {"Information Ratio", "-4.235"},
            {"Tracking Error", "0.237"},
            {"Treynor Ratio", "147.325"},
            {"Total Fees", "$73.27"},
            {"Fitness Score", "0.022"},
            {"Kelly Criterion Estimate", "-11.683"},
            {"Kelly Criterion Probability Value", "0.792"},
            {"Sortino Ratio", "-3.782"},
            {"Return Over Maximum Drawdown", "-11.953"},
            {"Portfolio Turnover", "0.337"},
            {"Total Insights Generated", "24"},
            {"Total Insights Closed", "22"},
            {"Total Insights Analysis Completed", "22"},
            {"Long Insight Count", "24"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$-1906811"},
            {"Total Accumulated Estimated Alpha Value", "$-900438.4"},
            {"Mean Population Estimated Insight Value", "$-40929.02"},
            {"Mean Population Direction", "27.2727%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "57.4228%"},
            {"Rolling Averaged Population Magnitude", "0%"}
        };
    }
}