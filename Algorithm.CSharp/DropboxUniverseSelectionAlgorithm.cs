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
using System.Text;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// In this algortihm we show how you can easily use the universe selection feature to fetch symbols
    /// to be traded using the AddUniverse method. This method accepts a function that will return the
    /// desired current set of symbols. Return Universe.Unchanged if no universe changes should be made
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="universes" />
    /// <meta name="tag" content="custom universes" />
    public class DropboxUniverseSelectionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        // the changes from the previous universe selection
        private SecurityChanges _changes = SecurityChanges.None;
        // only used in backtest for caching the file results
        private readonly Dictionary<DateTime, List<string>> _backtestSymbolsPerDay = new Dictionary<DateTime, List<string>>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        /// <seealso cref="QCAlgorithm.SetStartDate(System.DateTime)"/>
        /// <seealso cref="QCAlgorithm.SetEndDate(System.DateTime)"/>
        /// <seealso cref="QCAlgorithm.SetCash(decimal)"/>
        public override void Initialize()
        {
            // this sets the resolution for data subscriptions added by our universe
            UniverseSettings.Resolution = Resolution.Daily;

            // set our start and end for backtest mode
            SetStartDate(2017, 07, 04);
            SetEndDate(2018, 07, 04);

            // define a new custom universe that will trigger each day at midnight
            AddUniverse("my-dropbox-universe", Resolution.Daily, dateTime =>
            {
                // handle live mode file format
                if (LiveMode)
                {
                    // fetch the file from dropbox
                    var file = Download(@"https://www.dropbox.com/s/2l73mu97gcehmh7/daily-stock-picker-live.csv?dl=1");
                    // if we have a file for today, break apart by commas and return symbols
                    if (file.Length > 0) return file.ToCsv();
                    // no symbol today, leave universe unchanged
                    return Universe.Unchanged;
                }

                // backtest - first cache the entire file
                if (_backtestSymbolsPerDay.Count == 0)
                {
                    // No need for headers for authorization with dropbox, these two lines are for example purposes
                    var byteKey = Encoding.ASCII.GetBytes($"UserName:Password");
                    // The headers must be passed to the Download method as list of key/value pair.
                    var headers = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("Authorization", $"Basic ({Convert.ToBase64String(byteKey)})")
                    };

                    var file = Download(@"https://www.dropbox.com/s/ae1couew5ir3z9y/daily-stock-picker-backtest.csv?dl=1", headers);

                    // split the file into lines and add to our cache
                    foreach (var line in file.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var csv = line.ToCsv();
                        var date = DateTime.ParseExact(csv[0], "yyyyMMdd", null);
                        var symbols = csv.Skip(1).ToList();
                        _backtestSymbolsPerDay[date] = symbols;
                    }
                }

                // if we have symbols for this date return them, else specify Universe.Unchanged
                List<string> result;
                if (_backtestSymbolsPerDay.TryGetValue(dateTime.Date, out result))
                {
                    return result;
                }
                return Universe.Unchanged;
            });
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <code>
        /// TradeBars bars = slice.Bars;
        /// Ticks ticks = slice.Ticks;
        /// TradeBar spy = slice["SPY"];
        /// List{Tick} aaplTicks = slice["AAPL"]
        /// Quandl oil = slice["OIL"]
        /// dynamic anySymbol = slice[symbol];
        /// DataDictionary{Quandl} allQuandlData = slice.Get{Quand}
        /// Quandl oil = slice.Get{Quandl}("OIL")
        /// </code>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (slice.Bars.Count == 0) return;
            if (_changes == SecurityChanges.None) return;

            // start fresh
            Liquidate();

            var percentage = 1m/slice.Bars.Count;
            foreach (var tradeBar in slice.Bars.Values)
            {
                SetHoldings(tradeBar.Symbol, percentage);
            }

            // reset changes
            _changes = SecurityChanges.None;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="changes"></param>
        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            // each time our securities change we'll be notified here
            _changes = changes;
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
            {"Total Trades", "5059"},
            {"Average Win", "0.08%"},
            {"Average Loss", "-0.08%"},
            {"Compounding Annual Return", "14.950%"},
            {"Drawdown", "10.600%"},
            {"Expectancy", "0.075"},
            {"Net Profit", "14.950%"},
            {"Sharpe Ratio", "1.072"},
            {"Probabilistic Sharpe Ratio", "50.327%"},
            {"Loss Rate", "45%"},
            {"Win Rate", "55%"},
            {"Profit-Loss Ratio", "0.97"},
            {"Alpha", "0.137"},
            {"Beta", "-0.066"},
            {"Annual Standard Deviation", "0.121"},
            {"Annual Variance", "0.015"},
            {"Information Ratio", "0.083"},
            {"Tracking Error", "0.171"},
            {"Treynor Ratio", "-1.971"},
            {"Total Fees", "$6806.67"},
            {"Fitness Score", "0.694"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "1.265"},
            {"Return Over Maximum Drawdown", "1.409"},
            {"Portfolio Turnover", "1.296"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "1142077166"}
        };
    }
}
