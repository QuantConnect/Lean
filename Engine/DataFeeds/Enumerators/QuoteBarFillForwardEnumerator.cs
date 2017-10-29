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

using System.Collections;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// The QuoteBarFillForwardEnumerator wraps an existing base data enumerator
    /// If the current QuoteBar has null Bid and/or Ask bars, it copies them from the previous QuoteBar
    /// </summary>
    public class QuoteBarFillForwardEnumerator : IEnumerator<BaseData>
    {
        private QuoteBar _previous;
        private readonly IEnumerator<BaseData> _enumerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="FillForwardEnumerator"/> class
        /// </summary>
        public QuoteBarFillForwardEnumerator(IEnumerator<BaseData> enumerator)
        {
            _enumerator = enumerator;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public BaseData Current
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
        public bool MoveNext()
        {
            if (!_enumerator.MoveNext()) return false;

            var bar = _enumerator.Current as QuoteBar;
            if (bar != null)
            {
                if (_previous != null)
                {
                    if (bar.Bid == null)
                    {
                        bar.Bid = _previous.Bid;
                    }

                    if (bar.Ask == null)
                    {
                        bar.Ask = _previous.Ask;
                    }
                }

                _previous = bar;
            }

            Current = _enumerator.Current;

            return true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _enumerator.Dispose();
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
        public void Reset()
        {
            _enumerator.Reset();
        }
    }
}