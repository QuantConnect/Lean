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
            Security security = symbol.SecurityType == SecurityType.Future
                ? future
                : new FutureOption(symbol,
                    SecurityExchangeHours.AlwaysOpen(tz),
                    new Cash("USD", 0, 0),
                    new OptionSymbolProperties(SymbolProperties.GetDefault("USD")),
                    ErrorCurrencyConverter.Instance,
                    RegisteredSecurityDataTypesProvider.Null,
                    new SecurityCache(),
                    future);
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(1000 * expectedFee, fee.Value.Amount);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HongKongFutureFee(bool canonical)
        {
            var symbol = Symbols.CreateFutureSymbol(Futures.Indices.HangSeng, SecurityIdentifier.DefaultDate);
            if (!canonical)
            {
                symbol = Symbols.CreateFutureSymbol(Futures.Indices.HangSeng,
                    FuturesExpiryFunctions.FuturesExpiryFunction(symbol)(new DateTime(2021, 12, 1)));
            }
            var entry = MarketHoursDatabase.FromDataFolder().GetEntry(symbol.ID.Market, symbol, symbol.SecurityType);
            var properties = SymbolPropertiesDatabase.FromDataFolder()
                .GetSymbolProperties(symbol.ID.Market, symbol, symbol.SecurityType, null);
            var security = new Future(symbol, entry.ExchangeHours,
                new Cash(properties.QuoteCurrency, 0, 0),
                properties,
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.SetMarketPrice(new Tick(DateTime.UtcNow, security.Symbol, 100, 100));

            var fee = _feeModel.GetOrderFee(
                new OrderFeeParameters(
                    security,
                    new MarketOrder(security.Symbol, 1000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.HKD, fee.Value.Currency);
            Assert.AreEqual(1000 * 40m, fee.Value.Amount);
        }

        [Test]
        public void USAOptionFee()
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
                    new MarketOrder(security.Symbol, 1000, DateTime.UtcNow)
                )
            );

            Assert.AreEqual(Currencies.USD, fee.Value.Currency);
            Assert.AreEqual(250m, fee.Value.Amount);
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
                    var security = new Cfd(
                        SecurityExchangeHours.AlwaysOpen(tz),
                        new Cash("EUR", 0, 0),
                        new SubscriptionDataConfig(typeof(QuoteBar), Symbols.DE30EUR, Resolution.Minute, tz, tz, true, false, false),
                        new SymbolProperties("DE30EUR", "EUR", 1, 0.01m, 1m, string.Empty),
                        ErrorCurrencyConverter.Instance,
                        RegisteredSecurityDataTypesProvider.Null
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

        private static TestCaseData[] USAFuturesFeeTestCases => new []
        {
            new TestCaseData(Symbols.Future_ESZ18_Dec2018, 1.85m),
            new TestCaseData(Symbols.Future_CLF19_Jan2019, 1.85m),

            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.BTC, new DateTime(2022, 9, 16)), 6.0m),
            new TestCaseData(Symbols.CreateFutureOptionSymbol(Symbols.CreateFutureSymbol(Futures.Currencies.BTC, new DateTime(2022, 9, 16)), OptionRight.Call, 0m, new DateTime(2022, 9, 16)), 6.0m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroBTC, new DateTime(2022, 9, 16)), 3.25m),
            new TestCaseData(Symbols.CreateFutureOptionSymbol(Symbols.CreateFutureSymbol(Futures.Currencies.MicroBTC, new DateTime(2022, 9, 16)), OptionRight.Call, 0m, new DateTime(2022, 9, 16)), 2.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.BTICMicroBTC, new DateTime(2022, 9, 16)), 3.25m),
            new TestCaseData(Symbols.CreateFutureOptionSymbol(Symbols.CreateFutureSymbol(Futures.Currencies.BTICMicroBTC, new DateTime(2022, 9, 16)), OptionRight.Call, 0m, new DateTime(2022, 9, 16)), 2.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.BTICMicroEther, new DateTime(2022, 9, 16)), 1.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroEther, new DateTime(2022, 9, 16)), 1.20m),

            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Indices.MicroDow30EMini, new DateTime(2022, 9, 16)), 1.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Indices.MicroRussell2000EMini, new DateTime(2022, 9, 16)), 1.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Indices.MicroSP500EMini, new DateTime(2022, 9, 16)), 1.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Indices.MicroNASDAQ100EMini, new DateTime(2022, 9, 16)), 1.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Financials.MicroY2TreasuryBond, new DateTime(2022, 9, 16)), 1.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Financials.MicroY5TreasuryBond, new DateTime(2022, 9, 16)), 1.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Financials.MicroY10TreasuryNote, new DateTime(2022, 9, 16)), 1.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Financials.MicroY30TreasuryBond, new DateTime(2022, 9, 16)), 1.25m),

            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.EuroFXEmini, new DateTime(2022, 9, 16)), 1.50m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.JapaneseYenEmini, new DateTime(2022, 9, 16)), 1.50m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroAUD, new DateTime(2022, 9, 16)), 1.15m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroEUR, new DateTime(2022, 9, 16)), 1.15m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroGBP, new DateTime(2022, 9, 16)), 1.15m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroCADUSD, new DateTime(2022, 9, 16)), 1.15m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroJPY, new DateTime(2022, 9, 16)), 1.15m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroCHF, new DateTime(2022, 9, 16)), 1.15m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroUSDJPY, new DateTime(2022, 9, 16)), 1.15m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroINRUSD, new DateTime(2022, 9, 16)), 1.15m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroCAD, new DateTime(2022, 9, 16)), 1.15m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroUSDCHF, new DateTime(2022, 9, 16)), 1.15m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Currencies.MicroUSDCNH, new DateTime(2022, 9, 16)), 1.15m),

            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Metals.MicroGold, new DateTime(2022, 9, 16)), 1.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Metals.MicroGoldTAS, new DateTime(2022, 9, 16)), 1.85m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Metals.MicroSilver, new DateTime(2022, 9, 16)), 1.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Metals.MicroPalladium, new DateTime(2022, 9, 16)), 1.85m),

            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Energies.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, new DateTime(2022, 9, 16)), 1.85m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Energies.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, new DateTime(2022, 9, 16)), 1.85m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Energies.MicroCoalAPIFivefobNewcastleArgusMcCloskey, new DateTime(2022, 9, 16)), 1.85m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Energies.MicroSingaporeFuelOil380CSTPlatts, new DateTime(2022, 9, 16)), 1.85m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Energies.MicroCrudeOilWTI, new DateTime(2022, 9, 16)), 1.25m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Energies.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, new DateTime(2022, 9, 16)), 1.85m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Energies.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, new DateTime(2022, 9, 16)), 1.85m),
            new TestCaseData(Symbols.CreateFutureSymbol(Futures.Energies.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, new DateTime(2022, 9, 16)), 1.85m),



        };
    }
}
