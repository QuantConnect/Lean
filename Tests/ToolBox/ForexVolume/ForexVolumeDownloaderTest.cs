using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.ToolBox;
using QuantConnect.ToolBox.ForexVolumeDownloader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Oanda.RestV20.Model;
using DateTime = System.DateTime;

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
        public void SaveCsv(IEnumerable<IBaseData> data, string fileName)
        {
            var sb = new StringBuilder("DateTime,Volume,Transactions\n");

            foreach (var obs in data)
            {
                sb.AppendLine(string.Format("{0:yyyy/MM/dd HH:mm},{1},{2}", obs.Time, obs.Value, ((Data.Custom.ForexVolume)obs).Transanctions));
            }
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                fileName);
            File.WriteAllText(filePath, sb.ToString());
        }


        [TestCase]
        public void DailyDataIsCorrectlyRetrieved()
        {
            var symbol = Symbol.Create("EURUSD", SecurityType.Base, Market.Decode(code: 20));
            var downloader = new ForexVolumeDownloader();
            var data = downloader.Get(symbol, Resolution.Daily, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 22));
            //SaveCsv(data, "DailyData.csv");
            Assert.Fail("WIP");
        }

        [TestCase]
        public void HourlyDataIsCorrectlyRetrieved()
        {
            var symbol = Symbol.Create("EURUSD", SecurityType.Base, Market.Decode(code: 20));
            var downloader = new ForexVolumeDownloader();
            var data = downloader.Get(symbol, Resolution.Hour, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 10));
            //SaveCsv(data, "HourData.csv");
            Assert.Fail("WIP");
        }

        [TestCase]
        public void MinuteDataIsCorrectlyRetrieved()
        {
            var symbol = Symbol.Create("EURUSD", SecurityType.Base, Market.Decode(code: 20));
            var downloader = new ForexVolumeDownloader();
            var data = downloader.Get(symbol, Resolution.Minute, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 04));
            //SaveCsv(data, "MinuteData.csv");
            Assert.Fail("WIP");
        }

        //[Ignore("WIP")]
        [TestCase]
        public void RetrievedDataIsCorrectlySaved()
        {
            var resolution = Resolution.Daily;
            var symbol = Symbol.Create("EURUSD", SecurityType.Base, Market.Decode(code: 20));
            var downloader = new ForexVolumeDownloader();
            var data = downloader.Get(symbol, resolution, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 22));
            var asd = data.First().DataType;

            var rndName = Guid.NewGuid().ToString().Substring(startIndex: 0, length: 8);
            var testingTempFolder = Path.Combine(Path.GetTempPath(), rndName);

            var writer = new LeanDataWriter(resolution, symbol, testingTempFolder);
            writer.Write(data);
            Assert.Fail("WIP");
        }
    }
}