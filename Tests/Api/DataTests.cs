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
using NUnit.Framework;
using QuantConnect.Api;
using QuantConnect.Util;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// API Data endpoint tests
    /// </summary>
    [TestFixture, Explicit("Requires configured api access, and also makes calls to data endpoints which are charging transactions")]
    public class DataTests : ApiTestBase
    {
        private DataPricesList _pricesCache;
        private static object[] validForexDataTestCases =
        {
            new object[] { "EURUSD", Market.Oanda, new DateTime(2013,10,07), Resolution.Daily, TickType.Quote },
            new object[] { "EURUSD", Market.Oanda, new DateTime(2013,10,07), Resolution.Minute, TickType.Quote }
        };

        /// <summary>
        /// Test downloading data
        /// </summary>
        [TestCase("forex/oanda/minute/eurusd/20131011_quote.zip")]
        [TestCase("forex/oanda/daily/eurusd.zip")]
        public void DataDownloadedAndSaved(string fileToDownload)
        {
            var path = Path.Combine(DataFolder, fileToDownload);

            if (File.Exists(path))
                File.Delete(path);

            var downloadedData = ApiClient.DownloadData(path, TestOrganization);

            Assert.IsTrue(downloadedData);
            Assert.IsTrue(File.Exists(path));
        }

        /// <summary>
        /// Test attempting to fetch invalid data links
        /// </summary>
        [Test]
        public void InvalidDataLinks()
        {
            var fakePath = Path.Combine(DataFolder, "forex/oanda/minute/eurusd/19891011_quote.zip");
            var nonExistentData = ApiClient.DownloadData(fakePath, TestOrganization);

            Assert.IsFalse(nonExistentData);
            Assert.IsFalse(File.Exists(fakePath));
        }

        /// <summary>
        /// Test getting links to forex data for Oanda
        /// </summary>
        [TestCaseSource(nameof(validForexDataTestCases))]
        public void ValidForexDataLinks(string ticker, string market, DateTime date, Resolution resolution, TickType tickType)
        {
            var path = LeanData.GenerateRelativeZipFilePath(
                new Symbol(SecurityIdentifier.GenerateForex(ticker, market), ticker),
                date, resolution, tickType);
            var dataLink = ApiClient.ReadDataLink(path, TestOrganization);

            Assert.IsTrue(dataLink.Success);
            Assert.IsFalse(dataLink.Link.IsNullOrEmpty());
        }

        /// <summary>
        /// Test getting price for file
        /// </summary>
        /// <param name="filePath"></param>
        [TestCase("forex/oanda/daily/eurusd.zip")]
        [TestCase("crypto/coinbase/daily/btcusd_quote.zip")]
        public void GetPrices(string filePath)
        {
            if (_pricesCache == null)
            {
                _pricesCache = ApiClient.ReadDataPrices(TestOrganization);
            }

            // Make sure we actually have these prices for the test to work
            Assert.IsTrue(_pricesCache.Success);

            // Get the price
            int price = _pricesCache.GetPrice(filePath);
            Assert.AreNotEqual(price, -1);
        }

        /// <summary>
        /// Test regex implementation for DataPriceList price matching
        /// </summary>
        /// <param name="dataFile"></param>
        /// <param name="matchingRegex"></param>
        [TestCase("forex/oanda/daily/eurusd.zip", "/^(cfd|forex)\\/oanda\\/(daily|hour)\\/[^\\/]+.zip$/m")]
        [TestCase("forex/oanda/daily/eurusd.zip", "/^(cfd|forex)\\/oanda\\/(daily|hour)\\/[^\\/]+.zip$")]
        [TestCase("forex/oanda/daily/eurusd.zip", "^(cfd|forex)\\/oanda\\/(daily|hour)\\/[^\\/]+.zip$/")]
        [TestCase("forex/oanda/daily/eurusd.zip", "^(cfd|forex)\\/oanda\\/(daily|hour)\\/[^\\/]+.zip$")]
        public void DataPriceRegex(string dataFile, string matchingRegex)
        {
            var setPrice = 10;
            var dataList = new DataPricesList
            {
                Prices = new List<PriceEntry>() { new PriceEntry() { Price = setPrice, RawRegEx = matchingRegex } }
            };

            int price = dataList.GetPrice(dataFile);
            Assert.AreEqual(setPrice, price);
        }

        /// <summary>
        /// Test getting available data listings for directories
        /// </summary>
        /// <param name="directory"></param>
        [TestCase("alternative/sec/aapl/")]
        [TestCase("cfd/oanda/daily/")]
        [TestCase("crypto/coinbase/minute/btcusd/")]
        [TestCase("equity/usa/shortable/")]
        [TestCase("forex/oanda/minute/eurusd/")]
        [TestCase("forex\\oanda\\minute\\eurusd\\")] //Windows path case
        [TestCase("future/cbot/minute/zs")]
        [TestCase("futureoption/comex/minute/og")]
        [TestCase("index/usa/minute/spx")]
        [TestCase("indexoption/usa/minute/spx")]
        [TestCase("option/usa/minute/aapl")]
        public void GetDataListings(string directory)
        {
            var dataList = ApiClient.ReadDataDirectory(directory);
            Assert.IsTrue(dataList.Success);
            Assert.IsTrue(dataList.AvailableData.Count > 0);
        }
    }
}
