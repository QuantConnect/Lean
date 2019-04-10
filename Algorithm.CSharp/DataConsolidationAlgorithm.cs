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

using System;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Example algorithm giving an introduction into using IDataConsolidators.
    /// This is an advanced QC concept and requires a certain level of comfort using C# and its event system.
    ///
    /// What is an IDataConsolidator?
    /// IDataConsolidator is a plugin point that can be used to transform your data more easily.
    /// In this example we show one of the simplest consolidators, the TradeBarConsolidator.
    /// This type is capable of taking a timespan to indicate how long each bar should be, or an
    /// integer to indicate how many bars should be aggregated into one.
    ///
    /// When a new 'consolidated' piece of data is produced by the IDataConsolidator, an event is fired
    /// with the argument of the new data.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="consolidating data" />
    public class DataConsolidationAlgorithm : QCAlgorithm
    {
        private bool consolidatedHour;
        private bool consolidated45Minute;
        private TradeBar _last;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        /// <meta name="tag" content="using data" />
        /// <meta name="tag" content="consolidating data" />
        public override void Initialize()
        {
            AddEquity("SPY");
            AddForex("EURUSD", Resolution.Hour);

            // we have data for these dates locally
            var start = new DateTime(2013, 10, 07, 09, 30, 0);
            SetStartDate(start);
            SetEndDate(start.AddDays(60));

            // define our 30 minute trade bar consolidator. we can access the 30 minute bar
            // from the DataConsolidated events
            var thirtyMinuteConsolidator = new TradeBarConsolidator(TimeSpan.FromMinutes(30));

            // attach our event handler. the event handler is a function that will be called each time we produce
            // a new consolidated piece of data.
            thirtyMinuteConsolidator.DataConsolidated += ThirtyMinuteBarHandler;

            // this call adds our 30 minute consolidator to the manager to receive updates from the engine
            SubscriptionManager.AddConsolidator("SPY", thirtyMinuteConsolidator);

            // here we'll define a slightly more complex consolidator. what we're trying to produce is a 3
            // day bar.  Now we could just use a single TradeBarConsolidator like above and pass in TimeSpan.FromDays(3),
            // but in reality that's not what we want. For time spans of longer than a day we'll get incorrect results around
            // weekends and such. What we really want are tradeable days. So we'll create a daily consolidator, and then wrap
            // it with a 3 count consolidator.

            // first define a one day trade bar -- this produces a consolidated piece of data after a day has passed
            var oneDayConsolidator = new TradeBarConsolidator(TimeSpan.FromDays(1));

            // next define our 3 count trade bar -- this produces a consolidated piece of data after it sees 3 pieces of data
            var threeCountConsolidator = new TradeBarConsolidator(3);

            // here we combine them to make a new, 3 day trade bar. The SequentialConsolidator allows composition of consolidators.
            // it takes the consolidated output of one consolidator (in this case, the oneDayConsolidator) and pipes it through to
            // the threeCountConsolidator.  His output will be a 3 day bar.
            var three_oneDayBar = new SequentialConsolidator(oneDayConsolidator, threeCountConsolidator);

            // attach our handler
            three_oneDayBar.DataConsolidated += (sender, consolidated) => ThreeDayBarConsolidatedHandler(sender, (TradeBar) consolidated);

            // this call adds our 3 day to the manager to receive updates from the engine
            SubscriptionManager.AddConsolidator("SPY", three_oneDayBar);

            // API convenience method for easily receiving consolidated data
            Consolidate("SPY", TimeSpan.FromMinutes(45), FortyFiveMinuteBarHandler);
            Consolidate("SPY", Resolution.Hour, HourBarHandler);
            Consolidate("EURUSD", Resolution.Daily, DailyEurUsdBarHandler);

            // API convenience method for easily receiving weekly-consolidated data
            Consolidate("SPY", CalendarType.Weekly, CalendarTradeBarHandler);
            Consolidate("EURUSD", CalendarType.Weekly, CalendarQuoteBarHandler);

            // API convenience method for easily receiving monthly-consolidated data
            Consolidate("SPY", CalendarType.Monthly, CalendarTradeBarHandler);
            Consolidate("EURUSD", CalendarType.Monthly, CalendarQuoteBarHandler);

            // requires quote data subscription
            //Consolidate<QuoteBar>("EURUSD", TimeSpan.FromMinutes(45), FortyFiveMinuteBarHandler);
            //Consolidate<QuoteBar>("EURUSD", Resolution.Hour, HourBarHandler);

            // some securities may have trade and quote data available
            //Consolidate<TradeBar>("BTCUSD", Resolution.Hour, HourBarHandler);
            //Consolidate<QuoteBar>("BTCUSD", Resolution.Hour, HourBarHandler);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="bars">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars bars)
        {
            // we need to declare this method
        }

        /// <summary>
        /// End of a trading day event handler. This method is called at the end of the algorithm day (or multiple times if trading multiple assets).
        /// </summary>
        /// <param name="symbol">Asset symbol for this end of day event. Forex and equities have different closing hours.</param>
        public override void OnEndOfDay(string symbol)
        {
            // close up shop each day and reset our 'last' value so we start tomorrow fresh
            Liquidate(symbol);
            _last = null;
        }

        /// <summary>
        /// This is our event handler for our 30 minute trade bar defined above in Initialize(). So each time the consolidator
        /// produces a new 30 minute bar, this function will be called automatically. The 'sender' parameter will be the
        /// instance of the IDataConsolidator that invoked the event, but you'll almost never need that!
        /// </summary>
        private void ThirtyMinuteBarHandler(object sender, TradeBar consolidated)
        {
            if (_last != null && consolidated.Close > _last.Close)
            {
                Log($"{consolidated.Time:o} >> SPY >> LONG  >> 100 >> {Portfolio["SPY"].Quantity}");
                Order("SPY", 100);
            }
            else if (_last != null && consolidated.Close < _last.Close)
            {
                Log($"{consolidated.Time:o} >> SPY >> SHORT >> 100 >> {Portfolio["SPY"].Quantity}");
                Order("SPY", -100);
            }
            _last = consolidated;
        }

        /// <summary>
        /// This is our event handler for our 3 day trade bar defined above in Initialize(). So each time the consolidator
        /// produces a new 3 day bar, this function will be called automatically. The 'sender' parameter will be the
        /// instance of the IDataConsolidator that invoked the event, but you'll almost never need that!
        /// </summary>
        private void ThreeDayBarConsolidatedHandler(object sender, TradeBar consolidated)
        {
            Log($"{consolidated.Time:o} >> Plotting!");
            Plot(consolidated.Symbol, "3HourBar", consolidated.Close);
        }

        /// <summary>
        /// This is our event handler for our one hour consolidated defined using the Consolidate method
        /// </summary>
        private void HourBarHandler(TradeBar consolidated)
        {
            consolidatedHour = true;
            Log($"{consolidated.EndTime:o} Hour consolidated.");
        }

        /// <summary>
        /// This is our event handler for our 45 minute consolidated defined using the Consolidate method
        /// </summary>
        private void FortyFiveMinuteBarHandler(TradeBar consolidated)
        {
            consolidated45Minute = true;
            Log($"{consolidated.EndTime:o} 45 minute consolidated.");
        }

        private void DailyEurUsdBarHandler(QuoteBar consolidated)
        {
            Log($"{consolidated.EndTime:o} EURUSD Daily consolidated.");
        }

        private void CalendarTradeBarHandler(TradeBar tradeBar)
        {
            Log($"{Time} :: {tradeBar.Time:o} {tradeBar.Close}");
        }

        private void CalendarQuoteBarHandler(QuoteBar quoteBar)
        {
            Log($"{Time} :: {quoteBar.Time:o} {quoteBar.Close}");
        }

        public override void OnEndOfAlgorithm()
        {
            if (!consolidatedHour)
            {
                throw new Exception("Expected hourly consolidator to be fired.");
            }

            if (!consolidated45Minute)
            {
                throw new Exception("Expected 45-minute consolidator to be fired.");
            }
        }
    }
}
