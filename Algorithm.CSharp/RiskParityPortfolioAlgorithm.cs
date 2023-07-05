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
    /// Example algorithm of using RiskParityPortfolioConstructionModel
    /// </summary>
    public class RiskParityPortfolioAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2021, 2, 21);
            SetEndDate(2021, 3, 30);
            SetCash(100000);
            SetSecurityInitializer(security => security.SetMarketPrice(GetLastKnownPrice(security)));

            AddEquity("SPY", Resolution.Daily);
            AddEquity("AAPL", Resolution.Daily);
            
            AddAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));
            SetPortfolioConstruction(new RiskParityPortfolioConstructionModel());
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
        public long DataPoints => 252;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 509;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "30"},
            {"Average Win", "0.01%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "3.828%"},
            {"Drawdown", "4.900%"},
            {"Expectancy", "-1"},
            {"Net Profit", "0.391%"},
            {"Sharpe Ratio", "0.235"},
            {"Probabilistic Sharpe Ratio", "38.526%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "1.31"},
            {"Alpha", "-0.101"},
            {"Beta", "1.224"},
            {"Annual Standard Deviation", "0.201"},
            {"Annual Variance", "0.04"},
            {"Information Ratio", "-0.824"},
            {"Tracking Error", "0.09"},
            {"Treynor Ratio", "0.039"},
            {"Total Fees", "$30.65"},
            {"Estimated Strategy Capacity", "$1100000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "2.91%"},
            {"OrderListHash", "3c10caa2675a03d157fe476e3af28847"}
        };

    }
}
