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
using NodaTime;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect;

/// <summary>
/// Represents the parameters required for downloading universe data.
/// </summary>
public sealed class DataUniverseDownloaderGetParameters : DataDownloaderGetParameters
{
    /// <summary>
    /// The market hours database instance to use
    /// </summary>
    private readonly MarketHoursDatabase _marketHoursDatabase;

    /// <summary>
    /// The tick types supported for universe data.
    /// </summary>
    private readonly TickType[] UniverseTickTypes = { TickType.Trade, TickType.OpenInterest };

    /// <summary>
    /// Lazy-initialized instance of the security exchange hours.
    /// </summary>
    private readonly Lazy<SecurityExchangeHours> _securityExchangeHours;

    /// <summary>
    /// Lazy-initialized instance of the data time zone.
    /// </summary>
    private readonly Lazy<DateTimeZone> _dataTimeZone;

    /// <summary>
    /// Gets the underlying symbol associated with the universe.
    /// </summary>
    public Symbol UnderlyingSymbol { get => Symbol.Underlying; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataUniverseDownloaderGetParameters"/> class.
    /// </summary>
    /// <param name="canonicalSymbol">The canonical symbol for the data request.</param>
    /// <param name="startDate">The start date for the data request.</param>
    /// <param name="endDate">The end date for the data request.</param>
    /// <exception cref="ArgumentException">Thrown when the provided symbol is not canonical.</exception>
    public DataUniverseDownloaderGetParameters(Symbol canonicalSymbol, DateTime startDate, DateTime endDate)
        : base(
            canonicalSymbol.IsCanonical() ? canonicalSymbol : throw new ArgumentException("DataUniverseDownloaderGetParameters: Symbol must be canonical.", nameof(canonicalSymbol)),
            Resolution.Daily,
            startDate,
            endDate)
    {
        _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
        _securityExchangeHours = new(_marketHoursDatabase.GetExchangeHours(canonicalSymbol.ID.Market, canonicalSymbol, canonicalSymbol.SecurityType));
        _dataTimeZone = new(_marketHoursDatabase.GetDataTimeZone(canonicalSymbol.ID.Market, canonicalSymbol, canonicalSymbol.SecurityType));

        EndUtc = EndUtc.ConvertToUtc(_securityExchangeHours.Value.TimeZone);
        StartUtc = StartUtc.ConvertToUtc(_securityExchangeHours.Value.TimeZone);
    }

    /// <summary>
    /// Gets the file name where the universe data will be saved.
    /// </summary>
    /// <param name="processingDate">The date for which the file name is generated.</param>
    /// <returns>The universe file name.</returns>
    public string GetUniverseFileName(DateTime processingDate)
    {
        return OptionUniverse.GetUniverseFileName(Symbol, processingDate, createDirectory: true);
    }

    /// <summary>
    /// Creates data download parameters for each day in the range.
    /// </summary>
    public IEnumerable<(DateTime, IEnumerable<DataDownloaderGetParameters>)> CreateDataDownloaderGetParameters()
    {
        foreach (var processingDate in Time.EachTradeableDay(_securityExchangeHours.Value, StartUtc, EndUtc))
        {
            var processingDateUtc = processingDate.ConvertToUtc(_securityExchangeHours.Value.TimeZone);

            var requests = new List<DataDownloaderGetParameters>(3)
            {
                new(UnderlyingSymbol, Resolution, processingDateUtc, processingDateUtc.AddDays(1), TickType.Trade)
            };

            requests.AddRange(UniverseTickTypes.Select(tickType => new DataDownloaderGetParameters(Symbol, Resolution, processingDateUtc, processingDateUtc.AddDays(1), tickType)));

            yield return (processingDate, requests);
        }
    }
}
