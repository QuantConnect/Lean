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
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm reproducing GH issue #3914 where the option chain subscriptions wouldn't get removed
    /// </summary>
    public class OptionChainSubscriptionRemovalRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _optionCount;
        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Minute;
            SetStartDate(2014, 06, 05);
            SetEndDate(2014, 06, 09);

            // this line is the key of this test it changed the behavior if the resolution used
            // is < that Minute which is the Option resolution
            AddEquity("SPY", Resolution.Second);
            SetUniverseSelection(new TestOptionUniverseSelectionModel(SelectOptionChainSymbols));
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _optionCount += changes.AddedSecurities.Count(security => security.Symbol.SecurityType == SecurityType.Option);

            Log($"{GetStatusLog()} CHANGES: {changes}");
        }

        public override void OnEndOfAlgorithm()
        {
            if (_optionCount != 45)
            {
                throw new Exception($"Unexpected option count {_optionCount}, expected 45");
            }
        }

        private static IEnumerable<Symbol> SelectOptionChainSymbols(DateTime utcTime)
        {
            var newYorkTime = utcTime.ConvertFromUtc(TimeZones.NewYork);
            if (newYorkTime.Date < new DateTime(2014, 06, 06))
            {
                yield return QuantConnect.Symbol.Create("TWX", SecurityType.Option, Market.USA, "?TWX");
            }

            if (newYorkTime.Date >= new DateTime(2014, 06, 06))
            {
                yield return QuantConnect.Symbol.Create("AAPL", SecurityType.Option, Market.USA, "?AAPL");
            }
        }

        private string GetStatusLog()
        {
            Plot("Status", "UniverseCount", UniverseManager.Count);
            Plot("Status", "SubscriptionCount", SubscriptionManager.Subscriptions.Count());
            Plot("Status", "ActiveSymbolsCount", UniverseManager.ActiveSecurities.Count);

            // why 50? we select 15 option contracts, which add trade/quote/openInterest = 45 + SPY & underlying trade/quote + universe subscription => 50
            if (SubscriptionManager.Subscriptions.Count() > 50)
            {
                throw new Exception("Subscriptions aren't getting removed as expected!");
            }

            return $"{Time} | UniverseCount {UniverseManager.Count}. " +
                $"SubscriptionCount {SubscriptionManager.Subscriptions.Count()}. " +
                $"ActiveSymbols {string.Join(",", UniverseManager.ActiveSecurities.Keys)}";
        }

        class TestOptionUniverseSelectionModel : OptionUniverseSelectionModel
        {
            public TestOptionUniverseSelectionModel(Func<DateTime, IEnumerable<Symbol>> optionChainSymbolSelector)
                : base(TimeSpan.FromDays(1), optionChainSymbolSelector)
            {
            }

            protected override OptionFilterUniverse Filter(OptionFilterUniverse filter)
            {
                return filter.BackMonth().Contracts(filter.Take(15));
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
        public long DataPoints => 112808126;

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
            {"Information Ratio", "-11.639"},
            {"Tracking Error", "0.037"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
