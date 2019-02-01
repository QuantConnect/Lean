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
using System.Collections;
using System.Collections.Generic;

using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// A enumerator with range control
    /// </summary>
    public class DataPointEnumerator : IEnumerator<string>
    {
        /// <summary>
        /// The data point cache sorted by time ascending
        /// </summary>
        private readonly DataPointDictionary _dataPointCache;

        /// <summary>
        /// The subscription data config
        /// </summary>
        private readonly SubscriptionDataConfig _config;

        /// <summary>
        /// The start index boundary of this enumerator. Inclusive.
        /// </summary>
        private readonly int _startIndex;

        /// <summary>
        /// The end index boundary of this enumerator. Inclusive.
        /// </summary>
        private readonly int _endIndex;

        /// <summary>
        /// The current position of this enumerator
        /// </summary>
        private int _position;

        /// <summary>
        /// Initialize a new instance of the <see cref="DataPointEnumerator"/>
        /// </summary>
        /// <param name="dataPointDictionary">The data point dictionary sorted by date</param>
        /// <param name="config">The subscription config</param>
        /// <param name="startDate">The start date of this enumerator. Inclusive.</param>
        /// <param name="endDate">The end date of this enumerator. Inclusive.</param>
        public DataPointEnumerator(
            DataPointDictionary dataPointDictionary,
            SubscriptionDataConfig config,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                throw new ArgumentException(
                    $"Start date cannot be later than end date. Start date: " +
                    $"{startDate.ToString()}; End date: {endDate.ToString()}");
            }

            if (dataPointDictionary.IsEmpty())
            {
                throw new ArgumentNullException(
                    nameof(dataPointDictionary),
                    $"{nameof(dataPointDictionary)} cannot be null. There is no " +
                    $"data point to enumerate.");
            }

            this._dataPointCache = dataPointDictionary;
            this._config = config;
            this.FindStartEndDateIndex(out this._startIndex, out this._endIndex, startDate, endDate);
            this._position = this._startIndex - 1;
        }

        /// <summary>
        /// Get current value
        /// </summary>
        public string Current
        {
            get
            {
                if (this._position < this._startIndex || this._position > this._endIndex)
                {
                    return null;
                }

                return this._dataPointCache[this._position].Value;
            }
        }

        /// <summary>
        /// Return if this enumerator has next element
        /// </summary>
        /// <returns>True if there is more element. False otherwise.</returns>
        public bool EndOfStream
        {
            get
            {
                return this._position > this._endIndex;
            }
        }

        /// <summary>
        /// Return current value
        /// </summary>
        object IEnumerator.Current => this.Current;

        /// <summary>
        /// Dispose method for this enumerator
        /// </summary>
        public void Dispose()
        {
            // Nothing to be disposed since cache is referenced.
        }

        /// <summary>
        /// Move this enumerator to next available entry.
        /// </summary>
        /// <returns>True if next element exists; Otherwise False.</returns>
        public bool MoveNext()
        {
            this._position++;
            if (this._position < this._startIndex || this._position > this._endIndex)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reset current position back to start index.
        /// </summary>
        public void Reset()
        {
            this._position = this._startIndex - 1;
        }

        /// <summary>
        /// Calculate start and end index based on input dates and entries in the cache. StartIndex and EndIndex are inclusive.
        /// </summary>
        /// <param name="startIndex">The output start index</param>
        /// <param name="endIndex">The output end index</param>
        /// <param name="startDate">The input start date</param>
        /// <param name="endDate">The input end date</param>
        private void FindStartEndDateIndex(
            out int startIndex,
            out int endIndex,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            startIndex = 0;
            endIndex = this._dataPointCache.Count - 1;

            if (this._config.Resolution == Resolution.Daily || this._config.Resolution == Resolution.Hour)
            {
                DateTime startKey = startDate.HasValue && (startDate >= this._dataPointCache[0].Key) ?
                this._dataPointCache.First(k => k.Key >= startDate.Value).Key :  // Handle if start date is not trade day
                this._dataPointCache[0].Key;
                startIndex = this._dataPointCache.IndexOfKey(startKey);

                DateTime endKey = endDate.HasValue && (endDate > this._dataPointCache[this._dataPointCache.Count - 1].Key) ?
                    this._dataPointCache.Last(k => k.Key <= endDate.Value).Key : // Handle if end date is not trade day
                    this._dataPointCache[this._dataPointCache.Count - 1].Key;
                endIndex = this._dataPointCache.IndexOfKey(endKey);
            }
        }
    }
}
