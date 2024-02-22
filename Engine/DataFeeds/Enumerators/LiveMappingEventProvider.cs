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
    /// <remarks>Only special behavior is that it will refresh map file on each new tradable date event</remarks>
    public class LiveMappingEventProvider : MappingEventProvider
    {
        public override IEnumerable<BaseData> GetEvents(NewTradableDateEventArgs eventArgs)
        {
            var currentInstance = MapFile;
            // refresh map file instance
            InitializeMapFile();
            var newInstance = MapFile;

            Log.Trace($"LiveMappingEventProvider({Config}): new tradable date {eventArgs.Date:yyyyMMdd}. " +
                $"New MapFile: {!ReferenceEquals(currentInstance, newInstance)}. " +
                $"MapFile.Count Old: {currentInstance?.Count()} New: {newInstance?.Count()}");

            return base.GetEvents(eventArgs);
        }
    }
}
