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
using System.IO;
using NUnit.Framework;
using QuantConnect.Util;

namespace QuantConnect.Tests.API
{
    /// <summary>
    /// API Data endpoint tests
    /// </summary>
    [TestFixture]
    class DataTests : ApiTestBase
    {
        /// <summary>
        /// Test downloading data that does not come with the repo (Oanda)
        /// Requires that your account has this data; its free at quantconnect.com/data
        /// </summary>
        [Test]
        public void BacktestingData_CanBeDownloadedAndSaved_Successfully()
        {
            //TODO NOT WORKING EMPTY RESPONSE
            var minutePath = Path.Combine(DataFolder, "forex/oanda/minute/eurusd/20131011_quote.zip");
            var dailyPath = Path.Combine(DataFolder, "forex/oanda/daily/eurusd.zip");

            if (File.Exists(dailyPath))
                File.Delete(dailyPath);

            if (File.Exists(minutePath))
                File.Delete(minutePath);

            var downloadedMinuteData = ApiClient.DownloadData(minutePath, TestOrganization);
            var downloadedDailyData = ApiClient.DownloadData(dailyPath, TestOrganization);

            Assert.IsTrue(downloadedMinuteData);
            Assert.IsTrue(downloadedDailyData);

            Assert.IsTrue(File.Exists(dailyPath));
            Assert.IsTrue(File.Exists(minutePath));
        }

        /// <summary>
        /// Test downloading non existent data
        /// </summary>
        [Test]
        public void NonExistantData_WillBeDownloaded_Unsuccessfully()
        {
            var fakePath = Path.Combine(DataFolder, "forex/oanda/minute/eurusd/19891011_quote.zip");
            var nonExistentData = ApiClient.DownloadData(fakePath, TestOrganization);

            Assert.IsFalse(nonExistentData);
            Assert.IsFalse(Directory.Exists(fakePath));
        }

        /// <summary>
        /// Test getting links to forex data for FXCM
        /// </summary>
        [Test]
        public void FXCMDataLinks_CanBeRetrieved_Successfully()
        {
            var minutePath = LeanData.GenerateRelativeZipFilePath(
                new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM), "EURUSD"),
                new DateTime(2013, 10, 07),
                Resolution.Minute, TickType.Quote);
            var minuteDataLink = ApiClient.ReadDataLink(minutePath, TestOrganization);

            var dailyPath = LeanData.GenerateRelativeZipFilePath(
                new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM), "EURUSD"),
                new DateTime(2013, 10, 07),
                Resolution.Daily, TickType.Quote);
            var dailyDataLink = ApiClient.ReadDataLink(dailyPath, TestOrganization);

            Assert.IsTrue(minuteDataLink.Success);
            Assert.IsTrue(dailyDataLink.Success);
        }

        /// <summary>
        /// Test getting links to forex data for Oanda
        /// </summary>
        [Test]
        public void OandaDataLinks_CanBeRetrieved_Successfully()
        {
            var minutePath = LeanData.GenerateRelativeZipFilePath(
                new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.Oanda), "EURUSD"),
                new DateTime(2013, 10, 07),
                Resolution.Minute, TickType.Quote);
            var minuteDataLink = ApiClient.ReadDataLink(minutePath, TestOrganization);

            var dailyPath = LeanData.GenerateRelativeZipFilePath(
                new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.Oanda), "EURUSD"),
                new DateTime(2013, 10, 07),
                Resolution.Daily, TickType.Quote);
            var dailyDataLink = ApiClient.ReadDataLink(dailyPath, TestOrganization);

            Assert.IsTrue(minuteDataLink.Success);
            Assert.IsTrue(dailyDataLink.Success);
        }

        [Test]
        [TestCase("forex/oanda/minute/eurusd/19891011_quote.zip")]
        public void GetPrices(string filePath)
        {
            //TODO Broken, Regex issues
            var prices = ApiClient.ReadDataPrices(TestOrganization);
            int price = prices.GetPrice(filePath);

            Assert.IsNotNull(price);
        }

        [Test]
        public void GetDataListings()
        {
            //TODO NOT WORKING EMPTY RESPONSE
            var DataList = ApiClient.ReadDataDirectory("forex/oanda/minute/");

            Console.WriteLine("DONE");
        }
    }
}
