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
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Provides an implementation of <see cref="IDataConsolidator"/> that preserve the input
    /// data unmodified. The input data is filtering by the specified predicate function
    /// </summary>
    /// <typeparam name="T">The type of data</typeparam>
    public class FilteredIdentityDataConsolidator<T> : IdentityDataConsolidator<T>
        where T : IBaseData
    {
        private readonly Func<T, bool> _predicate;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilteredIdentityDataConsolidator{T}"/> class
        /// </summary>
        /// <param name="predicate">The predicate function, returning true to accept data and false to reject data</param>
        public FilteredIdentityDataConsolidator(Func<T, bool> predicate)
        {
            this._predicate = predicate;
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public override void Update(T data)
        {
            // only permit data that passes our predicate function to be passed through
            if (_predicate(data))
            {
                base.Update(data);
            }
        }
    }

    /// <summary>
    /// Provides factory methods for creating instances of <see cref="FilteredIdentityDataConsolidator{T}"/>
    /// </summary>
    public static class FilteredIdentityDataConsolidator
    {
        /// <summary>
        /// Creates a new instance of <see cref="FilteredIdentityDataConsolidator{T}"/> that filters ticks
        /// based on the specified <see cref="TickType"/>
        /// </summary>
        /// <param name="tickType">The tick type of data to accept</param>
        /// <returns>A new <see cref="FilteredIdentityDataConsolidator{T}"/> that filters based on the provided tick type</returns>
        public static FilteredIdentityDataConsolidator<Tick> ForTickType(TickType tickType)
        {
            return new FilteredIdentityDataConsolidator<Tick>(tick => tick.TickType == tickType);
        }
    }
}