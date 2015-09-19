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
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This algorithm demonstrates the various ways you can call the History function,
    /// what it returns, and what you can do with the returned values.
    /// </summary>
    public class HistoryAlgorithm : QCAlgorithm
    {
        public SimpleMovingAverage spyDailySma;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Daily);
            AddData<Quandl>("YAHOO/INDEX_SPY", Resolution.Daily);
            // specifying the exchange will allow the history methods that accept a number of bars to return to work properly
            Securities["YAHOO/INDEX_SPY"].Exchange = new EquityExchange();

            // we can get history in initialize to set up indicators and such
            spyDailySma = new SimpleMovingAverage(14);

            // get the last calendar year's worth of SPY data at the configured resolution (daily)
            var tradeBarHistory = History("SPY", TimeSpan.FromDays(365));
            
            // get the last calendar day's worth of SPY data at the specified resolution
            tradeBarHistory = History("SPY", TimeSpan.FromDays(1), Resolution.Minute);
            
            // get the last 14 bars of SPY at the configured resolution (daily)
            tradeBarHistory = History("SPY", 14).ToList();

            // get the last 14 minute bars of SPY
            tradeBarHistory = History("SPY", 14, Resolution.Minute);

            // we can loop over the return value from these functions and we get TradeBars
            // we can use these TradeBars to initialize indicators or perform other math
            foreach (TradeBar tradeBar in tradeBarHistory)
            {
                spyDailySma.Update(tradeBar.EndTime, tradeBar.Close);
            }

            // get the last calendar year's worth of quandl data at the configured resolution (daily)
            var quandlHistory = History<Quandl>("YAHOO/INDEX_SPY", TimeSpan.FromDays(365));

            // get the last 14 bars of SPY at the configured resolution (daily)
            quandlHistory = History<Quandl>("YAHOO/INDEX_SPY", 14);

            // get the last 14 minute bars of SPY

            // we can loop over the return values from these functions and we'll get Quandl data
            // this can be used in much the same way as the tradeBarHistory above
            foreach (Quandl quandl in quandlHistory)
            {
                spyDailySma.Update(quandl.EndTime, quandl.Value);
            }

            // get the last year's worth of all configured Quandl data at the configured resolution (daily)
            var allQuandlData = History<Quandl>(TimeSpan.FromDays(365));

            // get the last 14 bars worth of Quandl data for the specified symbols at the configured resolution (daily)
            allQuandlData = History<Quandl>(Securities.Keys, 14);
            
            // NOTE: using different resolutions require that they are properly implemented in your data type, since
            //  Quandl doesn't support minute data, this won't actually work, but if your custom data source has
            //  different resolutions, it would need to be implemented in the GetSource and Reader methods properly
            //quandlHistory = History<Quandl>("YAHOO/INDEX_SPY", TimeSpan.FromDays(7), Resolution.Minute);
            //quandlHistory = History<Quandl>("YAHOO/INDEX_SPY", 14, Resolution.Minute);
            //allQuandlData = History<Quandl>(TimeSpan.FromDays(365), Resolution.Minute);
            //allQuandlData = History<Quandl>(Securities.Keys, 14, Resolution.Minute);
            //allQuandlData = History<Quandl>(Securities.Keys, TimeSpan.FromDays(1), Resolution.Minute);
            //allQuandlData = History<Quandl>(Securities.Keys, 14, Resolution.Minute);

            // get the last calendar year's worth of all quandl data
            allQuandlData = History<Quandl>(Securities.Keys, TimeSpan.FromDays(365));

            // the return is a series of dictionaries containing all quandl data at each time
            // we can loop over it to get the individual dictionaries
            foreach (DataDictionary<Quandl> quandlsDataDictionary in allQuandlData)
            {
                // we can access the dictionary to get the quandl data we want
                var quandl = quandlsDataDictionary["YAHOO/INDEX_SPY"];
            }

            // we can also access the return value from the multiple symbol functions to request a single
            // symbol and then loop over it
            var singleSymbolQuandl = allQuandlData.Get("YAHOO/INDEX_SPY");
            foreach (Quandl quandl in singleSymbolQuandl)
            {
                // do something with 'YAHOO/INDEX_SPY' quandl data
            }

            // we can also access individual properties on our data, this will
            // get the 'YAHOO/INDEX_SPY' quandls like above, but then only return the Low properties
            var quandlSpyLows = allQuandlData.Get("YAHOO/INDEX_SPY", "Low");
            foreach (decimal low in quandlSpyLows)
            {
                // do something we each low value
            }

            // sometimes it's necessary to get the history for many configured symbols

            // request the last year's worth of history for all configured symbols at their configured resolutions
            var allHistory = History(TimeSpan.FromDays(365));

            // request the last days's worth of history at the minute resolution
            allHistory = History(TimeSpan.FromDays(1), Resolution.Minute);

            // request the last 100 bars for the specified securities at the configured resolution
            allHistory = History(Securities.Keys, 100);
            
            // request the last 100 minute bars for the specified securities
            allHistory = History(Securities.Keys, 100, Resolution.Minute);
            
            // request the last calendar years worth of history for the specified securities
            allHistory = History(Securities.Keys, TimeSpan.FromDays(365));
            // we can also specify the resolutin
            allHistory = History(Securities.Keys, TimeSpan.FromDays(1), Resolution.Minute);

            // if we loop over this allHistory, we get Slice objects
            foreach (Slice slice in allHistory)
            {
                // do something with each slice, these will come in time order
                // and will NOT have auxilliary data, just price data and your custom data
                // if those symbols were specified
            }

            // we can access the history for individual symbols from the all history by specifying the symbol
            // the type must be a trade bar!
            tradeBarHistory = allHistory.Get("SPY");

            // we can access all the closing prices in chronological order using this get function
            var closeHistory = allHistory.Get("SPY", Field.Close);
            foreach (decimal close in closeHistory)
            {
                // do something with each closing value in order
            }

            // we can convert the close history into your normal double array (double[]) using the ToDoubleArray method
            double[] doubleArray = closeHistory.ToDoubleArray();
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1);
                Debug("Purchased Stock");
            }
        }
    }
}