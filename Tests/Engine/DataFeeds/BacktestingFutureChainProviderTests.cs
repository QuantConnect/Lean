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
using System.Linq;
using NUnit.Framework;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class BacktestingFutureChainProviderTests
    {
        private ILogHandler _logHandler;
        private BacktestingFutureChainProvider _provider;

        [OneTimeSetUp]
        public void SetUp()
        {
            // Store initial Log Handler
            _logHandler = Log.LogHandler;
            _provider = new BacktestingFutureChainProvider();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            // Restore intial Log Handler
            Log.LogHandler = _logHandler;
        }

        [Test]
        public void CorrectlyDeterminesContractList()
        {
            var symbol = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, DateTime.Today);
            var result = _provider.GetFutureContractList(symbol, new DateTime(2013, 10, 11));

            Assert.IsNotEmpty(result);
        }

        [Test]
        public void ChecksBothOpenInterestAndQuoteFiles()
        {
            var testHandler = new QueueLogHandler();
            Log.LogHandler = testHandler;
            var symbol = Symbol.CreateFuture("NonExisting", Market.USA, DateTime.UtcNow);
            var result = _provider.GetFutureContractList(symbol, new DateTime(2013, 10, 11)).ToList();

            Assert.IsTrue(testHandler.Logs.Any(entry => 
            entry.Message.Contains("BacktestingFutureChainProvider.GetFutureContractList(): Failed, files not found:")));
            Assert.IsEmpty(result);
        }
    }
}
