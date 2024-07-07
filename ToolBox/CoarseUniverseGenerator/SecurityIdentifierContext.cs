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
using System.Linq;
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.ToolBox.CoarseUniverseGenerator
{
    /// <summary>
    /// Auxiliary class for handling map files and SID.
    /// </summary>
    internal class SecurityIdentifierContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityIdentifierContext"/> class.
        /// </summary>
        /// <param name="mapFile">The map file.</param>
        /// <param name="market">The market.</param>
        public SecurityIdentifierContext(MapFile mapFile, string market)
        {
            MapFile = mapFile;
            SID = SecurityIdentifier.GenerateEquity(MapFile.FirstDate, MapFile.FirstTicker, market);
            MapFileRows = MapFile
                .Select(mfr => new Tuple<DateTime, string>(mfr.Date, mfr.MappedSymbol))
                .ToArray();
            Tickers = MapFile
                .Select(mfr => mfr.MappedSymbol.ToLowerInvariant())
                .Distinct()
                .ToArray();
            LastTicker = MapFile.Last().MappedSymbol.ToLowerInvariant();
        }

        /// <summary>
        /// Gets the sid.
        /// </summary>
        /// <value>
        /// The sid.
        /// </value>
        public SecurityIdentifier SID { get; }

        /// <summary>
        /// Gets the map file.
        /// </summary>
        /// <value>
        /// The map file.
        /// </value>
        public MapFile MapFile { get; }

        /// <summary>
        /// Gets the map file rows.
        /// </summary>
        /// <value>
        /// The map file rows.
        /// </value>
        public Tuple<DateTime, string>[] MapFileRows { get; }

        /// <summary>
        /// Gets the tickers.
        /// </summary>
        /// <value>
        /// The tickers.
        /// </value>
        public string[] Tickers { get; }

        /// <summary>
        /// Gets the last ticker.
        /// </summary>
        /// <value>
        /// The last ticker.
        /// </value>
        public string LastTicker { get; }
    }
}
