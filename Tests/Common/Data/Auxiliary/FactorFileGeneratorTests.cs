using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.ToolBox;
using QuantConnect.ToolBox.YahooDownloader;
using QuantConnect.Util;

namespace QuantConnect.Tests.Common.Data.Auxiliary
{
    [TestFixture]
    public class FactorFileGeneratorTests
    {
        // Symbol can be choosen
        Symbol symbol = new Symbol(SecurityIdentifier.GenerateEquity("AAPL", "usa"), "1");
        private string dataPath = LeanData.GenerateZipFilePath(Config.Get("data-folder"), new Symbol(SecurityIdentifier.GenerateEquity("AAPL", "usa"), "AAPL"), DateTime.MaxValue, Resolution.Daily, TickType.Quote);

        [Test]
        public void SplitsAndDividends_CanBeDownloadedFromYahoo_Successfully()
        {
            var yahooDataDownloader = new YahooDataDownloader();

            var yahooEvents = yahooDataDownloader.DownloadSplitAndDividendData(symbol, DateTime.MinValue, DateTime.MaxValue);

            Assert.IsTrue(yahooEvents.Any());
        }

       
        [Test]
        public void DailyEquityData_CanBeRead_Successfully()
        {
            var factorFileGenerator = new FactorFileGenerator(symbol, dataPath);

            Assert.IsTrue(factorFileGenerator.DailyDataForEquity.Any());
        }

        [Test]
        public void FactorFile_CanBeCreatedFromYahooData_Successfully()
        {
            var factorFileGenerator = new FactorFileGenerator(symbol, dataPath);
            var yahooDataDownloader = new YahooDataDownloader();

            var yahooEvents = yahooDataDownloader.DownloadSplitAndDividendData(symbol, DateTime.Parse("01/01/1980"), DateTime.MaxValue);
            var factorFile  = factorFileGenerator.CreateFactorFileFromData(yahooEvents);

            Assert.IsTrue(factorFile.Permtick == symbol.ID.Symbol);
        }
    }
}
