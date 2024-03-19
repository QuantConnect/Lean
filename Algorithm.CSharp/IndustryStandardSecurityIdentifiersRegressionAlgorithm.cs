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

using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm illustrating how to get a security's industry-standard identifier from its <see cref="Symbol"/>
    /// </summary>
    public class IndustryStandardSecurityIdentifiersRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 05);

            var equity = AddEquity("AAPL").Symbol;

            var cusip = equity.CUSIP;
            var compositeFigi = equity.CompositeFIGI;
            var sedol = equity.SEDOL;
            var isin = equity.ISIN;
            var cik = equity.CIK;

            CheckSymbolRepresentation(cusip, "CUSIP");
            CheckSymbolRepresentation(compositeFigi, "Composite FIGI");
            CheckSymbolRepresentation(sedol, "SEDOL");
            CheckSymbolRepresentation(isin, "ISIN");
            CheckSymbolRepresentation($"{cik}", "CIK");

            // Check Symbol API vs QCAlgorithm API
            CheckAPIsSymbolRepresentations(cusip, CUSIP(equity), "CUSIP");
            CheckAPIsSymbolRepresentations(compositeFigi, CompositeFIGI(equity), "Composite FIGI");
            CheckAPIsSymbolRepresentations(sedol, SEDOL(equity), "SEDOL");
            CheckAPIsSymbolRepresentations(isin, ISIN(equity), "ISIN");
            CheckAPIsSymbolRepresentations($"{cik}", $"{CIK(equity)}", "CIK");

            Log($"\nAAPL CUSIP: {cusip}" +
                $"\nAAPL Composite FIGI: {compositeFigi}" +
                $"\nAAPL SEDOL: {sedol}" +
                $"\nAAPL ISIN: {isin}" +
                $"\nAAPL CIK: {cik}");
        }

        private static void CheckSymbolRepresentation(string symbol, string standard)
        {
            if (symbol.IsNullOrEmpty())
            {
                throw new Exception($"{standard} symbol representation is null or empty");
            }
        }

        private static void CheckAPIsSymbolRepresentations(string symbolApiSymbol, string algorithmApiSymbol, string standard)
        {
            if (symbolApiSymbol != algorithmApiSymbol)
            {
                throw new Exception($@"Symbol API {standard} symbol representation ({symbolApiSymbol}) does not match QCAlgorithm API {
                    standard} symbol representation ({algorithmApiSymbol})");
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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 795;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
