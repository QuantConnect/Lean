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
 *
*/

using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting the behavior of a zero time in universe setting. Related to GH issue #6653
    /// </summary>
    public class OptionNoTimeInUniverseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string UnderlyingTicker = "GOOG";
        private Symbol _optionSymbol;

        public override void Initialize()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);

            UniverseSettings.MinimumTimeInUniverse = TimeSpan.Zero;

            var option = AddOption(UnderlyingTicker);
            _optionSymbol = option.Symbol;

            // set our strike/expiry filter for this option chain
            option.SetFilter(u => u.Strikes(-1, +1)
                                   // Expiration method accepts TimeSpan objects or integer for days.
                                   // The following statements yield the same filtering criteria
                                   .Expiration(0, 60));
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            var optionContracts = slice.OptionChains.GetValue(_optionSymbol);
            var underlyingPrice = Securities[_optionSymbol.Underlying].Price;
            var strikes = optionContracts.Select(o => o.Strike)
                // when the strike matches the underlying price it's not taken into account in the +1 -1 range
                .Where(strike => strike != underlyingPrice)
                .ToHashSet();

            if (strikes.Count > 2)
            {
                throw new Exception($"At {Time} found {strikes.Count}. Underlying: {underlyingPrice}. Strikes: [{string.Join(",", strikes)}]");
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
        public long DataPoints => 464263;

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
