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

using System.Collections;
using System.Collections.Generic;
using QuantConnect.Indicators;

namespace QuantConnect
{
    /// <summary>
    /// Provides a base class for types that maintain a rolling window history of values.
    /// This is the single source of truth for window logic shared between indicators and consolidators.
    /// </summary>
    /// <typeparam name="T">The type of value stored in the rolling window</typeparam>
    public abstract class WindowBase<T> : IEnumerable<T>
    {
        private RollingWindow<T> _window;

        /// <summary>
        /// The default number of values to keep in the rolling window history
        /// </summary>
        public static int DefaultWindowSize { get; } = 2;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowBase{T}"/> class.
        /// </summary>
        protected WindowBase() { }

        /// <summary>
        /// Initializes the rolling window with the given size.
        /// </summary>
        protected WindowBase(int windowSize)
        {
            _window = new RollingWindow<T>(windowSize);
        }

        /// <summary>
        /// A rolling window keeping a history of values. The most recent value is at index 0.
        /// Uses lazy initialization to survive Python subclasses that do not call base constructors.
        /// </summary>
        public RollingWindow<T> Window => _window ??= new RollingWindow<T>(DefaultWindowSize);

        /// <summary>
        /// Gets the most recent value. The protected setter adds the value to the rolling window.
        /// </summary>
        public virtual T Current
        {
            get
            {
                return Window[0];
            }
            protected set
            {
                Window.Add(value);
            }
        }

        /// <summary>
        /// Gets the previous value, or default if fewer than two values have been produced.
        /// </summary>
        public virtual T Previous => Window.Count > 1 ? Window[1] : default;

        /// <summary>
        /// Indexes the history window, where index 0 is the most recent value.
        /// </summary>
        /// <param name="i">The index</param>
        /// <returns>The ith most recent value</returns>
        public T this[int i] => Window[i];

        /// <summary>
        /// Returns an enumerator that iterates through the history window.
        /// </summary>
        public IEnumerator<T> GetEnumerator() => Window.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the history window.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Resets the rolling window, clearing all stored values without allocating a new window
        /// if it has not yet been created.
        /// </summary>
        protected void ResetWindow()
        {
            _window?.Reset();
        }
    }
}
