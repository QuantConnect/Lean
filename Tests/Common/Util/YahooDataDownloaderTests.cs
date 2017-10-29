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
using QuantConnect.ToolBox.YahooDownloader;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class YahooDataDownloaderTests
    {
        private const string PermTick = "AAPL";
        private const string Market = "usa";
        readonly Symbol _symbol = new Symbol(SecurityIdentifier.GenerateEquity(PermTick, Market), PermTick);

        private YahooDataDownloader _yahooDataDownloader;
        [TestFixtureSetUp]
        public void Setup()
        {
            _yahooDataDownloader = new YahooDataDownloader();
        }

        [Test]
        public void GetMethod_ShouldReturn_Successfully()
        {
            var yahooData = _yahooDataDownloader.Get(_symbol, Resolution.Daily, new DateTime(2017,1,1), DateTime.Now);
            Assert.IsTrue(yahooData.Any());
        }

        [Test, Ignore("Yahoo Api as of 7/11/2017 has the adjusted close and close swapped.")]
        public void GetMethod_WithNormalDates_ShouldReturnCorrectHistoricalData()
        {
            //Arrange
            var yahooData = _yahooDataDownloader.Get(_symbol, Resolution.Daily, new DateTime(2017,2,1), new DateTime(2017,2,2));

            //Assert
            Assert.AreEqual(yahooData.ElementAt(0).Value, 128.75);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void GetMethod_WithNonDailyResolution_ShouldThrowException()
        {
            _yahooDataDownloader.Get(_symbol, Resolution.Minute, new DateTime(2017,2,1), new DateTime(2017, 2, 2));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void GetMethod_WithReveseDate_ShouldThrowException()
        {
            _yahooDataDownloader.Get(_symbol, Resolution.Daily, new DateTime(2017, 2, 2), new DateTime(2017, 2, 1));
        }

        [Test]
        [ExpectedException(typeof(NotSupportedException))]
        public void GetMethod_WithNonEquitySecurity_ShouldThrowException()
        {
            _yahooDataDownloader.Get(new Symbol(SecurityIdentifier.GenerateForex("EURUSD", "USA"), "EURUSD"), Resolution.Daily, new DateTime(2017, 2, 1), new DateTime(2017, 2, 2));
        }

    }
}