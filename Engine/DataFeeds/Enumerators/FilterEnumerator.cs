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

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Enumerator that allow applying a filtering function
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FilterEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private readonly Func<T, bool> _filter;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="enumerator">The underlying enumerator to filter on</param>
        /// <param name="filter">The filter to apply</param>
        public FilterEnumerator(IEnumerator<T> enumerator, Func<T, bool> filter)
        {
            _enumerator = enumerator;
            _filter = filter;
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Disposes the FilterEnumerator
        /// </summary>
        public void Dispose()
        {
            _enumerator.Dispose();
        }

        #endregion

        #region Implementation of IEnumerator

        /// <summary>
        /// Moves the FilterEnumerator to the next item
        /// </summary>
        public bool MoveNext()
        {
            // run the enumerator until it passes the specified filter
            while (_enumerator.MoveNext())
            {
                if (_filter(_enumerator.Current))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Resets the FilterEnumerator
        /// </summary>
        public void Reset()
        {
            _enumerator.Reset();
        }

        /// <summary>
        /// Gets the current item in the FilterEnumerator
        /// </summary>
        public T Current
        {
            get { return _enumerator.Current; }
        }

        object IEnumerator.Current
        {
            get { return _enumerator.Current; }
        }

        #endregion
    }
}
