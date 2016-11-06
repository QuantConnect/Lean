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
using QuantConnect.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Provides an implementation of <see cref="IDerivativeSecurityFilter"/> for use in selecting
    /// futures contracts based on a range of expiries
    /// </summary>
    public class ExpiryFutureFilter : IDerivativeSecurityFilter
    {
        private readonly TimeSpan _minExpiry;
        private readonly TimeSpan _maxExpiry;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpiryFutureFilter"/> class
        /// </summary>
        /// <param name="minExpiry">The minimum time until expiry, for example, 7 days would filter out contracts expiring sooner than 7 days</param>
        /// <param name="maxExpiry">The maximum time until expiry, for example, 30 days would filter out contracts expriring later than 30 days</param>
        public ExpiryFutureFilter(TimeSpan minExpiry, TimeSpan maxExpiry)
        {
            _minExpiry = minExpiry;
            _maxExpiry = maxExpiry;

            // prevent parameter mistakes that would prevent all contracts from coming through
            if (maxExpiry < minExpiry) throw new ArgumentException("maxExpiry must be greater than minExpiry");

            // protect from overflow on additions
            if (_maxExpiry > Time.MaxTimeSpan) _maxExpiry = Time.MaxTimeSpan;
        }

        /// <summary>
        /// Filters the input set of symbols 
        /// </summary>
        /// <param name="symbols">The derivative symbols to be filtered</param>
        /// <returns>The filtered set of symbols</returns>
        public IEnumerable<Symbol> Filter(IEnumerable<Symbol> symbols, BaseData underlying)
        {
            // we can't properly apply this filter without knowing the underlying time
            // so in the event we're missing the underlying, just skip the filtering step
            if (underlying == null)
            {
                return symbols;
            }
            // compute the bounds, no need to worry about rounding and such
            var minExpiry = underlying.Time.Date + _minExpiry;
            var maxExpiry = underlying.Time.Date + _maxExpiry;

            // ReSharper disable once PossibleMultipleEnumeration - ReSharper is wrong here due to the ToList call
            return from symbol in symbols
                   let contract = symbol.ID
                   where contract.Date >= minExpiry
                      && contract.Date <= maxExpiry
                   select symbol;
        }
    }
}