using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.ToolBox;
using QuantConnect.ToolBox.ForexVolumeDownloader;
using System;
using System.IO;
using System.Linq;

namespace QuantConnect.Tests.ToolBox.ForexVolume
{
    [SetUpFixture]
    public class SetUpClass
    {
        [SetUp]
        public void SetUpTests()
        {
            Market.Add("FXCMForexVolume", identifier: 20);
        }
    }

    [TestFixture]
    public class ForexVolumeDownloaderTest
    {
        [TestCase]
        public void DailyDataIsCorrectlyRetrieved()
        {
            var symbol = Symbol.Create("EURUSD", SecurityType.Base, Market.Decode(code: 20));
            var downloader = new ForexVolumeDownloader();
            var data = downloader.Get(symbol, Resolution.Daily, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 07));
            Assert.Fail("WIP");
        }

        [TestCase]
        public void HourlyDataIsCorrectlyRetrieved()
        {
            var symbol = Symbol.Create("EURUSD", SecurityType.Base, Market.Decode(code: 20));
            var downloader = new ForexVolumeDownloader();
            var data = downloader.Get(symbol, Resolution.Hour, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 07));
            Assert.Fail("WIP");
        }

        [TestCase]
        public void MinuteDataIsCorrectlyRetrieved()
        {
            var symbol = Symbol.Create("EURUSD", SecurityType.Base, Market.Decode(code: 20));
            var downloader = new ForexVolumeDownloader();
            var data = downloader.Get(symbol, Resolution.Minute, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 04));
            Assert.Fail("WIP");
        }

        [Ignore("WIP")]
        [TestCase]
        public void RetrievedDataIsCorrectlySaved()
        {
            var data = Enumerable.Empty<BaseData>();
            var symbol = Symbol.Empty;

            var rndName = Guid.NewGuid().ToString().Substring(startIndex: 0, length: 8);
            var testingTempFolder = Path.Combine(Path.GetTempPath(), rndName);

            var writer = new LeanDataWriter(Resolution.Daily, symbol, testingTempFolder);
            writer.Write(data);
            Assert.Fail("WIP");
        }
    }
}