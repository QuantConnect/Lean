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
using System.IO;
using System.Text;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class TradeBarTests
    {
        [Test]
        public void UpdatesProperly()
        {
            var bar = new TradeBar();
            bar.UpdateTrade(10, 10);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(10, bar.High);
            Assert.AreEqual(10, bar.Low);
            Assert.AreEqual(10, bar.Close);
            Assert.AreEqual(10, bar.Volume);

            bar.UpdateTrade(20, 5);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(20, bar.High);
            Assert.AreEqual(10, bar.Low);
            Assert.AreEqual(20, bar.Close);
            Assert.AreEqual(15, bar.Volume);

            bar.UpdateTrade(5, 50);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(20, bar.High);
            Assert.AreEqual(5, bar.Low);
            Assert.AreEqual(5, bar.Close);
            Assert.AreEqual(65, bar.Volume);

            bar.UpdateTrade(11, 100);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(20, bar.High);
            Assert.AreEqual(5, bar.Low);
            Assert.AreEqual(11, bar.Close);
            Assert.AreEqual(165, bar.Volume);
        }

        [Test]
        public void HandlesAssetWithValidZeroPrice()
        {
            var bar = new TradeBar();
            bar.UpdateTrade(10, 10);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(10, bar.High);
            Assert.AreEqual(10, bar.Low);
            Assert.AreEqual(10, bar.Close);
            Assert.AreEqual(10, bar.Volume);

            bar.UpdateTrade(0, 100);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(10, bar.High);
            Assert.AreEqual(0, bar.Low);
            Assert.AreEqual(0, bar.Close);
            Assert.AreEqual(110, bar.Volume);

            bar.UpdateTrade(-5, 100);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(10, bar.High);
            Assert.AreEqual(-5, bar.Low);
            Assert.AreEqual(-5, bar.Close);
            Assert.AreEqual(210, bar.Volume);

            bar.UpdateTrade(5, 100);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(10, bar.High);
            Assert.AreEqual(-5, bar.Low);
            Assert.AreEqual(5, bar.Close);
            Assert.AreEqual(310, bar.Volume);

            bar.UpdateTrade(50, 100);
            Assert.AreEqual(10, bar.Open);
            Assert.AreEqual(50, bar.High);
            Assert.AreEqual(-5, bar.Low);
            Assert.AreEqual(50, bar.Close);
            Assert.AreEqual(410, bar.Volume);
        }

        [Test]
        public void TradeBarParseScalesOptionsWithEquityUnderlying()
        {
            var factory = new TradeBar();
            var underlying = Symbol.Create("SPY", SecurityType.Equity, QuantConnect.Market.USA);
            var optionSymbol = Symbol.CreateOption(
                underlying,
                QuantConnect.Market.CME,
                OptionStyle.American,
                OptionRight.Put,
                4200m,
                SecurityIdentifier.DefaultDate);

            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                optionSymbol,
                Resolution.Minute,
                TimeZones.Chicago,
                TimeZones.Chicago,
                true,
                false,
                false,
                false,
                TickType.Trade,
                true,
                DataNormalizationMode.Raw);

            var tradeLine = "40560000,10000,15000,10000,15000,90";
            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(tradeLine));
            using var stream = new StreamReader(memoryStream);

            var tradeBarFromLine = (TradeBar)factory.Reader(config, tradeLine, new DateTime(2020, 9, 22), false);
            var tradeBarFromStream = (TradeBar)factory.Reader(config, stream, new DateTime(2020, 9, 22), false);

            Assert.AreEqual(new DateTime(2020, 9, 22, 11, 17, 0), tradeBarFromLine.EndTime);
            Assert.AreEqual(optionSymbol, tradeBarFromLine.Symbol);
            Assert.AreEqual(1m, tradeBarFromLine.Open);
            Assert.AreEqual(1.5m, tradeBarFromLine.High);
            Assert.AreEqual(1m, tradeBarFromLine.Low);
            Assert.AreEqual(1.5m, tradeBarFromLine.Close);
            Assert.AreEqual(90m, tradeBarFromLine.Volume);

            Assert.AreEqual(new DateTime(2020, 9, 22, 11, 17, 0), tradeBarFromStream.EndTime);
            Assert.AreEqual(optionSymbol, tradeBarFromStream.Symbol);
            Assert.AreEqual(1m, tradeBarFromStream.Open);
            Assert.AreEqual(1.5m, tradeBarFromStream.High);
            Assert.AreEqual(1m, tradeBarFromStream.Low);
            Assert.AreEqual(1.5m, tradeBarFromStream.Close);
            Assert.AreEqual(90m, tradeBarFromStream.Volume);
        }

        [Test]
        public void TradeBarParseDoesNotScaleOptionsWithNonEquityUnderlying()
        {
            var factory = new TradeBar();
            var underlying = Symbol.CreateFuture("ES", QuantConnect.Market.CME, new DateTime(2021, 3, 19));
            var optionSymbol = Symbol.CreateOption(
                underlying,
                QuantConnect.Market.CME,
                OptionStyle.American,
                OptionRight.Put,
                4200m,
                SecurityIdentifier.DefaultDate);

            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                optionSymbol,
                Resolution.Minute,
                TimeZones.Chicago,
                TimeZones.Chicago,
                true,
                false,
                false,
                false,
                TickType.Trade,
                true,
                DataNormalizationMode.Raw);

            var tradeLine = "40560000,1.0,1.5,1.0,1.5,90.0";
            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(tradeLine));
            using var stream = new StreamReader(memoryStream);

            var unscaledTradeBarFromLine = (TradeBar)factory.Reader(config, tradeLine, new DateTime(2020, 9, 22), false);
            var unscaledTradeBarFromStream = (TradeBar)factory.Reader(config, stream, new DateTime(2020, 9, 22), false);

            Assert.AreEqual(new DateTime(2020, 9, 22, 11, 17, 0), unscaledTradeBarFromLine.EndTime);
            Assert.AreEqual(optionSymbol, unscaledTradeBarFromLine.Symbol);
            Assert.AreEqual(1m, unscaledTradeBarFromLine.Open);
            Assert.AreEqual(1.5m, unscaledTradeBarFromLine.High);
            Assert.AreEqual(1m, unscaledTradeBarFromLine.Low);
            Assert.AreEqual(1.5m, unscaledTradeBarFromLine.Close);
            Assert.AreEqual(90m, unscaledTradeBarFromLine.Volume);

            Assert.AreEqual(new DateTime(2020, 9, 22, 11, 17, 0), unscaledTradeBarFromStream.EndTime);
            Assert.AreEqual(optionSymbol, unscaledTradeBarFromStream.Symbol);
            Assert.AreEqual(1m, unscaledTradeBarFromStream.Open);
            Assert.AreEqual(1.5m, unscaledTradeBarFromStream.High);
            Assert.AreEqual(1m, unscaledTradeBarFromStream.Low);
            Assert.AreEqual(1.5m, unscaledTradeBarFromStream.Close);
            Assert.AreEqual(90m, unscaledTradeBarFromStream.Volume);
        }

        [TestCase(Resolution.Minute, "43140000,21.04,21.44,20.4,21.24,0")]
        [TestCase(Resolution.Hour, "20200922 11:00,21.04,21.44,20.4,21.24,0")]
        [TestCase(Resolution.Daily, "20200921 00:00,21.04,21.44,20.4,21.24,0")]
        public void TradeBarIndexLowResolutionParsing(Resolution resolution, string tradeLine)
        {
            var factory = new TradeBar();
            var symbol = Symbols.CreateIndexSymbol("VIX");
            var entry = MarketHoursDatabase.FromDataFolder()
                .GetEntry(symbol.ID.Market, symbol, symbol.SecurityType);
            var config = new SubscriptionDataConfig(
                typeof(TradeBar),
                symbol,
                resolution,
                entry.DataTimeZone,
                entry.ExchangeHours.TimeZone,
                true,
                false,
                false,
                false,
                TickType.Trade,
                true,
                DataNormalizationMode.Raw);

            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(tradeLine));
            using var stream = new StreamReader(memoryStream);

            var fromLine = (TradeBar)factory.Reader(config, tradeLine, new DateTime(2020, 9, 22), false);
            var fromStream = (TradeBar)factory.Reader(config, stream, new DateTime(2020, 9, 22), false);

            var expectedEndTime = new DateTime(2020, 9, 22, 12, 0, 0);
            if (resolution == Resolution.Daily)
            {
                expectedEndTime = new DateTime(2020, 9, 22, 0, 0, 0);
            }
            Assert.AreEqual(expectedEndTime, fromLine.EndTime);
            Assert.AreEqual(symbol, fromLine.Symbol);
            Assert.AreEqual(21.04m, fromLine.Open);
            Assert.AreEqual(21.44m, fromLine.High);
            Assert.AreEqual(20.4m, fromLine.Low);
            Assert.AreEqual(21.24m, fromLine.Close);
            Assert.AreEqual(0m, fromLine.Volume);

            Assert.AreEqual(expectedEndTime, fromStream.EndTime);
            Assert.AreEqual(symbol, fromLine.Symbol);
            Assert.AreEqual(21.04m, fromLine.Open);
            Assert.AreEqual(21.44m, fromLine.High);
            Assert.AreEqual(20.4m, fromLine.Low);
            Assert.AreEqual(21.24m, fromLine.Close);
            Assert.AreEqual(0m, fromLine.Volume);
        }
    }
}
