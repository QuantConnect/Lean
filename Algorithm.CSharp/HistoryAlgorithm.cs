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
using QuantConnect.Util;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities.Equity;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrates the various ways you can call the History function,
    /// what it returns, and what you can do with the returned values.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="history and warm up" />
    /// <meta name="tag" content="history" />
    /// <meta name="tag" content="warm up" />
    public class HistoryAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private int _count;
        private SimpleMovingAverage _dailySma;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            // Find more symbols here: http://quantconnect.com/data
            var SPY = AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily).Symbol;
            var IBM = AddData<CustomData>("IBM", Resolution.Daily).Symbol;
            // specifying the exchange will allow the history methods that accept a number of bars to return to work properly
            Securities["IBM"].Exchange = new EquityExchange();

            // we can get history in initialize to set up indicators and such
            _dailySma = new SimpleMovingAverage(14);

            // get the last calendar year's worth of SPY data at the configured resolution (daily)
            var tradeBarHistory = History<TradeBar>("SPY", TimeSpan.FromDays(365));
            AssertHistoryCount("History<TradeBar>(\"SPY\", TimeSpan.FromDays(365))", tradeBarHistory, 250, SPY);

            // get the last calendar day's worth of SPY data at the specified resolution
            tradeBarHistory = History<TradeBar>("SPY", TimeSpan.FromDays(1), Resolution.Minute);
            AssertHistoryCount("History<TradeBar>(\"SPY\", TimeSpan.FromDays(1), Resolution.Minute)", tradeBarHistory, 390, SPY);

            // get the last 14 bars of SPY at the configured resolution (daily)
            tradeBarHistory = History<TradeBar>("SPY", 14).ToList();
            AssertHistoryCount("History<TradeBar>(\"SPY\", 14)", tradeBarHistory, 14, SPY);

            // get the last 14 minute bars of SPY
            tradeBarHistory = History<TradeBar>("SPY", 14, Resolution.Minute);
            AssertHistoryCount("History<TradeBar>(\"SPY\", 14, Resolution.Minute)", tradeBarHistory, 14, SPY);

            // we can loop over the return value from these functions and we get TradeBars
            // we can use these TradeBars to initialize indicators or perform other math
            foreach (TradeBar tradeBar in tradeBarHistory)
            {
                _dailySma.Update(tradeBar.EndTime, tradeBar.Close);
            }

            // get the last calendar year's worth of IBM data at the configured resolution (daily)
            var customDataHistory = History<CustomData>("IBM", TimeSpan.FromDays(365));
            AssertHistoryCount("History<CustomData>(\"IBM\", TimeSpan.FromDays(365))", customDataHistory, 250, IBM);

            // get the last 14 bars of IBM at the configured resolution (daily)
            customDataHistory = History<CustomData>("IBM", 14);
            AssertHistoryCount("History<CustomData>(\"IBM\", 14)", customDataHistory, 14, IBM);

            // we can loop over the return values from these functions and we'll get custom data
            // this can be used in much the same way as the tradeBarHistory above
            _dailySma.Reset();
            foreach (CustomData customData in customDataHistory)
            {
                _dailySma.Update(customData.EndTime, customData.Value);
            }

            // get the last year's worth of all configured custom data at the configured resolution (daily)
            var allCustomData = History<CustomData>(TimeSpan.FromDays(365));
            AssertHistoryCount("History<CustomData>(TimeSpan.FromDays(365))", allCustomData, 250, IBM, SPY);

            // get the last 14 bars worth of custom data for the specified symbols at the configured resolution (daily)
            allCustomData = History<CustomData>(Securities.Keys, 14);
            AssertHistoryCount("History<CustomData>(Securities.Keys, 14)", allCustomData, 14, IBM, SPY);

            // NOTE: Using different resolutions require that they are properly implemented in your data type. If your
            // custom data source has different resolutions, it would need to be implemented in the GetSource and Reader
            // methods properly.
            //customDataHistory = History<CustomData>("IBM", TimeSpan.FromDays(7), Resolution.Minute);
            //customDataHistory = History<CustomData>("IBM", 14, Resolution.Minute);
            //allCustomData = History<CustomData>(TimeSpan.FromDays(365), Resolution.Minute);
            //allCustomData = History<CustomData>(Securities.Keys, 14, Resolution.Minute);
            //allCustomData = History<CustomData>(Securities.Keys, TimeSpan.FromDays(1), Resolution.Minute);
            //allCustomData = History<CustomData>(Securities.Keys, 14, Resolution.Minute);

            // get the last calendar year's worth of all custom data
            allCustomData = History<CustomData>(Securities.Keys, TimeSpan.FromDays(365));
            AssertHistoryCount("History<CustomData>(Securities.Keys, TimeSpan.FromDays(365))", allCustomData, 250, IBM, SPY);

            // the return is a series of dictionaries containing all custom data at each time
            // we can loop over it to get the individual dictionaries
            foreach (DataDictionary<CustomData> customDataDictionary in allCustomData)
            {
                // we can access the dictionary to get the custom data we want
                var customData = customDataDictionary["IBM"];
            }

            // we can also access the return value from the multiple symbol functions to request a single
            // symbol and then loop over it
            var singleSymbolCustomData = allCustomData.Get("IBM");
            AssertHistoryCount("allCustomData.Get(\"IBM\")", singleSymbolCustomData, 250, IBM);
            foreach (CustomData customData in singleSymbolCustomData)
            {
                // do something with 'IBM' custom data
            }

            // we can also access individual properties on our data, this will
            // get the 'IBM' CustomData objects like above, but then only return the Value properties
            var customDataIbmValues = allCustomData.Get("IBM", "Value");
            AssertHistoryCount("allCustomData.Get(\"IBM\", \"Value\")", customDataIbmValues, 250);
            foreach (decimal value in customDataIbmValues)
            {
                // do something with each value
            }

            // sometimes it's necessary to get the history for many configured symbols

            // request the last year's worth of history for all configured symbols at their configured resolutions
            var allHistory = History(TimeSpan.FromDays(365));
            AssertHistoryCount("History(TimeSpan.FromDays(365))", allHistory, 250, SPY, IBM);

            // request the last days's worth of history at the minute resolution
            allHistory = History(TimeSpan.FromDays(1), Resolution.Minute);
            AssertHistoryCount("History(TimeSpan.FromDays(1), Resolution.Minute)", allHistory, 390, SPY, IBM);

            // request the last 100 bars for the specified securities at the configured resolution
            allHistory = History(Securities.Keys, 100);
            AssertHistoryCount("History(Securities.Keys, 100)", allHistory, 100, SPY, IBM);

            // request the last 100 minute bars for the specified securities
            allHistory = History(Securities.Keys, 100, Resolution.Minute);
            AssertHistoryCount("History(Securities.Keys, 100, Resolution.Minute)", allHistory, 100, SPY, IBM);

            // request the last calendar years worth of history for the specified securities
            allHistory = History(Securities.Keys, TimeSpan.FromDays(365));
            AssertHistoryCount("History(Securities.Keys, TimeSpan.FromDays(365))", allHistory, 250, SPY, IBM);
            // we can also specify the resolution
            allHistory = History(Securities.Keys, TimeSpan.FromDays(1), Resolution.Minute);
            AssertHistoryCount("History(Securities.Keys, TimeSpan.FromDays(1), Resolution.Minute)", allHistory, 390, SPY, IBM);

            // if we loop over this allHistory, we get Slice objects
            foreach (Slice slice in allHistory)
            {
                // do something with each slice, these will come in time order
                // and will NOT have auxilliary data, just price data and your custom data
                // if those symbols were specified
            }

            // we can access the history for individual symbols from the all history by specifying the symbol
            // the type must be a trade bar!
            tradeBarHistory = allHistory.Get<TradeBar>("SPY");
            AssertHistoryCount("allHistory.Get(\"SPY\")", tradeBarHistory, 390, SPY);

            // we can access all the closing prices in chronological order using this get function
            var closeHistory = allHistory.Get("SPY", Field.Close);
            AssertHistoryCount("allHistory.Get(\"SPY\", Field.Close)", closeHistory, 390);
            foreach (decimal close in closeHistory)
            {
                // do something with each closing value in order
            }

            // we can convert the close history into your normal double array (double[]) using the ToDoubleArray method
            double[] doubleArray = closeHistory.ToDoubleArray();

            // for the purposes of regression testing, we're explicitly requesting history
            // using the universe symbols. Requests for universe symbols are filtered out
            // and never sent to the history provider.
            var universeSecurityHistory = History(UniverseManager.Keys, TimeSpan.FromDays(10)).ToList();
            if (universeSecurityHistory.Count != 0)
            {
                throw new Exception("History request for universe symbols incorrectly returned data. "
                    + "These requests are intended to be filtered out and never sent to the history provider.");
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            _count++;

            if (_count > 5)
            {
                throw new Exception($"Invalid number of bars arrived. Expected exactly 5, but received {_count}");
            }

            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
                Debug("Purchased Stock");
            }
        }

        private void AssertHistoryCount<T>(string methodCall, IEnumerable<T> history, int expected, params Symbol[] expectedSymbols)
        {
            history = history.ToList();
            var count = history.Count();
            if (count != expected)
            {
                throw new Exception(methodCall + " expected " + expected + ", but received " + count);
            }

            IEnumerable<Symbol> unexpectedSymbols = null;
            if (typeof(T) == typeof(Slice))
            {
                var slices = (IEnumerable<Slice>) history;
                unexpectedSymbols = slices.SelectMany(slice => slice.Keys)
                    .Distinct()
                    .Where(sym => !expectedSymbols.Contains(sym))
                    .ToList();
            }
            else if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(DataDictionary<>))
            {
                if (typeof(T).GetGenericArguments()[0] == typeof(CustomData))
                {
                    var dictionaries = (IEnumerable<DataDictionary<CustomData>>) history;
                    unexpectedSymbols = dictionaries.SelectMany(dd => dd.Keys)
                        .Distinct()
                        .Where(sym => !expectedSymbols.Contains(sym))
                        .ToList();
                }
            }
            else if (typeof(IBaseData).IsAssignableFrom(typeof(T)))
            {
                var slices = (IEnumerable<IBaseData>)history;
                unexpectedSymbols = slices.Select(data => data.Symbol)
                    .Distinct()
                    .Where(sym => !expectedSymbols.Contains(sym))
                    .ToList();
            }
            else if (typeof(T) == typeof(decimal))
            {
                // if the enumerable doesn't contain symbols then we can't assert that certain symbols exist
                // this case is used when testing data dictionary extensions that select a property value,
                // such as dataDictionaries.Get("MySymbol", "MyProperty") => IEnumerable<decimal>
                return;
            }

            if (unexpectedSymbols == null)
            {
                throw new Exception("Unhandled case: " + typeof(T).GetBetterTypeName());
            }

            var unexpectedSymbolsString = string.Join(" | ", unexpectedSymbols);
            if (!string.IsNullOrWhiteSpace(unexpectedSymbolsString))
            {
                throw new Exception($"{methodCall} contains unexpected symbols: {unexpectedSymbolsString}");
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
        public long DataPoints => -1;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => -1;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "363.283%"},
            {"Drawdown", "1.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101694.38"},
            {"Net Profit", "1.694%"},
            {"Sharpe Ratio", "57.467"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.041"},
            {"Beta", "0.998"},
            {"Annual Standard Deviation", "0.177"},
            {"Annual Variance", "0.031"},
            {"Information Ratio", "-150.576"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "10.221"},
            {"Total Fees", "$3.45"},
            {"Estimated Strategy Capacity", "$970000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "25.24%"},
            {"OrderListHash", "39a84b9f15bb4e8ead0f0ecb59f28562"}
        };
    }
}
