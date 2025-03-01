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
using NUnit.Framework;
using Newtonsoft.Json;
using QuantConnect.Securities;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Engine.DataFeeds;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class HoldingTests
    {
        [TestCase(SecurityType.Future)]
        [TestCase(SecurityType.Equity)]
        [TestCase(SecurityType.Crypto)]
        [TestCase(SecurityType.Forex)]
        public void PriceRounding(SecurityType securityType)
        {
            var algo = new AlgorithmStub();
            Security security = null;
            if (securityType == SecurityType.Equity)
            {
                security = algo.AddEquity("SPY");
            }
            else if (securityType == SecurityType.Crypto)
            {
                security = algo.AddCrypto("BNTBTC");
            }
            else if (securityType == SecurityType.Forex)
            {
                security = algo.AddForex("EURUSD");
            }
            else if (securityType == SecurityType.Future)
            {
                security = algo.AddFuture("CL");
            }
            security.SetMarketPrice(new Tick(new DateTime(2022, 01, 04), security.Symbol, 10.0001m, 10.0001m));
            security.Holdings.SetHoldings(10.0000000000m, 10);

            var holding = new Holding(security);

            Assert.AreEqual(10, holding.Quantity);

            if (securityType == SecurityType.Equity || securityType == SecurityType.Future)
            {
                Assert.AreEqual(10.00m, holding.MarketPrice);
                Assert.AreEqual(10.00m, holding.AveragePrice);
            }
            else if(securityType == SecurityType.Forex || securityType == SecurityType.Crypto)
            {
                Assert.AreEqual(10.0001m, holding.MarketPrice);
                Assert.AreEqual(10.0000m, holding.AveragePrice);
            }
        }

        [Test]
        public void RoundTrip()
        {
            var algo = new AlgorithmStub();
            var security = algo.AddEquity("SPY");
            security.SetMarketPrice(new Tick(new DateTime(2022, 01, 04), security.Symbol, 10.0001m, 10.0001m));
            security.Holdings.SetHoldings(10.1000000000m, 10);

            var holding = new Holding(security);

            var result = JsonConvert.SerializeObject(holding);

            Assert.AreEqual("{\"a\":10.1,\"q\":10,\"p\":10,\"v\":100,\"u\":-2,\"up\":-1.98}", result);

            var deserialized = JsonConvert.DeserializeObject<Holding>(result);

            Assert.AreEqual(deserialized.AveragePrice, holding.AveragePrice);
            Assert.AreEqual(deserialized.Quantity, holding.Quantity);
            Assert.AreEqual(deserialized.MarketPrice, holding.MarketPrice);
            Assert.AreEqual(deserialized.MarketValue, holding.MarketValue);
            Assert.AreEqual(deserialized.UnrealizedPnL, holding.UnrealizedPnL);
            Assert.AreEqual(deserialized.UnrealizedPnLPercent, holding.UnrealizedPnLPercent);

            Assert.AreEqual(": 10 @ $10.1 - Market: $10", deserialized.ToString());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void BackwardsCompatible(bool upperCase)
        {
            string source;
            if (upperCase)
            {
                source = "{\"Symbol\":{\"value\":\"A\",\"id\":\"A RPTMYV3VC57P\",\"permtick\":\"A\"},\"Type\":1,\"CurrencySymbol\":\"$\",\"AveragePrice\":148.34,\"Quantity\":192.0," +
                    "\"MarketPrice\":145.21,\"ConversionRate\":1.0,\"MarketValue\":27880.3200,\"UnrealizedPnl\":-601.96,\"UnrealizedPnLPercent\":-2.11}";
            }
            else
            {
                source = "{\"symbol\":{\"value\":\"A\",\"id\":\"A RPTMYV3VC57P\",\"permtick\":\"A\"},\"type\":1,\"currencySymbol\":\"$\",\"averagePrice\":148.34,\"quantity\":192.0," +
                    "\"marketPrice\":145.21,\"conversionRate\":1.0,\"marketValue\":27880.3200,\"unrealizedPnl\":-601.96,\"unrealizedPnLPercent\":-2.11}";
            }
            var deserialized = JsonConvert.DeserializeObject<Holding>(source);

            Assert.IsNotNull(deserialized.Symbol);
            Assert.AreEqual("A", deserialized.Symbol.Value);
            Assert.AreEqual(148.34, deserialized.AveragePrice);
            Assert.AreEqual(192, deserialized.Quantity);
            Assert.AreEqual(145.21, deserialized.MarketPrice);
            Assert.AreEqual(27880, deserialized.MarketValue);
            Assert.AreEqual(-601.96, deserialized.UnrealizedPnL);
            Assert.AreEqual(-2.11, deserialized.UnrealizedPnLPercent);
        }

        [Test]
        public void DefaultHoldings()
        {
            var algo = new AlgorithmStub();
            var security = algo.AddEquity("SPY");
            var holding = new Holding(security);

            var result = JsonConvert.SerializeObject(new Dictionary<string, Holding> { { security.Symbol.ID.ToString(), holding } });

            Assert.AreEqual("{\"SPY R735QTJ8XC9X\":{}}", result);

            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, Holding>>(result);
            Assert.AreEqual(0, deserialized[security.Symbol.ID.ToString()].AveragePrice);
        }
    }
}
