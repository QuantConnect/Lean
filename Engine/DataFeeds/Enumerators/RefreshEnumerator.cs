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
using System.IO;
using QuantConnect.Util;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Provides an implementation of <see cref="IEnumerator{T}"/> that will
    /// always return true via MoveNext.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RefreshEnumerator<T> : IEnumerator<T>
    {
        private T _current;
        private IEnumerator<T> _enumerator;
        private readonly Func<IEnumerator<T>> _enumeratorFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshEnumerator{T}"/> class
        /// </summary>
        /// <param name="enumeratorFactory">Enumerator factory used to regenerate the underlying
        /// enumerator when it ends</param>
        public RefreshEnumerator(Func<IEnumerator<T>> enumeratorFactory)
        {
            _enumeratorFactory = enumeratorFactory;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            if (_enumerator == null)
            {
                _enumerator = _enumeratorFactory.Invoke();
            }

            var moveNext = false;
            try
            {
                moveNext = _enumerator.MoveNext();
                _current = _enumerator.Current;
            }
            catch (IOException exception)
            {
                // we will ignore stale file handle exceptions and retry instead, enumerator will be refreshed
                if (exception.Message == null || !exception.Message.Contains("Stale file handle", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw;
                }
            }

            if (!moveNext)
            {
                _enumerator.DisposeSafely();

                _enumerator = null;
                _current = default(T);
            }

            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public void Reset()
        {
            if (_enumerator != null)
            {
                _enumerator.Reset();
            }
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public T Current
        {
            get { return _current; }
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (_enumerator != null)
            {
                _enumerator.Dispose();
            }
        }
    }
}
