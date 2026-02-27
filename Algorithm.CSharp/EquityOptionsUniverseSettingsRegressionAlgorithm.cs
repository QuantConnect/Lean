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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that options from universe are added with the same resolution, fill forward and extended market hours settings as the universe settings.
    /// </summary>
    public class EquityOptionsUniverseSettingsRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private SecurityType[] _securityTypes;
        private HashSet<SecurityType> _checkedSecurityTypes = new();

        protected virtual DateTime TestStartDate => new DateTime(2015, 12, 24);

        public override void Initialize()
        {
            SetStartDate(TestStartDate);
            SetEndDate(TestStartDate.AddDays(1));
            SetCash(100000);

            UniverseSettings.Resolution = Resolution.Daily;
            UniverseSettings.FillForward = false;
            UniverseSettings.ExtendedMarketHours = true;

            _securityTypes = AddSecurity();
        }

        protected virtual SecurityType[] AddSecurity()
        {
            var equity = AddEquity("GOOG");
            var option = AddOption(equity.Symbol);
            option.SetFilter(u => u.StandardsOnly().Strikes(-2, +2).Expiration(0, 180));

            return [option.Symbol.SecurityType];
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            var securities = changes.AddedSecurities.Where(x => _securityTypes.Contains(x.Type) && !x.Symbol.IsCanonical()).Select(x => x.Symbol).ToList();
            var configs = SubscriptionManager.Subscriptions.Where(x => securities.Contains(x.Symbol));

            foreach (var config in configs)
            {
                if (config.Resolution != UniverseSettings.Resolution)
                {
                    throw new RegressionTestException($"Config '{config}' resolution {config.Resolution} does not match universe settings resolution {UniverseSettings.Resolution}");
                }

                if (config.FillDataForward != UniverseSettings.FillForward)
                {
                    throw new RegressionTestException($"Config '{config}' fill forward {config.FillDataForward} does not match universe settings fill forward {UniverseSettings.FillForward}");
                }

                if (config.ExtendedMarketHours != UniverseSettings.ExtendedMarketHours)
                {
                    throw new RegressionTestException($"Config '{config}' extended market hours {config.ExtendedMarketHours} does not match universe settings extended market hours {UniverseSettings.ExtendedMarketHours}");
                }

                _checkedSecurityTypes.Add(config.SecurityType);
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_checkedSecurityTypes.Count != _securityTypes.Length || !_securityTypes.All(_checkedSecurityTypes.Contains))
            {
                throw new RegressionTestException($"Not all security types were checked. Expected: {string.Join(", ", _securityTypes)}. Checked: {string.Join(", ", _checkedSecurityTypes)}");
            }
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
        public virtual long DataPoints => 4276;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
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
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
