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

using System.Linq;
using NUnit.Framework;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Engine.DataProviders
{
    [TestFixture]
    public class DefaultDataProviderTests
    {
        private DefaultDataProvider _defaultDataProvider;

        [TestFixtureSetUp]
        public void Setup()
        {
            _defaultDataProvider = new DefaultDataProvider();
        }

        [Test]
        public void DefaultDataProvider_CanReadDataThatExists()
        {
            var stream = _defaultDataProvider.Fetch("../../../Data/equity/usa/minute/aapl/20140606_trade.zip");
            
            Assert.IsNotNull(stream);
        }

        [Test]
        public void DefaultDataProvider_CannotReadDataThatDoesNotExist()
        {
            var stream = _defaultDataProvider.Fetch("../../../Data/equity/usa/minute/aapl/19980606_trade.zip");

            Assert.IsNull(stream);
        }

        [Test]
        public void DefaultDataProvider_Logging_MissingDirectory()
        {
            var existingLogHandler = Log.LogHandler;
            var testHandler = new QueueLogHandler();
            Log.LogHandler = testHandler;
            var stream = _defaultDataProvider.Fetch("../../../Data/equity/usa/minute/michel_angelo/19980606_trade.zip");
            stream = _defaultDataProvider.Fetch("../../../Data/equity/usa/minute/michel_angelo/19980606_trade.zip");
            stream = _defaultDataProvider.Fetch("../../../Data/equity/usa/minute/michel_angelo/19980606_trade.zip");
            stream = _defaultDataProvider.Fetch("../../../Data/equity/usa/minute/michel_angelo/19980606_trade.zip");

            Log.LogHandler = existingLogHandler;
            Assert.DoesNotThrow( () => testHandler.Logs.Single(entry => entry.Message.Contains("The specified directory was not found: ")));
        }

        [Test]
        public void DefaultDataProvider_Logging_MissingFile()
        {
            var existingLogHandler = Log.LogHandler;
            var testHandler = new QueueLogHandler();
            Log.LogHandler = testHandler;
            var stream = _defaultDataProvider.Fetch("../../../Data/equity/usa/minute/aapl/19980606_trade.zip");
            stream = _defaultDataProvider.Fetch("../../../Data/equity/usa/minute/aapl/19980607_trade.zip");

            Log.LogHandler = existingLogHandler;
            Assert.AreEqual(2, testHandler.Logs.Count(entry => entry.Message.Contains("The specified file was not found:")));
            Assert.DoesNotThrow(() => testHandler.Logs.Single(entry => entry.Message.Contains("19980606_trade.zip")));
            Assert.DoesNotThrow(() => testHandler.Logs.Single(entry => entry.Message.Contains("19980607_trade.zip")));
        }
    }
}
