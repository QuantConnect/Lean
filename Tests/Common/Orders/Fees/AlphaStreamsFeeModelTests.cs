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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Cfd;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    class AlphaStreamsFeeModelTests
    {
        private decimal _liborRate = 0.024m;
        private readonly IFeeModel _feeModel = new AlphaStreamsFeeModel(0.024m);

        [Test]
        public void USAEquityFeeInUSD()
        {
            var security = SecurityTests.GetSecurity();
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual((0.004m + _liborRate) * security.Price * 1000, fee.Value.Amount);
        }

        [Test]
        public void USAFutureFee()
        {
            var tz = TimeZones.NewYork;
            var security = new Future(Symbols.Fut_SPY_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USD", 0, 0),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(1000 * 0.50m, fee.Value.Amount);
        }

        [Test]
        public void USAOptionFee()
        {
            var tz = TimeZones.NewYork;
            var security = new Option(Symbols.SPY_C_192_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USD", 0, 0),
                new OptionSymbolProperties(SymbolProperties.GetDefault("USD")),
                ErrorCurrencyConverter.Instance
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(0.50m * 1000, fee.Value.Amount);
        }

        [Test]
        public void USAOptionMinimumFee()
        {
            var tz = TimeZones.NewYork;
            var security = new Option(Symbols.SPY_C_192_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USD", 0, 0),
                new OptionSymbolProperties(SymbolProperties.GetDefault("USD")),
                ErrorCurrencyConverter.Instance
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(0.50m, fee.Value.Amount);
        }

        [Test]
        public void ForexFeeUSD()
        {
            var tz = TimeZones.NewYork;
            var security = new Forex(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USD", 0, 1),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.EURUSD, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("EURUSD", "USD", 1, 0.01m, 0.00000001m),
                ErrorCurrencyConverter.Instance
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(0.000002m * security.Price * 1000, fee.Value.Amount);
        }

        [Test]
        public void ForexFee_NonUSD()
        {
            var tz = TimeZones.NewYork;
            var security = new Forex(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("GBP", 0, 1.2m),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.EURGBP, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("EURGBP", "GBP", 1, 0.01m, 0.00000001m),
                ErrorCurrencyConverter.Instance
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1000000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(0.000002m * security.Price * 1000000 * 1.2m, fee.Value.Amount);
        }

        // Add USD Forex Test

        [Test]
        public void GetOrderFeeThrowsForUnsupportedSecurityType()
        {
            Assert.Throws<ArgumentException>(
                () =>
                {
                    var tz = TimeZones.NewYork;
                    var security = new Cfd(
                        SecurityExchangeHours.AlwaysOpen(tz),
                        new Cash("EUR", 0, 0),
                        new SubscriptionDataConfig(typeof(QuoteBar), Symbols.DE30EUR, Resolution.Minute, tz, tz, true, false, false),
                        new SymbolProperties("DE30EUR", "EUR", 1, 0.01m, 1m),
                        ErrorCurrencyConverter.Instance
                    );
                    security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 12000, 12000));

                    _feeModel.GetOrderFee(
                        new OrderFeeParameters(
                            security,
                            new MarketOrder(security.Symbol, 1, DateTime.UtcNow)
                        )
                    );
                });
        }
    }
}
