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

using System.Globalization;
using QuantConnect.Logging;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.DownloaderDataProvider.Launcher.Models.Constants;

namespace QuantConnect.DownloaderDataProvider.Launcher.Models;

/// <summary>
/// Abstract base class for configuring data download parameters, including common properties and initialization logic.
/// </summary>
public abstract class BaseDataDownloadConfig
{
    /// <summary>
    /// Gets the start date for the data download.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets the end date for the data download.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the resolution of the downloaded data.
    /// </summary>
    public Resolution Resolution { get; protected set; }

    /// <summary>
    /// Gets or sets the market name for which the data will be downloaded.
    /// </summary>
    public string MarketName { get; protected set; }

    /// <summary>
    /// Gets the type of security for which the data is being downloaded.
    /// </summary>
    public SecurityType SecurityType { get; set; }

    /// <summary>
    /// Gets or sets the type of tick data to be downloaded.
    /// </summary>
    public TickType TickType { get; protected set; }

    /// <summary>
    /// The type of data based on <see cref="TickTypes"/>
    /// </summary>
    public abstract Type DataType { get; }

    /// <summary>
    /// Gets the list of symbols for which the data will be downloaded.
    /// </summary>
    public IReadOnlyCollection<Symbol> Symbols { get; protected set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDataDownloadConfig"/> class.
    /// </summary>
    protected BaseDataDownloadConfig()
    {
        StartDate = ParseDate(Config.Get(DownloaderCommandArguments.CommandStartDate).ToString());
        EndDate = ParseDate(Config.Get(DownloaderCommandArguments.CommandEndDate).ToString());

        SecurityType = ParseEnum<SecurityType>(Config.Get(DownloaderCommandArguments.CommandSecurityType).ToString());

        MarketName = Config.Get(DownloaderCommandArguments.CommandMarketName).ToString().ToLower(CultureInfo.InvariantCulture);

        if (string.IsNullOrEmpty(MarketName))
        {
            MarketName = DefaultBrokerageModel.DefaultMarketMap[SecurityType];
            Log.Trace($"{nameof(BaseDataDownloadConfig)}: Default market '{MarketName}' applied for SecurityType '{SecurityType}'");
        }

        if (!Market.SupportedMarkets().Contains(MarketName))
        {
            throw new ArgumentException($"The specified market '{MarketName}' is not supported. Supported markets are: {string.Join(", ", Market.SupportedMarkets())}.");
        }

        Symbols = LoadSymbols(Config.GetValue<Dictionary<string, string>>(DownloaderCommandArguments.CommandTickers), SecurityType, MarketName);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataDownloadConfig"/> class with the specified parameters.
    /// </summary>
    /// <param name="tickType">The type of tick data to be downloaded.</param>
    /// <param name="securityType">The type of security for which data is being downloaded.</param>
    /// <param name="resolution">The resolution of the data being downloaded.</param>
    /// <param name="startDate">The start date for the data download range.</param>
    /// <param name="endDate">The end date for the data download range.</param>
    /// <param name="marketName">The name of the market from which the data is being downloaded.</param>
    /// <param name="symbols">A list of symbols for which data is being downloaded.</param>
    protected BaseDataDownloadConfig(TickType tickType, SecurityType securityType, Resolution resolution, DateTime startDate, DateTime endDate, string marketName, List<Symbol> symbols)
    {
        StartDate = startDate;
        EndDate = endDate;
        Resolution = resolution;
        MarketName = marketName;
        SecurityType = securityType;
        TickType = tickType;
        Symbols = symbols;
    }

    /// <summary>
    /// Loads the symbols for which data will be downloaded.
    /// </summary>
    /// <param name="tickers">A dictionary of tickers to load symbols for.</param>
    /// <param name="securityType">The type of security to download data for.</param>
    /// <param name="market">The market for which the symbols are valid.</param>
    /// <returns>A collection of symbols for the specified market and security type.</returns>
    /// <summary>
    private static IReadOnlyCollection<Symbol> LoadSymbols(Dictionary<string, string> tickers, SecurityType securityType, string market)
    {
        if (tickers == null || tickers.Count == 0)
        {
            throw new ArgumentException($"{nameof(BaseDataDownloadConfig)}.{nameof(LoadSymbols)}: The tickers dictionary cannot be null or empty.");
        }

        return tickers.Keys.Select(ticker => Symbol.Create(ticker, securityType, market)).ToList();
    }

    /// <summary>
    /// Parses a string to a <see cref="DateTime"/> using a specific date format.
    /// </summary>
    /// <param name="date">The date string to parse.</param>
    /// <returns>The parsed <see cref="DateTime"/> value.</returns>
    protected static DateTime ParseDate(string date) => DateTime.ParseExact(date, DateFormat.EightCharacter, CultureInfo.InvariantCulture);


    /// <summary>
    /// Parses a string value into an enum of the specified type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to parse the value into.</typeparam>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The parsed enum value.</returns>
    /// <exception cref="ArgumentException">Thrown if the value cannot be parsed or is not a valid enum value.</exception>
    protected static TEnum ParseEnum<TEnum>(string value) where TEnum : struct, Enum
    {
        if (!Enum.TryParse(value, true, out TEnum result) || !Enum.IsDefined(result))
        {
            throw new ArgumentException($"Invalid {typeof(TEnum).Name} specified: '{value}'. Please provide a valid {typeof(TEnum).Name}. " +
                $"Valid values are: {string.Join(", ", Enum.GetNames<TEnum>())}.");
        }
        return result;
    }
}
