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
using System.Threading;
using QuantConnect.Interfaces;
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Data
{
    /// <summary>
    /// Helper class to wrap a consolidator and keep track of the next scan time we should trigger
    /// </summary>
    internal class ConsolidatorWrapper : IDisposable
    {
        // helps us guarantee a deterministic ordering by addition/creation
        private static long _counter;

        private readonly IDataConsolidator _consolidator;
        private readonly LocalTimeKeeper _localTimeKeeper;
        private readonly TimeSpan _minimumIncrement;
        private readonly ITimeKeeper _timeKeeper;
        private readonly long _id;
        private TimeSpan? _barSpan;

        /// <summary>
        /// True if this consolidator has been removed
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// The next utc scan time
        /// </summary>
        public DateTime UtcScanTime { get; private set; }

        /// <summary>
        /// Get enqueue time
        /// </summary>
        public ConsolidatorScanPriority Priority => new (UtcScanTime, _id);

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public ConsolidatorWrapper(IDataConsolidator consolidator, TimeSpan configIncrement, ITimeKeeper timeKeeper, LocalTimeKeeper localTimeKeeper)
        {
            _id = Interlocked.Increment(ref _counter);

            _timeKeeper = timeKeeper;
            _consolidator = consolidator;
            _localTimeKeeper = localTimeKeeper;

            _minimumIncrement = configIncrement < Time.OneSecond ? Time.OneSecond : configIncrement;

            _consolidator.DataConsolidated += AdvanceScanTime;
            AdvanceScanTime();
        }

        /// <summary>
        /// Scans the current consolidator
        /// </summary>
        public void Scan()
        {
            _consolidator.Scan(_localTimeKeeper.LocalTime);

            // it might not of emitted at all, could happen if we got no data or it's not expected to emit like in a weekend
            // but we still need to advance the next scan time
            AdvanceScanTime();
        }

        public void Dispose()
        {
            Disposed = true;
            _consolidator.DataConsolidated -= AdvanceScanTime;
        }

        /// <summary>
        /// Helper method to set the next scan time
        /// </summary>
        private void AdvanceScanTime(object _ = null, IBaseData consolidated = null)
        {
            if (consolidated == null && UtcScanTime > _timeKeeper.UtcTime)
            {
                // already set
                return;
            }

            if (_barSpan.HasValue)
            {
                var reference = _timeKeeper.UtcTime;
                if (consolidated != null)
                {
                    reference = consolidated.EndTime.ConvertToUtc(_localTimeKeeper.TimeZone);
                }
                UtcScanTime = reference + _barSpan.Value;
            }
            else
            {
                if (consolidated != null)
                {
                    _barSpan = consolidated.EndTime - consolidated.Time;
                    if (_barSpan < _minimumIncrement)
                    {
                        _barSpan = _minimumIncrement;
                    }

                    UtcScanTime = consolidated.EndTime.ConvertToUtc(_localTimeKeeper.TimeZone) + _barSpan.Value;
                }
                else if (_consolidator.WorkingData == null)
                {
                    // we have no reference
                    UtcScanTime = _timeKeeper.UtcTime + _minimumIncrement;
                }
                else
                {
                    var pontetialEndTime = _consolidator.WorkingData.EndTime.ConvertToUtc(_localTimeKeeper.TimeZone);
                    if (pontetialEndTime > _timeKeeper.UtcTime)
                    {
                        UtcScanTime = pontetialEndTime;
                    }
                    else
                    {
                        UtcScanTime = _timeKeeper.UtcTime + _minimumIncrement;
                    }
                }
            }
        }
    }

    internal class ConsolidatorScanPriority : IComparable
    {
        /// <summary>
        /// The next utc scan time
        /// </summary>
        public DateTime UtcScanTime { get; }

        /// <summary>
        /// Unique Id of the associated consolidator
        /// </summary>
        public long Id { get; }

        public ConsolidatorScanPriority(DateTime utcScanTime, long id)
        {
            Id = id;
            UtcScanTime = utcScanTime;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            var other = (ConsolidatorScanPriority)obj;
            var result = UtcScanTime.CompareTo(other.UtcScanTime);
            if (result == 0)
            {
                // if they are the same let's compare Ids too
                return Id.CompareTo(other.Id);
            }
            return result;
        }
    }
}
