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

using QuantConnect;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.USTreasuryYieldCurve
{
    public class USTreasuryYieldCurveDownloader
    {
        private readonly DirectoryInfo _destinationDirectory;
        private readonly RateGate _rateGate;
        private readonly int _retries = 5;

        public USTreasuryYieldCurveDownloader(string destinationDirectory)
        {
            _destinationDirectory = new DirectoryInfo(destinationDirectory);
            _destinationDirectory.Create();
            // Let's be gentle with government websites that might rely on legacy technology
            _rateGate = new RateGate(1, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Downloads all available yield curve data
        /// </summary>
        public void Download()
        {
            Log.Trace($"USTreasuryYieldCurveRateDownloader.Download(): Downloading yield curve data");

            var tempFilePath = new FileInfo(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp"));
            var finalPath = new FileInfo(Path.Combine(_destinationDirectory.FullName, $"yieldcurverates.xml"));

            for (var retry = 1; retry < _retries; retry++)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        _rateGate.WaitToProceed();

                        Log.Trace($"USTreasuryYieldCurveRateDownloader.Download(): Downloading yield curve data to: {tempFilePath.FullName}");
                        client.DownloadFile("http://data.treasury.gov/feed.svc/DailyTreasuryYieldCurveRateData", tempFilePath.FullName);

                        if (finalPath.Exists)
                        {
                            Log.Trace($"USTreasuryYieldCurveRateDownloader.Downlaod(): Deleting existing file: {finalPath.FullName}");
                            finalPath.Delete();
                        }

                        Log.Trace($"USTreasuryYieldCurveRateDownloader.Download(): Moving file from: {tempFilePath.FullName} - to: {finalPath.FullName}");
                        tempFilePath.MoveTo(finalPath.FullName);

                        Log.Trace("USTreasuryYieldCurveRateDownloader.Download(): Successfully downloaded yield curve data");
                        return;
                    }
                }
                catch (WebException e)
                {
                    var response = (HttpWebResponse) e.Response;
                    if (response == null)
                    {
                        Log.Error($"USTreasuryYieldCurveRateDownloader.Download(): Response was null. Retrying ({retry}/{_retries})");
                        continue;
                    }

                    Log.Error(e, $"USTreasuryYieldCurveRateDownloader.Download(): Web client error with status code {(int)response.StatusCode} - Retrying ({retry}/{_retries})");
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Unknown error occured. Retrying ({retry}/{_retries})");
                }
            }

            throw new Exception("Maximum retries exceeded");
        }
    }
}
