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

using NUnit.Framework;
using QuantConnect.Logging;
using QuantConnect.ToolBox.BinanceDownloader;
using System;
using System.Linq;

namespace QuantConnect.Tests.Brokerages.Binance
{
    [TestFixture]
    public class BinanceBrokerageExchangeInfoTests
    {
        [Test]
        public void GetsExchangeInfo()
        {
            var downloader = new BinanceExchangeInfoDownloader();
            var tickers = downloader.Get().ToList();

            Assert.IsTrue(tickers.Any());

            foreach (var t in tickers)
            {
                Assert.IsTrue(t.StartsWith(Market.Binance, StringComparison.OrdinalIgnoreCase));
            }

            Log.Trace("Tickers retrieved: " + tickers.Count);
        }
    }
}
