using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Util;

namespace QuantConnect.Data.Custom.FRED
{
    public class Observation
    {
        [JsonProperty("realtime_start")]
        public string RealtimeStart { get; set; }

        [JsonProperty("realtime_end")]
        public string RealtimeEnd { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class FREDApi : BaseData
    {
        [JsonProperty("realtime_start")]
        public string RealtimeStart { get; set; }

        [JsonProperty("realtime_end")]
        public string RealtimeEnd { get; set; }

        [JsonProperty("observation_start")]
        public string ObservationStart { get; set; }

        [JsonProperty("observation_end")]
        public string ObservationEnd { get; set; }

        [JsonProperty("units")]
        public string Units { get; set; }

        [JsonProperty("output_type")]
        public int OutputType { get; set; }

        [JsonProperty("file_type")]
        public string FileType { get; set; }

        [JsonProperty("order_by")]
        public string OrderBy { get; set; }

        [JsonProperty("sort_order")]
        public string SortOrder { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("observations")]
        public IList<Observation> Observations { get; set; }

        /// <summary>
        /// Gets the FRED API token.
        /// </summary>
        public static string AuthCode { get; private set; } = string.Empty;

        /// <summary>
        /// Returns true if the FRED API token has been set.
        /// </summary>
        public static bool IsAuthCodeSet { get; private set; }

        /// <summary>
        /// Sets the EIA API token.
        /// </summary>
        /// <param name="authCode">The EIA API token</param>
        public static void SetAuthCode(string authCode)
        {
            if (string.IsNullOrWhiteSpace(authCode)) return;

            AuthCode = authCode;
            IsAuthCodeSet = true;
        }

        /// <summary>
        /// Return the URL string source of the file. This will be converted to a stream
        /// </summary>
        /// <param name="config">Configuration object</param>
        /// <param name="date">Date of this source file</param>
        /// <param name="isLiveMode">true if we're in live mode, false for backtesting mode</param>
        /// <returns>
        /// String URL of source file.
        /// </returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource(
                $"https://api.stlouisfed.org/fred/series/observations?file_type=json&observation_start=1998-01-01&api_key={AuthCode}&series_id={config.Symbol}",
                SubscriptionTransportMedium.Rest,
                FileFormat.Collection);
        }

        /// <summary>
        /// Readers the specified configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="content">The content.</param>
        /// <param name="date">The date.</param>
        /// <param name="isLiveMode">if set to <c>true</c> [is live mode].</param>
        /// <returns></returns>
        public override BaseData Reader(SubscriptionDataConfig config, string content, DateTime date, bool isLiveMode)
        {
            var series = JsonConvert.DeserializeObject<FREDApi>(content).Observations;
            var objectList = new List<FREDApi>();
            foreach (var observation in series)
            {
                decimal value;
                if (decimal.TryParse(observation.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                {
                    objectList.Add(new FREDApi
                    {
                        Symbol = config.Symbol,
                        Time = observation.Date - TimeSpan.FromDays(1),
                        EndTime = observation.Date,
                        Value = value
                    });
                }
            }
            return new BaseDataCollection(date, config.Symbol, objectList);
        }
    }
}
