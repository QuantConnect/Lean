using System;
using Newtonsoft.Json;

namespace QuantConnect.OandaDownloader
{
    /// <summary>
    /// Represents the configuration settings for the application
    /// </summary>
    public class ConfigSettings
    {
        [JsonProperty("access-token")]
        public string AccessToken { get; set; }

        [JsonProperty("account-id")]
        public int AccountId { get; set; }

        [JsonProperty("output-folder")]
        public string OutputFolder { get; set; }

        [JsonProperty("instrument-list")]
        public string[] InstrumentList { get; set; }

        [JsonProperty("start-date")]
        public DateTime StartDate { get; set; }

        [JsonProperty("end-date")]
        public DateTime EndDate { get; set; }

        [JsonProperty("bars-per-request")]
        public int BarsPerRequest { get; set; }

        [JsonProperty("output-format")]
        public string OutputFormat { get; set; }

        [JsonProperty("enable-trace")]
        public bool EnableTrace { get; set; }
    }
}