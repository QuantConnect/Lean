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
using System.Linq;
using System.Text;
using NUnit.Framework;
using QuantConnect.Data;
using System.Globalization;
using QuantConnect.Securities;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Common.Data.Market
{
    [TestFixture]
    public class TickTests
    {
        [Test]
        public void ConstructsFromLine()
        {
            const string line = "15093000,1456300,100,P,T,0";

            var baseDate = new DateTime(2013, 10, 08);
            var tick = new Tick(Symbols.SPY, line, baseDate);

            var ms = (tick.Time - baseDate).TotalMilliseconds;
            Assert.AreEqual(15093000, ms);
            Assert.AreEqual(1456300, tick.LastPrice * 10000m);
            Assert.AreEqual(100, tick.Quantity);
            Assert.AreEqual("P", tick.ExchangeCode);
            Assert.AreEqual("ARCA", tick.Exchange);
            Assert.AreEqual("T", tick.SaleCondition);
            Assert.AreEqual(false, tick.Suspicious);
        }

        [TestCase("18000677.3,3669.12,0.0040077,3669.13,3.40618718", "18000677.3", "3669.12", "0.0040077", "3669.13", "3.40618718")]
        [TestCase("18000677.3111,3669.12,0.0040077,3669.13,3.40618718", "18000677.3111", "3669.12", "0.0040077", "3669.13", "3.40618718")]
        public void ConstructsFromLineWithDecimalTimestamp(string line, string milliseconds, string bidPrice,
            string bidSize, string askPrice, string askSize)
        {
            var config = new SubscriptionDataConfig(
                typeof(Tick), Symbols.BTCUSD, Resolution.Tick, TimeZones.Utc, TimeZones.Utc,
                false, false, false, false, TickType.Quote);
            var baseDate = new DateTime(2019, 1, 15);

            var tick = new Tick(config, line, baseDate);

            var ms = (tick.Time - baseDate).TotalMilliseconds;
            Assert.AreEqual( decimal.Parse(milliseconds, CultureInfo.InvariantCulture), ms);
            Assert.AreEqual(decimal.Parse(bidPrice, CultureInfo.InvariantCulture), tick.BidPrice);
            Assert.AreEqual(decimal.Parse(bidSize, CultureInfo.InvariantCulture), tick.BidSize);
            Assert.AreEqual(decimal.Parse(askPrice, CultureInfo.InvariantCulture), tick.AskPrice);
            Assert.AreEqual(decimal.Parse(askSize, CultureInfo.InvariantCulture), tick.AskSize);
        }

        [Test]
        public void ReadsFuturesTickFromLine()
        {
            const string line = "86399572,52.62,5,usa,,0,False";

            var baseDate = new DateTime(2013, 10, 08);
            var symbol = Symbol.CreateFuture(Futures.Energy.CrudeOilWTI, QuantConnect.Market.NYMEX, new DateTime(2017, 2, 28));
            var config = new SubscriptionDataConfig(typeof(Tick), symbol, Resolution.Tick, TimeZones.NewYork, TimeZones.NewYork, false, false, false);
            var tick = new Tick(config, line, baseDate);

            var ms = (tick.Time - baseDate).TotalMilliseconds;
            Assert.AreEqual(86399572, ms);
            Assert.AreEqual(52.62, tick.LastPrice);
            Assert.AreEqual(5, tick.Quantity);
            Assert.AreEqual("", tick.Exchange);
            Assert.AreEqual("", tick.SaleCondition);
            Assert.AreEqual(false, tick.Suspicious);
        }

        [TestCase(SecurityType.Crypto, TickType.Trade, "1234567,18000,0.0001,0")]
        [TestCase(SecurityType.Crypto, TickType.Quote, "1234567,18000,10,18100,15,0")]
        [TestCase(SecurityType.CryptoFuture, TickType.Trade, "1234567,18000,0.0001,0")]
        [TestCase(SecurityType.CryptoFuture, TickType.Quote, "1234567,18000,10,18100,15,0")]
        public void ReadsCryptoAndCryptoFuturesTickFromLine(SecurityType securityType, TickType tickType, string line)
        {
            var baseDate = new DateTime(2013, 10, 08);
            var symbol = Symbol.Create("BTCUSDT", securityType, QuantConnect.Market.Binance);
            var config = new SubscriptionDataConfig(typeof(Tick), symbol, Resolution.Tick, TimeZones.NewYork, TimeZones.NewYork, false, false, false,
                tickType: tickType);
            var tick = new Tick(config, line, baseDate);

            var ms = (tick.Time - baseDate).TotalMilliseconds;
            Assert.AreEqual(1234567d, ms);
            Assert.AreEqual("", tick.Exchange);
            Assert.AreEqual("", tick.SaleCondition);
            Assert.AreEqual(false, tick.Suspicious);

            if (tickType == TickType.Trade)
            {
                Assert.AreEqual(18000m, tick.Value);
                Assert.AreEqual(0.0001m, tick.Quantity);
                Assert.AreEqual("", tick.Exchange);
                Assert.AreEqual("", tick.SaleCondition);
                Assert.AreEqual(false, tick.Suspicious);
            }
            else
            {
                Assert.AreEqual((18000m + 18100m) / 2m, tick.Value);
                Assert.AreEqual(18000m, tick.BidPrice);
                Assert.AreEqual(10m, tick.BidSize);
                Assert.AreEqual(18100m, tick.AskPrice);
                Assert.AreEqual(15m, tick.AskSize);
            }
        }

        [TestCase("14400135,0,0,1680000,400,NASDAQ,00000001,0", 0, 0, 168, 400)]
        [TestCase("14400135,10000,10,0,0,NASDAQ,00000001,0", 1, 10, 0, 0)]
        [TestCase("14400135,10000,10,20000,20,NASDAQ,00000001,0", 1, 10, 2, 20)]
        public void EquityQuoteTick(string line, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            var baseDate = new DateTime(2013, 10, 08);
            var config = new SubscriptionDataConfig(typeof(Tick),
                Symbols.SPY,
                Resolution.Tick,
                TimeZones.NewYork,
                TimeZones.NewYork,
                false,
                false,
                false,
                false,
                TickType.Quote);
            var tick = new Tick(config, line, baseDate);

            var expectedValue = (askPrice + bidPrice) / 2;
            if (askPrice == 0 || bidPrice == 0)
            {
                expectedValue = askPrice + bidPrice;
            }

            var ms = (tick.Time - baseDate).TotalMilliseconds;
            Assert.AreEqual(14400135, ms);
            Assert.AreEqual(expectedValue, tick.Value);
            Assert.AreEqual(expectedValue, tick.LastPrice);
            Assert.AreEqual(0, tick.Quantity);
            Assert.AreEqual(askPrice, tick.AskPrice);
            Assert.AreEqual(askSize, tick.AskSize);
            Assert.AreEqual(bidPrice, tick.BidPrice);
            Assert.AreEqual(bidSize, tick.BidSize);
            Assert.AreEqual("NASDAQ", tick.Exchange);
            Assert.AreEqual("00000001", tick.SaleCondition);
            Assert.IsFalse(tick.Suspicious);
        }

        [Test]
        public void OptionWithUnderlyingEquityScaled()
        {
            var factory = new Tick();
            var tickLine = "40560000,10000,10,NYSE,00000001,0";
            var underlying = Symbol.Create("SPY", SecurityType.Equity, QuantConnect.Market.USA);
            var optionSymbol = Symbol.CreateOption(
                underlying,
                QuantConnect.Market.USA,
                OptionStyle.American,
                OptionRight.Put,
                4200m,
                SecurityIdentifier.DefaultDate);

            var config = new SubscriptionDataConfig(
                typeof(Tick),
                optionSymbol,
                Resolution.Tick,
                TimeZones.Chicago,
                TimeZones.Chicago,
                true,
                false,
                false,
                false,
                TickType.Trade,
                true,
                DataNormalizationMode.Raw);

            var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(tickLine)));

            var tickFromLine = (Tick)factory.Reader(config, tickLine, new DateTime(2020, 9, 22), false);
            var tickFromStream = (Tick)factory.Reader(config, stream, new DateTime(2020, 9, 22), false);

            Assert.AreEqual(new DateTime(2020, 9, 22, 11, 16, 0), tickFromLine.Time);
            Assert.AreEqual(1m, tickFromLine.Price);
            Assert.AreEqual(10, tickFromLine.Quantity);

            Assert.AreEqual(new DateTime(2020, 9, 22, 11, 16, 0), tickFromStream.Time);
            Assert.AreEqual(1m, tickFromStream.Price);
            Assert.AreEqual(10, tickFromStream.Quantity);
        }

        [Test]
        public void OptionWithUnderlyingFutureNotScaled()
        {
            var factory = new Tick();
            var tickLine = "40560000,10000,10,CME,00000001,0";
            var underlying = Symbol.CreateFuture("ES", QuantConnect.Market.CME, new DateTime(2021, 3, 19));
            var optionSymbol = Symbol.CreateOption(
                underlying,
                QuantConnect.Market.CME,
                OptionStyle.American,
                OptionRight.Put,
                4200m,
                SecurityIdentifier.DefaultDate);

            var config = new SubscriptionDataConfig(
                typeof(Tick),
                optionSymbol,
                Resolution.Tick,
                TimeZones.Chicago,
                TimeZones.Chicago,
                true,
                false,
                false,
                false,
                TickType.Trade,
                true,
                DataNormalizationMode.Raw);

            var stream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(tickLine)));

            var tickFromLine = (Tick)factory.Reader(config, tickLine, new DateTime(2020, 9, 22), false);
            var tickFromStream = (Tick)factory.Reader(config, stream, new DateTime(2020, 9, 22), false);

            Assert.AreEqual(new DateTime(2020, 9, 22, 11, 16, 0), tickFromLine.Time);
            Assert.AreEqual(10000m, tickFromLine.Price);
            Assert.AreEqual(10, tickFromLine.Quantity);

            Assert.AreEqual(new DateTime(2020, 9, 22, 11, 16, 0), tickFromStream.Time);
            Assert.AreEqual(10000m, tickFromStream.Price);
            Assert.AreEqual(10, tickFromStream.Quantity);
        }

        [Test]
        public void ExchangeSetterHandlesNonExpectedEncoding()
        {
            const string line = "15093000,1456300,100,P,T,0";

            var baseDate = new DateTime(2013, 10, 08);
            var tick = new Tick(Symbols.SPY, line, baseDate);
            Assert.DoesNotThrow(()=> tick.ExchangeCode = "LL");
            Assert.AreEqual(Exchange.UNKNOWN, tick.Exchange.GetPrimaryExchange(), "Failed at Exchange Property");
            Assert.AreEqual((string)Exchange.UNKNOWN, tick.ExchangeCode, "Failed at ExchangeCode Property");
        }

        [Test]
        public void ExchangeSetterHandlesDefinedExchanges()
        {
            var baseDate = new DateTime(2013, 10, 08);
            const string line = "15093000,1456300,100,P,T,0";

            var exchanges = typeof(Exchange)
                .GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                .Where(p => p.PropertyType == typeof(Exchange))
                .Select(propa => propa.GetValue(null))
                .OfType<Exchange>()
                .Where(exchange => exchange.Market == QuantConnect.Market.USA && exchange.SecurityTypes.Contains(SecurityType.Equity))
                .ToList();

            Assert.GreaterOrEqual(exchanges.Count, 20);

            foreach (var exchange in exchanges)
            {
                {
                    var tick = new Tick(Symbols.SPY, line, baseDate);
                    Assert.DoesNotThrow(() => tick.ExchangeCode = exchange.Code);

                    Assert.AreEqual(exchange.Name, tick.Exchange, $"ExchangeCode: Failed at Exchange Property: {exchange}");
                    Assert.AreEqual(exchange.Code, tick.ExchangeCode, $"ExchangeCode: Failed at ExchangeCode Property: {exchange}");
                }
                {
                    var tick = new Tick(Symbols.SPY, line, baseDate);
                    Assert.DoesNotThrow(() => tick.Exchange = exchange);

                    Assert.AreEqual(exchange.Name, tick.Exchange, $"Exchange: Failed at Exchange Property: {exchange}");
                    Assert.AreEqual(exchange.Code, tick.ExchangeCode, $"Exchange: Failed at ExchangeCode Property: {exchange}");
                }
            }
        }
    }
}
