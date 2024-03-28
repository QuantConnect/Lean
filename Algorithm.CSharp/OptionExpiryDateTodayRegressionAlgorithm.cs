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
using System.Linq;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm exercising an equity covered option asserting that greeks can be accessed
    /// and have are not all zero, the same day as the contract expiration date.
    /// </summary>
    public class OptionExpiryDateTodayRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _optionSymbol;
        private bool _triedGreeksCalculation;

        public override void Initialize()
        {
            SetStartDate(2014, 06, 9);
            SetEndDate(2014, 06, 15);

            var option = AddOption("AAPL", Resolution.Minute);
            option.SetFilter((universeFilter) =>
            {
                return universeFilter.IncludeWeeklys().Strikes(-2, +2).Expiration(0, 10);
            });
            option.PriceModel = OptionPriceModels.BaroneAdesiWhaley();
            _optionSymbol = option.Symbol;

            SetWarmUp(TimeSpan.FromDays(3));
        }

        public override void OnData(Slice slice)
        {
            if (IsWarmingUp || Time.Hour > 10)
            {
                return;
            }

            foreach (var kvp in slice.OptionChains)
            {
                if (kvp.Key != _optionSymbol)
                {
                    continue;
                }

                var chain = kvp.Value;
                // Find the call options expiring today
                var contracts = chain
                    .Where(contract => contract.Expiry.Date == Time.Date && contract.Strike < chain.Underlying.Price)
                    .ToList();

                if (contracts.Count == 0)
                {
                    return;
                }

                _triedGreeksCalculation = true;

                foreach (var contract in contracts)
                {
                    var greeks = contract.Greeks;
                    if (greeks.Delta == 0m && greeks.Gamma == 0m && greeks.Theta == 0m && greeks.Vega == 0m && greeks.Rho == 0m)
                    {
                        throw new Exception($"Expected greeks to not be zero simultaneously for {contract.Symbol} at contract expiration date {contract.Expiry}");
                    }
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_triedGreeksCalculation)
            {
                throw new Exception("Expected to have tried greeks calculation");
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
        public long DataPoints => 8605047;

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
            {"Information Ratio", "5.176"},
            {"Tracking Error", "0.071"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
