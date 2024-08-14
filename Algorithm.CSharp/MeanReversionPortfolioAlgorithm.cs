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
        public long DataPoints => 1113;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 52;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "60"},
            {"Average Win", "1.88%"},
            {"Average Loss", "-0.79%"},
            {"Compounding Annual Return", "8.069%"},
            {"Drawdown", "11.900%"},
            {"Expectancy", "0.748"},
            {"Start Equity", "100000"},
            {"End Equity", "103872.25"},
            {"Net Profit", "3.872%"},
            {"Sharpe Ratio", "0.349"},
            {"Sortino Ratio", "0.375"},
            {"Probabilistic Sharpe Ratio", "29.228%"},
            {"Loss Rate", "48%"},
            {"Win Rate", "52%"},
            {"Profit-Loss Ratio", "2.37"},
            {"Alpha", "-0.085"},
            {"Beta", "1.234"},
            {"Annual Standard Deviation", "0.238"},
            {"Annual Variance", "0.057"},
            {"Information Ratio", "-0.331"},
            {"Tracking Error", "0.16"},
            {"Treynor Ratio", "0.067"},
            {"Total Fees", "$114.36"},
            {"Estimated Strategy Capacity", "$700000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "18.24%"},
            {"OrderListHash", "22337335b8bbfb4fc1093879c3ddd4d8"}
        };
    }
}
