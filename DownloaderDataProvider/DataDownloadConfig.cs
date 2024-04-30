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
using QuantConnect.Configuration;
using QuantConnect.DownloaderDataProvider.Launcher.Models.Constants;

namespace QuantConnect.DownloaderDataProvider.Launcher
{
    /// <summary>
    /// Represents the configuration for data download.
    /// </summary>
    public struct DataDownloadConfig
    {
        /// <summary>
        /// Type of tick data to download.
        /// </summary>
        public TickType TickType { get; }

        /// <summary>
        /// Type of security for which data is to be downloaded.
        /// </summary>
        public SecurityType SecurityType { get; }

        /// <summary>
        /// Resolution of the downloaded data.
        /// </summary>
        public Resolution Resolution { get; }

        /// <summary>
        /// Start date for the data download.
        /// </summary>
        public DateTime StartDate { get; }

        /// <summary>
        /// End date for the data download.
        /// </summary>
        public DateTime EndDate { get; }

        /// <summary>
        /// Market name for which the data is to be downloaded.
        /// </summary>
        public string MarketName { get; }

        /// <summary>
        /// List of symbols for which data is to be downloaded.
        /// </summary>
        public List<Symbol> Symbols { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDownloadConfig"/> struct.
        /// </summary>
        /// <param name="parameters">Dictionary containing the parameters for data download.</param>
        public DataDownloadConfig()
        {
            TickType = ParseEnum<TickType>(Config.Get(DownloaderCommandArguments.CommandDataType).ToString());
            SecurityType = ParseEnum<SecurityType>(Config.Get(DownloaderCommandArguments.CommandSecurityType).ToString());
            Resolution = ParseEnum<Resolution>(Config.Get(DownloaderCommandArguments.CommandResolution).ToString());

            StartDate = DateTime.ParseExact(Config.Get(DownloaderCommandArguments.CommandStartDate).ToString(), DateFormat.EightCharacter, CultureInfo.InvariantCulture);
            EndDate = DateTime.ParseExact(Config.Get(DownloaderCommandArguments.CommandEndDate).ToString(), DateFormat.EightCharacter, CultureInfo.InvariantCulture);

#pragma warning disable CA1308 // class Market keeps all name in lowercase
            MarketName = Config.Get(DownloaderCommandArguments.CommandMarketName).ToString()?.ToLower(CultureInfo.InvariantCulture) ?? Market.USA;
#pragma warning restore CA1308
            if (!Market.SupportedMarkets().Contains(MarketName))
            {
                throw new ArgumentException($"The specified market '{MarketName}' is not supported. Supported markets are: {string.Join(", ", Market.SupportedMarkets())}.");
            }

            foreach (var ticker in (Config.GetValue<Dictionary<string, string>>(DownloaderCommandArguments.CommandTickers))!.Keys)
            {
                Symbols.Add(Symbol.Create(ticker, SecurityType, MarketName));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDownloadConfig"/> class with the specified parameters.
        /// </summary>
        /// <param name="tickType">The type of tick data to be downloaded.</param>
        /// <param name="securityType">The type of security for which data is being downloaded.</param>
        /// <param name="resolution">The resolution of the data being downloaded.</param>
        /// <param name="startDate">The start date for the data download range.</param>
        /// <param name="endDate">The end date for the data download range.</param>
        /// <param name="market">The name of the market from which the data is being downloaded.</param>
        /// <param name="symbols">A list of symbols for which data is being downloaded.</param>
        public DataDownloadConfig(TickType tickType, SecurityType securityType, Resolution resolution, DateTime startDate, DateTime endDate, string market, List<Symbol> symbols)
        {
            TickType = tickType;
            SecurityType = securityType;
            Resolution = resolution;
            StartDate = startDate;
            EndDate = endDate;
            MarketName = market;
            Symbols = symbols;
        }

        /// <summary>
        /// Returns a string representation of the <see cref="DataDownloadConfig"/> struct.
        /// </summary>
        /// <returns>A string representation of the <see cref="DataDownloadConfig"/> struct.</returns>
        public override string ToString()
        {
            return $"TickType: {TickType}, " +
                   $"SecurityType: {SecurityType}, " +
                   $"Resolution: {Resolution}, " +
                   $"StartDate: {StartDate:yyyyMMdd}, " +
                   $"EndDate: {EndDate:yyyyMMdd}, " +
                   $"MarketName: {MarketName}, " +
                   $"Symbols: {string.Join(", ", Symbols.Select(s => s.ToString()))}";
        }

        /// <summary>
        /// Parses a string value to an enum of type <typeparamref name="TEnum"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to parse to.</typeparam>
        /// <param name="value">The string value to parse.</param>
        /// <returns>The parsed enum value.</returns>
        private static TEnum ParseEnum<TEnum>(string value) where TEnum : struct, Enum
        {
            if (!Enum.TryParse(value, true, out TEnum result) || !Enum.IsDefined(typeof(TEnum), result))
            {
                throw new ArgumentException($"Invalid {typeof(TEnum).Name} specified. Please provide a valid {typeof(TEnum).Name}.");
            }

            return result;
        }
    }
}
