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
using System;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// A data consolidator that can make calendar weekly bars from smaller ones.
    ///
    /// Use this consolidator to turn data of a higher resolution into calendar weekly data.
    /// </summary>
    public class DynamicDataCalendarConsolidator : CalendarConsolidatorBase<DynamicData, TradeBar>
    {
        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the period
        /// </summary>
        /// <param name="calendarType">The minimum span of time before emitting a consolidated bar</param>
        public DynamicDataCalendarConsolidator(CalendarType calendarType = CalendarType.Weekly)
        {
            SetCalendarType(calendarType);
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing one-month period
        /// </summary>
        public static DynamicDataCalendarConsolidator Monthly()
        {
            return new DynamicDataCalendarConsolidator(CalendarType.Monthly);
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing one-week period
        /// </summary>
        public static DynamicDataCalendarConsolidator Weekly()
        {
            return new DynamicDataCalendarConsolidator(CalendarType.Weekly);
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new trade bar</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref TradeBar workingBar, DynamicData data)
        {
            // grab the properties, if they don't exist just use the .Value property
            var open = GetNamedPropertyOrValueProperty(data, "Open");
            var high = GetNamedPropertyOrValueProperty(data, "High");
            var low = GetNamedPropertyOrValueProperty(data, "Low");
            var close = GetNamedPropertyOrValueProperty(data, "Close");

            // if we have volume, use it, otherwise just use zero
            var volume = data.HasProperty("Volume")
                ? (long)Convert.ChangeType(data.GetProperty("Volume"), typeof(long))
                : 0L;

            if (workingBar == null)
            {
                workingBar = new TradeBar
                {
                    Symbol = data.Symbol,
                    Time = GetRoundedBarTime(data.Time),
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume
                };
            }
            else
            {
                //Aggregate the working bar
                workingBar.Close = close;
                workingBar.Volume += volume;
                if (low < workingBar.Low) workingBar.Low = low;
                if (high > workingBar.High) workingBar.High = high;
            }
        }

        private static decimal GetNamedPropertyOrValueProperty(DynamicData data, string propertyName)
        {
            return data.HasProperty(propertyName)
                ? (decimal)Convert.ChangeType(data.GetProperty(propertyName), typeof(decimal))
                : data.Value;
        }
    }
}