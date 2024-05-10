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
using Python.Runtime;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Consolidates QuoteBars into larger QuoteBars
    /// </summary>
    public class QuoteBarConsolidator : PeriodCountConsolidatorBase<QuoteBar, QuoteBar>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuoteBarConsolidator"/> class
        /// </summary>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public QuoteBarConsolidator(TimeSpan period)
            : base(period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuoteBarConsolidator"/> class
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emitting a consolidated bar</param>
        public QuoteBarConsolidator(int maxCount)
            : base(maxCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuoteBarConsolidator"/> class
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emitting a consolidated bar</param>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public QuoteBarConsolidator(int maxCount, TimeSpan period)
            : base(maxCount, period)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'QuoteBar' representing the last count pieces of data or the period, whichever comes first
        /// </summary>
        /// <param name="func">Func that defines the start time of a consolidated data</param>
        public QuoteBarConsolidator(Func<DateTime, CalendarInfo> func)
            : base(func)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'QuoteBar' representing the last count pieces of data or the period, whichever comes first
        /// </summary>
        /// <param name="pyfuncobj">Python function object that defines the start time of a consolidated data</param>
        public QuoteBarConsolidator(PyObject pyfuncobj)
            : base(pyfuncobj)
        {
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new consolidated bar</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref QuoteBar workingBar, QuoteBar data)
        {
            var bid = data.Bid;
            var ask = data.Ask;

            if (workingBar == null)
            {
                workingBar = new QuoteBar(GetRoundedBarTime(data), data.Symbol, null, 0, null, 0, IsTimeBased && Period.HasValue ? Period : data.Period);

                // open ask and bid should match previous close ask and bid
                if (Consolidated != null)
                {
                    // note that we will only fill forward previous close ask and bid when a new data point comes in and we generate a new working bar which is not a fill forward bar
                    var previous = Consolidated as QuoteBar;
                    workingBar.Update(0, previous.Bid?.Close ?? 0, previous.Ask?.Close ?? 0, 0, previous.LastBidSize, previous.LastAskSize);
                }
            }
            else if (!IsTimeBased)
            {
                // we should only increment the period after the first data we get, else we would be accouting twice for the inital bars period
                // because in the `if` above we are already providing the `data.Period` as argument. See test 'AggregatesNewCountQuoteBarProperly' which assert period
                workingBar.Period += data.Period;
            }

            // update the bid and ask
            if (bid != null)
            {
                workingBar.LastBidSize = data.LastBidSize;
                if (workingBar.Bid == null)
                {
                    workingBar.Bid = new Bar(bid.Open, bid.High, bid.Low, bid.Close);
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
                    workingBar.Ask = new Bar(ask.Open, ask.High, ask.Low, ask.Close);
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
