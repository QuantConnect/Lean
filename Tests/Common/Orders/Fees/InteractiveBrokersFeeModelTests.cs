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
                new { Symbol = Futures.Energies.MicroCrudeOilWTI, Type = SecurityType.Future, ExpectedFee = 0.57m },
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
                new { Symbol = Futures.Energies.MicroCrudeOilWTI, Type = SecurityType.FutureOption, ExpectedFee = 0.47m },
                // Cryptocurrency futures
                new { Symbol = Futures.Currencies.BTC, Type = SecurityType.Future, ExpectedFee = 11.02m },
                new { Symbol = Futures.Currencies.MicroBTC, Type = SecurityType.Future, ExpectedFee = 4.77m },
                new { Symbol = Futures.Currencies.MicroEther, Type = SecurityType.Future, ExpectedFee = 0.42m },
                // Cryptocurrency future options
                new { Symbol = Futures.Currencies.BTC, Type = SecurityType.FutureOption, ExpectedFee = 10.02m },
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
                new { Symbol = Futures.Energies.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroCoalAPIFivefobNewcastleArgusMcCloskey, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroSingaporeFuelOil380CSTPlatts, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Currencies.USD, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Currencies.CAD, Type = SecurityType.Future, ExpectedFee = 2.47m },
                new { Symbol = Futures.Currencies.EUR, Type = SecurityType.Future, ExpectedFee = 2.47m },
                // Other future options
                new { Symbol = Futures.Metals.MicroGoldTAS, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Metals.MicroPalladium, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroGasoilZeroPointOnePercentBargesFOBARAPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroEuropeanThreePointFivePercentFuelOilCargoesFOBMedPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroCoalAPIFivefobNewcastleArgusMcCloskey, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroSingaporeFuelOil380CSTPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroEuropeanThreePointFivePercentOilBargesFOBRdamPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroEuropeanFOBRdamMarineFuelZeroPointFivePercentBargesPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
                new { Symbol = Futures.Energies.MicroSingaporeFOBMarineFuelZeroPointFivePercetPlatts, Type = SecurityType.FutureOption, ExpectedFee = 2.47m },
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
