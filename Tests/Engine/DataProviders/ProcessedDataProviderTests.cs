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
 *
*/

using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds;
using System.IO;

namespace QuantConnect.Tests.Engine.DataProviders
{
    [TestFixture]
    public class ProcessedDataProviderTests
    {
        private ProcessedDataProvider _processedDataProvider;
        private string _originalProcessedDataDirectory;

        [OneTimeSetUp]
        public void Setup()
        {
            // Set up the processed data provider
            _originalProcessedDataDirectory = Config.Get("processed-data-directory");
            Config.Set("processed-data-directory", "TestData");

            _processedDataProvider = new ProcessedDataProvider();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _processedDataProvider.Dispose();

            // Revert the configuration
            Config.Set("processed-data-directory", _originalProcessedDataDirectory);
        }

        private static TestCaseData[] ExistingDataTestCases => new[]
        {
            // Exists in the processed data directory
            new TestCaseData(Path.Combine(Globals.DataFolder, "spy_10_min.txt"), true),
            // Does not exist in the processed data directory but exists in the main data folder
            new TestCaseData(Path.Combine(Globals.DataFolder, "equity/usa/minute/aapl/20140606_trade.zip"), true),
            // Neither the processed data directory nor in the main data folder
            new TestCaseData(Path.Combine(Globals.DataFolder, "equity/usa/minute/somestock/19980606_trade.zip"), false)
        };

        [TestCaseSource(nameof(ExistingDataTestCases))]
        public void ProcessedDataProvider_CanReadDataThatExists(string path, bool exists)
        {
            var stream = _processedDataProvider.Fetch(path);

            if (exists)
            {
                Assert.IsNotNull(stream);
            }
            else
            {
                Assert.IsNull(stream);
            }
        }
    }
}
