using CsvHelper;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace QuantConnect.ToolBox.AlphaVantageDownloader
{
    /// <summary>
    /// Alpha Vantage data downloader
    /// </summary>
    public class AlphaVantageDataDownloader : IDataDownloader
    {
        private readonly IRestClient _avClient;

        /// <summary>
        /// Construct AlphaVantageDataDownloader with default RestClient
        /// </summary>
        /// <param name="apiKey">API key</param>
        public AlphaVantageDataDownloader(string apiKey) : this(new RestClient(), apiKey)
        {
        }

        /// <summary>
        /// Dependency injection constructor
        /// </summary>
        /// <param name="restClient">The <see cref="RestClient"/> to use</param>
        /// <param name="apiKey">API key</param>
        public AlphaVantageDataDownloader(IRestClient restClient, string apiKey)
        {
            _avClient = restClient;
            _avClient.BaseUrl = new Uri("https://www.alphavantage.co/");
            _avClient.Authenticator = new AlphaVantageAuthenticator(apiKey);
        }

        /// <inheritdoc/>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            var request = new RestRequest("query", DataFormat.Json);
            request.AddParameter("symbol", symbol.Value);
            request.AddParameter("datatype", "csv");

            IEnumerable<TimeSeries> data = null;
            switch (resolution)
            {
                case Resolution.Minute:
                case Resolution.Hour:
                    data = GetIntradayData(request, startUtc, endUtc, resolution);
                    break;
                case Resolution.Daily:
                    data = GetDailyData(request, startUtc, endUtc);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), $"{resolution} resolution not supported by API.");
            }

            var period = resolution.ToTimeSpan();
            return data.Select(d => new TradeBar(d.Time, symbol, d.Open, d.High, d.Low, d.Close, d.Volume, period));
        }

        /// <summary>
        /// Get data from daily API
        /// </summary>
        /// <param name="request">Base request</param>
        /// <param name="startUtc">Start time</param>
        /// <param name="endUtc">End time</param>
        /// <returns></returns>
        private IEnumerable<TimeSeries> GetDailyData(RestRequest request, DateTime startUtc, DateTime endUtc)
        {
            request.AddParameter("function", "TIME_SERIES_DAILY");
            // The default output only includes 100 trading days of data. If we want need more, specify full output
            if (GetBusinessDays(startUtc, endUtc) > 100)
                request.AddParameter("outputsize", "full");

            return GetTimeSeries(request);
        }

        /// <summary>
        /// Get data from intraday API
        /// </summary>
        /// <param name="request">Base request</param>
        /// <param name="startUtc">Start time</param>
        /// <param name="endUtc">End time</param>
        /// <param name="resolution">Data resolution to request</param>
        /// <returns></returns>
        private IEnumerable<TimeSeries> GetIntradayData(RestRequest request, DateTime startUtc, DateTime endUtc, Resolution resolution)
        {
            request.AddParameter("function", "TIME_SERIES_INTRADAY_EXTENDED");
            request.AddParameter("adjusted", "false");
            switch (resolution)
            {
                case Resolution.Minute:
                    request.AddParameter("interval", "1min");
                    break;
                case Resolution.Hour:
                    request.AddParameter("interval", "60min");
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"{resolution} resolution not supported by intraday API.");
            }

            var slices = GetSlices(startUtc, endUtc);
            foreach (var slice in slices)
            {
                request.AddOrUpdateParameter("slice", slice);
                var data = GetTimeSeries(request);
                foreach (var record in data)
                    yield return record;
            }
        }

        /// <summary>
        /// Execute request and parse response.
        /// </summary>
        /// <param name="request">The request</param>
        /// <returns><see cref="TimeSeries"/> data</returns>
        private IEnumerable<TimeSeries> GetTimeSeries(RestRequest request)
        {
            var response = _avClient.Get(request);

            using (var reader = new StringReader(response.Content))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                return csv.GetRecords<TimeSeries>()
                          .OrderBy(t => t.Time)
                          .ToList(); // Execute query before readers are disposed.
        }

        /// <summary>
        /// Get slice names for date range.
        /// See https://www.alphavantage.co/documentation/#intraday-extended
        /// </summary>
        /// <param name="startUtc">Start date</param>
        /// <param name="endUtc">End date</param>
        /// <returns>Slice names</returns>
        private IEnumerable<string> GetSlices(DateTime startUtc, DateTime endUtc)
        {
            if ((DateTime.UtcNow - startUtc).TotalDays > 365 * 2)
                throw new ArgumentOutOfRangeException("Intraday data is only available for the last 2 years.");

            var timeSpan = endUtc - startUtc;
            var months = (int)Math.Floor(timeSpan.TotalDays / 30);

            for (var i = months; i >= 0; i--)
            {
                var year = i / 12 + 1;
                var month = i % 12 + 1;
                yield return $"year{year}month{month}";
            }
        }

        /// <summary>
        /// From https://stackoverflow.com/questions/1617049/calculate-the-number-of-business-days-between-two-dates
        /// </summary>
        public static double GetBusinessDays(DateTime start, DateTime end)
        {
            double days = ((end - start).TotalDays * 5 - (start.DayOfWeek - end.DayOfWeek) * 2) / 7;

            if (end.DayOfWeek == DayOfWeek.Saturday) days--;
            if (start.DayOfWeek == DayOfWeek.Sunday) days--;

            return Math.Round(days);
        }
    }
}