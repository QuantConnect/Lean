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
using System.Linq;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm asserting that using OnlyApplyFilterAtMarketOpen along with other dynamic filters will make the filters be applied only on market
    /// open, regardless of the order of configuration of the filters
    /// </summary>
    public class AddOptionWithOnMarketOpenOnlyFilterRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2014, 6, 5);
            SetEndDate(2014, 6, 10);

            // OnlyApplyFilterAtMarketOpen as first filter
            AddOption("AAPL", Resolution.Minute).SetFilter(u =>
                u.OnlyApplyFilterAtMarketOpen()
                 .Strikes(-5, 5)
                 .Expiration(0, 100)
                 .IncludeWeeklys());

            // OnlyApplyFilterAtMarketOpen as last filter
            AddOption("TWX", Resolution.Minute).SetFilter(u =>
                u.Strikes(-5, 5)
                 .Expiration(0, 100)
                 .IncludeWeeklys()
                 .OnlyApplyFilterAtMarketOpen());
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            // This will be the first call, the underlying securities are added.
            if (changes.AddedSecurities.All(s => s.Type != SecurityType.Option))
            {
                return;
            }

            var changeOptions = changes.AddedSecurities.Concat(changes.RemovedSecurities)
                                                                        .Where(s => s.Type == SecurityType.Option);

            // Susbtract one minute to get the actual market open. If market open is at 9:30am, this will be invoked at 9:31am
            var expectedTime = Time.TimeOfDay - TimeSpan.FromMinutes(1);
            var allOptionsWereChangedOnMarketOpen = changeOptions.All(s =>
            {
                var firstMarketSegment = s.Exchange.Hours.MarketHours[Time.DayOfWeek].Segments
                                                         .First(segment => segment.State == MarketHoursState.Market);

                return firstMarketSegment.Start == expectedTime;
            });

            if (!allOptionsWereChangedOnMarketOpen)
            {
                throw new Exception("Expected options filter to be run only on market open");
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
        /// Data Points count of all time slices of algorithm
        /// </summary>
        public long DataPoints => 5952220;

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
            {"Information Ratio", "-10.144"},
            {"Tracking Error", "0.033"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
