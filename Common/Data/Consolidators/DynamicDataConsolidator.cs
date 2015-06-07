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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// A data csolidator that can make trade bars from DynamicData derived types. This is useful for
    /// aggregating Quandl and other highly flexible dynamic custom data types.
    /// </summary>
    public class DynamicDataConsolidator : TradeBarConsolidatorBase<DynamicData>
    {
        private bool _first = true;
        private readonly bool _hasVolume;
        private readonly bool _isTradeBar;

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the period. Setting both isTradeBar and hasVolume to
        /// false will result in time-value aggregation only.
        /// </summary>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        /// <param name="isTradeBar">Set to true if the dynamic data has Open, High, Low, and Close properties defined</param>
        /// <param name="hasVolume">Set to true if the dynamic data has Volume defined</param>
        public DynamicDataConsolidator(TimeSpan period, bool isTradeBar, bool hasVolume)
            : base(period)
        {
            _isTradeBar = isTradeBar;
            _hasVolume = hasVolume;
        }
        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the last count pieces of data. Setting both isTradeBar and hasVolume to
        /// false will result in time-value aggregation only.
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        /// <param name="isTradeBar">Set to true if the dynamic data has Open, High, Low, and Close properties defined</param>
        /// <param name="hasVolume">Set to true if the dynamic data has Volume defined</param>
        public DynamicDataConsolidator(int maxCount, bool isTradeBar, bool hasVolume)
            : base(maxCount)
        {
            _isTradeBar = isTradeBar;
            _hasVolume = hasVolume;
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'TradeBar' representing the last count pieces of data or the period, whichever comes first.
        /// Setting both isTradeBar and hasVolume to false will result in time-value aggregation only.
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting a consolidated bar</param>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        /// <param name="isTradeBar">Set to true if the dynamic data has Open, High, Low, and Close properties defined</param>
        /// <param name="hasVolume">Set to true if the dynamic data has Volume defined</param>
        public DynamicDataConsolidator(int maxCount, TimeSpan period, bool isTradeBar, bool hasVolume)
            : base(maxCount, period)
        {
            _isTradeBar = isTradeBar;
            _hasVolume = hasVolume;
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new trade bar</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref TradeBar workingBar, DynamicData data)
        {
            if (_first)
            {
                _first = false;
                // the first time through we're going to inspect the data instance and verify
                // the properties exist. We'll check them all first and throw a message containing
                // all expected and all missing properties names.
                VerifyDataShape(data);
            }

            decimal open, high, low, close;
            long volume = 0;
            if (_isTradeBar)
            {
                open = (decimal) Convert.ChangeType(data.GetProperty("Open"), typeof (decimal));
                high = (decimal) Convert.ChangeType(data.GetProperty("High"), typeof (decimal));
                low = (decimal) Convert.ChangeType(data.GetProperty("Low"), typeof (decimal));
                close = (decimal) Convert.ChangeType(data.GetProperty("Close"), typeof (decimal));
            }
            else
            {
                // fall back on regular time-value aggregation
                open = high = low = close = data.Value;
            }
            if (_hasVolume)
            {
                volume = (long) Convert.ChangeType(data.GetProperty("Volume"), typeof (long));
            }

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

        private void VerifyDataShape(DynamicData data)
        {
            var expected = new List<string>();
            if (_isTradeBar)
            {
                // expect OHLC data
                expected.AddRange(new[]{"open", "high", "low", "close"});
            }
            if (_hasVolume)
            {
                expected.Add("volume");
            }

            var missing = expected.Where(propertyName => !data.HasProperty(propertyName)).ToList();
            if (missing.Any())
            {
                var message = string.Format("Error in DynamicDataConsolidator while consolidating type '{0}' with symbol '{1}'. " +
                    "Expected property names: {2} but the following were missing: {3}", 
                    data.GetType().Name, 
                    data.Symbol,
                    string.Join(", ", expected), 
                    string.Join(", ", missing)
                    );

                throw new Exception(message);
            }
        }
    }
}