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
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm to test universe additions and removals with open positions
    /// </summary>
    /// <meta name="tag" content="regression test" />
    public class WeeklyUniverseSelectionRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private SecurityChanges _changes = SecurityChanges.None;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 1);  //Set Start Date
            SetEndDate(2013, 10, 31);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            UniverseSettings.Resolution = Resolution.Hour;

            // select IBM once a week, empty universe the other days
            AddUniverse("my-custom-universe", dt => dt.Day % 7 == 0 ? new List<string> { "IBM" } : Enumerable.Empty<string>());
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars dictionary object keyed by symbol containing the stock data</param>
        public void OnData(TradeBars data)
        {
            if (_changes == SecurityChanges.None) return;

            // liquidate securities removed from our universe
            foreach (var security in _changes.RemovedSecurities)
            {
                if (security.Invested)
                {
                    Log(Time + " Liquidate " + security.Symbol.Value);
                    Liquidate(security.Symbol);
                }
            }

            // we'll simply go long each security we added to the universe
            foreach (var security in _changes.AddedSecurities)
            {
                if (!security.Invested)
                {
                    Log(Time + " Buy " + security.Symbol.Value);
                    SetHoldings(security.Symbol, 1);
                }
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes">Object containing AddedSecurities and RemovedSecurities</param>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            _changes = changes;
            Log(Time + " " + changes);
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
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "8"},
            {"Average Win", "0.66%"},
            {"Average Loss", "-0.53%"},
            {"Compounding Annual Return", "-10.557%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "-0.441"},
            {"Net Profit", "-0.943%"},
            {"Sharpe Ratio", "-1.531"},
            {"Loss Rate", "75%"},
            {"Win Rate", "25%"},
            {"Profit-Loss Ratio", "1.24"},
            {"Alpha", "-0.098"},
            {"Beta", "0.034"},
            {"Annual Standard Deviation", "0.055"},
            {"Annual Variance", "0.003"},
            {"Information Ratio", "-4.077"},
            {"Tracking Error", "0.121"},
            {"Treynor Ratio", "-2.442"},
            {"Total Fees", "$25.73"}
        };
    }
}
