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
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Consolidates ticks into quote bars. This consolidator ignores trade ticks
    /// </summary>
    public class TickQuoteBarConsolidator : PeriodCountConsolidatorBase<Tick, QuoteBar>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TickQuoteBarConsolidator"/> class
        /// </summary>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public TickQuoteBarConsolidator(TimeSpan period)
            : base(period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TickQuoteBarConsolidator"/> class
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        public TickQuoteBarConsolidator(int maxCount)
            : base(maxCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TickQuoteBarConsolidator"/> class
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public TickQuoteBarConsolidator(int maxCount, TimeSpan period)
            : base(maxCount, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TickQuoteBarConsolidator"/> class
        /// </summary>
        /// <param name="func">Func that defines the start time of a consolidated data</param>
        public TickQuoteBarConsolidator(Func<DateTime, CalendarInfo> func)
            : base(func)
        {
        }

        /// <summary>
        /// Determines whether or not the specified data should be processd
        /// </summary>
        /// <param name="data">The data to check</param>
        /// <returns>True if the consolidator should process this data, false otherwise</returns>
        protected override bool ShouldProcess(Tick data)
        {
            return data.TickType == TickType.Quote;
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new consolidated bar</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref QuoteBar workingBar, Tick data)
        {
            if (workingBar == null)
            {
                workingBar = new QuoteBar
                {
                    Symbol = data.Symbol,
                    Time = GetRoundedBarTime(data.Time),
                    Bid = null,
                    Ask = null
                };

                if (Period.HasValue) workingBar.Period = Period.Value;
            }

            // update the bid and ask
            workingBar.Update(0, data.BidPrice, data.AskPrice, 0, data.BidSize, data.AskSize);
            if (!Period.HasValue) workingBar.EndTime = GetRoundedBarTime(data.EndTime);
        }
    }
}