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

using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm with existing holdings consuming an alpha streams portfolio state and trading based on it
    /// </summary>
    public class AlphaStreamsDifferentAccountCurrencyBasicTemplateAlgorithm : AlphaStreamsWithHoldingsBasicTemplateAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetAccountCurrency("EUR");
            base.Initialize();
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
        public override long DataPoints => 6202;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 61;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0.01%"},
            {"Average Loss", "0.00%"},
            {"Compounding Annual Return", "-78.519%"},
            {"Drawdown", "3.100%"},
            {"Expectancy", "7.797"},
            {"Net Profit", "-1.134%"},
            {"Sharpe Ratio", "-2.456"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "50%"},
            {"Win Rate", "50%"},
            {"Profit-Loss Ratio", "16.59"},
            {"Alpha", "0.006"},
            {"Beta", "1.011"},
            {"Annual Standard Deviation", "0.343"},
            {"Annual Variance", "0.117"},
            {"Information Ratio", "-0.859"},
            {"Tracking Error", "0.004"},
            {"Treynor Ratio", "-0.832"},
            {"Total Fees", "€2.89"},
            {"Estimated Strategy Capacity", "€8900000000.00"},
            {"Lowest Capacity Asset", "AAPL R735QTJ8XC9X"},
            {"Portfolio Turnover", "33.78%"},
            {"OrderListHash", "a9dd0a0ab6070455479d1b9caaa4e69c"}
        };
    }
}
