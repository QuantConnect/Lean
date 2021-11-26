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
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Event provider who will emit <see cref="SymbolChangedEvent"/> events
    /// </summary>
    public class MappingEventProvider : ITradableDateEventProvider
    {
        private IMapFileProvider _mapFileProvider;
        private SubscriptionDataConfig _config;
        private MapFile _mapFile;

        /// <summary>
        /// Initializes this instance
        /// </summary>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="factorFileProvider">The factor file provider to use</param>
        /// <param name="mapFileProvider">The <see cref="Data.Auxiliary.MapFile"/> provider to use</param>
        /// <param name="startTime">Start date for the data request</param>
        public virtual void Initialize(
            SubscriptionDataConfig config,
            IFactorFileProvider factorFileProvider,
            IMapFileProvider mapFileProvider,
            DateTime startTime)
        {
            _mapFileProvider = mapFileProvider;
            _config = config;
            InitializeMapFile();

            if (_mapFile.HasData(startTime.Date))
            {
                // initialize mapped symbol using request start date
                _config.MappedSymbol = _mapFile.GetMappedSymbol(startTime.Date, _config.MappedSymbol);
            }
        }

        /// <summary>
        /// Check for new mappings
        /// </summary>
        /// <param name="eventArgs">The new tradable day event arguments</param>
        /// <returns>New mapping event if any</returns>
        public virtual IEnumerable<BaseData> GetEvents(NewTradableDateEventArgs eventArgs)
        {
            if (_config.Symbol == eventArgs.Symbol
                && _mapFile.HasData(eventArgs.Date))
            {
                var old = _config.MappedSymbol;
                var newSymbol = _mapFile.GetMappedSymbol(eventArgs.Date, _config.MappedSymbol);
                _config.MappedSymbol = newSymbol;

                // check to see if the symbol was remapped
                if (old != _config.MappedSymbol)
                {
                    var changed = new SymbolChangedEvent(
                        _config.Symbol,
                        eventArgs.Date,
                        old,
                        _config.MappedSymbol);
                    yield return changed;
                }
            }
        }

        /// <summary>
        /// Initializes the map file to use
        /// </summary>
        protected void InitializeMapFile()
        {
            _mapFile = _mapFileProvider.ResolveMapFile(_config);
        }
    }
}
