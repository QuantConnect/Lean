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
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 1115;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 52;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "65"},
            {"Average Win", "2.31%"},
            {"Average Loss", "-0.41%"},
            {"Compounding Annual Return", "20.043%"},
            {"Drawdown", "12.300%"},
            {"Expectancy", "1.841"},
            {"Start Equity", "100000"},
            {"End Equity", "109374.62"},
            {"Net Profit", "9.375%"},
            {"Sharpe Ratio", "0.636"},
            {"Sortino Ratio", "0.722"},
            {"Probabilistic Sharpe Ratio", "36.899%"},
            {"Loss Rate", "57%"},
            {"Win Rate", "43%"},
            {"Profit-Loss Ratio", "5.63"},
            {"Alpha", "-0.02"},
            {"Beta", "1.3"},
            {"Annual Standard Deviation", "0.246"},
            {"Annual Variance", "0.061"},
            {"Information Ratio", "0.126"},
            {"Tracking Error", "0.163"},
            {"Treynor Ratio", "0.12"},
            {"Total Fees", "$122.78"},
            {"Estimated Strategy Capacity", "$370000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "17.55%"},
            {"OrderListHash", "7bf3020a7da8a4acd6b71025281161d9"}
        };
    }
}
