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

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Provides a base implementation for consolidators, including a built-in rolling window
    /// that stores the history of consolidated bars.
    /// </summary>
    public abstract class ConsolidatorBase : IEnumerable<IBaseData>
    {
        /// <summary>
        /// The default number of consolidated bars to keep in the rolling window history
        /// </summary>
        public static int DefaultWindowSize { get; } = 2;

        /// <summary>
        /// A rolling window keeping a history of the consolidated bars. The most recent bar is at index 0.
        /// </summary>
        public RollingWindow<IBaseData> Window { get; } = new RollingWindow<IBaseData>(DefaultWindowSize);

        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet.
        /// </summary>
        public IBaseData Consolidated { get; protected set; }

        /// <summary>
        /// Indexes the history window, where index 0 is the most recently consolidated bar.
        /// </summary>
        /// <param name="i">The index</param>
        /// <returns>The ith most recently consolidated bar</returns>
        public IBaseData this[int i] => Window[i];

        /// <summary>
        /// Returns an enumerator that iterates through the history window.
        /// </summary>
        public IEnumerator<IBaseData> GetEnumerator() => Window.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the history window.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Updates <see cref="Consolidated"/> and adds the bar to the rolling window.
        /// </summary>
        protected void UpdateConsolidated(IBaseData consolidated)
        {
            Consolidated = consolidated;
            Window.Add(consolidated);
        }

        /// <summary>
        /// Resets this consolidator, clearing consolidated data and the rolling window.
        /// </summary>
        public virtual void Reset()
        {
            Consolidated = null;
            Window.Reset();
        }
    }
}
