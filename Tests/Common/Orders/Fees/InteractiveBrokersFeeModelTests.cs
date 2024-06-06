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
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.FutureOption;
using QuantConnect.Securities.Option;
using QuantConnect.Tests.Common.Securities;

namespace QuantConnect.Tests.Common.Orders.Fees
{
    [TestFixture]
    public class InteractiveBrokersFeeModelTests
    {
        private readonly IFeeModel _feeModel = new InteractiveBrokersFeeModel();

        [Test]
        public void USAEquityMinimumFeeInUSD()
        {
            var security = SecurityTests.GetSecurity();
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(1m, fee.Value.Amount);
        }

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
            Assert.AreEqual(5m, fee.Value.Amount);
        }

        [TestCaseSource(nameof(USAFuturesFeeTestCases))]
        public void USAFutureFee(Symbol symbol, decimal expectedFee)
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
            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, new MarketOrder(security.Symbol, 1000, time)));

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
            var fee = _feeModel.GetOrderFee(new OrderFeeParameters(security, order));

            Assert.AreEqual(quoteCurrency, fee.Value.Currency);
            Assert.AreEqual(expectedFee, fee.Value.Amount);
        }

        [TestCase(OrderType.ComboMarket, 0.01, 250)]
        [TestCase(OrderType.ComboLimit, 0.01, 250)]
        [TestCase(OrderType.ComboLegLimit, 0.01, 250)]
        [TestCase(OrderType.Limit, 0.01, 250)]
        [TestCase(OrderType.StopLimit, 0.01, 250)]
        [TestCase(OrderType.LimitIfTouched, 0.01, 250)]
        [TestCase(OrderType.StopMarket, 0.01, 250)]
        [TestCase(OrderType.TrailingStop, 0.01, 250)]
        [TestCase(OrderType.Market, 0.01, 250)]
        [TestCase(OrderType.MarketOnClose, 0.01, 250)]
        [TestCase(OrderType.MarketOnOpen, 0.01, 250)]
        [TestCase(OrderType.ComboMarket, 0.2, 650)]
        [TestCase(OrderType.ComboLimit, 0.2, 650)]
        [TestCase(OrderType.ComboLegLimit, 0.2, 650)]
        [TestCase(OrderType.Limit, 0.2, 650)]
        [TestCase(OrderType.StopLimit, 0.2, 650)]
        [TestCase(OrderType.LimitIfTouched, 0.2, 650)]
        [TestCase(OrderType.StopMarket, 0.2, 650)]
        [TestCase(OrderType.TrailingStop, 0.2, 650)]
        [TestCase(OrderType.Market, 0.2, 650)]
        [TestCase(OrderType.MarketOnClose, 0.2, 650)]
        [TestCase(OrderType.MarketOnOpen, 0.2, 650)]
        [TestCase(OrderType.ComboMarket, 0.07, 500)]
        [TestCase(OrderType.ComboLimit, 0.07, 500)]
        [TestCase(OrderType.ComboLegLimit, 0.07, 500)]
        [TestCase(OrderType.Limit, 0.07, 500)]
        [TestCase(OrderType.StopLimit, 0.07, 500)]
        [TestCase(OrderType.LimitIfTouched, 0.07, 500)]
        [TestCase(OrderType.StopMarket, 0.07, 500)]
        [TestCase(OrderType.TrailingStop, 0.07, 500)]
        [TestCase(OrderType.Market, 0.07, 500)]
        [TestCase(OrderType.MarketOnClose, 0.07, 500)]
        [TestCase(OrderType.MarketOnOpen, 0.07, 500)]
        public void USAOptionFee(OrderType orderType, double price, double expectedFees)
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

            var fee = _feeModel.GetOrderFee(
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

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(1m, fee.Value.Amount);
        }

        [Test]
        public void ForexFee_NonUSD()
        {
            var tz = TimeZones.NewYork;
            var security = new Forex(
                SecurityExchangeHours.AlwaysOpen(tz),
                new Cash("GBP", 0, 0),
                new Cash("EUR", 0, 0),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.EURGBP, Resolution.Minute, tz, tz, true, false, false),
                new SymbolProperties("EURGBP", "GBP", 1, 0.01m, 0.00000001m, string.Empty),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(2m, fee.Value.Amount);
        }

        [Test]
        public void GetOrderFeeThrowsForUnsupportedSecurityType()
        {
            Assert.Throws<ArgumentException>(
                () =>
                {
                    var tz = TimeZones.NewYork;
                    var security = new Crypto(
                        Symbols.BTCUSD,
                        SecurityExchangeHours.AlwaysOpen(tz),
                        new Cash("USD", 0, 0),
                        new Cash("BTC", 0, 0),
                        SymbolProperties.GetDefault("USD"),
                        ErrorCurrencyConverter.Instance,
                        RegisteredSecurityDataTypesProvider.Null,
                        new SecurityCache()
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

        private static TestCaseData[] USAFuturesFeeTestCases()
        {
            return new[]
            {
                // E-mini Futures
                new { Symbol = Futures.Indices.Dow30EMini, Type = SecurityType.Future, ExpectedFee = 2.15m },
                new { Symbol = Futures.Indices.Russell2000EMini, Type = SecurityType.Future, ExpectedFee = 2.15m },
                new { Symbol = Futures.Indices.SP500EMini, Type = SecurityType.Future, ExpectedFee = 2.15m },
                new { Symbol = Futures.Indices.NASDAQ100EMini, Type = SecurityType.Future, ExpectedFee = 2.15m },
                // E-mini Future options
                new { Symbol = Futures.Indices.Dow30EMini, Type = SecurityType.FutureOption, ExpectedFee = 1.42m },
                new { Symbol = Futures.Indices.Russell2000EMini, Type = SecurityType.FutureOption, ExpectedFee = 1.42m },
                new { Symbol = Futures.Indices.SP500EMini, Type = SecurityType.FutureOption, ExpectedFee = 1.42m },
                new { Symbol = Futures.Indices.NASDAQ100EMini, Type = SecurityType.FutureOption, ExpectedFee = 1.42m },
                // Micro E-mini Futures
                new { Symbol = Futures.Indices.MicroDow30EMini, Type = SecurityType.Future, ExpectedFee = 0.57m },
                new { Symbol = Futures.Indices.MicroRussell2000EMini, Type = SecurityType.Future, ExpectedFee = 0.57m },
                new { Symbol = Futures.Indices.MicroSP500EMini, Type = SecurityType.Future, ExpectedFee = 0.57m },
                new { Symbol = Futures.Indices.MicroNASDAQ100EMini, Type = SecurityType.Future, ExpectedFee = 0.57m },
                new { Symbol = Futures.Financials.MicroY2TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.57m },
                new { Symbol = Futures.Financials.MicroY5TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.57m },
                new { Symbol = Futures.Financials.MicroY10TreasuryNote, Type = SecurityType.Future, ExpectedFee = 0.57m },
                new { Symbol = Futures.Financials.MicroY30TreasuryBond, Type = SecurityType.Future, ExpectedFee = 0.57m },
                new { Symbol = Futures.Metals.MicroGold, Type = SecurityType.Future, ExpectedFee = 0.57m },
                new { Symbol = Futures.Metals.MicroSilver, Type = SecurityType.Future, ExpectedFee = 0.57m },
                new { Symbol = Futures.Energy.MicroCrudeOilWTI, Type = SecurityType.Future, ExpectedFee = 0.57m },
                // Micro E-mini Future options
                new { Symbol = Futures.Indices.MicroDow30EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.47m },
                new { Symbol = Futures.Indices.MicroRussell2000EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.47m },
                new { Symbol = Futures.Indices.MicroSP500EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.47m },
                new { Symbol = Futures.Indices.MicroNASDAQ100EMini, Type = SecurityType.FutureOption, ExpectedFee = 0.47m },
                new { Symbol = Futures.Financials.MicroY2TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.47m },
                new { Symbol = Futures.Financials.MicroY5TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.47m },
                new { Symbol = Futures.Financials.MicroY10TreasuryNote, Type = SecurityType.FutureOption, ExpectedFee = 0.47m },
                new { Symbol = Futures.Financials.MicroY30TreasuryBond, Type = SecurityType.FutureOption, ExpectedFee = 0.47m },
                new { Symbol = Futures.Metals.MicroGold, Type = SecurityType.FutureOption, ExpectedFee = 0.47m },
                new { Symbol = Futures.Metals.MicroSilver, Type = SecurityType.FutureOption, ExpectedFee = 0.47m },
                new { Symbol = Futures.Energy.MicroCrudeOilWTI, Type = SecurityType.FutureOption, ExpectedFee = 0.47m },
                // Cryptocurrency futures
                new { Symbol = Futures.Currencies.BTC, Type = SecurityType.Future, ExpectedFee = 11.02m },
                new { Symbol = Futures.Currencies.ETH, Type = SecurityType.Future, ExpectedFee = 7.02m },
                new { Symbol = Futures.Currencies.MicroBTC, Type = SecurityType.Future, ExpectedFee = 4.77m },
                new { Symbol = Futures.Currencies.MicroEther, Type = SecurityType.Future, ExpectedFee = 0.42m },
                // Cryptocurrency future options
                new { Symbol = Futures.Currencies.BTC, Type = SecurityType.FutureOption, ExpectedFee = 10.02m },
                new { Symbol = Futures.Currencies.ETH, Type = SecurityType.FutureOption, ExpectedFee = 7.02m },
                new { Symbol = Futures.Currencies.MicroBTC, Type = SecurityType.FutureOption, ExpectedFee = 3.77m },
                new { Symbol = Futures.Currencies.MicroEther, Type = SecurityType.FutureOption, ExpectedFee = 0.32m },
                // E-mini FX (currencies) Futures
                new { Symbol = Futures.Currencies.EuroFXEmini, Type = SecurityType.Future, ExpectedFee = 1.37m },
                new { Symbol = Futures.Currencies.JapaneseYenEmini, Type = SecurityType.Future, ExpectedFee = 1.37m },
                // Micro E-mini FX (currencies) Futures
                new { Symbol = Futures.Currencies.MicroAUD, Type = SecurityType.Future, ExpectedFee = 0.41m },
                new { Symbol = Futures.Currencies.MicroEUR, Type = SecurityType.Future, ExpectedFee = 0.41m },
                new { Symbol = Futures.Currencies.MicroGBP, Type = SecurityType.Future, ExpectedFee = 0.41m },
                new { Symbol = Futures.Currencies.MicroCADUSD, Type = SecurityType.Future, ExpectedFee = 0.41m },
                new { Symbol = Futures.Currencies.MicroJPY, Type = SecurityType.Future, ExpectedFee = 0.41m },
                new { Symbol = Futures.Currencies.MicroCHF, Type = SecurityType.Future, ExpectedFee = 0.41m },
                new { Symbol = Futures.Currencies.MicroUSDJPY, Type = SecurityType.Future, ExpectedFee = 0.41m },
                new { Symbol = Futures.Currencies.MicroINRUSD, Type = SecurityType.Future, ExpectedFee = 0.41m },
                new { Symbol = Futures.Currencies.MicroCAD, Type = SecurityType.Future, ExpectedFee = 0.41m },
                new { Symbol = Futures.Currencies.MicroUSDCHF, Type = SecurityType.Future, ExpectedFee = 0.41m },
                new { Symbol = Futures.Currencies.MicroUSDCNH, Type = SecurityType.Future, ExpectedFee = 0.41m },
                // Other futures
                new { Symbol = Futures.Metals.MicroGoldTAS, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Metals.MicroPalladium, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroCoalAPIFivefobNewcastleArgusMcCloskey, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroSingaporeFuelOil380CSTPlatts, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Currencies.USD, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Currencies.CAD, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Currencies.EUR, Type = SecurityType.Future, ExpectedFee = 2.47m },
                // Other future options
                new { Symbol = Futures.Metals.MicroGoldTAS, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Metals.MicroPalladium, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroCoalAPIFivefobNewcastleArgusMcCloskey, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroSingaporeFuelOil380CSTPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energy.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Currencies.USD, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Currencies.CAD, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Currencies.EUR, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
            }.Select(x =>
            {
                var symbol = Symbols.CreateFutureSymbol(x.Symbol, SecurityIdentifier.DefaultDate);
                if (x.Type == SecurityType.FutureOption)
                {
                    symbol = Symbols.CreateFutureOptionSymbol(symbol, OptionRight.Call, 0m, SecurityIdentifier.DefaultDate);
                }

                return new TestCaseData(symbol, x.ExpectedFee);
            }).ToArray();
        }
    }
}
