using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class BaseResultsHandlerTests
    {
        private BaseResultsHandlerTestable _baseResultsHandler;
        private const string ResultsDestinationFolderKey = "results-destination-folder";
        private const string ResultsDestinationFilePrefixKey = "results-destination-file-prefix";
        private const string AlgorithmId = "MyAlgorithm";
        
        [TestCase(true, "./temp")]
        [TestCase(false, "IGNORED")]
        [Test]
        public void ResultsDestinationFolderIsCorrect(bool overrideDefault, string overrideValue)
        {
            Config.Reset();
            if (overrideDefault)
            {
                Config.Set(ResultsDestinationFolderKey, overrideValue);
            }
            
            _baseResultsHandler = new BaseResultsHandlerTestable(AlgorithmId);

            var expectedValue = overrideDefault ? overrideValue : Directory.GetCurrentDirectory();
            
            Assert.AreEqual(expectedValue, _baseResultsHandler.GetResultsDestinationFolder);
        }

        [TestCase(true, "MyFilePrefix")]
        [TestCase(false, "IGNORED")]
        [Test]
        public void ResultsDestinationFilePrefixIsCorrect(bool overrideDefault, string overrideValue)
        {
            Config.Reset();
            if (overrideDefault)
            {
                Config.Set(ResultsDestinationFilePrefixKey, overrideValue);
            }
            
            _baseResultsHandler = new BaseResultsHandlerTestable(AlgorithmId);

            var expectedValue = overrideDefault ? overrideValue : AlgorithmId;
            
            Assert.AreEqual(expectedValue, _baseResultsHandler.GetResultsDestinationFilePrefix);
        }

        [Test]
        public void CheckSaveLogs()
        {
            _baseResultsHandler = new BaseResultsHandlerTestable(AlgorithmId);

            var tempPath = Path.GetTempPath();
            
            _baseResultsHandler.SetResultsDestinationFolder(tempPath);
            
            const string id = "test";
            var logEntries = new List<LogEntry>
            {
                new LogEntry("Message 1"),
                new LogEntry("Message 2"),
                new LogEntry("Message 3"),
            };

            var saveLocation = _baseResultsHandler.SaveLogs(id, logEntries);
            
            Assert.True(File.Exists(saveLocation));
            Assert.AreEqual(Path.Combine(tempPath, $"{id}-log.txt"), saveLocation);
        }
        
        private class BaseResultsHandlerTestable : BaseResultsHandler
        {
            public BaseResultsHandlerTestable(string algorithmId)
            {
                AlgorithmId = algorithmId;
            }

            public void SetResultsDestinationFolder(string folder)
            {
                ResultsDestinationFolder = folder;
            }
            public string GetResultsDestinationFolder => ResultsDestinationFolder;
            public string GetResultsDestinationFilePrefix => ResultsDestinationFilePrefix;

            public new string GetResultsPath(string filename) => base.GetResultsPath(filename);
            
            protected override void Run()
            {
                throw new NotImplementedException();
            }

            protected override void StoreResult(Packet packet)
            {
                throw new NotImplementedException();
            }

            protected override void Sample(string chartName, 
                                           string seriesName, 
                                           int seriesIndex, 
                                           SeriesType seriesType, 
                                           DateTime time, 
                                           decimal value, 
                                           string unit = "$")
            {
                throw new NotImplementedException();
            }
        }
    }
}
