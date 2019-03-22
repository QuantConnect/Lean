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
    public class TradeBarCalendarConsolidator : CalendarConsolidatorBase<TradeBar, TradeBar>
    {
        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the period
        /// </summary>
        /// <param name="calendarType">The minimum span of time before emitting a consolidated bar</param>
        public TradeBarCalendarConsolidator(CalendarType calendarType = CalendarType.Weekly)
        {
            SetCalendarType(calendarType);
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing one-month period
        /// </summary>
        public static TradeBarCalendarConsolidator Monthly()
        {
            return new TradeBarCalendarConsolidator(CalendarType.Monthly);
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing one-week period
        /// </summary>
        public static TradeBarCalendarConsolidator Weekly()
        {
            return new TradeBarCalendarConsolidator(CalendarType.Weekly);
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new trade bar</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref TradeBar workingBar, TradeBar data)
        {
            if (workingBar == null)
            {
                workingBar = new TradeBar(data)
                {
                    Time = GetRoundedBarTime(data.Time),
                    Period = Period
                };
            }
            else
            {
                //Aggregate the working bar
                workingBar.Close = data.Close;
                workingBar.Volume += data.Volume;
                if (data.Low < workingBar.Low) workingBar.Low = data.Low;
                if (data.High > workingBar.High) workingBar.High = data.High;
            }
        }
    }
}