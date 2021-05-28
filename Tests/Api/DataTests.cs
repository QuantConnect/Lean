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
            var minutePath = Path.Combine(DataFolder, "forex/oanda/minute/eurusd/20131011_quote.zip");
            var dailyPath = Path.Combine(DataFolder, "forex/oanda/daily/eurusd.zip");

            if (File.Exists(dailyPath))
                File.Delete(dailyPath);

            if (File.Exists(minutePath))
                File.Delete(minutePath);

            var downloadedMinuteData = ApiClient.DownloadData(minutePath);
            var downloadedDailyData = ApiClient.DownloadData(dailyPath);

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
            var nonExistentData = ApiClient.DownloadData(fakePath);

            Assert.IsFalse(nonExistentData);
            Assert.IsFalse(Directory.Exists(fakePath));
        }

        /// <summary>
        /// Test getting links to forex data for FXCM
        /// </summary>
        [Test, Ignore("Requires configured FXCM account")]
        public void FXCMDataLinks_CanBeRetrieved_Successfully()
        {
            var minutePath = LeanData.GenerateRelativeZipFilePath(
                new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM), "EURUSD"),
                new DateTime(2013, 10, 07),
                Resolution.Minute, TickType.Quote);
            var minuteDataLink = ApiClient.ReadDataLink(minutePath);

            var dailyPath = LeanData.GenerateRelativeZipFilePath(
                new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.FXCM), "EURUSD"),
                new DateTime(2013, 10, 07),
                Resolution.Daily, TickType.Quote);
            var dailyDataLink = ApiClient.ReadDataLink(dailyPath);

            Assert.IsTrue(minuteDataLink.Success);
            Assert.IsTrue(dailyDataLink.Success);
        }

        /// <summary>
        /// Test getting links to forex data for Oanda
        /// </summary>
        [Test, Ignore("Requires configured Oanda account")]
        public void OandaDataLinks_CanBeRetrieved_Successfully()
        {
            var minutePath = LeanData.GenerateRelativeZipFilePath(
                new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.Oanda), "EURUSD"),
                new DateTime(2013, 10, 07),
                Resolution.Minute, TickType.Quote);
            var minuteDataLink = ApiClient.ReadDataLink(minutePath);

            var dailyPath = LeanData.GenerateRelativeZipFilePath(
                new Symbol(SecurityIdentifier.GenerateForex("EURUSD", Market.Oanda), "EURUSD"),
                new DateTime(2013, 10, 07),
                Resolution.Daily, TickType.Quote);
            var dailyDataLink = ApiClient.ReadDataLink(dailyPath);

            Assert.IsTrue(minuteDataLink.Success);
            Assert.IsTrue(dailyDataLink.Success);
        }
    }
}
