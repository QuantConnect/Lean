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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.GDAXDownloader
{
    /// <summary>
    /// GDAX Data Downloader class 
    /// </summary>
    public class GDAXDownloader : IDataDownloader
    {
        const int MaxDatapointsPerRequest = 200;
        const int MaxRequestsPerSecond = 2;
        const string HistoricCandlesUrl = "http://api.gdax.com/products/{0}/candles?start={1}&end={2}&granularity={3}";

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end times(in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Only Tick is currently supported</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            var returnData = new List<BaseData>();
            var granularity = resolution.ToTimeSpan().TotalSeconds;

            DateTime windowStartTime = startUtc;
            DateTime windowEndTime = startUtc;

            do
            {
                windowStartTime = windowEndTime;
                windowEndTime = windowStartTime.AddSeconds(MaxDatapointsPerRequest * granularity);
                windowEndTime = windowEndTime > endUtc ? endUtc : windowEndTime;

                Log.Trace(String.Format("Getting data for timeperiod from {0} to {1}..", windowStartTime, windowEndTime));

                var requestURL = string.Format(HistoricCandlesUrl, symbol.Value, windowStartTime.ToString(), windowEndTime.ToString(), granularity);
                var request = (HttpWebRequest)WebRequest.Create(requestURL);
                request.UserAgent = ".NET Framework Test Client";

                string data = GetWithRetry(request);
                returnData.AddRange(ParseCandleData(symbol, granularity, data));
            }
            while (windowStartTime != windowEndTime);
            
            return returnData;
        }

        /// <summary>
        /// Get request with retry on failure
        /// </summary>
        /// <param name="request">Web request to get.</param>
        /// <returns>web response as string</returns>
        string GetWithRetry(HttpWebRequest request)
        {
            string data = string.Empty;
            int retryCount = 0;
            while (data == string.Empty)
            {
                try
                {
                    Thread.Sleep(1000 / MaxRequestsPerSecond + 1);
                    var response = (HttpWebResponse)request.GetResponse();
                    var encoding = Encoding.ASCII;

                    using (var reader = new StreamReader(response.GetResponseStream(), encoding))
                    {
                        data = reader.ReadToEnd();
                    }
                }
                catch (WebException ex)
                {
                    ++retryCount;
                    if (retryCount > 3)
                    {
                        Log.Error("REQUEST FAILED: " + request.Address);
                        throw;
                    }
                    Log.Trace("WARNING: Web request failed with message " + ex.Message + "Retrying... " + retryCount + " times");
                }
            }
            return data;
        }

        /// <summary>
        /// Parse string response from web response
        /// </summary>
        /// <param name="Symbol">Crypto security symbol.</param>
        /// <param name="granularity">Resolution in seconds.</param>
        /// <param name="data">Web response as string.</param>
        /// <returns>web response as string</returns>
        List<BaseData> ParseCandleData(Symbol symbol, double granularity, string data)
        {
            List<BaseData> returnData = new List<BaseData>();
            if (data.Length > 0)
            {
                var parsedData = JsonConvert.DeserializeObject<string[][]>(data);

                foreach (var datapoint in parsedData)
                {
                    var epochs = double.Parse(datapoint[0].ToString());
                    var tradeBar = new TradeBar()
                    {
                        Time = Time.UnixTimeStampToDateTime(epochs),
                        Symbol = symbol,
                        Low = decimal.Parse(datapoint[1].ToString()),
                        High = decimal.Parse(datapoint[2].ToString()),
                        Open = decimal.Parse(datapoint[3].ToString()),
                        Close = decimal.Parse(datapoint[4].ToString()),
                        Volume = decimal.Parse(datapoint[5].ToString()),
                        Value = decimal.Parse(datapoint[4].ToString()),
                        DataType = MarketDataType.TradeBar,
                        Period = new TimeSpan(0, 0, (int)granularity),
                        EndTime = Time.UnixTimeStampToDateTime(epochs).AddSeconds(granularity)
                    };
                    returnData.Add(tradeBar);
                }
            }
            return returnData.OrderBy(datapoint => datapoint.Time).ToList();
        }
    }
}
