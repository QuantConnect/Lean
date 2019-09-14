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
using QuantConnect.Data.Custom.PsychSignal;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Consolidates PsychSignal data to the time period specified
    /// </summary>
    public class PsychSignalConsolidator : PeriodCountConsolidatorBase<PsychSignalSentimentData, PsychSignalConsolidated>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PsychSignalConsolidator"/> class
        /// </summary>
        /// <param name="period">The minimum span of time before emitting <see cref="PsychSignalConsolidated"/></param>
        public PsychSignalConsolidator(TimeSpan period)
            : base(period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PsychSignalConsolidator"/> class
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting <see cref="PsychSignalConsolidated"/></param>
        public PsychSignalConsolidator(int maxCount)
            : base(maxCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PsychSignalConsolidator"/> class
        /// </summary>
        /// <param name="maxCount">The number of pieces to accept before emiting <see cref="PsychSignalConsolidated"/></param>
        /// <param name="period">The minimum span of time before emitting <see cref="PsychSignalConsolidated"/></param>
        public PsychSignalConsolidator(int maxCount, TimeSpan period)
            : base(maxCount, period)
        {
        }

        /// <summary>
        /// Creates a consolidator to produce a new 'PsychSignalConsolidated' representing the last count pieces of data or the period, whichever comes first
        /// </summary>
        /// <param name="func">Func that defines the start time of a consolidated data</param>
        public PsychSignalConsolidator(Func<DateTime, CalendarInfo> func)
            : base(func)
        {
        }

        /// <summary>
        /// Aggregates the new 'data' into the 'workingBar'. The 'workingBar' will be
        /// null following the event firing
        /// </summary>
        /// <param name="workingBar">The <see cref="PsychSignalConsolidated"/> instance we're building, null if the event was just fired and we're starting a new instance</param>
        /// <param name="data">The new data</param>
        protected override void AggregateBar(ref PsychSignalConsolidated workingBar, PsychSignalSentimentData data)
        {
            var bullIntensity = data.BullIntensity;
            var bearIntensity = data.BearIntensity;
            var bullMinusBear = data.BullMinusBear;
            var bullScoredMessages = data.BullScoredMessages;
            var bearScoredMessages = data.BearScoredMessages;
            var bullBearMessageRatio = data.BullBearMessageRatio;
            var totalScoredMessages = data.TotalScoredMessages;

            if (workingBar == null)
            {
                workingBar = new PsychSignalConsolidated
                {
                    Symbol = data.Symbol,
                    Time = GetRoundedBarTime(data.Time),
                    BullIntensity = new Bar(data.BullIntensity, data.BullIntensity, data.BullIntensity, data.BullIntensity),
                    BearIntensity = new Bar(data.BearIntensity, data.BearIntensity, data.BearIntensity, data.BearIntensity),
                    BullMinusBear = new Bar(data.BullMinusBear, data.BullMinusBear, data.BullMinusBear, data.BullMinusBear),
                    BullScoredMessages = data.BullScoredMessages,
                    BearScoredMessages = data.BearScoredMessages,
                    BullBearMessageRatio = new Bar(data.BullBearMessageRatio, data.BullBearMessageRatio, data.BullBearMessageRatio, data.BullBearMessageRatio),
                    TotalScoredMessages = data.TotalScoredMessages
                };

                return;
            }

            UpdateBar(workingBar.BullIntensity, bullIntensity);
            UpdateBar(workingBar.BearIntensity, bearIntensity);
            UpdateBar(workingBar.BullMinusBear, bullMinusBear);
            workingBar.BullScoredMessages += data.BullScoredMessages;
            workingBar.BearScoredMessages += data.BearScoredMessages;
            UpdateBar(workingBar.BullBearMessageRatio, bullBearMessageRatio);
            workingBar.TotalScoredMessages += data.TotalScoredMessages;
            workingBar.EndTime = GetRoundedBarTime(data.EndTime);
        }

        /// <summary>
        /// Copy of Tick's <see cref="Tick.Update(decimal, decimal, decimal, decimal, decimal, decimal)"/> method
        /// but does not skip zeroes, nor does it re-assign all values to zero if Open is zero
        /// </summary>
        /// <param name="data">Bar data to mutate</param>
        /// <param name="value">Value to set high or low if it matches the criteria, and sets close to value</param>
        private void UpdateBar(Bar data, decimal value)
        {
            if (value > data.High) data.High = value;
            if (value < data.Low) data.Low = value;
            data.Close = value;
        }
    }
}