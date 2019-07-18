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
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// An instance of the <see cref="IDataProvider"/> that will attempt to retrieve files not present on the filesystem from the API
    /// </summary>
    public class ApiDataProvider : IDataProvider
    {
        private readonly int _uid = Config.GetInt("job-user-id", 0);
        private readonly string _token = Config.Get("api-access-token", "1");
        private readonly string _dataPath = Config.Get("data-folder", "../../../Data/");
        private readonly Api.Api _api;

        /// <summary>
        /// Initialize a new instance of the <see cref="ApiDataProvider"/>
        /// </summary>
        public ApiDataProvider()
        {
            _api = new Api.Api();

            _api.Initialize(_uid, _token, _dataPath);
        }

        /// <summary>
        /// Retrieves data to be used in an algorithm.
        /// If file does not exist, an attempt is made to download them from the api
        /// </summary>
        /// <param name="key">A string representing where the data is stored</param>
        /// <returns>A <see cref="Stream"/> of the data requested</returns>
        public Stream Fetch(string key)
        {
            if (File.Exists(key))
            {
                return new FileStream(key, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            // If the file cannot be found on disc, attempt to retrieve it from the API
            Symbol symbol;
            DateTime date;
            Resolution resolution;

            if (LeanData.TryParsePath(key, out symbol, out date, out resolution))
            {
                Log.Trace("ApiDataProvider.Fetch(): Attempting to get data from QuantConnect.com's data library for symbol({0}), resolution({1}) and date({2}).",
                    symbol.Value,
                    resolution,
                    date.Date.ToShortDateString());

                var downloadSuccessful = _api.DownloadData(symbol, resolution, date);

                if (downloadSuccessful)
                {
                    Log.Trace("ApiDataProvider.Fetch(): Successfully retrieved data for symbol({0}), resolution({1}) and date({2}).",
                        symbol.Value,
                        resolution,
                        date.Date.ToShortDateString());

                    return new FileStream(key, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
            }

            Log.Error("ApiDataProvider.Fetch(): Unable to remotely retrieve data for path {0}. " +
                      "Please make sure you have the necessary data in your online QuantConnect data library.",
                       key);

            return null;
        }
    }
}
