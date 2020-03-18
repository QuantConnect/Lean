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
