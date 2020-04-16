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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Enumerator that will concatenate enumerators together sequentially enumerating them in the provided order
    /// </summary>
    public class ConcatEnumerator : IEnumerator<BaseData>
    {
        private readonly IEnumerator<BaseData> _concatEnumerator;
        private readonly List<IEnumerator<BaseData>> _enumerators;
        private readonly bool _skipDuplicateEndTimes;
        private DateTime? _lastEnumeratorEndTime;

        /// <summary>
        /// The current BaseData object
        /// </summary>
        public BaseData Current { get; set; }
        object IEnumerator.Current => Current;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="skipDuplicateEndTimes">True will skip data points from enumerators if before or at the last end time</param>
        /// <param name="enumerators">The sequence of enumerators to concatenate</param>
        public ConcatEnumerator(bool skipDuplicateEndTimes,
            params IEnumerator<BaseData>[] enumerators
            )
        {
            _enumerators = enumerators.ToList();
            _skipDuplicateEndTimes = skipDuplicateEndTimes;
            _concatEnumerator = GetConcatEnumerator();
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>True if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            var moveNext = _concatEnumerator.MoveNext();
            Current = moveNext ? _concatEnumerator.Current : null;
            return moveNext;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            foreach (var enumerator in _enumerators)
            {
                enumerator.Reset();
            }
            _concatEnumerator.Reset();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var enumerator in _enumerators)
            {
                enumerator.DisposeSafely();
            }
            _concatEnumerator.DisposeSafely();
        }

        private IEnumerator<BaseData> GetConcatEnumerator()
        {
            foreach (var enumerator in _enumerators)
            {
                while (enumerator.MoveNext())
                {
                    if (_skipDuplicateEndTimes
                        && _lastEnumeratorEndTime.HasValue
                        && enumerator.Current != null
                        && enumerator.Current.EndTime < _lastEnumeratorEndTime)
                    {
                        continue;
                    }

                    Current = enumerator.Current;
                    yield return Current;
                }
                _lastEnumeratorEndTime = Current?.EndTime;
            }
        }
    }
}
