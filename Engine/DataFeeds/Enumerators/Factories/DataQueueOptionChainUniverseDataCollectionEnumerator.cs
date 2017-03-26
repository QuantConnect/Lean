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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using QuantConnect.Data.Auxiliary;
using System.Collections;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Enumerates live options symbol universe data  into <see cref="OptionChainUniverseDataCollection"/> instances
    /// </summary>
    public class DataQueueOptionChainUniverseDataCollectionEnumerator : IEnumerator<OptionChainUniverseDataCollection>
    {
        private readonly List<BaseData> _symbolUniverse;
        private readonly IEnumerator<BaseData> _underlying;
        private readonly Symbol _symbol;
        private OptionChainUniverseDataCollection _current;
        private bool _needNewCurrent;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataQueueOptionChainUniverseDataCollectionEnumerator"/> class.
        /// </summary>
        /// <param name="symbol">Option contract symbol</param>
        /// <param name="underlying">Underlying enumerator</param>
        /// <param name="symbolUniverse">Symbol universe of the data queue</param>
        public DataQueueOptionChainUniverseDataCollectionEnumerator(Symbol symbol, IEnumerator<BaseData> underlying, List<BaseData> symbolUniverse)
        {
            _symbolUniverse = symbolUniverse;
            _underlying = underlying;
            _needNewCurrent = true;
            _symbol = symbol;

            _current = new OptionChainUniverseDataCollection { Symbol = _symbol, Data = symbolUniverse };
        }

        /// <summary>
        /// Returns current option chain enumerator position 
        /// </summary>
        public OptionChainUniverseDataCollection Current
        {
            get
            {
                if (_underlying.Current == null)
                {
                    return null;
                }

                // if we need to update the enumerator, we update it wrapping around the underlying enumerator
                if (_needNewCurrent)
                {
                    var current = (OptionChainUniverseDataCollection)_current.Clone();
                    current.Underlying = _underlying.Current;
                    current.Time = _underlying.Current.Time;
                    current.EndTime = _underlying.Current.EndTime;

                    _current = current;
                    _needNewCurrent = false;
                }

                return _current;
            }
        }

        /// <summary>
        /// Returns current option chain enumerator position 
        /// </summary>
        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _underlying.Dispose();
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            _needNewCurrent = _underlying.MoveNext();
            return _needNewCurrent;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            _underlying.Reset();
            _needNewCurrent = true;
        }
    }
}