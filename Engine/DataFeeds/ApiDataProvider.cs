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
using System.Threading;
using QuantConnect.Api;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Configuration;
using System.Collections.Concurrent;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// An instance of the <see cref="IDataProvider"/> that will download and update data files as needed via QC's Api.
    /// </summary>
    public class ApiDataProvider : DefaultDataProvider
    {
        private readonly int _uid = Config.GetInt("job-user-id", 0);
        private readonly string _token = Config.Get("api-access-token", "1");
        private readonly string _organizationId = Config.Get("job-organization-id");
        private readonly string _dataPath = Config.Get("data-folder", "../../../Data/");
        private readonly ConcurrentDictionary<string, string> _currentDownloads;
        private readonly HashSet<SecurityType> _unsupportedSecurityType;
        private readonly bool _subscribedToEquityMapAndFactorFiles;
        private volatile bool _invalidSecurityTypeLog;
        private readonly DataPricesList _dataPrices;

        /// <summary>
        /// Data Purchase limit measured in QCC (QuantConnect Credits)
        /// </summary>
        private decimal _purchaseLimit = Config.GetValue("data-purchase-limit", decimal.MaxValue);

        /// <summary>
        /// Period to re-download 
        /// </summary>
        private static readonly int DownloadPeriod = Config.GetInt("api-data-update-period", 1);
        private readonly Api.Api _api;

        /// <summary>
        /// Initialize a new instance of the <see cref="ApiDataProvider"/>
        /// </summary>
        public ApiDataProvider()
        {
            _api = new Api.Api();
            _unsupportedSecurityType = new HashSet<SecurityType> { SecurityType.Future, SecurityType.FutureOption, SecurityType.Index, SecurityType.IndexOption };
            _currentDownloads = new ConcurrentDictionary<string, string>();
            _api.Initialize(_uid, _token, _dataPath);

            // If we have no value for organization get account preferred
            if (string.IsNullOrEmpty(_organizationId))
            {
                var account = _api.ReadAccount();
                _organizationId = account?.OrganizationId;
                Log.Trace($"ApiDataProvider(): Will use organization Id '{_organizationId}'.");
            }

            // Read in data prices and organization details
            _dataPrices = _api.ReadDataPrices(_organizationId);
            var organization = _api.ReadOrganization(_organizationId);

            // Determine if the user is subscribed to map and factor files
            if (organization.Products.Where(x => x.Type == ProductType.Data).Any(x => x.Items.Any(x => x.Name.Contains("Factor", StringComparison.InvariantCultureIgnoreCase))))
            {
                _subscribedToEquityMapAndFactorFiles = true;
            }

            // Verify user has agreed to data provider agreements
            if (organization.DataAgreement.Signed)
            {
                //Log Agreement Highlights
                Log.Trace("ApiDataProvider(): Data Terms of Use has been signed. \r\n" +
                    $" Find full agreement at: {_dataPrices.AgreementUrl} \r\n" +
                    "==========================================================================\r\n" +
                    $"CLI API Access Agreement: On {organization.DataAgreement.SignedTime:d} You Agreed:\r\n" +
                    " - Display or distribution of data obtained through CLI API Access is not permitted.  \r\n" +
                    " - Data and Third Party Data obtained via CLI API Access can only be used for individual or internal employee's use.\r\n" +
                    " - Data is provided in LEAN format can not be manipulated for transmission or use in other applications. \r\n" +
                    " - QuantConnect is not liable for the quality of data received and is not responsible for trading losses. \r\n" +
                    "==========================================================================");
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
            else
            {
                // Log URL to go accept terms
                throw new InvalidOperationException($"ApiDataProvider(): Must agree to terms at {_dataPrices.AgreementUrl}, before using the ApiDataProvider");
            }

            // Verify we have the balance to maintain our purchase limit, if not adjust it to meet our balance
            var balance = organization.Credit.Balance;
            if (balance < _purchaseLimit)
            {
                if (_purchaseLimit != decimal.MaxValue)
                {
                    Log.Error("ApiDataProvider(): Purchase limit is greater than balance." +
                        $" Setting purchase limit to balance : {balance}");
                }
                _purchaseLimit = balance;
            }
        }

        /// <summary>
        /// Retrieves data to be used in an algorithm.
        /// If file does not exist, an attempt is made to download them from the api
        /// </summary>
        /// <param name="key">File path representing where the data requested</param>
        /// <returns>A <see cref="Stream"/> of the data requested</returns>
        public override Stream Fetch(string key)
        {
            // If we don't already have this file or its out of date, download it
            if (NeedToDownload(key))
            {
                lock (key)
                {
                    // only one thread can add a path at the same time
                    // - The thread that adds the path, downloads the file and removes the path from the collection after it finishes.
                    // - Threads that don't add the path, will get the value in the collection and try taking a lock on it, since the downloading
                    // thread takes the lock on it first, they will wait till he finishes.
                    // This will allow different threads to download different paths at the same time.
                    if (_currentDownloads.TryAdd(key, key))
                    {
                        // Verify we have enough credit to handle this
                        var pricePath = _api.FormatPathForDataRequest(key);
                        var price = _dataPrices.GetPrice(pricePath);

                        // No price found
                        if (price == -1)
                        {
                            throw new ArgumentException($"ApiDataProvider.Fetch(): No price found for {pricePath}");
                        }

                        if (_purchaseLimit < price)
                        {
                            throw new ArgumentException($"ApiDataProvider.Fetch(): Cost {price} for {pricePath} data exceeds remaining purchase limit: {_purchaseLimit}");
                        }

                        var result = DownloadData(key);
                        if (result != null)
                        {
                            // Update our purchase limit.
                            _purchaseLimit -= price;
                        }
                        _currentDownloads.TryRemove(key, out _);
                        return result;
                    }

                    // this is rare
                    _currentDownloads.TryGetValue(key, out var existingKey);
                    lock (existingKey ?? new object())
                    {
                        return base.Fetch(key);
                    }
                }
            }

            return base.Fetch(key);
        }

        /// <summary>
        /// Determine if the file is out of date based on configuration and needs to be updated
        /// </summary>
        /// <param name="resolution">Data resolution</param>
        /// <param name="filepath">Path to the file</param>
        /// <returns>True if the file is out of date</returns>
        /// <remarks>Files are only "out of date" for Hourly/Daily data because this data is stored all in one file</remarks>
        public static bool IsOutOfDate(Resolution resolution, string filepath)
        {
            return resolution >= Resolution.Hour &&
                (DateTime.Now - TimeSpan.FromDays(DownloadPeriod)) > File.GetLastWriteTime(filepath);
        }

        /// <summary>
        /// Attempt to download data using the Api for and return a FileStream of that data.
        /// </summary>
        /// <param name="filePath">The path to store the file</param>
        /// <returns>A FileStream of the data</returns>
        protected virtual FileStream DownloadData(string filePath)
        {
            Log.Debug($"ApiDataProvider.Fetch(): Attempting to get data from QuantConnect.com's data library for {filePath}.");

            if (_api.DownloadData(filePath, _organizationId))
            {
                Log.Trace($"ApiDataProvider.Fetch(): Successfully retrieved data for {filePath}.");
                return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            // Failed to download; _api.DownloadData() will post error
            return null;
        }

        /// <summary>
        /// Helper method to determine if this file needs to be downloaded
        /// </summary>
        /// <param name="filePath">File we are looking at</param>
        /// <returns>True if should download</returns>
        private bool NeedToDownload(string filePath)
        {
            var fileExists = File.Exists(filePath);

            if (CanParsePath(filePath) && LeanData.TryParsePath(filePath, out SecurityType securityType, out Resolution resolution))
            {
                var shouldDownload = !fileExists || IsOutOfDate(resolution, filePath);

                if (_unsupportedSecurityType.Contains(securityType))
                {
                    if (!_invalidSecurityTypeLog)
                    {
                        // let's log this once. Will still use any existing data on disk
                        _invalidSecurityTypeLog = true;
                        Log.Error($"ApiDataProvider(): does not support security types: {string.Join(", ", _unsupportedSecurityType)}");
                    }
                    return false;
                }

                // If we need to download this, symbol is an equity, and user is not subscribed to map and factor files throw an error
                if (shouldDownload && (securityType == SecurityType.Equity || securityType == SecurityType.Option))
                {
                    CheckMapFactorFileSubscription();
                }

                return shouldDownload;
            }

            // For files that can't be parsed or fail to be
            return !fileExists || (DateTime.Now - TimeSpan.FromDays(DownloadPeriod)) > File.GetLastWriteTime(filePath);
        }

        /// <summary>
        /// Helper method to determine if we can parse this path with TryParsePath()
        /// Used to limit error throwing by calling TryParsePath()
        /// </summary>
        /// <param name="filepath">Filepath to check</param>
        /// <returns>True if can be parsed</returns>
        private bool CanParsePath(string filepath)
        {
            // Only not true for these cases
            var equitiesAuxData = filepath.Contains("map_files", StringComparison.InvariantCulture)
                || filepath.Contains("factor_files", StringComparison.InvariantCulture)
                || filepath.Contains("fundamental", StringComparison.InvariantCulture);

            if (equitiesAuxData)
            {
                CheckMapFactorFileSubscription();
            }

            return !equitiesAuxData;
        }

        private void CheckMapFactorFileSubscription()
        {
            if(!_subscribedToEquityMapAndFactorFiles)
            {
                throw new ArgumentException("ApiDataProvider(): Must be subscribed to map and factor files to use the ApiDataProvider" +
                    "to download Equity data from QuantConnect.");
            }
        }
    }
}
