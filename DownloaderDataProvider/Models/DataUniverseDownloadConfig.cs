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

using QuantConnect.Configuration;
using QuantConnect.Securities.Option;
using QuantConnect.DownloaderDataProvider.Launcher.Models.Constants;
using QuantConnect.Data.UniverseSelection;
using static QuantConnect.Messages;

namespace QuantConnect.DownloaderDataProvider.Launcher.Models;

/// <summary>
/// Represents the configuration for downloading data for a universe of securities.
/// </summary>
public sealed class DataUniverseDownloadConfig : BaseDataDownloadConfig
{
    /// <summary>
    /// Supported security types for universe data download.
    /// </summary>
    private readonly HashSet<SecurityType> _supportsSecurityTypes = [SecurityType.Option, SecurityType.IndexOption];

    /// <summary>
    /// Gets the type of data universe download.
    /// </summary>
    public override Type DateType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataUniverseDownloadConfig"/> class using configuration settings.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when an unsupported security type is specified.</exception>
    public DataUniverseDownloadConfig()
    {
        if (!_supportsSecurityTypes.Contains(SecurityType))
        {
            throw new ArgumentException($"DataUniverseDownloadConfig: The specified SecurityType '{SecurityType}' is not supported. Supported types are: {string.Join(", ", _supportsSecurityTypes)}.");
        }

        Resolution = Resolution.Daily;
        Symbols = LoadSymbols(Config.GetValue<Dictionary<string, string>>(DownloaderCommandArguments.CommandTickers), SecurityType, MarketName);

        DateType = GetDataUniverseType(SecurityType);
    }

    /// <summary>
    /// Creates an enumerable collection of <see cref="DataUniverseDownloaderGetParameters"/> for each symbol.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="DataUniverseDownloaderGetParameters"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if no symbols are available for the data universe download.
    /// </exception>
    public IEnumerable<DataUniverseDownloaderGetParameters> CreateDataUniverseDownloaderGetParameters()
    {
        if (Symbols.Count == 0)
        {
            throw new ArgumentException("DataUniverseDownloadConfig.CreateDataUniverseDownloaderGetParameters(): No symbols provided for data universe download.");
        }

        foreach (var symbol in Symbols)
        {
            yield return new DataUniverseDownloaderGetParameters(symbol, StartDate, TickType.Trade);
        }
    }

    /// <summary>
    /// Loads symbols based on the provided tickers and security type.
    /// </summary>
    /// <param name="tickers">Dictionary of ticker symbols.</param>
    /// <param name="securityType">The security type of the data being downloaded.</param>
    /// <param name="market">The market in which the securities are traded.</param>
    /// <returns>A read-only collection of symbols.</returns>
    /// <exception cref="ArgumentException">Thrown if an unsupported security type is provided.</exception>
    protected override IReadOnlyCollection<Symbol> LoadSymbols(Dictionary<string, string> tickers, SecurityType securityType, string market)
    {
        if (tickers == null || tickers.Count == 0)
        {
            return [];
        }

        var symbols = new List<Symbol>();
        foreach (var ticker in tickers.Keys)
        {
            switch (SecurityType)
            {
                case SecurityType.Option:
                    symbols.Add(CreateCanonicalOption(ticker, market));
                    break;
                case SecurityType.IndexOption:
                    symbols.Add(CreateIndexCanonicalOption(ticker, market));
                    break;
                default:
                    throw new NotImplementedException($"DataUniverseDownloadConfig.LoadSymbols(): SecurityType '{securityType}' is not supported in LoadSymbols.");
            }
        }
        return symbols;
    }

    /// <summary>
    /// Creates a canonical option symbol for an underlying equity ticker.
    /// </summary>
    /// <param name="ticker">The ticker symbol of the underlying equity.</param>
    /// <param name="market">The market where the security is traded.</param>
    /// <returns>The canonical option symbol.</returns>
    private static Symbol CreateCanonicalOption(string ticker, string market)
    {
        var underlying = Symbol.Create(ticker, SecurityType.Equity, market);
        return Symbol.CreateCanonicalOption(underlying);
    }

    /// <summary>
    /// Creates a canonical index option symbol for an underlying index.
    /// </summary>
    /// <param name="ticker">The ticker symbol of the index option.</param>
    /// <param name="market">The market where the security is traded.</param>
    /// <returns>The canonical index option symbol.</returns>
    private static Symbol CreateIndexCanonicalOption(string ticker, string market)
    {
        var underlyingTicker = OptionSymbol.MapToUnderlying(ticker, SecurityType.Index);
        var underlyingIndex = Symbol.Create(underlyingTicker, SecurityType.Index, market);
        return Symbol.CreateCanonicalOption(underlyingIndex, ticker, market, null);
    }

    /// <summary>
    /// Retrieves the corresponding data universe type based on the specified security type.
    /// </summary>
    /// <param name="securityType">The security type for which the data universe type is determined.</param>
    /// <returns>The corresponding <see cref="Type"/> of the data universe.</returns>
    /// <exception cref="NotImplementedException">
    /// Thrown when the specified <paramref name="securityType"/> is not supported.
    /// </exception>
    private static Type GetDataUniverseType(SecurityType securityType)
    {
        switch (securityType)
        {
            case SecurityType.Option:
            case SecurityType.IndexOption:
                return typeof(OptionUniverse);
            default:
                throw new NotImplementedException($"DataUniverseDownloadConfig.GetDataUniverseType(): The data universe type for SecurityType '{securityType}' is not implemented.");
        }
    }
}
