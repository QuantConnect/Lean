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

using System.Linq;
using QuantConnect.Data;
using System.Collections;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Enumerator that performs universe selection
    /// </summary>
    public class UniverseSelectionEnumerator : IEnumerator<BaseData>
    {
        private BaseDataCollection _previous;
        private readonly SubscriptionRequest _request;
        private readonly IEnumerator<BaseData> _baseEnumerator;
        private readonly TimeZoneOffsetProvider _offsetProvider;

        /// <summary>
        /// The current data point
        /// </summary>
        public BaseData Current { get; set; }
        object IEnumerator.Current => Current;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public UniverseSelectionEnumerator(IEnumerator<BaseData> baseEnumerator, SubscriptionRequest request)
        {
            _request = request;
            _baseEnumerator = baseEnumerator;
            _offsetProvider = new TimeZoneOffsetProvider(request.Configuration.ExchangeTimeZone, request.StartTimeUtc, request.EndTimeUtc);
        }

        /// <summary>
        /// Will move this enumerator next
        /// </summary>
        public bool MoveNext()
        {
            var result = _baseEnumerator.MoveNext();
            Current = _baseEnumerator.Current;
            if (result && Current is BaseDataCollection collection)
            {
                var timeUtc = _offsetProvider.ConvertToUtc(collection.EndTime);
                var selection = _request.Universe.SelectSymbols(timeUtc, collection);
                if (ReferenceEquals(selection, Universe.Unchanged))
                {
                    collection.FilteredContracts = _previous.FilteredContracts;
                }
                else
                {
                    collection.FilteredContracts = selection.ToHashSet();
                }
                _previous = collection;
            }
            return result;
        }

        /// <summary>
        /// Reset this enumerator
        /// </summary>
        public void Reset()
        {
            _previous = null;
            _baseEnumerator.Reset();
        }

        /// <summary>
        /// Dispose of this enumerator
        /// </summary>
        public void Dispose()
        {
            _baseEnumerator.Dispose();
        }
    }
}
