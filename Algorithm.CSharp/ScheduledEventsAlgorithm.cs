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
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class ScheduledEventsAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, "SPY", Resolution.Minute);

            // you can now schedule code to run by using the Schedule property
            // DateRules and TimeRules are properties on QCAlgorithm that make it easy
            // to define common times for events.
            //
            // DateRules.EveryDay:
            //      Produces dates for every day the algorithm runs
            //
            // TimeRules.MarketOpen(symbol, minutesAfter):
            //      Filters dates for when the exchange is open, schedules event
            //      at the specified number of minutes after market open
            //
            Schedule.On(DateRules.EveryDay(), TimeRules.AfterMarketOpen("SPY", 5), () =>
            {
                // spy market open +5 minutes event
                Console.WriteLine();
                Console.WriteLine("{0}: {1} ON AFTER MARKET OPEN", Time, "SPY");
                if (Securities["SPY"].Holdings.UnrealizedProfitPercent >= 0.005m)
                {
                    // sell 1/10th of shares
                    var qty = Securities["SPY"].Holdings.Quantity / 10;
                    MarketOrder("SPY", -qty);
                }
            });

            // same event as above using the fluent sytax
            //Schedule.Event()
            //    .EveryDay()
            //    .AfterMarketOpen("SPY", 5)
            //    .Run(() =>{ /*This will fire five minutes after SPY market open*/ });

            //
            // DateRules.Every(params DayOfWeek[]):
            //      Produces dates that match the specified days of the week
            //
            // TimeRules.MarketOpen(symbol, minutesAfter):
            //      Filters dates for when the exchange is open, schedules event
            //      at the specified number of minutes after market open
            //
            Schedule.On(DateRules.Every(DayOfWeek.Tuesday, DayOfWeek.Thursday), TimeRules.BeforeMarketClose("SPY", 15), () =>
            {
                // tuesdays/thursdays, 15 minutes before market close
                Console.WriteLine("{0}: {1} ON {2} BEFORE CLOSE", Time, "SPY", Time.DayOfWeek.ToString().ToUpper());
                SetHoldings("SPY", 1.0);
            });

            // same event as above using the fluent syntax
            //Schedule.Event()
            //    .Every(DayOfWeek.Tuesday, DayOfWeek.Thursday)
            //    .BeforeMarketClose("SPY", 15)
            //    .Run(() => {/*This will fire on the first SPY trading day of the month 15 minutes before market close*/});
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
        }
    }
}