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
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Enumerator that will concatenate enumerators together sequentially enumerating them in the provided order
    /// </summary>
    public class ConcatEnumerator : IEnumerator<BaseData>
    {
        private readonly List<IEnumerator<BaseData>> _enumerators;
        private readonly bool _skipDuplicateEndTimes;
        private DateTime? _lastEnumeratorEndTime;
        private int _currentIndex;

        /// <summary>
        /// The current BaseData object
        /// </summary>
        public BaseData Current { get; set; }

        /// <summary>
        /// True if emitting a null data point is expected
        /// </summary>
        /// <remarks>Warmup enumerators are not allowed to return true and setting current to Null, this is because it's not a valid behavior for backtesting enumerators,
        /// for example <see cref="FillForwardEnumerator"/></remarks>
        public bool CanEmitNull { get; set; }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="skipDuplicateEndTimes">True will skip data points from enumerators if before or at the last end time</param>
        /// <param name="enumerators">The sequence of enumerators to concatenate. Note that the order here matters, it will consume enumerators
        /// and dispose of them, even if they return true and their current is null, except for the last which will be kept!</param>
        public ConcatEnumerator(
            bool skipDuplicateEndTimes,
            params IEnumerator<BaseData>[] enumerators
        )
        {
            CanEmitNull = true;
            _skipDuplicateEndTimes = skipDuplicateEndTimes;
            _enumerators = enumerators.Where(enumerator => enumerator != null).ToList();
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>True if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            for (; _currentIndex < _enumerators.Count; _currentIndex++)
            {
                var enumerator = _enumerators[_currentIndex];
                while (enumerator.MoveNext())
                {
                    if (
                        enumerator.Current == null
                        && (_currentIndex < _enumerators.Count - 1 || !CanEmitNull)
                    )
                    {
                        // if there are more enumerators and the current stopped providing data drop it
                        // in live trading, some enumerators will always return true (see TimeTriggeredUniverseSubscriptionEnumeratorFactory & InjectionEnumerator)
                        // but unless it's the last enumerator we drop it, because these first are the warmup enumerators
                        // or we are not allowed to return null
                        break;
                    }

                    if (
                        _skipDuplicateEndTimes
                        && _lastEnumeratorEndTime.HasValue
                        && enumerator.Current != null
                        && enumerator.Current.EndTime <= _lastEnumeratorEndTime
                    )
                    {
                        continue;
                    }

                    Current = enumerator.Current;
                    return true;
                }

                _lastEnumeratorEndTime = Current?.EndTime;

                if (Log.DebuggingEnabled)
                {
                    Log.Debug(
                        $"ConcatEnumerator.MoveNext(): disposing enumerator at position: {_currentIndex} Name: {enumerator.GetType().Name}"
                    );
                }

                // we wont be using this enumerator again, dispose of it and clear reference
                enumerator.DisposeSafely();
                _enumerators[_currentIndex] = null;
            }

            Current = null;
            return false;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            throw new InvalidOperationException($"Can not reset {nameof(ConcatEnumerator)}");
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
        }
    }
}
