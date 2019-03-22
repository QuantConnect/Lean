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
    /// A data consolidator that can make calendar weekly quotebars from smaller ones.
    ///
    /// Use this consolidator to turn data of a higher resolution into calendar weekly quotebars.
    /// </summary>
    public class QuoteBarCalendarConsolidator : CalendarConsolidatorBase<QuoteBar, QuoteBar>
    {
        /// <summary>
        /// Creates a consolidator to produce a new 'QuoteBar' representing the period
        /// </summary>
        /// <param name="calendarType">The minimum span of time before emitting a consolidated bar</param>
        public QuoteBarCalendarConsolidator(CalendarType calendarType = CalendarType.Weekly)
        {
            SetCalendarType(calendarType);
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'QuoteBar' representing one-month period
        /// </summary>
        public static QuoteBarCalendarConsolidator Monthly()
        {
            return new QuoteBarCalendarConsolidator(CalendarType.Monthly);
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'QuoteBar' representing one-week period
        /// </summary>
        public static QuoteBarCalendarConsolidator Weekly()
        {
            return new QuoteBarCalendarConsolidator(CalendarType.Weekly);
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new trade bar</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref QuoteBar workingBar, QuoteBar data)
        {
            var bid = data.Bid;
            var ask = data.Ask;

            if (workingBar == null)
            {
                workingBar = data.Clone() as QuoteBar;
                workingBar.Time = GetRoundedBarTime(data.Time);
                workingBar.Period = Period;
            }

            // update the bid and ask
            if (bid != null)
            {
                workingBar.LastBidSize = data.LastBidSize;
                if (workingBar.Bid == null)
                {
                    workingBar.Bid = bid.Clone();
                }
                else
                {
                    workingBar.Bid.Close = bid.Close;
                    if (workingBar.Bid.High < bid.High) workingBar.Bid.High = bid.High;
                    if (workingBar.Bid.Low > bid.Low) workingBar.Bid.Low = bid.Low;
                }
            }
            if (ask != null)
            {
                workingBar.LastAskSize = data.LastAskSize;
                if (workingBar.Ask == null)
                {
                    workingBar.Ask = ask.Clone();
                }
                else
                {
                    workingBar.Ask.Close = ask.Close;
                    if (workingBar.Ask.High < ask.High) workingBar.Ask.High = ask.High;
                    if (workingBar.Ask.Low > ask.Low) workingBar.Ask.Low = ask.Low;
                }
            }
            workingBar.Value = data.Value;
        }
    }
}