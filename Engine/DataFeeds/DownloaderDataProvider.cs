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

using System;
using System.IO;
using System.Linq;
using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Data provider which downloads data using an <see cref="IDataDownloader"/> or <see cref="IBrokerage"/> implementation
    /// </summary>
    public class DownloaderDataProvider : BaseDownloaderDataProvider
    {
        private readonly IDataDownloader _dataDownloader;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public DownloaderDataProvider()
        {
            var dataDownloaderConfig = Config.Get("data-downloader");
            if (!string.IsNullOrEmpty(dataDownloaderConfig))
            {
                _dataDownloader = Composer.Instance.GetExportedValueByTypeName<IDataDownloader>(dataDownloaderConfig);
            }
            else
            {
                throw new ArgumentException("DownloaderDataProvider(): requires 'data-downloader' to be set with a valid type name");
            }
        }

        /// <summary>
        /// Determines if it should downloads new data and retrieves data from disc
        /// </summary>
        /// <param name="key">A string representing where the data is stored</param>
        /// <returns>A <see cref="Stream"/> of the data requested</returns>
        public override Stream Fetch(string key)
        {
            return DownloadOnce(key, s =>
            {
                if (LeanData.TryParsePath(key, out var symbol, out var date, out var resolution, out var tickType, out var dataType))
                {
                    var dataTimeZone = MarketHoursDatabase.FromDataFolder().GetDataTimeZone(symbol.ID.Market, symbol, symbol.SecurityType);

                    DateTime startTimeUtc;
                    DateTime endTimeUtc;
                    // we will download until yesterday so we are sure we don't get partial data
                    var endTimeUtcLimit = DateTime.UtcNow.Date.AddDays(-1);
                    if (resolution < Resolution.Hour)
                    {
                        // we can get the date from the path
                        startTimeUtc = date.ConvertToUtc(dataTimeZone);
                        // let's get the whole day
                        endTimeUtc = date.AddDays(1).ConvertToUtc(dataTimeZone);
                        if(endTimeUtc > endTimeUtcLimit)
                        {
                            // we are at the limit, avoid getting partial data
                            return;
                        }
                    }
                    else
                    {
                        // since hourly & daily are a single file we fetch the whole file
                        try
                        {
                            startTimeUtc = symbol.ID.Date;
                        }
                        catch (InvalidOperationException)
                        {
                            startTimeUtc = Time.BeginningOfTime;
                        }
                        endTimeUtc = endTimeUtcLimit;
                    }

                    // Save the data
                    var writer = new LeanDataWriter(resolution, symbol, Globals.DataFolder, tickType);
                    try
                    {
                        var getParams = new DataDownloaderGetParameters(symbol, resolution, startTimeUtc, endTimeUtc, tickType);

                        var data = _dataDownloader.Get(getParams)
                            .Where(baseData => symbol.SecurityType == SecurityType.Base || baseData.GetType() == dataType)
                            // for canonical symbols, downloader will return data for all of the chain
                            .GroupBy(baseData => baseData.Symbol);

                        foreach (var dataPerSymbol in data)
                        {
                            writer.Write(dataPerSymbol);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            });
        }

        /// <summary>
        /// Main filter to determine if this file needs to be downloaded
        /// </summary>
        /// <param name="filePath">File we are looking at</param>
        /// <returns>True if should download</returns>
        protected override bool NeedToDownload(string filePath)
        {
            // Ignore null and invalid data requests
            if (filePath == null
                || filePath.Contains("fine", StringComparison.InvariantCultureIgnoreCase) && filePath.Contains("fundamental", StringComparison.InvariantCultureIgnoreCase)
                || filePath.Contains("map_files", StringComparison.InvariantCultureIgnoreCase)
                || filePath.Contains("factor_files", StringComparison.InvariantCultureIgnoreCase)
                || filePath.Contains("margins", StringComparison.InvariantCultureIgnoreCase) && filePath.Contains("future", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            // Only download if it doesn't exist or is out of date.
            // Files are only "out of date" for non date based files (hour, daily, margins, etc.) because this data is stored all in one file
            return !File.Exists(filePath) || filePath.IsOutOfDate();
        }
    }
}
