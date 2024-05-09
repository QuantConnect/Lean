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
    /// Example algorithm of using RiskParityPortfolioConstructionModel.
    /// Reproduces https://github.com/QuantConnect/Lean/issues/7476
    /// </summary>
    public class RiskParityPortfolioWeightsCheckAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private CustomRiskParityPortfolioConstructionModel _portfolioConstructionModel;

        public override void Initialize()
        {
            SetStartDate(2021, 2, 21);
            SetEndDate(2021, 3, 30);
            SetCash(100000);
            SetSecurityInitializer(security => security.SetMarketPrice(GetLastKnownPrice(security)));

            AddEquity("SPY", Resolution.Daily);
            AddEquity("AAPL", Resolution.Daily);

            AddAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));

            _portfolioConstructionModel = new CustomRiskParityPortfolioConstructionModel();
            SetPortfolioConstruction(_portfolioConstructionModel);
        }

        public override void OnEndOfAlgorithm()
        {
            foreach (var kvp in _portfolioConstructionModel.Weights)
            {
                var weights = kvp.Value;
                if (weights.Count < 2)
                {
                    throw new Exception($"Expected multiple different weigths from the PCM for {kvp.Key}");
                }
            }
        }

        private class CustomRiskParityPortfolioConstructionModel : RiskParityPortfolioConstructionModel
        {
            public Dictionary<Symbol, HashSet<double>> Weights { get; } = new();

            protected override Dictionary<Insight, double> DetermineTargetPercent(List<Insight> activeInsights)
            {
                var result = base.DetermineTargetPercent(activeInsights);
                foreach (var kvp in result)
                {
                    if (!Weights.TryGetValue(kvp.Key.Symbol, out var symbolWeigths))
                    {
                        symbolWeigths = new HashSet<double>();
                        Weights[kvp.Key.Symbol] = symbolWeigths;
                    }

                    symbolWeigths.Add(kvp.Value);
                }

                return result;
            }
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
        public long DataPoints => 252;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 514;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "31"},
            {"Average Win", "0.01%"},
            {"Average Loss", "-0.01%"},
            {"Compounding Annual Return", "5.011%"},
            {"Drawdown", "4.900%"},
            {"Expectancy", "-0.273"},
            {"Start Equity", "100000"},
            {"End Equity", "100509.82"},
            {"Net Profit", "0.510%"},
            {"Sharpe Ratio", "0.265"},
            {"Sortino Ratio", "0.371"},
            {"Probabilistic Sharpe Ratio", "39.108%"},
            {"Loss Rate", "58%"},
            {"Win Rate", "42%"},
            {"Profit-Loss Ratio", "0.75"},
            {"Alpha", "-0.092"},
            {"Beta", "1.22"},
            {"Annual Standard Deviation", "0.2"},
            {"Annual Variance", "0.04"},
            {"Information Ratio", "-0.748"},
            {"Tracking Error", "0.088"},
            {"Treynor Ratio", "0.043"},
            {"Total Fees", "$31.65"},
            {"Estimated Strategy Capacity", "$1300000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "3.08%"},
            {"OrderListHash", "2766e0ba2ed0419a2db5240b41494390"}
        };

    }
}
