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
using QuantConnect.Data.Market;
using Python.Runtime;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Type capable of consolidating open interest
    /// </summary>
    public class OpenInterestConsolidator : PeriodCountConsolidatorBase<Tick, OpenInterest>
    {
        private bool _hourOrDailyConsolidation;
        // Keep track of the last input to detect hour or date change
        private Tick _lastInput;

        /// <summary>
        /// Create a new OpenInterestConsolidator for the desired resolution
        /// </summary>
        /// <param name="resolution">The resolution desired</param>
        /// <returns>A consolidator that produces data on the resolution interval</returns>
        public static OpenInterestConsolidator FromResolution(Resolution resolution)
        {
            return new OpenInterestConsolidator(resolution.ToTimeSpan());
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'OpenInterest' representing the period
        /// </summary>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public OpenInterestConsolidator(TimeSpan period)
            : base(period)
        {
            _hourOrDailyConsolidation = period >= Time.OneHour;
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'OpenInterest' representing the last count pieces of data
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emitting a consolidated bar</param>
        public OpenInterestConsolidator(int maxCount)
            : base(maxCount)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'OpenInterest' representing the last count pieces of data or the period, whichever comes first
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emitting a consolidated bar</param>
        /// <param name="period">The minimum span of time before emitting a consolidated bar</param>
        public OpenInterestConsolidator(int maxCount, TimeSpan period)
            : base(maxCount, period)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'OpenInterest'
        /// </summary>
        /// <param name="func">Func that defines the start time of a consolidated data</param>
        public OpenInterestConsolidator(Func<DateTime, CalendarInfo> func)
            : base(func)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'OpenInterest'
        /// </summary>
        /// <param name="pyfuncobj">Python function object that defines the start time of a consolidated data</param>
        public OpenInterestConsolidator(PyObject pyfuncobj)
            : base(pyfuncobj)
        {
        }


        /// <summary>
        /// Determines whether or not the specified data should be processed
        /// </summary>
        /// <param name="data">The data to check</param>
        /// <returns>True if the consolidator should process this data, false otherwise</returns>
        protected override bool ShouldProcess(Tick data)
        {
            return data.TickType == TickType.OpenInterest;
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The bar we're building, null if the event was just fired and we're starting a new OI bar</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref OpenInterest workingBar, Tick data)
        {
            if (workingBar == null)
            {
                workingBar = new OpenInterest
                {
                    Symbol = data.Symbol,
                    Time = _hourOrDailyConsolidation ? data.EndTime : GetRoundedBarTime(data),
                    Value = data.Value
                };

            }
            else
            {
                //Update the working bar
                workingBar.Value = data.Value;

                // If we are consolidating hourly or daily, we need to update the time of the working bar
                // for the end time to match the last data point time
                if (_hourOrDailyConsolidation)
                {
                    workingBar.Time = data.EndTime;
                }
            }
        }

        /// <summary>
        /// Updates this consolidator with the specified data. This method is
        /// responsible for raising the DataConsolidated event.
        /// It will check for date or hour change and force consolidation if needed.
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(Tick data)
        {
            if (_lastInput != null &&
                _hourOrDailyConsolidation &&
                // Detect hour or date change
                ((Period == Time.OneHour && data.EndTime.Hour != _lastInput.EndTime.Hour) ||
                 (Period == Time.OneDay && data.EndTime.Date != _lastInput.EndTime.Date)))
            {
                // Date or hour change, force consolidation, no need to wait for the whole period to pass.
                // Force consolidation by scanning at a time after the end of the period
                Scan(_lastInput.EndTime.Add(Period.Value + Time.OneSecond));
            }

            base.Update(data);
            _lastInput = data;
        }
    }
}
