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
    /// In this algorithm we show how you can easily use the universe selection feature to fetch symbols
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

            // Order margin value has to have a minimum of 0.5% of Portfolio value, allows filtering out small trades and reduce fees.
            // Commented so regression algorithm is more sensitive
            //Settings.MinimumOrderMarginPortfolioPercentage = 0.005m;

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
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 5291;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "5059"},
            {"Average Win", "0.08%"},
            {"Average Loss", "-0.08%"},
            {"Compounding Annual Return", "15.960%"},
            {"Drawdown", "10.400%"},
            {"Expectancy", "0.079"},
            {"Start Equity", "100000"},
            {"End Equity", "115960.04"},
            {"Net Profit", "15.960%"},
            {"Sharpe Ratio", "0.865"},
            {"Sortino Ratio", "0.809"},
            {"Probabilistic Sharpe Ratio", "49.486%"},
            {"Loss Rate", "45%"},
            {"Win Rate", "55%"},
            {"Profit-Loss Ratio", "0.96"},
            {"Alpha", "0.015"},
            {"Beta", "0.985"},
            {"Annual Standard Deviation", "0.11"},
            {"Annual Variance", "0.012"},
            {"Information Ratio", "0.348"},
            {"Tracking Error", "0.041"},
            {"Treynor Ratio", "0.096"},
            {"Total Fees", "$5870.38"},
            {"Estimated Strategy Capacity", "$320000.00"},
            {"Lowest Capacity Asset", "BNO UN3IMQ2JU1YD"},
            {"Portfolio Turnover", "107.21%"},
            {"OrderListHash", "a7da6309cc1fb69e6f197ec9eb152a67"}
        };
    }
}
