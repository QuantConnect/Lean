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

using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// In this algorithm we demonstrate how to use the UniverseSettings
    /// to define the data normalization mode (raw)
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="universes" />
    /// <meta name="tag" content="coarse universes" />
    /// <meta name="tag" content="regression test" />
    public class RawPricesUniverseRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            // what resolution should the data *added* to the universe be?
            UniverseSettings.Resolution = Resolution.Daily;

            // Use raw prices
            UniverseSettings.DataNormalizationMode = DataNormalizationMode.Raw;

            SetStartDate(2014,3,24);
            SetEndDate(2014,4,7);
            SetCash(50000);

            // Set the security initializer with zero fees and price initial seed
            var securitySeeder = new FuncSecuritySeeder(GetLastKnownPrices);
            SetSecurityInitializer(new CompositeSecurityInitializer(
                new FuncSecurityInitializer(x => x.SetFeeModel(new ConstantFeeModel(0))),
                new FuncSecurityInitializer(security => securitySeeder.SeedSecurity(security))));

            AddUniverse("MyUniverse", Resolution.Daily, SelectionFunction);
        }

        public IEnumerable<string> SelectionFunction(DateTime dateTime)
        {
            return dateTime.Day % 2 == 0
                ? new[] { "SPY", "IWM", "QQQ" }
                : new[] { "AIG", "BAC", "IBM" };
        }

        // this event fires whenever we have changes to our universe
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var security in changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Liquidate(security.Symbol);
                }
            }

            // we want 20% allocation in each security in our universe
            foreach (var security in changes.AddedSecurities)
            {
                SetHoldings(security.Symbol, 0.2m);
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 135;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 30;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "12"},
            {"Average Win", "0.34%"},
            {"Average Loss", "-0.14%"},
            {"Compounding Annual Return", "4.586%"},
            {"Drawdown", "0.700%"},
            {"Expectancy", "0.158"},
            {"Start Equity", "50000"},
            {"End Equity", "50090.17"},
            {"Net Profit", "0.180%"},
            {"Sharpe Ratio", "5.991"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "99.393%"},
            {"Loss Rate", "67%"},
            {"Win Rate", "33%"},
            {"Profit-Loss Ratio", "2.47"},
            {"Alpha", "0.17"},
            {"Beta", "0.029"},
            {"Annual Standard Deviation", "0.028"},
            {"Annual Variance", "0.001"},
            {"Information Ratio", "2.734"},
            {"Tracking Error", "0.098"},
            {"Treynor Ratio", "5.803"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$99000000.00"},
            {"Lowest Capacity Asset", "AIG R735QTJ8XC9X"},
            {"Portfolio Turnover", "15.96%"},
            {"OrderListHash", "d915ae36ce856457b32ebbfce4581281"}
        };
    }
}
