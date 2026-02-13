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

using System.Linq;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.DownloaderDataProvider.Launcher.Models;

namespace QuantConnect.Tests.DownloaderDataProvider
{
    [TestFixture]
    public class DataDownloadConfigTests
    {
        [TestCase(null, "BTCUSDT", SecurityType.Crypto, "coinbase", false)]
        [TestCase(null, "BTCUSDT", SecurityType.Crypto, "coinbase", true)]
        [TestCase("", "ETHUSDT", SecurityType.Crypto, "coinbase", false)]
        [TestCase("", "ETHUSDT", SecurityType.Crypto, "coinbase", true)]
        [TestCase(null, "AAPL", SecurityType.Equity, "usa", false)]
        [TestCase(null, "AAPL", SecurityType.Equity, "usa", true)]
        [TestCase("", "AAPL", SecurityType.Equity, "usa", false)]
        [TestCase("", "AAPL", SecurityType.Equity, "usa", true)]
        [TestCase("USA", "AAPL", SecurityType.Equity, "usa")]
        [TestCase("ICE", "AAPL", SecurityType.Equity, "ice")]
        public void ValidateMarketArguments(string market, string ticker, SecurityType securityType, string expectedMarket, bool skipConfigMarket = false)
        {
            Config.Set("data-type", "Trade");
            Config.Set("resolution", "Daily");
            Config.Set("security-type", $"{securityType}");
            Config.Set("tickers", $"{{\"{ticker}\": \"\"}}");
            Config.Set("start-date", "20240101");
            Config.Set("end-date", "20240202");

            if (!skipConfigMarket)
            {
                Config.Set("market", market);
            }

            var dataDownloadConfig = new DataDownloadConfig();

            Assert.That(dataDownloadConfig.MarketName, Is.EqualTo(expectedMarket));

            Config.Reset();
        }

        [TestCase(Market.CME, Securities.Futures.Indices.SP500EMini + "H6", SecurityType.Future, false)]
        [TestCase(Market.CME, Securities.Futures.Indices.SP500EMini, SecurityType.Future, true)]
        [TestCase(Market.CME, "E", SecurityType.Future, true)]
        [TestCase(Market.USA, "AAPL", SecurityType.Option, true)]
        [TestCase(Market.USA, "AAPL260213C00262500", SecurityType.Option, false)]
        [TestCase(Market.USA, "AAPL  260213C00262500", SecurityType.Option, false)]
        [TestCase(Market.USA, "SPXW260213C06050000", SecurityType.IndexOption, false)]
        [TestCase(Market.USA, "SPXW  260213C06050000", SecurityType.IndexOption, false)]
        public void ShouldParseSymbolContractAndDownload(string market, string ticker, SecurityType securityType, bool expectedIsCanonical)
        {
            Config.Set("data-type", "Trade");
            Config.Set("resolution", "Daily");
            Config.Set("security-type", $"{securityType}");
            Config.Set("tickers", $"{{\"{ticker}\": \"\"}}");
            Config.Set("start-date", "20260201");
            Config.Set("end-date", "20260213");
            Config.Set("market", market);

            var dataDownloadConfig = new DataDownloadConfig();

            Assert.IsNotEmpty(dataDownloadConfig.Symbols);
            Assert.AreEqual(1, dataDownloadConfig.Symbols.Count);

            var symbol = dataDownloadConfig.Symbols.First();
            Assert.IsNotNull(symbol);
            Assert.AreEqual(expectedIsCanonical, symbol.IsCanonical());
        }
    }
}
