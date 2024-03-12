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
using System.Linq;
using QuantConnect.Interfaces;
using System.Collections.Generic;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Mapping extensions helper methods
    /// </summary>
    public static class MappingExtensions
    {
        /// <summary>
        /// Helper method to resolve the mapping file to use.
        /// </summary>
        /// <remarks>This method is aware of the data type being added for <see cref="SecurityType.Base"/>
        /// to the <see cref="SecurityIdentifier.Symbol"/> value</remarks>
        /// <param name="mapFileProvider">The map file provider</param>
        /// <param name="dataConfig">The configuration to fetch the map file for</param>
        /// <returns>The mapping file to use</returns>
        public static MapFile ResolveMapFile(this IMapFileProvider mapFileProvider, SubscriptionDataConfig dataConfig)
        {
            var resolver = MapFileResolver.Empty;
            if (dataConfig.TickerShouldBeMapped())
            {
                resolver = mapFileProvider.Get(AuxiliaryDataKey.Create(dataConfig.Symbol));
            }
            return resolver.ResolveMapFile(dataConfig.Symbol, dataConfig.Type.Name);
        }

        /// <summary>
        /// Helper method to resolve the mapping file to use.
        /// </summary>
        /// <remarks>This method is aware of the data type being added for <see cref="SecurityType.Base"/>
        /// to the <see cref="SecurityIdentifier.Symbol"/> value</remarks>
        /// <param name="mapFileResolver">The map file resolver</param>
        /// <param name="symbol">The symbol that we want to map</param>
        /// <param name="dataType">The string data type name if any</param>
        /// <returns>The mapping file to use</returns>
        public static MapFile ResolveMapFile(this MapFileResolver mapFileResolver,
            Symbol symbol,
            string dataType = null)
        {
            // Load the symbol and date to complete the mapFile checks in one statement
            var symbolID = symbol.HasUnderlying ? symbol.Underlying.ID.Symbol : symbol.ID.Symbol;
            if (dataType == null && symbol.SecurityType == SecurityType.Base)
            {
                SecurityIdentifier.TryGetCustomDataType(symbol.ID.Symbol, out dataType);
            }
            symbolID = symbol.SecurityType == SecurityType.Base && dataType != null ? symbolID.RemoveFromEnd($".{dataType}") : symbolID;

            MapFile result;
            if (ReferenceEquals(mapFileResolver, MapFileResolver.Empty))
            {
                result = mapFileResolver.ResolveMapFile(symbol.Value, Time.BeginningOfTime);
            }
            else
            {
                var date = symbol.HasUnderlying ? symbol.Underlying.ID.Date : symbol.ID.Date;
                result = mapFileResolver.ResolveMapFile(symbolID, date);
            }

            return result;
        }

        /// <summary>
        /// Some historical provider supports ancient data. In fact, the ticker could be restructured to new one.
        /// </summary>
        /// <param name="mapFileProvider">Provides instances of <see cref="MapFileResolver"/> at run time</param>
        /// <param name="symbol">Represents a unique security identifier</param>
        /// <param name="startDateTime">The date since we began our search for the historical name of the symbol.</param>
        /// <param name="endDateTime">The end date and time of the historical data range.</param>
        /// <returns>
        /// An enumerable collection of tuples containing symbol ticker, start date and time, and end date and time
        /// representing the historical definitions of the symbol within the specified time range.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="mapFileProvider"/> is null.</exception>
        /// <example>
        /// For instances, get "GOOGL" since 2013 to 2018:
        /// It returns: { ("GOOG", 2013, 2014), ("GOOGL", 2014, 2018) }
        /// </example>
        /// <remarks>
        /// GOOGLE: IPO: August 19, 2004 Name = GOOG then it was restructured: from "GOOG" to "GOOGL" on April 2, 2014
        /// </remarks>
        public static IEnumerable<TickerDateRange> RetrieveSymbolHistoricalDefinitionsInDateRange
            (this IMapFileProvider mapFileProvider, Symbol symbol, DateTime startDateTime, DateTime endDateTime)
        {
            if (mapFileProvider == null)
            {
                throw new ArgumentNullException(nameof(mapFileProvider));
            }

            var mapFileResolver = mapFileProvider.Get(AuxiliaryDataKey.Create(symbol));
            var symbolMapFile = mapFileResolver.ResolveMapFile(symbol);

            if (!symbolMapFile.Any())
            {
                yield break;
            }

            var newStartDateTime = startDateTime;
            foreach (var mappedTicker in symbolMapFile.Skip(1)) // Skip: IPO Ticker's DateTime 
            {
                if (mappedTicker.Date >= newStartDateTime)
                {
                    // Shifts endDateTime by one day to include all data up to and including the endDateTime.
                    var newEndDateTime = mappedTicker.Date.AddDays(1);
                    if (newEndDateTime > endDateTime)
                    {
                        yield return new(mappedTicker.MappedSymbol, newStartDateTime, endDateTime);
                        // the request EndDateTime was achieved
                        yield break;
                    }

                    yield return new(mappedTicker.MappedSymbol, newStartDateTime, newEndDateTime);
                    // the end of the current request is the start of the next
                    newStartDateTime = newEndDateTime;
                }
            }
        }

        /// <summary>
        /// Retrieves all ticker data download parameters from map files for a specific symbol and ticker.
        /// </summary>
        /// <param name="mapFileProvider">The provider for map files containing ticker data.</param>
        /// <param name="symbol">The symbol to get <see cref="MapFileResolver"/> and generate new Symbol.</param>
        /// <param name="resolution">The resolution of the data to be downloaded.</param>
        /// <param name="startDateTime">The start date and time for the data download.</param>
        /// <param name="endDateTime">The end date and time for the data download.</param>
        /// <param name="tickType">The tick type of the data to be downloaded.</param>
        /// <returns>An enumerable collection of DataDownloaderGetParameters objects representing the parameters for downloading ticker data.</returns>
        /// <exception cref="ArgumentException">Throw if <paramref name="mapFileProvider"/> is null.</exception>
        public static IEnumerable<DataDownloaderGetParameters> GetAllTickerFromMapFiles(
            this IMapFileProvider mapFileProvider,
            Symbol symbol,
            Resolution resolution,
            DateTime startDateTime,
            DateTime endDateTime,
            TickType tickType)
        {
            if (mapFileProvider == null)
            {
                throw new ArgumentException("The map file provider cannot be null.", nameof(mapFileProvider));
            }

            var tickerUpperCase = symbol?.Value.ToUpperInvariant();
            // Sometime Symbol is started from special sign.
            if (!Char.IsLetter(symbol.Value[0]))
            {
                tickerUpperCase = tickerUpperCase[1..];
            }

            var mapFileResolver = mapFileProvider.Get(AuxiliaryDataKey.Create(symbol));

            var yieldStatus = default(bool);
            foreach (var mapFile in mapFileResolver)
            {
                // Check if 'mapFile' contains the desired ticker symbol.
                if (!mapFile.Any(mapFileRow => mapFileRow.MappedSymbol == tickerUpperCase))
                {
                    continue;
                }

                // Exclude: When resolution is less then hour, we downloaded with using specific start/end DateTime
                if (startDateTime > mapFile.DelistingDate && Resolution.Hour > resolution)
                {
                    continue;
                }

                var sid = SecurityIdentifier.GenerateEquity(mapFile.FirstDate, mapFile.Permtick, symbol?.ID.Market);

                var newEndDateTimeUtc = endDateTime;
                foreach (var tickerDateRange in mapFile.GetTickerDateRanges(tickerUpperCase))
                {
                    // Exclude: requested startDateTimeUtc is not passed mapFile's Date Range 
                    if (resolution < Resolution.Hour && !(tickerDateRange.StartDate <= startDateTime && startDateTime < tickerDateRange.EndDate))
                    {
                        continue;
                    }

                    // We have downloaded all range with date from MapFile
                    if (resolution >= Resolution.Hour)
                    {
                        startDateTime = tickerDateRange.StartDate;
                        newEndDateTimeUtc = tickerDateRange.EndDate > endDateTime ? endDateTime : tickerDateRange.EndDate;
                    }

                    yield return new DataDownloaderGetParameters(new Symbol(sid, tickerUpperCase), resolution, startDateTime, newEndDateTimeUtc, tickType);
                    yieldStatus = true;
                }
            }

            if (!yieldStatus)
            {
                yield return new DataDownloaderGetParameters(symbol, resolution, startDateTime, endDateTime, tickType);
            }
        }

        /// <summary>
        /// Retrieves the date ranges associated with a specific ticker symbol from the provided map file.
        /// </summary>
        /// <param name="mapFile">The map file containing the data ranges for various ticker.</param>
        /// <param name="ticker">The ticker for which to retrieve the date ranges.</param>
        /// <returns>An enumerable collection of tuples representing the start and end dates for each date range associated with the specified ticker symbol.</returns>
        private static IEnumerable<(DateTime StartDate, DateTime EndDate)> GetTickerDateRanges(this MapFile mapFile, string ticker)
        {
            var mapFileRows = mapFile.ToList();
            for (var i = 0; i < mapFileRows.Count - 1; i++)
            {
                if (ticker != mapFileRows[i + 1].MappedSymbol)
                {
                    continue;
                }
                // Shifts endDateTime by one day to include all data up to and including the endDateTime.
                yield return (mapFileRows[i].Date, mapFileRows[i + 1].Date.AddDays(1));
            }
        }
    }
}
