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
using System.Threading;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Monitors and reports the progress of a backtest
    /// </summary>
    public class BacktestProgressMonitor
    {
        private const int ProcessedDaysCountInvalid = 0;
        private const int ProcessedDaysCountValid = 1;

        private readonly ITimeKeeper _timeKeeper;

        private readonly DateTime _startUtcTime;

        private int _processedDays;
        private int _isProcessedDaysCountValid;

        /// <summary>
        /// Gets the total days the algorithm will run
        /// </summary>
        public int TotalDays { get; private set; }

        /// <summary>
        /// Gets the current days the algorithm has been running for
        /// </summary>
        public int ProcessedDays {
            get
            {
                if (Interlocked.CompareExchange(ref _isProcessedDaysCountValid, ProcessedDaysCountValid, ProcessedDaysCountInvalid) == ProcessedDaysCountInvalid)
                {
                    try
                    {
                        // We use 'int' so it's thread safe
                        _processedDays = (int)(_timeKeeper.UtcTime - _startUtcTime).TotalDays;
                    }
                    catch (OverflowException)
                    {
                    }
                }

                return _processedDays;
            }
        }

        /// <summary>
        /// Gets the current progress of the backtest
        /// </summary>
        public decimal Progress
        {
            get { return Math.Min((decimal)ProcessedDays / TotalDays, 0.999m); }
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="timeKeeper">The time keeper to use</param>
        /// <param name="startUtcTime">The start UTC time</param>
        /// <param name="endUtcTime">The end UTC time</param>
        public BacktestProgressMonitor(ITimeKeeper timeKeeper, DateTime startUtcTime, DateTime endUtcTime)
        {
            _timeKeeper = timeKeeper;
            _startUtcTime = startUtcTime;
            TotalDays = Convert.ToInt32((endUtcTime.Date - _timeKeeper.UtcTime.Date).TotalDays) + 1;
        }

        /// <summary>
        /// Invalidates the processed days count value so it gets recalculated next time it is needed
        /// </summary>
        public void InvalidateProcessedDays()
        {
            Interlocked.Exchange(ref _isProcessedDaysCountValid, ProcessedDaysCountInvalid);
        }
    }
}
