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
using QuantConnect.Lean.DownloaderDataProvider.Models.Constants;

namespace QuantConnect.Lean.DownloaderDataProvider
{
    /// <summary>
    /// Represents the configuration for data download.
    /// </summary>
    public struct DataDownloadConfig
    {
        /// <summary>
        /// The tick type as a string.
        /// </summary>
        private string _tickType;

        /// <summary>
        /// The security type as a string.
        /// </summary>
        private string _securityType;

        /// <summary>
        /// The resolution as a string.
        /// </summary>
        private string _resolution;

        /// <summary>
        /// The start date as a string.
        /// </summary>
        private string _startDate;

        /// <summary>
        /// The end date as a string.
        /// </summary>
        private string _endDate;

        /// <summary>
        /// Full name of the data provider for downloading data.
        /// </summary>
        public string DataProviderFullName { get; }

        /// <summary>
        /// Type of tick data to download.
        /// </summary>
        public TickType TickType { get => ParseEnum<TickType>(_tickType); }

        /// <summary>
        /// Type of security for which data is to be downloaded.
        /// </summary>
        public SecurityType SecurityType { get => ParseEnum<SecurityType>(_securityType); }

        /// <summary>
        /// Resolution of the downloaded data.
        /// </summary>
        public Resolution Resolution { get => ParseEnum<Resolution>(_resolution); }

        /// <summary>
        /// Start date for the data download.
        /// </summary>
        public DateTime StartDate { get => DateTime.ParseExact(_startDate, DateFormat.EightCharacter, CultureInfo.InvariantCulture); }

        /// <summary>
        /// End date for the data download.
        /// </summary>
        public DateTime EndDate { get => DateTime.ParseExact(_endDate, DateFormat.EightCharacter, CultureInfo.InvariantCulture); }

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
            DataProviderFullName = Config.Get(DownloaderCommandArguments.CommandDownloaderDataDownloader).ToString() ?? string.Empty;

            _tickType = Config.Get(DownloaderCommandArguments.CommandDataType).ToString() ?? string.Empty;
            _securityType = Config.Get(DownloaderCommandArguments.CommandSecurityType).ToString() ?? string.Empty;
            _resolution = Config.Get(DownloaderCommandArguments.CommandResolution).ToString() ?? string.Empty;

            _startDate = Config.Get(DownloaderCommandArguments.CommandStartDate).ToString() ?? string.Empty;
            _endDate = Config.Get(DownloaderCommandArguments.CommandEndDate).ToString() ?? string.Empty;

            MarketName = Config.Get(DownloaderCommandArguments.CommandMarketName).ToString()?.ToLower() ?? Market.USA;

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
        /// Returns a string representation of the <see cref="DataDownloadConfig"/> struct.
        /// </summary>
        /// <returns>A string representation of the <see cref="DataDownloadConfig"/> struct.</returns>
        public override string ToString()
        {
            return $"DataProviderFullName: {DataProviderFullName}, " +
                   $"TickType: {TickType}, " +
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
        private TEnum ParseEnum<TEnum>(string value) where TEnum : struct, Enum
        {
            if (!Enum.TryParse(value, true, out TEnum result) || !Enum.IsDefined(typeof(TEnum), result))
            {
                throw new ArgumentException($"Invalid {typeof(TEnum).Name} specified. Please provide a valid {typeof(TEnum).Name}.");
            }

            return result;
        }
    }
}
