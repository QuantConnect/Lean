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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// This type exists for transport of data as a single packet
    /// </summary>
    public class BaseDataCollection : BaseData, IEnumerable<BaseData>
    {
        private DateTime _endTime;

        /// <summary>
        /// The associated underlying price data if any
        /// </summary>
        public BaseData Underlying { get; set; }

        /// <summary>
        /// Gets or sets the contracts selected by the universe
        /// </summary>
        public HashSet<Symbol> FilteredContracts { get; set; }

        /// <summary>
        /// Gets the data list
        /// </summary>
        public List<BaseData> Data { get; set; }

        /// <summary>
        /// Gets or sets the end time of this data
        /// </summary>
        public override DateTime EndTime
        {
            get
            {
                if (_endTime == default)
                {
                    // to be user friendly let's return Time if not set, like BaseData does
                    return Time;
                }
                return _endTime;
            }
            set
            {
                _endTime = value;
            }
        }

        /// <summary>
        /// Initializes a new default instance of the <see cref="BaseDataCollection"/> c;ass
        /// </summary>
        public BaseDataCollection()
            : this(DateTime.MinValue, Symbol.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataCollection"/> class
        /// </summary>
        /// <param name="time">The time of this data</param>
        /// <param name="symbol">A common identifier for all data in this packet</param>
        /// <param name="data">The data to add to this collection</param>
        public BaseDataCollection(DateTime time, Symbol symbol, IEnumerable<BaseData> data = null)
            : this(time, time, symbol, data)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataCollection"/> class
        /// </summary>
        /// <param name="time">The start time of this data</param>
        /// <param name="endTime">The end time of this data</param>
        /// <param name="symbol">A common identifier for all data in this packet</param>
        /// <param name="data">The data to add to this collection</param>
        /// <param name="underlying">The associated underlying price data if any</param>
        /// <param name="filteredContracts">The contracts selected by the universe</param>
        public BaseDataCollection(DateTime time, DateTime endTime, Symbol symbol, IEnumerable<BaseData> data = null, BaseData underlying = null, HashSet<Symbol> filteredContracts = null)
            : this(time, endTime, symbol, data != null ? data.ToList() : new List<BaseData>(), underlying, filteredContracts)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataCollection"/> class
        /// </summary>
        /// <param name="time">The start time of this data</param>
        /// <param name="endTime">The end time of this data</param>
        /// <param name="symbol">A common identifier for all data in this packet</param>
        /// <param name="data">The data to add to this collection</param>
        /// <param name="underlying">The associated underlying price data if any</param>
        /// <param name="filteredContracts">The contracts selected by the universe</param>
        public BaseDataCollection(DateTime time, DateTime endTime, Symbol symbol, List<BaseData> data, BaseData underlying, HashSet<Symbol> filteredContracts)
        {
            Symbol = symbol;
            Time = time;
            _endTime = endTime;
            Underlying = underlying;
            FilteredContracts = filteredContracts;
            if (data != null && data.Count == 1 && data[0] is BaseDataCollection collection && collection.Data.Count > 0)
            {
                // we were given a base data collection, let's be nice and fetch it's data if it has any
                Data = collection.Data;
            }
            else
            {
                Data = data ?? new List<BaseData>();
            }
        }

        /// <summary>
        /// Creates the universe symbol for the target market
        /// </summary>
        /// <returns>The universe symbol to use</returns>
        public virtual Symbol UniverseSymbol(string market = null)
        {
            market ??= QuantConnect.Market.USA;
            var ticker = $"{GetType().Name}-{market}-{Guid.NewGuid()}";
            return Symbol.Create(ticker, SecurityType.Base, market, baseDataType: GetType());
        }

        /// <summary>
        /// Indicates whether this contains data that should be stored in the security cache
        /// </summary>
        /// <returns>Whether this contains data that should be stored in the security cache</returns>
        public override bool ShouldCacheToSecurity()
        {
            if (Data.Count == 0)
            {
                return true;
            }
            // if we hold the same data type we are, else we ask underlying type
            return Data[0].GetType() == GetType() || Data[0].ShouldCacheToSecurity();
        }

        /// <summary>
        /// Adds a new data point to this collection
        /// </summary>
        /// <param name="newDataPoint">The new data point to add</param>
        public virtual void Add(BaseData newDataPoint)
        {
            Data.Add(newDataPoint);
        }

        /// <summary>
        /// Adds a new data points to this collection
        /// </summary>
        /// <param name="newDataPoints">The new data points to add</param>
        public virtual void AddRange(IEnumerable<BaseData> newDataPoints)
        {
            Data.AddRange(newDataPoints);
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <remarks>
        /// This base implementation uses reflection to copy all public fields and properties
        /// </remarks>
        /// <returns>A clone of the current object</returns>
        public override BaseData Clone()
        {
            return new BaseDataCollection(Time, EndTime, Symbol, Data, Underlying, FilteredContracts);
        }

        /// <summary>
        /// Returns an IEnumerator for this enumerable Object.  The enumerator provides
        /// a simple way to access all the contents of a collection.
        /// </summary>
        public IEnumerator<BaseData> GetEnumerator()
        {
            return (Data ?? Enumerable.Empty<BaseData>()).GetEnumerator();
        }

        /// <summary>
        /// Returns an IEnumerator for this enumerable Object.  The enumerator provides
        /// a simple way to access all the contents of a collection.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
