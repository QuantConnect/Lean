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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Algorithm illustrating how to manually set market hours and symbol properties database entries to be picked up by the algorithm's securities.
    /// This specific case illustrates how to do it for CFDs to match InteractiveBrokers brokerage, which has different market hours
    /// depending on the CFD underlying asset.
    /// </summary>
    public class ManuallySetMarketHoursAndSymbolPropertiesDatabaseEntriesAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 07);
            SetCash(100000);

            SetBrokerageModel(Brokerages.BrokerageName.InteractiveBrokersBrokerage);

            // Some brokerages like InteractiveBrokers make a difference on CFDs depending on the underlying (equity, index, metal, forex).
            // Depending on this, the market hours can be different. In order to be more specific with the market hours,
            // we can set the MarketHoursDatabase entry for the CFDs.

            // Equity CFDs are usually traded the same hours as the equity market.
            var equityMarketHoursEntry = MarketHoursDatabase.GetEntry(Market.USA, (string)null, SecurityType.Equity);
            MarketHoursDatabase.SetEntry(Market.InteractiveBrokers, null, SecurityType.Cfd,
                equityMarketHoursEntry.ExchangeHours, equityMarketHoursEntry.DataTimeZone);

            // The same can be done for the symbol properties, in case they are different depending on the underlying
            var equitySymbolProperties = SymbolPropertiesDatabase.GetSymbolProperties(Market.USA, null, SecurityType.Equity, Currencies.USD);
            SymbolPropertiesDatabase.SetEntry(Market.InteractiveBrokers, null, SecurityType.Cfd, equitySymbolProperties);

            var spyCfd = AddCfd("SPY", market: Market.InteractiveBrokers);

            if (!ReferenceEquals(spyCfd.Exchange.Hours, equityMarketHoursEntry.ExchangeHours))
            {
                throw new Exception("Expected the SPY CFD market hours to be the same as the underlying equity market hours.");
            }

            if (!ReferenceEquals(spyCfd.SymbolProperties, equitySymbolProperties))
            {
                throw new Exception("Expected the SPY CFD symbol properties to be the same as the underlying equity symbol properties.");
            }

            // We can also do it for a specific ticker:
            var audUsdForexMarketHoursEntry = MarketHoursDatabase.GetEntry(Market.Oanda, (string)null, SecurityType.Forex);
            MarketHoursDatabase.SetEntry(Market.InteractiveBrokers, "AUDUSD", SecurityType.Cfd,
                audUsdForexMarketHoursEntry.ExchangeHours, audUsdForexMarketHoursEntry.DataTimeZone);

            var audUsdForexSymbolProperties = SymbolPropertiesDatabase.GetSymbolProperties(Market.Oanda, "AUDUSD", SecurityType.Forex, Currencies.USD);
            SymbolPropertiesDatabase.SetEntry(Market.InteractiveBrokers, "AUDUSD", SecurityType.Cfd, audUsdForexSymbolProperties);

            var audUsdCfd = AddCfd("AUDUSD", market: Market.InteractiveBrokers);

            if (!ReferenceEquals(audUsdCfd.Exchange.Hours, audUsdForexMarketHoursEntry.ExchangeHours))
            {
                throw new Exception("Expected the AUDUSD CFD market hours to be the same as the underlying forex market hours.");
            }

            if (!ReferenceEquals(audUsdCfd.SymbolProperties, audUsdForexSymbolProperties))
            {
                throw new Exception("Expected the AUDUSD CFD symbol properties to be the same as the underlying forex symbol properties.");
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
        public long DataPoints => 1;

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
