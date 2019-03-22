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

using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// A data consolidator that can make calendar weekly bars from smaller ones.
    ///
    /// Use this consolidator to turn data of a higher resolution into calendar weekly data.
    /// </summary>
    public class TickQuoteBarCalendarConsolidator : CalendarConsolidatorBase<Tick, QuoteBar>
    {
        /// <summary>
        /// Creates a consolidator to produce a new 'QuoteBar' representing the period
        /// </summary>
        /// <param name="calendarType">The minimum span of time before emitting a consolidated bar</param>
        public TickQuoteBarCalendarConsolidator(CalendarType calendarType = CalendarType.Weekly)
        {
            SetCalendarType(calendarType);
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'QuoteBar' representing one-month period
        /// </summary>
        public static TickQuoteBarCalendarConsolidator Monthly()
        {
            return new TickQuoteBarCalendarConsolidator(CalendarType.Monthly);
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'QuoteBar' representing one-week period
        /// </summary>
        public static TickQuoteBarCalendarConsolidator Weekly()
        {
            return new TickQuoteBarCalendarConsolidator(CalendarType.Weekly);
        }

        /// <summary>
        /// Determines whether or not the specified data should be processd
        /// </summary>
        /// <param name="data">The data to check</param>
        /// <returns>True if the consolidator should process this data, false otherwise</returns>
        protected override bool ShouldProcess(Tick data) => data.TickType == TickType.Quote;

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new trade bar</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref QuoteBar workingBar, Tick data)
        {
            if (workingBar == null)
            {
                workingBar = new QuoteBar()
                {
                    Symbol = data.Symbol,
                    Time = GetRoundedBarTime(data.Time),
                    Period = Period,
                    DataType = data.DataType
                };
            }
            // Update the working bar
            workingBar.Update(data.LastPrice, data.BidPrice, data.AskPrice, data.Quantity, data.BidSize, data.AskSize);
        }
    }
}