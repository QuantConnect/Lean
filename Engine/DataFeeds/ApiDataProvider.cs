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

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// An instance of the <see cref="IDataProvider"/> that will download and update data files as needed via QC's Api.
    /// </summary>
    public class ApiDataProvider : BaseDownloaderDataProvider
    {
        private decimal _purchaseLimit = Config.GetValue("data-purchase-limit", decimal.MaxValue); //QCC

        private readonly HashSet<SecurityType> _unsupportedSecurityType;
        private readonly DataPricesList _dataPrices;
        private readonly IApi _api;
        private readonly bool _subscribedToIndiaEquityMapAndFactorFiles;
        private readonly bool _subscribedToUsaEquityMapAndFactorFiles;
        private readonly bool _subscribedToFutureMapAndFactorFiles;
        private volatile bool _invalidSecurityTypeLog;

        /// <summary>
        /// Initialize a new instance of the <see cref="ApiDataProvider"/>
        /// </summary>
        public ApiDataProvider()
        {
            _unsupportedSecurityType = new HashSet<SecurityType> { SecurityType.Future, SecurityType.FutureOption, SecurityType.Index, SecurityType.IndexOption };

            _api = Composer.Instance.GetPart<IApi>();

            // If we have no value for organization get account preferred
            if (string.IsNullOrEmpty(Globals.OrganizationID))
            {
                var account = _api.ReadAccount();
                Globals.OrganizationID = account?.OrganizationId;
                Log.Trace($"ApiDataProvider(): Will use organization Id '{Globals.OrganizationID}'.");
            }

            // Read in data prices and organization details
            _dataPrices = _api.ReadDataPrices(Globals.OrganizationID);
            var organization = _api.ReadOrganization(Globals.OrganizationID);

            foreach (var productItem in organization.Products.Where(x => x.Type == ProductType.Data).SelectMany(product => product.Items))
            {
                if (productItem.Id == 37)
                {
                    // Determine if the user is subscribed to Equity map and factor files (Data product Id 37)
                    _subscribedToUsaEquityMapAndFactorFiles = true;
                }
                else if (productItem.Id == 137)
                {
                    // Determine if the user is subscribed to Future map and factor files (Data product Id 137)
                    _subscribedToFutureMapAndFactorFiles = true;
                }
                else if (productItem.Id == 172)
                {
                    // Determine if the user is subscribed to India map and factor files (Data product Id 172)
                    _subscribedToIndiaEquityMapAndFactorFiles = true;
                }
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
            return DownloadOnce(key, s =>
            {
                // Verify we have enough credit to handle this
                var pricePath = Api.Api.FormatPathForDataRequest(key);
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

                if (DownloadData(key))
                {
                    // Update our purchase limit.
                    _purchaseLimit -= price;
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
            // Ignore null
            if (filePath == null)
            {
                return false;
            }

            // Some security types can't be downloaded, lets attempt to extract that information
            if (LeanData.TryParseSecurityType(filePath, out SecurityType securityType, out var market) &&
                _unsupportedSecurityType.Contains(securityType) &&
                // We do support universe data for some security types (options and futures)
                !IsUniverseData(securityType, filePath))
            {
                // we do support future auxiliary data (map and factor files)
                if (securityType != SecurityType.Future || !IsAuxiliaryData(filePath))
                {
                    if (!_invalidSecurityTypeLog)
                    {
                        // let's log this once. Will still use any existing data on disk
                        _invalidSecurityTypeLog = true;
                        Log.Error($"ApiDataProvider(): does not support security types: {string.Join(", ", _unsupportedSecurityType)}");
                    }
                    return false;
                }
            }

            if (securityType == SecurityType.Equity && filePath.Contains("fine", StringComparison.InvariantCultureIgnoreCase) && filePath.Contains("fundamental", StringComparison.InvariantCultureIgnoreCase))
            {
                // Ignore fine fundamental data requests
                return false;
            }

            // Only download if it doesn't exist or is out of date.
            // Files are only "out of date" for non date based files (hour, daily, margins, etc.) because this data is stored all in one file
            var shouldDownload = !File.Exists(filePath) || filePath.IsOutOfDate();

            if (shouldDownload)
            {
                if (securityType == SecurityType.Future)
                {
                    if (!_subscribedToFutureMapAndFactorFiles)
                    {
                        throw new ArgumentException("ApiDataProvider(): Must be subscribed to map and factor files to use the ApiDataProvider " +
                            "to download Future auxiliary data from QuantConnect. " +
                            "Please visit https://www.quantconnect.com/datasets/quantconnect-us-futures-security-master for details.");
                    }
                }
                // Final check; If we want to download and the request requires equity data we need to be sure they are subscribed to map and factor files
                else if (!_subscribedToUsaEquityMapAndFactorFiles && market.Equals(Market.USA, StringComparison.InvariantCultureIgnoreCase)
                         && (securityType == SecurityType.Equity || securityType == SecurityType.Option || IsAuxiliaryData(filePath)))
                {
                    throw new ArgumentException("ApiDataProvider(): Must be subscribed to map and factor files to use the ApiDataProvider " +
                        "to download Equity data from QuantConnect. " +
                        "Please visit https://www.quantconnect.com/datasets/quantconnect-security-master for details.");
                }
                else if (!_subscribedToIndiaEquityMapAndFactorFiles && market.Equals(Market.India, StringComparison.InvariantCultureIgnoreCase)
                         && (securityType == SecurityType.Equity || securityType == SecurityType.Option || IsAuxiliaryData(filePath)))
                {
                    throw new ArgumentException("ApiDataProvider(): Must be subscribed to map and factor files to use the ApiDataProvider " +
                        "to download India data from QuantConnect. " +
                        "Please visit https://www.quantconnect.com/datasets/truedata-india-equity-security-master for details.");
                }
            }

            return shouldDownload;
        }

        /// <summary>
        /// Attempt to download data using the Api for and return a FileStream of that data.
        /// </summary>
        /// <param name="filePath">The path to store the file</param>
        /// <returns>A FileStream of the data</returns>
        protected virtual bool DownloadData(string filePath)
        {
            if (Log.DebuggingEnabled)
            {
                Log.Debug($"ApiDataProvider.Fetch(): Attempting to get data from QuantConnect.com's data library for {filePath}.");
            }

            if (_api.DownloadData(filePath, Globals.OrganizationID))
            {
                Log.Trace($"ApiDataProvider.Fetch(): Successfully retrieved data for {filePath}.");
                return true;
            }
            // Failed to download; _api.DownloadData() will post error
            return false;
        }

        /// <summary>
        /// Helper method to determine if this filepath is auxiliary data
        /// </summary>
        /// <param name="filepath">The target file path</param>
        /// <returns>True if this file is of auxiliary data</returns>
        private static bool IsAuxiliaryData(string filepath)
        {
            return filepath.Contains("map_files", StringComparison.InvariantCulture)
                || filepath.Contains("factor_files", StringComparison.InvariantCulture)
                || filepath.Contains("fundamental", StringComparison.InvariantCulture)
                || filepath.Contains("shortable", StringComparison.InvariantCulture);
        }

        /// <summary>
        /// Helper method to determine if this file path if for a universe file
        /// </summary>
        private static bool IsUniverseData(SecurityType securityType, string filepath)
        {
            return (securityType.IsOption() || securityType == SecurityType.Future) &&
                filepath.Contains("universes", StringComparison.InvariantCulture);
        }
    }
}
