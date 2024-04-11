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
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;
using NodaTime;

namespace QuantConnect.Data
{
    /// <summary>
    /// Contains extension methods for the Downloader functionality.
    /// </summary>
    public static class DownloaderExtensions
    {
        /// <summary>
        /// Get <see cref="DataDownloaderGetParameters"/> for all mapped <seealso cref="Symbol"/> with appropriate ticker name in specific date time range.
        /// </summary>
        /// <param name="dataDownloaderParameter">Generated class in "Lean.Engine.DataFeeds.DownloaderDataProvider"</param>
        /// <param name="mapFileProvider">Provides instances of <see cref="MapFileResolver"/> at run time</param>
        /// <param name="exchangeTimeZone">Provides the time zone this exchange</param>
        /// <returns>
        /// Return DataDownloaderGetParameters with different
        /// <see cref="DataDownloaderGetParameters.StartUtc"/> - <seealso cref="DataDownloaderGetParameters.EndUtc"/> range
        /// and <seealso cref="Symbol"/>
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataDownloaderParameter"/> is null.</exception>
        public static IEnumerable<DataDownloaderGetParameters> GetDataDownloaderParameterForAllMappedSymbols(
            this DataDownloaderGetParameters dataDownloaderParameter,
            IMapFileProvider mapFileProvider,
            DateTimeZone exchangeTimeZone)
        {
            if (dataDownloaderParameter == null)
            {
                throw new ArgumentNullException(nameof(dataDownloaderParameter));
            }

            if (dataDownloaderParameter.Symbol.SecurityType != SecurityType.Future
                && dataDownloaderParameter.Symbol.RequiresMapping()
                && dataDownloaderParameter.Resolution >= Resolution.Hour)
            {
                var yieldMappedSymbol = default(bool);
                foreach (var symbolDateRange in mapFileProvider.RetrieveAllMappedSymbolInDateRange(dataDownloaderParameter.Symbol))
                {
                    var startDateTimeUtc = symbolDateRange.StartDateTimeLocal.ConvertToUtc(exchangeTimeZone);
                    var endDateTimeUtc = symbolDateRange.EndDateTimeLocal.ConvertToUtc(exchangeTimeZone);

                    // The first start date returns from mapFile like IPO (DateTime) and can not be greater then request StartTime
                    // The Downloader doesn't know start DateTime exactly, it always download all data, except for options and index options
                    if (dataDownloaderParameter.Symbol.SecurityType == SecurityType.Option ||
                        dataDownloaderParameter.Symbol.SecurityType == SecurityType.IndexOption)
                    {
                        // The symbol was delisted before the request start time
                        if (endDateTimeUtc < dataDownloaderParameter.StartUtc)
                        {
                            continue;
                        }

                        if (startDateTimeUtc < dataDownloaderParameter.StartUtc)
                        {
                            startDateTimeUtc = dataDownloaderParameter.StartUtc;
                        }
                    }

                    if (endDateTimeUtc > dataDownloaderParameter.EndUtc)
                    {
                        endDateTimeUtc = dataDownloaderParameter.EndUtc;
                    }

                    yield return new DataDownloaderGetParameters(
                        symbolDateRange.Symbol, dataDownloaderParameter.Resolution, startDateTimeUtc, endDateTimeUtc, dataDownloaderParameter.TickType);
                    yieldMappedSymbol = true;
                }

                if (!yieldMappedSymbol)
                {
                    yield return dataDownloaderParameter;
                }
            }
            else
            {
                yield return dataDownloaderParameter;
            }
        }
    }
}
