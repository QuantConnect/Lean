using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using RestSharp;

namespace QuantConnect.ToolBox.PolygonDownloader
{
    /// <summary>
    /// Polygon API data downloader class
    /// </summary>
    public class PolygonDataDownloader
    {
        // Readonly variables (initialized in constructor)
        private readonly string _apiKey;
        private readonly RestClient _restClient;
        private readonly MarketHoursDatabase _mhdb;

        // Polygon API constants
        private const string RestBaseUrl = "https://api.polygon.io";
        private const int MaxResponseSizeEquity = 50000;
        private const int MaxResponseSizeForex = 10000;
    

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonDataDownloader"/> class
        /// </summary>
        /// <param name="apiKey">Api key to authorize endpoint requests</param>
        public PolygonDataDownloader(string apiKey)
        {
            _apiKey = apiKey;
            _restClient = new RestClient(RestBaseUrl);

            // market hours database is in Globals.DataFolder
            _mhdb = MarketHoursDatabase.FromDataFolder();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // .NET 4.5 doesn't use TLS 1.2 by default we have to enable it explicitly:
            // https://stackoverflow.com/questions/49885842/c-sharp-web-request-w-restsharp-the-request-was-aborted-could-not-create-ssl
        }

        /// <summary>
        /// Get historical ticks enumerable for a sgingle equity symbol on a single day.
        /// </summary>
        /// <param name="symbol">Symbol for the data</param>
        /// <param name="timeUtc">Start time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> DownloadHistoricEquityTrades(Symbol symbol, DateTime timeUtc)
        {
            // we are trying to fetch as much ticks as possible per every request
            // offset is used to shift to the next page of results
            var ticksCount = MaxResponseSizeEquity;
            long offset = 0;

            // API returns equity ticks Unix timestamped,
            // we need to convert them to the time zone 
            // in which Lean expects to see them
            var dataTimeZone = _mhdb.GetDataTimeZone(symbol.ID.Market, symbol, symbol.SecurityType);

            // symbol must be converted to Upper otherwise API will return a null array of ticks
            var symbolToUpper = symbol.Value.LazyToUpper();
            var dateFormatted = timeUtc.ToString("yyyy-M-d", CultureInfo.InvariantCulture);

            // continue as long as we are getting the max number of ticks per response
            while (ticksCount == MaxResponseSizeEquity)
            {
                var endpoint =
                    $"v1/historic/trades/{symbolToUpper}/{dateFormatted}" +
                    $"?offset={offset}&limit={MaxResponseSizeEquity}&apiKey={_apiKey}";

                // execute request
                var request = new RestRequest(endpoint, Method.GET);
                var response = _restClient.Execute(request);

                // deserialize raw string to a texture object and get ticks
                var responseObject = JsonConvert.DeserializeObject<EquityHistoricTradesResponseTexture>(response.Content);
                var ticks = responseObject.Ticks;

                // break if current page contains no ticks - unlikely but possible scenario
                if (ticks == null) break;

                // register amount of ticks received
                ticksCount = ticks.Length;

                foreach (var tick in responseObject.Ticks)
                {
                    // timestamp offset, used for pagination. see polygon.io documentation.
                    offset = tick.T;
                    // convert time from utc to data time zone
                    var unixTime = Time.UnixMillisecondTimeStampToDateTime(tick.T);
                    var dataTime = unixTime.ConvertFromUtc(dataTimeZone);

                    yield return new Tick
                    {
                        // map id {Int32} to the tape string literal
                        Exchange = Mapping.IdToTape[tick.E],    
                        Value = tick.P,
                        Quantity = tick.S,
                        Time = dataTime

                        // TO-DO : learn more about conditions 
                        //SaleCondition = "" ??   

                    };
                }
            }
        }

        /// <summary>
        /// Get historical dividends and splits for the symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public IEnumerable<BaseData> DownloadStockDividendsAndSplits(Symbol symbol)
        {
            throw new NotImplementedException();
        }
    }
}
