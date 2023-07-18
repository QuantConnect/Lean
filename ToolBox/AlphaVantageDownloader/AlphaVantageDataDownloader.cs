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

using CsvHelper;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Util;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using QuantConnect.Securities;

namespace QuantConnect.ToolBox.AlphaVantageDownloader
{
    /// <summary>
    /// Alpha Vantage data downloader
    /// </summary>
    public class AlphaVantageDataDownloader : IDataDownloader, IDisposable
    {
        private readonly MarketHoursDatabase _marketHoursDatabase;
        private readonly IRestClient _avClient;
        private readonly RateGate _rateGate;
        private bool _disposed;

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
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            _avClient.BaseUrl = new Uri("https://www.alphavantage.co/");
            _avClient.Authenticator = new AlphaVantageAuthenticator(apiKey);

            _rateGate = new RateGate(5, TimeSpan.FromMinutes(1)); // Free API is limited to 5 requests/minute
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="dataDownloaderGetParameters">model class for passing in parameters for historical data</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
        {
            var symbol = dataDownloaderGetParameters.Symbol;
            var resolution = dataDownloaderGetParameters.Resolution;
            var startUtc = dataDownloaderGetParameters.StartUtc;
            var endUtc = dataDownloaderGetParameters.EndUtc;
            var tickType = dataDownloaderGetParameters.TickType;

            if (tickType != TickType.Trade)
            {
                return Enumerable.Empty<BaseData>();
            }

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
                    data = GetDailyData(request, startUtc, endUtc, symbol);
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
        /// <param name="symbol">Symbol to download</param>
        /// <returns></returns>
        private IEnumerable<TimeSeries> GetDailyData(RestRequest request, DateTime startUtc, DateTime endUtc, Symbol symbol)
        {
            request.AddParameter("function", "TIME_SERIES_DAILY");

            // The default output only includes 100 trading days of data. If we want need more, specify full output
            if (GetBusinessDays(startUtc, endUtc, symbol) > 100)
            {
                request.AddParameter("outputsize", "full");
            }

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
                {
                    yield return record;
                }
            }
        }

        /// <summary>
        /// Execute request and parse response.
        /// </summary>
        /// <param name="request">The request</param>
        /// <returns><see cref="TimeSeries"/> data</returns>
        private IEnumerable<TimeSeries> GetTimeSeries(RestRequest request)
        {
            if (_rateGate.IsRateLimited)
            {
                Log.Trace("Requests are limited to 5 per minute. Reduce the time between start and end times or simply wait, and this process will continue automatically.");
            }

            _rateGate.WaitToProceed();
            //var url = _avClient.BuildUri(request);
            Log.Trace("Downloading /{0}?{1}", request.Resource, string.Join("&", request.Parameters));
            var response = _avClient.Get(request);

            if (response.ContentType != "application/x-download")
            {
                throw new FormatException($"Unexpected content received from API.\n{response.Content}");
            }

            using var reader = new StringReader(response.Content);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var result = csv.GetRecords<TimeSeries>()
                      .OrderBy(t => t.Time)
                      .ToList(); // Execute query before readers are disposed.
            return result;
        }

        /// <summary>
        /// Get slice names for date range.
        /// See https://www.alphavantage.co/documentation/#intraday-extended
        /// </summary>
        /// <param name="startUtc">Start date</param>
        /// <param name="endUtc">End date</param>
        /// <returns>Slice names</returns>
        private static IEnumerable<string> GetSlices(DateTime startUtc, DateTime endUtc)
        {
            if ((DateTime.UtcNow - startUtc).TotalDays > 365 * 2)
            {
                throw new ArgumentOutOfRangeException(nameof(startUtc), "Intraday data is only available for the last 2 years.");
            }

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
        private int GetBusinessDays(DateTime start, DateTime end, Symbol symbol)
        {
            var exchangeHours = _marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);

            var current = start.Date;
            var days = 0;
            while (current < end)
            {
                if (exchangeHours.IsDateOpen(current))
                {
                    days++;
                }
                current = current.AddDays(1);
            }

            return days;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    _rateGate.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
