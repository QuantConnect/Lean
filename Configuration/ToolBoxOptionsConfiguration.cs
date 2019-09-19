using System;
using Newtonsoft.Json;

namespace QuantConnect.Configuration
{
    /// <summary>
    /// This object is a container for toolbox options configuration
    /// </summary>
    [Serializable]
    class ToolBoxOptionsConfiguration
    {
        /// <summary>
        /// Target tool.
        /// </summary>
        [JsonProperty("app")]
        public string Application { get; set; }

        /// <summary>
        /// Tickers
        /// </summary>
        [JsonProperty("tickers")]
        public string[] Tickers { get; set; }

        /// <summary>
        /// Resolution
        /// </summary>
        [JsonProperty("resolution")]
        public string Resolution { get; set; }

        /// <summary>
        /// Start date
        /// </summary>
        [JsonProperty("from-date")]
        public string FromDate { get; set; }

        /// <summary>
        /// End date
        /// </summary>
        [JsonProperty("to-date")]
        public string ToData { get; set; }

        /// <summary>
        /// The exchange to process. If not defined, all exchanges will be processed.
        /// </summary>
        [JsonProperty("exchange")]
        public string Exchange { get; set; }

        /// <summary>
        /// Api key required for some downloaders
        /// </summary>
        [JsonProperty("api-key")]
        public string ApiKey { get; set; }

        /// <summary>
        /// Date for the option bz files, required for some downloaders.
        /// </summary>
        [JsonProperty("date")]
        public string Date { get; set; }

        /// <summary>
        /// Source directory required for converters
        /// </summary>
        [JsonProperty("source-dir")]
        public string SourceDir { get; set; }

        /// <summary>
        /// Destination directory required for converters
        /// </summary>
        [JsonProperty("destination-dir")]
        public string DestinationDir { get; set; }

        /// <summary>
        /// REQUIRED for IVolatilityEquityConverter. Meta source directory 
        /// </summary>
        [JsonProperty("source-meta-dir")]
        public string SourceMetaDir { get; set; }

        /// <summary>
        /// REQUIRED for RandomDataGenerator. Start date 
        /// </summary>
        [JsonProperty("start")]
        public string Start { get; set; }

        /// <summary>
        /// REQUIRED for RandomDataGenerator. End date 
        /// </summary>
        [JsonProperty("end")]
        public string End { get; set; }

        /// <summary>
        /// REQUIRED for RandomDataGenerator. Number of symbols to generate data for.
        /// </summary>
        [JsonProperty("symbol-count")]
        public string SymbolCount { get; set; }

        /// <summary>
        /// OPTIONAL for RandomDataGenerator. Market of generated symbols. Defaults to default market for security type.
        /// </summary>
        [JsonProperty("market")]
        public string Market { get; set; }

        /// <summary>
        /// OPTIONAL for RandomDataGenerator. Security type of generated symbols, defaults to Equity.
        /// </summary>
        [JsonProperty("security-type")]
        public string SecurityType { get; set; }

        /// <summary>
        /// OPTIONAL for RandomDataGenerator. Defaults to Dense.
        /// </summary>
        [JsonProperty("data-density")]
        public string DataDensity { get; set; }

        /// <summary>
        /// OPTIONAL for RandomDataGenerator. Only used for Equity, defaults to true
        /// </summary>
        [JsonProperty("include-coarse")]
        public string IncludeCoarse { get; set; }

        /// <summary>
        /// OPTIONAL for RandomDataGenerator. Sets the ratio of generated quotes to generated trades.
        /// </summary>
        [JsonProperty("quote-trade-ratio")]
        public string QuoteTradeRatio { get; set; }

        /// <summary>
        /// OPTIONAL for RandomDataGenerator. Sets the random number generator seed. Defaults to null (random seed).
        /// </summary>
        [JsonProperty("random-seed")]
        public string RandomSeed { get; set; }

        /// <summary>
        /// OPTIONAL for RandomDataGenerator. Sets the probability each equity generated will have an IPO event.
        /// </summary>
        [JsonProperty("ipo-percentage")]
        public string IpoPercentage { get; set; }

        /// <summary>
        /// OPTIONAL for RandomDataGenerator. Sets the probability each equity generated will have a rename event.
        /// </summary>
        [JsonProperty("rename-percentage")]
        public string RenamePercentage { get; set; }

        /// <summary>
        /// OPTIONAL for RandomDataGenerator. Sets the probability each equity generated will have a stock split event. 
        /// </summary>
        [JsonProperty("splits-percentage")]
        public string SplitsPercentage { get; set; }

        /// <summary>
        /// OPTIONAL for RandomDataGenerator. Sets the probability each equity generated will have dividends. 
        /// </summary>
        [JsonProperty("dividends-percentage")]
        public string DividendsPercentage { get; set; }

        /// <summary>
        /// OPTIONAL for RandomDataGenerator. Sets the probability each equity generated will have a dividend event every quarter.
        /// </summary>
        [JsonProperty("dividend-every-quarter-percentage")]
        public string DividendEveryQuarterPercentage { get; set; }

        /// <summary>
        /// OPTIONAL for PsychSignalDataDownloader. This is the kind of data you want to get from PsychSignal's API.  
        /// </summary>
        [JsonProperty("data-source")]
        public string DataSource { get; set; }
    }
}
