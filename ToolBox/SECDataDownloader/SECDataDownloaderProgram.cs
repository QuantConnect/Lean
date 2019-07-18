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
using QuantConnect.Data.Custom.SEC;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.SECDataDownloader
{
    public static class SECDataDownloaderProgram
    {
        /// <summary>
        /// Downloads the raw SEC data
        /// </summary>
        /// <param name="rawDestination">Destination where raw data will be written to</param>
        /// <param name="start">Start date</param>
        /// <param name="end">End date</param>
        public static void SECDataDownloader(string rawDestination, DateTime start, DateTime end)
        {
            var download = new SECDataDownloader();
            Log.Trace("SecDataDownloaderProgram.SecDataDownloader(): Begin downloading raw files from SEC website...");
            download.Download(rawDestination, start, end);
        }
        
        /// <summary>
        /// Converts the downloaded raw SEC data archives
        /// </summary>
        /// <param name="rawSource">Source of the raw data</param>
        /// <param name="destination">Destination to write processed data to</param>
        /// <param name="date">Date to process data for</param>
        public static void SECDataConverter(string rawSource, string destination, DateTime date)
        {
            var converter = new SECDataConverter(rawSource, destination);
            Log.Trace("SecDataDownloaderProgram.SecDataDownloader(): Begin parsing raw files from disk...");
            converter.Process(date);
        }
    }
}
