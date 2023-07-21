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
            _provider = new BacktestingFutureChainProvider(TestGlobals.DataCacheProvider);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            // Restore intial Log Handler
            Log.LogHandler = _logHandler;
        }

        [TestCase("20131011")]
        // saturday, will fetch previous tradable date instead
        [TestCase("20131012")]
        public void CorrectlyDeterminesContractList(string date)
        {
            var dateTime = Time.ParseDate(date);
            var symbol = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, dateTime.AddDays(10));
            var result = _provider.GetFutureContractList(symbol, dateTime);

            Assert.IsNotEmpty(result);
        }

        [Test]
        public void ChecksBothOpenInterestAndQuoteFiles()
        {
            var testHandler = new QueueLogHandler();
            Log.LogHandler = testHandler;
            var originalValue = Log.DebuggingEnabled;
            Log.DebuggingEnabled = true;
            var symbol = Symbol.CreateFuture("NonExisting", Market.USA, new DateTime(2013, 11, 11));
            var result = _provider.GetFutureContractList(symbol, new DateTime(2013, 10, 11)).ToList();

            Log.DebuggingEnabled = originalValue;
            Assert.IsTrue(testHandler.Logs.Any(entry =>
            entry.Message.Contains("found no source of contracts for NONEXISTING 2X for date 20131011 for any tick type", StringComparison.InvariantCultureIgnoreCase)));
            Assert.IsEmpty(result);
        }

        [TestCase("20201007", 2)]
        [TestCase("20131007", 5)]
        public void UsesMultipleResolutions(string strDate, int expectedCount)
        {
            // we don't have minute data for this date
            var date = Time.ParseDate(strDate);

            var symbol = Symbol.CreateFuture(Futures.Indices.SP500EMini, Market.CME, date);
            var futureChain = _provider.GetFutureContractList(symbol, date).ToList();

            Assert.IsTrue(futureChain.All(x => x.ID.Date.Date >= date));
            Assert.IsTrue(futureChain.All(x => x.SecurityType == SecurityType.Future));
            Assert.IsTrue(futureChain.All(x => x.ID.Symbol == Futures.Indices.SP500EMini));
            Assert.AreEqual(expectedCount, futureChain.Count);
        }
    }
}
