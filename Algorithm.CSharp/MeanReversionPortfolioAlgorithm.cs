/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http, //www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

using System;
using System.Collections.Generic;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Interfaces;

namespace QuantConnect.DataLibrary.Tests
{
    /// <summary>
    /// Example algorithm of using MeanReversionPortfolioConstructionModel
    /// </summary>
    public class MeanReversionPortfolioAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2020, 9, 1);
            SetEndDate(2021, 2, 28);
            SetCash(100000);
            
            SetSecurityInitializer(security => security.SetMarketPrice(GetLastKnownPrice(security)));

            foreach (var ticker in new List<string>{"SPY", "AAPL"})
            {
                AddEquity(ticker, Resolution.Daily);
            }
            
            AddAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));
            SetPortfolioConstruction(new MeanReversionPortfolioConstructionModel());
        }

        public bool CanRunLocally { get; } = true;
        public Language[] Languages { get; } = {Language.CSharp};

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1172;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "74"},
            {"Average Win", "2.31%"},
            {"Average Loss", "-0.30%"},
            {"Compounding Annual Return", "19.882%"},
            {"Drawdown", "12.300%"},
            {"Expectancy", "2.064"},
            {"Net Profit", "9.303%"},
            {"Sharpe Ratio", "0.642"},
            {"Probabilistic Sharpe Ratio", "36.784%"},
            {"Loss Rate", "65%"},
            {"Win Rate", "35%"},
            {"Profit-Loss Ratio", "7.68"},
            {"Alpha", "-0.022"},
            {"Beta", "1.299"},
            {"Annual Standard Deviation", "0.246"},
            {"Annual Variance", "0.06"},
            {"Information Ratio", "0.12"},
            {"Tracking Error", "0.163"},
            {"Treynor Ratio", "0.121"},
            {"Total Fees", "$134.65"},
            {"Estimated Strategy Capacity", "$250000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Fitness Score", "0.144"},
            {"Kelly Criterion Estimate", "-0.659"},
            {"Kelly Criterion Probability Value", "0.566"},
            {"Sortino Ratio", "0.917"},
            {"Return Over Maximum Drawdown", "1.618"},
            {"Portfolio Turnover", "0.211"},
            {"Total Insights Generated", "248"},
            {"Total Insights Closed", "244"},
            {"Total Insights Analysis Completed", "244"},
            {"Long Insight Count", "248"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$678641.05829662688740489801743"},
            {"Total Accumulated Estimated Alpha Value", "$4053937.76629692613000"},
            {"Mean Population Estimated Insight Value", "$16614.499042200516926229508197"},
            {"Mean Population Direction", "39.344262295081955%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "37.87194284256898%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "2784f0faf44e32c02b4083579565d1cc"}
        };
    }
}
