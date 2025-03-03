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
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect;

/// <summary>
/// Represents the parameters required for downloading universe data.
/// </summary>
public sealed class DataUniverseDownloaderGetParameters : DataDownloaderGetParameters
{
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
    /// Gets the processing date for the data request.
    /// </summary>
    public DateTime ProcessingDate { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataUniverseDownloaderGetParameters"/> class.
    /// Ensures the provided symbol is canonical.
    /// </summary>
    /// <param name="canonicalSymbol">The canonical symbol for the data request.</param>
    /// <param name="processingDate">The date for which data is being processed.</param>
    /// <param name="tickType"></param>
    /// <exception cref="ArgumentException">Thrown when the provided symbol is not canonical.</exception>
    public DataUniverseDownloaderGetParameters(Symbol canonicalSymbol, DateTime processingDate, TickType tickType)
        : base(
            canonicalSymbol.IsCanonical() ? canonicalSymbol : throw new ArgumentException("DataUniverseDownloaderGetParameters: Symbol must be canonical.", nameof(canonicalSymbol)),
            Resolution.Daily,
            processingDate,
            processingDate.Date.AddDays(1).AddTicks(-1),
            tickType)
    {
        var marketHoursDatabaseLazy = MarketHoursDatabase.FromDataFolder();

        _securityExchangeHours = new(marketHoursDatabaseLazy.GetExchangeHours(canonicalSymbol.ID.Market, canonicalSymbol, canonicalSymbol.SecurityType));
        _dataTimeZone = new(marketHoursDatabaseLazy.GetDataTimeZone(canonicalSymbol.ID.Market, canonicalSymbol, canonicalSymbol.SecurityType));

        ProcessingDate = processingDate;
    }

    /// <summary>
    /// Creates history requests for the given symbol.
    /// </summary>
    /// <param name="symbol">The symbol for which to create history requests.</param>
    /// <returns>An enumerable collection of <see cref="HistoryRequest"/> objects.</returns>
    /// TODO: Virtual in base class instead of extension method ???
    public IEnumerable<HistoryRequest> CreateHistoryRequest(Symbol symbol)
    {
        // Validate that the symbol is either an option or a future contract
        if (!symbol.SecurityType.IsOption() && symbol.SecurityType != SecurityType.Future)
        {
            throw new NotSupportedException($"DataUniverseDownloaderGetParameters: The security type {symbol.SecurityType} is not supported for creating a historical universe request.");
        }

        foreach (var tickType in UniverseTickTypes)
        {
            yield return new HistoryRequest(
                StartUtc,
                EndUtc,
                LeanData.GetDataType(Resolution, tickType),
                symbol,
                Resolution,
                _securityExchangeHours.Value,
                _dataTimeZone.Value,
                Resolution,
                includeExtendedMarketHours: true,
                isCustomData: false,
                dataNormalizationMode: DataNormalizationMode.Raw,
                tickType);
        }
    }
}
