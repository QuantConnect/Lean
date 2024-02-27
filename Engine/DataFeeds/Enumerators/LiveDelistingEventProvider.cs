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
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Delisting event provider implementation which will source the delisting date based on new map files
    /// </summary>
    public class LiveDelistingEventProvider : DelistingEventProvider
    {
        /// <summary>
        /// Check for delistings
        /// </summary>
        /// <param name="eventArgs">The new tradable day event arguments</param>
        /// <returns>New delisting event if any</returns>
        public override IEnumerable<BaseData> GetEvents(NewTradableDateEventArgs eventArgs)
        {
            var currentInstance = MapFile;
            // refresh map file instance
            InitializeMapFile();
            var newInstance = MapFile;

            if (currentInstance?.LastOrDefault()?.Date != newInstance?.LastOrDefault()?.Date)
            {
                Log.Trace($"LiveDelistingEventProvider({Config}): new tradable date {eventArgs.Date:yyyyMMdd}. " +
                    $"MapFile.LastDate Old: {currentInstance?.LastOrDefault()?.Date:yyyyMMdd} New: {newInstance?.LastOrDefault()?.Date:yyyyMMdd}");
            }

            return base.GetEvents(eventArgs);
        }
    }
}
