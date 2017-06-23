using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.ToolBox.ForexVolumeDownloader;

namespace QuantConnect.Tests.ToolBox.ForexVolume
{
    [TestFixture]
    public class ForexVolumeDownloaderTest
    {
        [TestCase()]
        public void DailyDataIsCorrectlyRequest()
        {
            var symbol = Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM);
            var downloader = new ForexVolumeDownloader();
            var data = downloader.Get(symbol, Resolution.Daily, new DateTime(2017, 04, 02),
                new DateTime(2017, 04, 07));
            Assert.Fail("WIP");
        }

        [TestCase()]
        public void HourlyDataIsCorrectlyRequest()
        {
            Assert.Fail("WIP");
        }

        [TestCase()]
        public void MinuteDataIsCorrectlyRequest()
        {
            Assert.Fail("WIP");
        }
    }
}
