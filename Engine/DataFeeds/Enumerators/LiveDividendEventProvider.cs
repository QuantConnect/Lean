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
using QuantConnect.Logging;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Event provider who will emit <see cref="SymbolChangedEvent"/> events
    /// </summary>
    /// <remarks>Only special behavior is that it will refresh factor file on each new tradable date event</remarks>
    public class LiveDividendEventProvider : DividendEventProvider
    {
        /// <summary>
        /// Check for dividends and returns them
        /// </summary>
        /// <param name="eventArgs">The new tradable day event arguments</param>
        /// <returns>New Dividend event if any</returns>
        public override IEnumerable<BaseData> GetEvents(NewTradableDateEventArgs eventArgs)
        {
            var currentInstance = FactorFile;
            // refresh factor file instance
            InitializeFactorFile();
            var newInstance = FactorFile;

            if (currentInstance?.Count() != newInstance?.Count())
            {
                Log.Trace($"LiveDividendEventProvider({Config}): new tradable date {eventArgs.Date:yyyyMMdd}. " +
                    $"New FactorFile: {!ReferenceEquals(currentInstance, newInstance)}. " +
                    $"FactorFile.Count Old: {currentInstance?.Count()} New: {newInstance?.Count()}");
            }

            return base.GetEvents(eventArgs);
        }
    }
}
