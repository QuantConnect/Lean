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
using Moq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Cfd;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.FutureOption;
using QuantConnect.Securities.Option;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class InteractiveBrokersTieredFeeModelTests
    {
        private static IFeeModel GetFeeModel(int tier = 0)
        {
            switch(tier)
            {
                case 1:
                    return new InteractiveBrokersTieredFeeModel(500000m, 1500m, 1.5e9m, 25000m, 500000m);
                case 2:
                    return new InteractiveBrokersTieredFeeModel(5000000m, 15000m, 2.5e9m, 75000m, 1500000m);
                case 3:
                    return new InteractiveBrokersTieredFeeModel(50000000m, 150000m, 5.5e9m, 150000m, 1500000m);
                case 4:
                    return new InteractiveBrokersTieredFeeModel(500000000m, 150000m, 5.5e9m, 150000m, 1500000m);
                default:
                    return new InteractiveBrokersTieredFeeModel();
            }
        }

        [Test]
        public void USAEquityMinimumFeeInUSD()
        {
            var security = new Equity(
                Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 1, 1));

            var model = GetFeeModel();
            var fee = model.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(0.3534718m, fee.Value.Amount);
        }

        [TestCase(0, 0.1, OrderType.Market, 1.55134)]
        [TestCase(0, 0.1, OrderType.MarketOnOpen, 1.55134)]
        [TestCase(0, 0.1, OrderType.MarketOnClose, 1.55134)]
        [TestCase(0, 100, OrderType.Market, 306.52996)]
        [TestCase(0, 100, OrderType.MarketOnOpen, 306.52996)]
        [TestCase(0, 100, OrderType.MarketOnClose, 306.52996)]
        [TestCase(1, 0.1, OrderType.Market, 1.55134)]
        [TestCase(1, 0.1, OrderType.MarketOnOpen, 1.55134)]
        [TestCase(1, 0.1, OrderType.MarketOnClose, 1.55134)]
        [TestCase(1, 100, OrderType.Market, 305.02912)]
        [TestCase(1, 100, OrderType.MarketOnOpen, 305.02912)]
        [TestCase(1, 100, OrderType.MarketOnClose, 305.02912)]
        [TestCase(2, 0.1, OrderType.Market, 1.55134)]
        [TestCase(2, 0.1, OrderType.MarketOnOpen, 1.55134)]
        [TestCase(2, 0.1, OrderType.MarketOnClose, 1.55134)]
        [TestCase(2, 100, OrderType.Market, 304.52884)]
        [TestCase(2, 100, OrderType.MarketOnOpen, 304.52884)]
        [TestCase(2, 100, OrderType.MarketOnClose, 304.52884)]
        [TestCase(3, 0.1, OrderType.Market, 1.55134)]
        [TestCase(3, 0.1, OrderType.MarketOnOpen, 1.55134)]
        [TestCase(3, 0.1, OrderType.MarketOnClose, 1.55134)]
        [TestCase(3, 100, OrderType.Market, 304.02856)]
        [TestCase(3, 100, OrderType.MarketOnOpen, 304.02856)]
        [TestCase(3, 100, OrderType.MarketOnClose, 304.02856)]
        [TestCase(4, 0.1, OrderType.Market, 1.05106)]
        [TestCase(4, 0.1, OrderType.MarketOnOpen, 1.05106)]
        [TestCase(4, 0.1, OrderType.MarketOnClose, 1.05106)]
        [TestCase(4, 100, OrderType.Market, 303.52828)]
        [TestCase(4, 100, OrderType.MarketOnOpen, 303.52828)]
        [TestCase(4, 100, OrderType.MarketOnClose, 303.52828)]
        public void USAEquityFeeInUsdByTiers(int tier, decimal price, OrderType orderType, decimal expectedFee)
        {
            var security = new Equity(
                Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, price, price));

            Order order;
            switch (orderType)
            {
                case OrderType.MarketOnOpen:
                    order = new MarketOnOpenOrder(security.Symbol, 1000, DateTime.UtcNow);
                    break;
                    
                case OrderType.MarketOnClose:
                    order = new MarketOnCloseOrder(security.Symbol, 1000, DateTime.UtcNow);
                    break;

                default:
                    order = new MarketOrder(security.Symbol, 1000, DateTime.UtcNow);
                    break;
            }
            var model = GetFeeModel(tier);
            var fee = model.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    order
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(expectedFee, fee.Value.Amount);
        }

        [TestCaseSource(nameof(USAFuturesFeeTestCases))]
        public void USAFutureFeeByTier(int tier, Symbol symbol, decimal expectedFee)
        {
            var tz = TimeZones.NewYork;
            var future = new Future(symbol,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USD", 0, 0),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());
            var security = (Security) (symbol.SecurityType == SecurityType.Future
                ? future
                : new FutureOption(symbol,
                    SecurityExchangeHours.AlwaysOpen(tz),
                    new Cash("USD", 0, 0),
                    new OptionSymbolProperties(SymbolProperties.GetDefault("USD")),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache(),
                    future));
            var time = new DateTime(2022, 8, 18);
            security.SetMarketPrice(new Tick(time, security.Symbol, 100, 100));
            var model = GetFeeModel(tier);
            var fee = model.GetOrderFee(new OrderFeeParameters(security, new MarketOrder(security.Symbol, 1000, time)));

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(1000 * expectedFee, fee.Value.Amount);
        }

        [TestCase("USD", 70000, 0.00002 * 70000)]
        [TestCase("USD", 100000, 0.00002 * 100000)]
        [TestCase("USD", 10000, 1)] // The calculated fee will be under 1, but the minimum fee is 1 USD
        [TestCase("JPY", 3000000, 0.00002 * 3000000)]
        [TestCase("JPY", 1000000, 40)]// The calculated fee will be under 40, but the minimum fee is 40 JPY
        [TestCase("HKD", 600000, 0.00002 * 600000)]
        [TestCase("HKD", 200000, 10)]// The calculated fee will be under 10, but the minimum fee is 10 HKD
        public void CalculatesCFDFee(string quoteCurrency, decimal price, decimal expectedFee)
        {
            var security = new Cfd(Symbols.DE10YBEUR,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(quoteCurrency, 0, 0),
                SymbolProperties.GetDefault(quoteCurrency),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache());
            security.QuoteCurrency.ConversionRate = 1;


            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, price, price));

            var order = new MarketOrder(security.Symbol, 1, DateTime.UtcNow);
            var model = GetFeeModel();
            var fee = model.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(quoteCurrency, fee.Value.Currency);
            Assert.AreEqual(expectedFee, fee.Value.Amount);
        }

        // Tier 1
        [TestCase(0, OrderType.ComboMarket, 0.01, 293.95)]
        [TestCase(0, OrderType.ComboLimit, 0.01, 293.95)]
        [TestCase(0, OrderType.ComboLegLimit, 0.01, 293.95)]
        [TestCase(0, OrderType.Limit, 0.01, 293.95)]
        [TestCase(0, OrderType.StopLimit, 0.01, 293.95)]
        [TestCase(0, OrderType.LimitIfTouched, 0.01, 293.95)]
        [TestCase(0, OrderType.StopMarket, 0.01, 293.95)]
        [TestCase(0, OrderType.TrailingStop, 0.01, 293.95)]
        [TestCase(0, OrderType.Market, 0.01, 293.95)]
        [TestCase(0, OrderType.MarketOnClose, 0.01, 293.95)]
        [TestCase(0, OrderType.MarketOnOpen, 0.01, 293.95)]
        [TestCase(0, OrderType.ComboMarket, 0.2, 693.95)]
        [TestCase(0, OrderType.ComboLimit, 0.2, 693.95)]
        [TestCase(0, OrderType.ComboLegLimit, 0.2, 693.95)]
        [TestCase(0, OrderType.Limit, 0.2, 693.95)]
        [TestCase(0, OrderType.StopLimit, 0.2, 693.95)]
        [TestCase(0, OrderType.LimitIfTouched, 0.2, 693.95)]
        [TestCase(0, OrderType.StopMarket, 0.2, 693.95)]
        [TestCase(0, OrderType.TrailingStop, 0.2, 693.95)]
        [TestCase(0, OrderType.Market, 0.2, 693.95)]
        [TestCase(0, OrderType.MarketOnClose, 0.2, 693.95)]
        [TestCase(0, OrderType.MarketOnOpen, 0.2, 693.95)]
        [TestCase(0, OrderType.ComboMarket, 0.07, 543.95)]
        [TestCase(0, OrderType.ComboLimit, 0.07, 543.95)]
        [TestCase(0, OrderType.ComboLegLimit, 0.07, 543.95)]
        [TestCase(0, OrderType.Limit, 0.07, 543.95)]
        [TestCase(0, OrderType.StopLimit, 0.07, 543.95)]
        [TestCase(0, OrderType.LimitIfTouched, 0.07, 543.95)]
        [TestCase(0, OrderType.StopMarket, 0.07, 543.95)]
        [TestCase(0, OrderType.TrailingStop, 0.07, 543.95)]
        [TestCase(0, OrderType.Market, 0.07, 543.95)]
        [TestCase(0, OrderType.MarketOnClose, 0.07, 543.95)]
        [TestCase(0, OrderType.MarketOnOpen, 0.07, 543.95)]
        [TestCase(0, OrderType.ComboMarket, -0.01, 293.95)]
        [TestCase(0, OrderType.ComboLimit, -0.01, 293.95)]
        [TestCase(0, OrderType.ComboLegLimit, -0.01, 293.95)]
        [TestCase(0, OrderType.Limit, -0.01, 293.95)]
        [TestCase(0, OrderType.StopLimit, -0.01, 293.95)]
        [TestCase(0, OrderType.LimitIfTouched, -0.01, 293.95)]
        [TestCase(0, OrderType.StopMarket, -0.01, 293.95)]
        [TestCase(0, OrderType.TrailingStop, -0.01, 293.95)]
        [TestCase(0, OrderType.Market, -0.01, 293.95)]
        [TestCase(0, OrderType.MarketOnClose, -0.01, 293.95)]
        [TestCase(0, OrderType.MarketOnOpen, -0.01, 293.95)]
        // Tier 2
        [TestCase(1, OrderType.Market, 0.01, 293.95)]
        [TestCase(1, OrderType.Market, 0.2, 543.95)]
        [TestCase(1, OrderType.Market, 0.07, 543.95)]
        [TestCase(1, OrderType.Market, -0.01, 293.95)]
        // Tier 3
        [TestCase(2, OrderType.Market, 0.01, 293.95)]
        [TestCase(2, OrderType.Market, 0.2, 293.95)]
        [TestCase(2, OrderType.Market, 0.07, 293.95)]
        [TestCase(2, OrderType.Market, -0.01, 293.95)]
        // Tier 4
        [TestCase(3, OrderType.Market, 0.01, 193.95)]
        [TestCase(3, OrderType.Market, 0.2, 193.95)]
        [TestCase(3, OrderType.Market, 0.07, 193.95)]
        [TestCase(3, OrderType.Market, -0.01, 193.95)]
        public void USAOptionFeeByTier(int tier, OrderType orderType, double price, double expectedFees)
        {
            var optionPrice = (decimal)price;
            var tz = TimeZones.NewYork;
            var security = new Option(Symbols.SPY_C_192_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USD", 0, 0),
                new OptionSymbolProperties(SymbolProperties.GetDefault("USD")),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache(),
                null
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, optionPrice, 0));
            var order = (new Mock<Order>()).Object;
            var groupOrderManager = new GroupOrderManager(0, 2, 10);

            switch (orderType)
            {
                case OrderType.ComboMarket:
                    order = new ComboMarketOrder(security.Symbol, 1000, DateTime.UtcNow, groupOrderManager);
                    break;
                case OrderType.ComboLimit:
                    order = new ComboLimitOrder(security.Symbol, 1000, optionPrice, DateTime.UtcNow, groupOrderManager);
                    break;
                case OrderType.ComboLegLimit:
                    order = new ComboLegLimitOrder(security.Symbol, 1000, optionPrice, DateTime.UtcNow, groupOrderManager);
                    break;
                case OrderType.Limit:
                    order = new LimitOrder(security.Symbol, 1000, optionPrice, DateTime.UtcNow);
                    break;
                case OrderType.StopLimit:
                    order = new StopLimitOrder(security.Symbol, 1000, optionPrice, optionPrice, DateTime.UtcNow);
                    break;
                case OrderType.LimitIfTouched:
                    order = new LimitIfTouchedOrder(security.Symbol, 1000, optionPrice, optionPrice, DateTime.UtcNow);
                    break;
                case OrderType.StopMarket:
                    order = new StopMarketOrder(security.Symbol, 1000, optionPrice, DateTime.UtcNow);
                    break;
                case OrderType.TrailingStop:
                    order = new TrailingStopOrder(security.Symbol, 1000, optionPrice, optionPrice, false, DateTime.UtcNow);
                    break;
                case OrderType.Market:
                    order = new MarketOrder(security.Symbol, 1000, DateTime.UtcNow);
                    break;
                case OrderType.MarketOnClose:
                    order = new MarketOnCloseOrder(security.Symbol, 1000, DateTime.UtcNow);
                    break;
                case OrderType.MarketOnOpen:
                    order = new MarketOnOpenOrder(security.Symbol, 1000, DateTime.UtcNow);
                    break;
            }

            var model = GetFeeModel(tier);
            var fee = model.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    order
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual((decimal)expectedFees, fee.Value.Amount);
        }

        [Test]
        public void USAOptionMinimumFee()
        {
            var tz = TimeZones.NewYork;
            var security = new Option(Symbols.SPY_C_192_Feb19_2016,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USD", 0, 0),
                new OptionSymbolProperties(SymbolProperties.GetDefault("USD")),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache(),
                null
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var model = GetFeeModel();
            var fee = model.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(1.04395m, fee.Value.Amount);
        }

        [TestCase(0, 100, 2)]
        [TestCase(1, 100, 1.5)]
        [TestCase(2, 100, 1.25)]
        [TestCase(3, 100, 1)]
        [TestCase(0, 10000000, 40000)]
        [TestCase(1, 10000000, 30000)]
        [TestCase(2, 10000000, 20000)]
        [TestCase(3, 10000000, 16000)]
        public void ForexFeeByTier(int tier, decimal orderSize, decimal expectedFee)
        {
            var tz = TimeZones.NewYork;
            var security = new Forex(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("GBP", 0, 2),
                new Cash("EUR", 0, 0),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.EURGBP, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("EURGBP", "GBP", 1, 0.01m, 0.00000001m, string.Empty),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var model = GetFeeModel(tier);
            var fee = model.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, orderSize, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(expectedFee, fee.Value.Amount);
        }

        [TestCase(0, 1, 1)]
        [TestCase(1, 1, 1)]
        [TestCase(2, 1, 1)]
        [TestCase(0, 2, 1.75)]
        [TestCase(1, 2, 1.75)]
        [TestCase(2, 2, 1.75)]
        [TestCase(0, 100, 18)]
        [TestCase(1, 100, 15)]
        [TestCase(2, 100, 12)]
        public void CryptoFeeByTier(int tier, decimal orderSize, decimal expectedFee)
        {
            var tz = TimeZones.NewYork;
            var security = new Crypto(
                Symbols.BTCUSD,
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("USD", 0, 1),
                new Cash("BTC", 0, 0),
                SymbolProperties.GetDefault("USD"),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var model = GetFeeModel(tier);
            var fee = model.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, orderSize, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(expectedFee, fee.Value.Amount);
        }

        [Test]
        public void GetOrderFeeThrowsForUnsupportedSecurityType()
        {
            Assert.Throws<ArgumentException>(
                () =>
                {
                    var tz = TimeZones.NewYork;
                    var security = new CryptoFuture(
                        Symbols.BTCUSD_Future,
                        SecurityExchangeHours.AlwaysOpen(tz),
                        new Cash("USD", 0, 0),
                        new Cash("BTC", 0, 0),
                        SymbolProperties.GetDefault("USD"),
                        ErrorCurrencyConverter.Instance,
                        RegisteredSecurityDataTypesProvider.Null,
                        new SecurityCache()
                    );
                    var date = new DateTime(2018, 2, 1);
                    security.SetMarketPrice(new Tick(date, security.Symbol, 12000, 12000));

                    GetFeeModel().GetOrderFee(
                        new OrderFeeParameters(
                            security,
                            new MarketOrder(security.Symbol, 1, date)
                        )
                    );
                });
        }

        [Test]
        public void MonthlyRollingTierChangeTest()
        {
            var security = new Equity(
                Symbols.SPY,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            // Tier 1
            var feeModel = GetFeeModel();
            var fee = feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 300000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(91958.988m, fee.Value.Amount);

            // Tier 2
            fee = feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 3000000 - 300000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(827630.892m, fee.Value.Amount);

            // Tier 3
            fee = feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 20000000 - 3000000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(5185484.3m, fee.Value.Amount);

            // Tier 4
            fee = feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 100000000 - 20000000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(24362248.3, fee.Value.Amount);

            // Tier 5
            fee = feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 300000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(91208.568, fee.Value.Amount);

            // Reset to tier 1 on next month
            fee = feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 300000, DateTime.UtcNow.AddMonths(1))      // Roll 1 month
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(91958.988m, fee.Value.Amount);
        }

        private static TestCaseData[] USAFuturesFeeTestCases()
        {
            return new[]
            {
                // E-mini Futures
                new { Tier = 0, Symbol = Futures.Indices.Dow30EMini, Type = SecurityType.Future, ExpectedFee = 2.13m },
                new { Tier = 0, Symbol = Futures.Indices.Russell2000EMini, Type = SecurityType.Future, ExpectedFee = 2.13m },
                new { Tier = 0, Symbol = Futures.Indices.SP500EMini, Type = SecurityType.Future, ExpectedFee = 2.13m },
                new { Tier = 0, Symbol = Futures.Indices.NASDAQ100EMini, Type = SecurityType.Future, ExpectedFee = 2.13m },
                new { Tier = 1, Symbol = Futures.Indices.Dow30EMini, Type = SecurityType.Future, ExpectedFee = 1.93m },
                new { Tier = 1, Symbol = Futures.Indices.Russell2000EMini, Type = SecurityType.Future, ExpectedFee = 1.93m },
                new { Tier = 1, Symbol = Futures.Indices.SP500EMini, Type = SecurityType.Future, ExpectedFee = 1.93m },
                new { Tier = 1, Symbol = Futures.Indices.NASDAQ100EMini, Type = SecurityType.Future, ExpectedFee = 1.93m },
                new { Tier = 2, Symbol = Futures.Indices.Dow30EMini, Type = SecurityType.Future, ExpectedFee = 1.73m },
                new { Tier = 2, Symbol = Futures.Indices.Russell2000EMini, Type = SecurityType.Future, ExpectedFee = 1.73m },
                new { Tier = 2, Symbol = Futures.Indices.SP500EMini, Type = SecurityType.Future, ExpectedFee = 1.73m },
                new { Tier = 2, Symbol = Futures.Indices.NASDAQ100EMini, Type = SecurityType.Future, ExpectedFee = 1.73m },
                new { Tier = 3, Symbol = Futures.Indices.Dow30EMini, Type = SecurityType.Future, ExpectedFee = 1.53m },
                new { Tier = 3, Symbol = Futures.Indices.Russell2000EMini, Type = SecurityType.Future, ExpectedFee = 1.53m },
                new { Tier = 3, Symbol = Futures.Indices.SP500EMini, Type = SecurityType.Future, ExpectedFee = 1.53m },
                new { Tier = 3, Symbol = Futures.Indices.NASDAQ100EMini, Type = SecurityType.Future, ExpectedFee = 1.53m },
                // E-mini Future options
                new { Tier = 0, Symbol = Futures.Indices.Dow30EMini, Type = SecurityType.FutureOption, ExpectedFee = 1.4m },
                new { Tier = 0, Symbol = Futures.Indices.Russell2000EMini, Type = SecurityType.FutureOption, ExpectedFee = 1.4m },
                new { Tier = 0, Symbol = Futures.Indices.SP500EMini, Type = SecurityType.FutureOption, ExpectedFee = 1.4m },
                new { Tier = 0, Symbol = Futures.Indices.NASDAQ100EMini, Type = SecurityType.FutureOption, ExpectedFee = 1.4m },
                new { Tier = 1, Symbol = Futures.Indices.Dow30EMini, Type = SecurityType.FutureOption, ExpectedFee = 1.2m },
                new { Tier = 1, Symbol = Futures.Indices.Russell2000EMini, Type = SecurityType.FutureOption, ExpectedFee = 1.2m },
                new { Tier = 1, Symbol = Futures.Indices.SP500EMini, Type = SecurityType.FutureOption, ExpectedFee = 1.2m },
                new { Tier = 1, Symbol = Futures.Indices.NASDAQ100EMini, Type = SecurityType.FutureOption, ExpectedFee = 1.2m },
                new { Tier = 2, Symbol = Futures.Indices.Dow30EMini, Type = SecurityType.FutureOption, ExpectedFee = 1m },
                new { Tier = 2, Symbol = Futures.Indices.Russell2000EMini, Type = SecurityType.FutureOption, ExpectedFee = 1m },
                new { Tier = 2, Symbol = Futures.Indices.SP500EMini, Type = SecurityType.FutureOption, ExpectedFee = 1m },
                new { Tier = 2, Symbol = Futures.Indices.NASDAQ100EMini, Type = SecurityType.FutureOption, ExpectedFee = 1m },
                new { Tier = 3, Symbol = Futures.Indices.Dow30EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.8m },
                new { Tier = 3, Symbol = Futures.Indices.Russell2000EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.8m },
                new { Tier = 3, Symbol = Futures.Indices.SP500EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.8m },
                new { Tier = 3, Symbol = Futures.Indices.NASDAQ100EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.8m },
                // Micro E-mini Futures
                new { Tier = 0, Symbol = Futures.Indices.MicroDow30EMini, Type = SecurityType.Future, ExpectedFee = 0.55m },
                new { Tier = 0, Symbol = Futures.Indices.MicroRussell2000EMini, Type = SecurityType.Future, ExpectedFee = 0.55m },
                new { Tier = 0, Symbol = Futures.Indices.MicroSP500EMini, Type = SecurityType.Future, ExpectedFee = 0.55m },
                new { Tier = 0, Symbol = Futures.Indices.MicroNASDAQ100EMini, Type = SecurityType.Future, ExpectedFee = 0.55m },
                new { Tier = 0, Symbol = Futures.Financials.MicroY2TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.55m },
                new { Tier = 0, Symbol = Futures.Financials.MicroY5TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.55m },
                new { Tier = 0, Symbol = Futures.Financials.MicroY10TreasuryNote, Type = SecurityType.Future, ExpectedFee = 0.55m },
                new { Tier = 0, Symbol = Futures.Financials.MicroY30TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.55m },
                new { Tier = 0, Symbol = Futures.Metals.MicroGold, Type = SecurityType.Future, ExpectedFee = 0.55m },
                new { Tier = 0, Symbol = Futures.Metals.MicroSilver, Type = SecurityType.Future, ExpectedFee = 0.55m },
                new { Tier = 0, Symbol = Futures.Energy.MicroCrudeOilWTI, Type = SecurityType.Future, ExpectedFee = 0.55m },
                new { Tier = 1, Symbol = Futures.Indices.MicroDow30EMini, Type = SecurityType.Future, ExpectedFee = 0.5m },
                new { Tier = 1, Symbol = Futures.Indices.MicroRussell2000EMini, Type = SecurityType.Future, ExpectedFee = 0.5m },
                new { Tier = 1, Symbol = Futures.Indices.MicroSP500EMini, Type = SecurityType.Future, ExpectedFee = 0.5m },
                new { Tier = 1, Symbol = Futures.Indices.MicroNASDAQ100EMini, Type = SecurityType.Future, ExpectedFee = 0.5m },
                new { Tier = 1, Symbol = Futures.Financials.MicroY2TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.5m },
                new { Tier = 1, Symbol = Futures.Financials.MicroY5TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.5m },
                new { Tier = 1, Symbol = Futures.Financials.MicroY10TreasuryNote, Type = SecurityType.Future, ExpectedFee = 0.5m },
                new { Tier = 1, Symbol = Futures.Financials.MicroY30TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.5m },
                new { Tier = 1, Symbol = Futures.Metals.MicroGold, Type = SecurityType.Future, ExpectedFee = 0.5m },
                new { Tier = 1, Symbol = Futures.Metals.MicroSilver, Type = SecurityType.Future, ExpectedFee = 0.5m },
                new { Tier = 1, Symbol = Futures.Energy.MicroCrudeOilWTI, Type = SecurityType.Future, ExpectedFee = 0.5m },
                new { Tier = 2, Symbol = Futures.Indices.MicroDow30EMini, Type = SecurityType.Future, ExpectedFee = 0.45m },
                new { Tier = 2, Symbol = Futures.Indices.MicroRussell2000EMini, Type = SecurityType.Future, ExpectedFee = 0.45m },
                new { Tier = 2, Symbol = Futures.Indices.MicroSP500EMini, Type = SecurityType.Future, ExpectedFee = 0.45m },
                new { Tier = 2, Symbol = Futures.Indices.MicroNASDAQ100EMini, Type = SecurityType.Future, ExpectedFee = 0.45m },
                new { Tier = 2, Symbol = Futures.Financials.MicroY2TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.45m },
                new { Tier = 2, Symbol = Futures.Financials.MicroY5TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.45m },
                new { Tier = 2, Symbol = Futures.Financials.MicroY10TreasuryNote, Type = SecurityType.Future, ExpectedFee = 0.45m },
                new { Tier = 2, Symbol = Futures.Financials.MicroY30TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.45m },
                new { Tier = 2, Symbol = Futures.Metals.MicroGold, Type = SecurityType.Future, ExpectedFee = 0.45m },
                new { Tier = 2, Symbol = Futures.Metals.MicroSilver, Type = SecurityType.Future, ExpectedFee = 0.45m },
                new { Tier = 2, Symbol = Futures.Energy.MicroCrudeOilWTI, Type = SecurityType.Future, ExpectedFee = 0.45m },
                new { Tier = 3, Symbol = Futures.Indices.MicroDow30EMini, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 3, Symbol = Futures.Indices.MicroRussell2000EMini, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 3, Symbol = Futures.Indices.MicroSP500EMini, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 3, Symbol = Futures.Indices.MicroNASDAQ100EMini, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 3, Symbol = Futures.Financials.MicroY2TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 3, Symbol = Futures.Financials.MicroY5TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 3, Symbol = Futures.Financials.MicroY10TreasuryNote, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 3, Symbol = Futures.Financials.MicroY30TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 3, Symbol = Futures.Metals.MicroGold, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 3, Symbol = Futures.Metals.MicroSilver, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 3, Symbol = Futures.Energy.MicroCrudeOilWTI, Type = SecurityType.Future, ExpectedFee = 0.4m },
                // Micro E-mini Future options
                new { Tier = 0, Symbol = Futures.Indices.MicroDow30EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.45m },
                new { Tier = 0, Symbol = Futures.Indices.MicroRussell2000EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.45m },
                new { Tier = 0, Symbol = Futures.Indices.MicroSP500EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.45m },
                new { Tier = 0, Symbol = Futures.Indices.MicroNASDAQ100EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.45m },
                new { Tier = 0, Symbol = Futures.Financials.MicroY2TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.45m },
                new { Tier = 0, Symbol = Futures.Financials.MicroY5TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.45m },
                new { Tier = 0, Symbol = Futures.Financials.MicroY10TreasuryNote, Type = SecurityType.FutureOption, ExpectedFee = 0.45m },
                new { Tier = 0, Symbol = Futures.Financials.MicroY30TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.45m },
                new { Tier = 0, Symbol = Futures.Metals.MicroGold, Type = SecurityType.FutureOption, ExpectedFee = 0.45m },
                new { Tier = 0, Symbol = Futures.Metals.MicroSilver, Type = SecurityType.FutureOption, ExpectedFee = 0.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroCrudeOilWTI, Type = SecurityType.FutureOption, ExpectedFee = 0.45m },
                new { Tier = 1, Symbol = Futures.Indices.MicroDow30EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.4m },
                new { Tier = 1, Symbol = Futures.Indices.MicroRussell2000EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.4m },
                new { Tier = 1, Symbol = Futures.Indices.MicroSP500EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.4m },
                new { Tier = 1, Symbol = Futures.Indices.MicroNASDAQ100EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.4m },
                new { Tier = 1, Symbol = Futures.Financials.MicroY2TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.4m },
                new { Tier = 1, Symbol = Futures.Financials.MicroY5TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.4m },
                new { Tier = 1, Symbol = Futures.Financials.MicroY10TreasuryNote, Type = SecurityType.FutureOption, ExpectedFee = 0.4m },
                new { Tier = 1, Symbol = Futures.Financials.MicroY30TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.4m },
                new { Tier = 1, Symbol = Futures.Metals.MicroGold, Type = SecurityType.FutureOption, ExpectedFee = 0.4m },
                new { Tier = 1, Symbol = Futures.Metals.MicroSilver, Type = SecurityType.FutureOption, ExpectedFee = 0.4m },
                new { Tier = 1, Symbol = Futures.Energy.MicroCrudeOilWTI, Type = SecurityType.FutureOption, ExpectedFee = 0.4m },
                new { Tier = 2, Symbol = Futures.Indices.MicroDow30EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.35m },
                new { Tier = 2, Symbol = Futures.Indices.MicroRussell2000EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.35m },
                new { Tier = 2, Symbol = Futures.Indices.MicroSP500EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.35m },
                new { Tier = 2, Symbol = Futures.Indices.MicroNASDAQ100EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.35m },
                new { Tier = 2, Symbol = Futures.Financials.MicroY2TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.35m },
                new { Tier = 2, Symbol = Futures.Financials.MicroY5TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.35m },
                new { Tier = 2, Symbol = Futures.Financials.MicroY10TreasuryNote, Type = SecurityType.FutureOption, ExpectedFee = 0.35m },
                new { Tier = 2, Symbol = Futures.Financials.MicroY30TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.35m },
                new { Tier = 2, Symbol = Futures.Metals.MicroGold, Type = SecurityType.FutureOption, ExpectedFee = 0.35m },
                new { Tier = 2, Symbol = Futures.Metals.MicroSilver, Type = SecurityType.FutureOption, ExpectedFee = 0.35m },
                new { Tier = 2, Symbol = Futures.Energy.MicroCrudeOilWTI, Type = SecurityType.FutureOption, ExpectedFee = 0.35m },
                new { Tier = 3, Symbol = Futures.Indices.MicroDow30EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 3, Symbol = Futures.Indices.MicroRussell2000EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 3, Symbol = Futures.Indices.MicroSP500EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 3, Symbol = Futures.Indices.MicroNASDAQ100EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 3, Symbol = Futures.Financials.MicroY2TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 3, Symbol = Futures.Financials.MicroY5TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 3, Symbol = Futures.Financials.MicroY10TreasuryNote, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 3, Symbol = Futures.Financials.MicroY30TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 3, Symbol = Futures.Metals.MicroGold, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 3, Symbol = Futures.Metals.MicroSilver, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 3, Symbol = Futures.Energy.MicroCrudeOilWTI, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                // Cryptocurrency futures
                new { Tier = 0, Symbol = Futures.Currencies.BTC, Type = SecurityType.Future, ExpectedFee = 11m },
                new { Tier = 0, Symbol = Futures.Currencies.ETH, Type = SecurityType.Future, ExpectedFee = 7m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroBTC, Type = SecurityType.Future, ExpectedFee = 4.75m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroEther, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 1, Symbol = Futures.Currencies.BTC, Type = SecurityType.Future, ExpectedFee = 11m },
                new { Tier = 1, Symbol = Futures.Currencies.ETH, Type = SecurityType.Future, ExpectedFee = 7m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroBTC, Type = SecurityType.Future, ExpectedFee = 4.75m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroEther, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 2, Symbol = Futures.Currencies.BTC, Type = SecurityType.Future, ExpectedFee = 11m },
                new { Tier = 2, Symbol = Futures.Currencies.ETH, Type = SecurityType.Future, ExpectedFee = 7m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroBTC, Type = SecurityType.Future, ExpectedFee = 4.75m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroEther, Type = SecurityType.Future, ExpectedFee = 0.4m },
                new { Tier = 3, Symbol = Futures.Currencies.BTC, Type = SecurityType.Future, ExpectedFee = 11m },
                new { Tier = 3, Symbol = Futures.Currencies.ETH, Type = SecurityType.Future, ExpectedFee = 7m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroBTC, Type = SecurityType.Future, ExpectedFee = 4.75m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroEther, Type = SecurityType.Future, ExpectedFee = 0.4m },
                // Cryptocurrency future options
                new { Tier = 0, Symbol = Futures.Currencies.BTC, Type = SecurityType.FutureOption, ExpectedFee = 10m },
                new { Tier = 0, Symbol = Futures.Currencies.ETH, Type = SecurityType.FutureOption, ExpectedFee = 7m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroBTC, Type = SecurityType.FutureOption, ExpectedFee = 3.75m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroEther, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 1, Symbol = Futures.Currencies.BTC, Type = SecurityType.FutureOption, ExpectedFee = 10m },
                new { Tier = 1, Symbol = Futures.Currencies.ETH, Type = SecurityType.FutureOption, ExpectedFee = 7m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroBTC, Type = SecurityType.FutureOption, ExpectedFee = 3.75m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroEther, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 2, Symbol = Futures.Currencies.BTC, Type = SecurityType.FutureOption, ExpectedFee = 10m },
                new { Tier = 2, Symbol = Futures.Currencies.ETH, Type = SecurityType.FutureOption, ExpectedFee = 7m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroBTC, Type = SecurityType.FutureOption, ExpectedFee = 3.75m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroEther, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                new { Tier = 3, Symbol = Futures.Currencies.BTC, Type = SecurityType.FutureOption, ExpectedFee = 10m },
                new { Tier = 3, Symbol = Futures.Currencies.ETH, Type = SecurityType.FutureOption, ExpectedFee = 7m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroBTC, Type = SecurityType.FutureOption, ExpectedFee = 3.75m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroEther, Type = SecurityType.FutureOption, ExpectedFee = 0.3m },
                // E-mini FX (currencies) Futures
                new { Tier = 0, Symbol = Futures.Currencies.EuroFXEmini, Type = SecurityType.Future, ExpectedFee = 1.35m },
                new { Tier = 0, Symbol = Futures.Currencies.JapaneseYenEmini, Type = SecurityType.Future, ExpectedFee = 1.35m },
                new { Tier = 1, Symbol = Futures.Currencies.EuroFXEmini, Type = SecurityType.Future, ExpectedFee = 1.25m },
                new { Tier = 1, Symbol = Futures.Currencies.JapaneseYenEmini, Type = SecurityType.Future, ExpectedFee = 1.25m },
                new { Tier = 2, Symbol = Futures.Currencies.EuroFXEmini, Type = SecurityType.Future, ExpectedFee = 1.15m },
                new { Tier = 2, Symbol = Futures.Currencies.JapaneseYenEmini, Type = SecurityType.Future, ExpectedFee = 1.15m },
                new { Tier = 3, Symbol = Futures.Currencies.EuroFXEmini, Type = SecurityType.Future, ExpectedFee = 1m },
                new { Tier = 3, Symbol = Futures.Currencies.JapaneseYenEmini, Type = SecurityType.Future, ExpectedFee = 1m },
                // Micro E-mini FX (currencies) Futures
                new { Tier = 0, Symbol = Futures.Currencies.MicroAUD, Type = SecurityType.Future, ExpectedFee = 0.39m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroEUR, Type = SecurityType.Future, ExpectedFee = 0.39m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroGBP, Type = SecurityType.Future, ExpectedFee = 0.39m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroCADUSD, Type = SecurityType.Future, ExpectedFee = 0.39m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroJPY, Type = SecurityType.Future, ExpectedFee = 0.39m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroCHF, Type = SecurityType.Future, ExpectedFee = 0.39m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroUSDJPY, Type = SecurityType.Future, ExpectedFee = 0.39m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroINRUSD, Type = SecurityType.Future, ExpectedFee = 0.39m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroCAD, Type = SecurityType.Future, ExpectedFee = 0.39m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroUSDCHF, Type = SecurityType.Future, ExpectedFee = 0.39m },
                new { Tier = 0, Symbol = Futures.Currencies.MicroUSDCNH, Type = SecurityType.Future, ExpectedFee = 0.39m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroAUD, Type = SecurityType.Future, ExpectedFee = 0.36m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroEUR, Type = SecurityType.Future, ExpectedFee = 0.36m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroGBP, Type = SecurityType.Future, ExpectedFee = 0.36m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroCADUSD, Type = SecurityType.Future, ExpectedFee = 0.36m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroJPY, Type = SecurityType.Future, ExpectedFee = 0.36m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroCHF, Type = SecurityType.Future, ExpectedFee = 0.36m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroUSDJPY, Type = SecurityType.Future, ExpectedFee = 0.36m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroINRUSD, Type = SecurityType.Future, ExpectedFee = 0.36m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroCAD, Type = SecurityType.Future, ExpectedFee = 0.36m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroUSDCHF, Type = SecurityType.Future, ExpectedFee = 0.36m },
                new { Tier = 1, Symbol = Futures.Currencies.MicroUSDCNH, Type = SecurityType.Future, ExpectedFee = 0.36m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroAUD, Type = SecurityType.Future, ExpectedFee = 0.32m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroEUR, Type = SecurityType.Future, ExpectedFee = 0.32m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroGBP, Type = SecurityType.Future, ExpectedFee = 0.32m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroCADUSD, Type = SecurityType.Future, ExpectedFee = 0.32m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroJPY, Type = SecurityType.Future, ExpectedFee = 0.32m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroCHF, Type = SecurityType.Future, ExpectedFee = 0.32m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroUSDJPY, Type = SecurityType.Future, ExpectedFee = 0.32m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroINRUSD, Type = SecurityType.Future, ExpectedFee = 0.32m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroCAD, Type = SecurityType.Future, ExpectedFee = 0.32m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroUSDCHF, Type = SecurityType.Future, ExpectedFee = 0.32m },
                new { Tier = 2, Symbol = Futures.Currencies.MicroUSDCNH, Type = SecurityType.Future, ExpectedFee = 0.32m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroAUD, Type = SecurityType.Future, ExpectedFee = 0.29m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroEUR, Type = SecurityType.Future, ExpectedFee = 0.29m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroGBP, Type = SecurityType.Future, ExpectedFee = 0.29m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroCADUSD, Type = SecurityType.Future, ExpectedFee = 0.29m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroJPY, Type = SecurityType.Future, ExpectedFee = 0.29m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroCHF, Type = SecurityType.Future, ExpectedFee = 0.29m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroUSDJPY, Type = SecurityType.Future, ExpectedFee = 0.29m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroINRUSD, Type = SecurityType.Future, ExpectedFee = 0.29m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroCAD, Type = SecurityType.Future, ExpectedFee = 0.29m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroUSDCHF, Type = SecurityType.Future, ExpectedFee = 0.29m },
                new { Tier = 3, Symbol = Futures.Currencies.MicroUSDCNH, Type = SecurityType.Future, ExpectedFee = 0.29m },
                // Other futures
                new { Tier = 0, Symbol = Futures.Metals.MicroGoldTAS, Type = SecurityType.Future, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Metals.MicroPalladium, Type = SecurityType.Future, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, Type = SecurityType.Future, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, Type = SecurityType.Future, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroCoalAPIFivefobNewcastleArgusMcCloskey, Type = SecurityType.Future, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroSingaporeFuelOil380CSTPlatts, Type = SecurityType.Future, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, Type = SecurityType.Future, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, Type = SecurityType.Future, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, Type = SecurityType.Future, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Currencies.USD, Type = SecurityType.Future, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Currencies.CAD, Type = SecurityType.Future, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Currencies.EUR, Type = SecurityType.Future, ExpectedFee = 2.45m },
                new { Tier = 1, Symbol = Futures.Metals.MicroGoldTAS, Type = SecurityType.Future, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Metals.MicroPalladium, Type = SecurityType.Future, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, Type = SecurityType.Future, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, Type = SecurityType.Future, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroCoalAPIFivefobNewcastleArgusMcCloskey, Type = SecurityType.Future, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroSingaporeFuelOil380CSTPlatts, Type = SecurityType.Future, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, Type = SecurityType.Future, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, Type = SecurityType.Future, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, Type = SecurityType.Future, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Currencies.USD, Type = SecurityType.Future, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Currencies.CAD, Type = SecurityType.Future, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Currencies.EUR, Type = SecurityType.Future, ExpectedFee = 2.25m },
                new { Tier = 2, Symbol = Futures.Metals.MicroGoldTAS, Type = SecurityType.Future, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Metals.MicroPalladium, Type = SecurityType.Future, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, Type = SecurityType.Future, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, Type = SecurityType.Future, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroCoalAPIFivefobNewcastleArgusMcCloskey, Type = SecurityType.Future, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroSingaporeFuelOil380CSTPlatts, Type = SecurityType.Future, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, Type = SecurityType.Future, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, Type = SecurityType.Future, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, Type = SecurityType.Future, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Currencies.USD, Type = SecurityType.Future, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Currencies.CAD, Type = SecurityType.Future, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Currencies.EUR, Type = SecurityType.Future, ExpectedFee = 2.05m },
                new { Tier = 3, Symbol = Futures.Metals.MicroGoldTAS, Type = SecurityType.Future, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Metals.MicroPalladium, Type = SecurityType.Future, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, Type = SecurityType.Future, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, Type = SecurityType.Future, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroCoalAPIFivefobNewcastleArgusMcCloskey, Type = SecurityType.Future, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroSingaporeFuelOil380CSTPlatts, Type = SecurityType.Future, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, Type = SecurityType.Future, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, Type = SecurityType.Future, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, Type = SecurityType.Future, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Currencies.USD, Type = SecurityType.Future, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Currencies.CAD, Type = SecurityType.Future, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Currencies.EUR, Type = SecurityType.Future, ExpectedFee = 1.85m },
                // Other future options
                new { Tier = 0, Symbol = Futures.Metals.MicroGoldTAS, Type = SecurityType.FutureOption, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Metals.MicroPalladium, Type = SecurityType.FutureOption, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroCoalAPIFivefobNewcastleArgusMcCloskey, Type = SecurityType.FutureOption, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroSingaporeFuelOil380CSTPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Energy.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Currencies.USD, Type = SecurityType.FutureOption, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Currencies.CAD, Type = SecurityType.FutureOption, ExpectedFee = 2.45m },
                new { Tier = 0, Symbol = Futures.Currencies.EUR, Type = SecurityType.FutureOption, ExpectedFee = 2.45m },
                new { Tier = 1, Symbol = Futures.Metals.MicroGoldTAS, Type = SecurityType.FutureOption, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Metals.MicroPalladium, Type = SecurityType.FutureOption, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroCoalAPIFivefobNewcastleArgusMcCloskey, Type = SecurityType.FutureOption, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroSingaporeFuelOil380CSTPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Energy.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Currencies.USD, Type = SecurityType.FutureOption, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Currencies.CAD, Type = SecurityType.FutureOption, ExpectedFee = 2.25m },
                new { Tier = 1, Symbol = Futures.Currencies.EUR, Type = SecurityType.FutureOption, ExpectedFee = 2.25m },
                new { Tier = 2, Symbol = Futures.Metals.MicroGoldTAS, Type = SecurityType.FutureOption, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Metals.MicroPalladium, Type = SecurityType.FutureOption, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroCoalAPIFivefobNewcastleArgusMcCloskey, Type = SecurityType.FutureOption, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroSingaporeFuelOil380CSTPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Energy.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Currencies.USD, Type = SecurityType.FutureOption, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Currencies.CAD, Type = SecurityType.FutureOption, ExpectedFee = 2.05m },
                new { Tier = 2, Symbol = Futures.Currencies.EUR, Type = SecurityType.FutureOption, ExpectedFee = 2.05m },
                new { Tier = 3, Symbol = Futures.Metals.MicroGoldTAS, Type = SecurityType.FutureOption, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Metals.MicroPalladium, Type = SecurityType.FutureOption, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, Type = SecurityType.FutureOption, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, Type = SecurityType.FutureOption, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroCoalAPIFivefobNewcastleArgusMcCloskey, Type = SecurityType.FutureOption, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroSingaporeFuelOil380CSTPlatts, Type = SecurityType.FutureOption, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, Type = SecurityType.FutureOption, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, Type = SecurityType.FutureOption, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Energy.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, Type = SecurityType.FutureOption, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Currencies.USD, Type = SecurityType.FutureOption, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Currencies.CAD, Type = SecurityType.FutureOption, ExpectedFee = 1.85m },
                new { Tier = 3, Symbol = Futures.Currencies.EUR, Type = SecurityType.FutureOption, ExpectedFee = 1.85m },
            }.Select(x =>
            {
                var symbol = Symbols.CreateFutureSymbol(x.Symbol, SecurityIdentifier.DefaultDate);
                if (x.Type == SecurityType.FutureOption)
                {
                    symbol = Symbols.CreateFutureOptionSymbol(symbol, OptionRight.Call, 0m, SecurityIdentifier.DefaultDate);
                }

                return new TestCaseData(x.Tier, symbol, x.ExpectedFee);
            }).ToArray();
        }
    }
}
