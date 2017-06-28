using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.ToolBox;
using QuantConnect.ToolBox.ForexVolumeDownloader; 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ionic.Zip;
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
        Symbol _symbol = Symbol.Create("EURUSD", SecurityType.Base, Market.Decode(code: 20));
        ForexVolumeDownloader _downloader = new ForexVolumeDownloader();

        [Ignore("WIP")]
        [TestCase]
        public void DailyDataIsCorrectlyRetrieved()
        {
            var data = _downloader.Get(_symbol, Resolution.Daily, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 22));
            //SaveCsv(data, "DailyData.csv");
            Assert.Fail("WIP");
        }

        [Ignore("WIP")]
        [TestCase]
        public void HourlyDataIsCorrectlyRetrieved()
        {
            var data = _downloader.Get(_symbol, Resolution.Hour, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 10));
            //SaveCsv(data, "HourData.csv");
            Assert.Fail("WIP");
        }

        [Ignore("WIP")]
        [TestCase]
        public void MinuteDataIsCorrectlyRetrieved()
        {
            var data = _downloader.Get(_symbol, Resolution.Minute, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 04));
            //SaveCsv(data, "MinuteData.csv");
            Assert.Fail("WIP");
        }

        [TestCase]
        public void RetrievedDailyDataIsCorrectlySaved()
        {
            // Arrange
            var resolution = Resolution.Daily;
            var data = _downloader.Get(_symbol, resolution, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 22));
            // Create a temporary folder to save testing data
            var rndName = Guid.NewGuid().ToString().Substring(startIndex: 0, length: 8);
            var testingTempFolder = Path.Combine(Path.GetTempPath(), rndName);

            // Act
            var writer = new LeanDataWriter(resolution, _symbol, testingTempFolder);
            writer.Write(data);

            // Assert
            var expectedData = data.Cast<Data.Custom.ForexVolume>().ToArray();
            var outputFile = Path.Combine(testingTempFolder, "base\\fxcmforexvolume\\daily\\eurusd.zip");

            var actualdata = ReadZipFileData(outputFile);
            var lines = actualdata.Count;
            for (int i = 0; i < lines-1; i++)
            {
                Assert.AreEqual(expectedData[i].Value, int.Parse(actualdata[i][1]));
                Assert.AreEqual(expectedData[i].Transanctions, int.Parse(actualdata[i][2]));
            }
        }

        [TestCase]
        public void RetrievedHourDataIsCorrectlySaved()
        {
            // Arrange
            var resolution = Resolution.Hour;
            var symbol = Symbol.Create("EURUSD", SecurityType.Base, Market.Decode(code: 20));
            var downloader = new ForexVolumeDownloader();
            var data = downloader.Get(symbol, resolution, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 15));
            // Create a temporary folder to save testing data
            var rndName = Guid.NewGuid().ToString().Substring(startIndex: 0, length: 8);
            var testingTempFolder = Path.Combine(Path.GetTempPath(), rndName);

            // Act
            var writer = new LeanDataWriter(resolution, symbol, testingTempFolder);
            writer.Write(data);

            // Assert
            var expectedData = data.Cast<Data.Custom.ForexVolume>().ToArray();
            var outputFile = Path.Combine(testingTempFolder, "base\\fxcmforexvolume\\hour\\eurusd.zip");

            var actualdata = ReadZipFileData(outputFile);
            var lines = actualdata.Count;
            for (int i = 0; i < lines - 1; i++)
            {
                Assert.AreEqual(expectedData[i].Value, int.Parse(actualdata[i][1]));
                Assert.AreEqual(expectedData[i].Transanctions, int.Parse(actualdata[i][2]));
            }
        }

        [TestCase]
        public void RetrievedMinuteDataIsCorrectlySaved()
        {
            // Arrange
            var resolution = Resolution.Minute;
            var data = _downloader.Get(_symbol, resolution, new DateTime(year: 2017, month: 04, day: 02),
                new DateTime(year: 2017, month: 04, day: 7));
            // Create a temporary folder to save testing data
            var rndName = Guid.NewGuid().ToString().Substring(startIndex: 0, length: 8);
            var testingTempFolder = Path.Combine(Path.GetTempPath(), rndName);

            // Act
            var writer = new LeanDataWriter(resolution, _symbol, testingTempFolder);
            writer.Write(data);

            // Assert
            var expectedData = data.Cast<Data.Custom.ForexVolume>().ToArray();
            var outputFolder = Path.Combine(testingTempFolder, "base\\fxcmforexvolume\\minute");

            var actualdata = ReadZipFolderData(outputFolder);
            var lines = actualdata.Count;
            for (int i = 0; i < lines - 1; i++)
            {
                Assert.AreEqual(expectedData[i].Value, int.Parse(actualdata[i][1]));
                Assert.AreEqual(expectedData[i].Transanctions, int.Parse(actualdata[i][2]));
            }
        }


        #region Auxiliary methods
        private List<string[]> ReadZipFolderData(string outputFolder)
        {
            var actualdata = new List<string[]>();
            var files = Directory.GetFiles(outputFolder, "*.zip");
            foreach (var file in files)
            {
                actualdata.AddRange(ReadZipFileData(file));
            }
            return actualdata;
        }

        private static List<string[]> ReadZipFileData(string dataZipFile)
        {
            var actualdata = new List<string[]>();
            ZipFile zipFile;
            using (var unzipped = QuantConnect.Compression.Unzip(dataZipFile, out zipFile))
            {
                string line;
                while ((line = unzipped.ReadLine()) != null)
                {
                    actualdata.Add(line.Split(','));
                }
            }
            return actualdata;
        }

        void SaveCsv(IEnumerable<IBaseData> data, string fileName)
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

        #endregion
    }
}