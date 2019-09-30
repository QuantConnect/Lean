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
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Option;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class OptionPriceModelTests
    {

        [Test]
        public void PutCallParityTest()
        {
            const decimal underlyingPrice = 200m;
            const decimal underlyingVol = 0.15m;
            const decimal riskFreeRate = 0.01m;
            var tz = TimeZones.NewYork;
            var evaluationDate = new DateTime(2015, 2, 19);
            var SPY_C_192_Feb19_2016E = Symbol.CreateOption("SPY", Market.USA, OptionStyle.European, OptionRight.Call, 192m, new DateTime(2016, 02, 19));
            var SPY_P_192_Feb19_2016E = Symbol.CreateOption("SPY", Market.USA, OptionStyle.European, OptionRight.Put, 192m, new DateTime(2016, 02, 19));

            // setting up underlying
            var equity = new Equity(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            equity.VolatilityModel = new DummyVolatilityModel(underlyingVol);

            // setting up European style call option
            var contractCall = new OptionContract(SPY_C_192_Feb19_2016E, Symbols.SPY) { Time = evaluationDate };
            var optionCall = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), SPY_C_192_Feb19_2016E, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionCall.Underlying = equity;

            // setting up European style put option
            var contractPut = new OptionContract(SPY_P_192_Feb19_2016E, Symbols.SPY) { Time = evaluationDate };
            var optionPut = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), SPY_P_192_Feb19_2016E, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionPut.Underlying = equity;

            // running evaluation
            var priceModel = OptionPriceModels.BlackScholes();
            var resultsCall = priceModel.Evaluate(optionCall, null, contractCall);
            var resultsPut = priceModel.Evaluate(optionPut, null, contractPut);
            var callPrice = resultsCall.TheoreticalPrice;
            var putPrice = resultsPut.TheoreticalPrice;

            // Put-call parity equation
            var rightPart = putPrice + underlyingPrice; // no yield
            var leftPart = callPrice + contractCall.Strike * (decimal)Math.Exp((double)-riskFreeRate);

            Assert.AreEqual((double)leftPart, (double)rightPart, 0.0001);
        }

        [Test]
        public void BlackScholesPortfolioTest()
        {
            const decimal price = 20.00m;
            const decimal underlyingPrice = 200m;
            const decimal underlyingVol = 0.15m;
            const decimal riskFreeRate = 0.01m;
            var tz = TimeZones.NewYork;
            var evaluationDate = new DateTime(2015, 2, 19);
            var SPY_C_192_Feb19_2016E = Symbol.CreateOption("SPY", Market.USA, OptionStyle.European, OptionRight.Call, 192m, new DateTime(2016, 02, 19));

            // setting up underlying
            var equity = new Equity(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            equity.VolatilityModel = new DummyVolatilityModel(underlyingVol);

            // setting up European style option
            var contract = new OptionContract(SPY_C_192_Feb19_2016E, Symbols.SPY) { Time = evaluationDate };
            var optionCall = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), SPY_C_192_Feb19_2016E, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Underlying = equity;

            // running evaluation
            var priceModel = OptionPriceModels.BlackScholes();
            var results = priceModel.Evaluate(optionCall, null, contract);
            var callPrice = results.TheoreticalPrice;
            var greeks = results.Greeks;

            // BS equation
            var rightPart = greeks.Theta + riskFreeRate * underlyingPrice * greeks.Delta + 0.5m * underlyingVol * underlyingVol * underlyingPrice * underlyingPrice * greeks.Gamma;
            var leftPart = riskFreeRate * callPrice;

            Assert.AreEqual((double)leftPart, (double)rightPart, 0.0001);
        }

        [Test]
        public void BaroneAdesiWhaleyPortfolioTest()
        {
            const decimal price = 30.00m;
            const decimal underlyingPrice = 200m;
            const decimal underlyingVol = 0.25m;
            const decimal riskFreeRate = 0.01m;
            var tz = TimeZones.NewYork;
            var evaluationDate = new DateTime(2015, 2, 19);

            var equity = new Equity(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            equity.VolatilityModel = new DummyVolatilityModel(underlyingVol);

            var contract = new OptionContract(Symbols.SPY_C_192_Feb19_2016, Symbols.SPY) { Time = evaluationDate };
            var optionCall = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY_C_192_Feb19_2016,
                    Resolution.Minute,
                    tz,
                    tz,
                    true,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Underlying = equity;

            var priceModel = OptionPriceModels.BaroneAdesiWhaley();
            var results = priceModel.Evaluate(optionCall, null, contract);

            var callPrice = results.TheoreticalPrice;
            var impliedVolatility = results.ImpliedVolatility;
            var greeks = results.Greeks;

            Assert.Greater(price, callPrice);
            Assert.Greater(impliedVolatility, underlyingVol);

            // BS equation (inequality)
            var rightPart = greeks.Theta + riskFreeRate * underlyingPrice * greeks.Delta + 0.5m * underlyingVol * underlyingVol * underlyingPrice * underlyingPrice * greeks.Gamma;
            var leftPart = riskFreeRate * callPrice;

            Assert.GreaterOrEqual(Math.Round(leftPart, 4), Math.Round(rightPart,4));
        }

        [Test]
        public void EvaluationDateWorksInPortfolioTest()
        {
            const decimal price = 30.00m;
            const decimal underlyingPrice = 200m;
            const decimal underlyingVol = 0.25m;
            const decimal riskFreeRate = 0.01m;
            var tz = TimeZones.NewYork;
            var evaluationDate1 = new DateTime(2015, 2, 19);
            var evaluationDate2 = new DateTime(2015, 2, 20);

            var equity = new Equity(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            equity.VolatilityModel = new DummyVolatilityModel(underlyingVol);

            var contract = new OptionContract(Symbols.SPY_C_192_Feb19_2016, Symbols.SPY) { Time = evaluationDate1 };
            var optionCall = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY_C_192_Feb19_2016,
                    Resolution.Minute,
                    tz,
                    tz,
                    true,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionCall.SetMarketPrice(new Tick { Value = price });
            optionCall.Underlying = equity;

            var priceModel = OptionPriceModels.BaroneAdesiWhaley();
            var results = priceModel.Evaluate(optionCall, null, contract);

            var callPrice1 = results.TheoreticalPrice;

            contract.Time = evaluationDate2;
            results = priceModel.Evaluate(optionCall, null, contract);

            var callPrice2 = results.TheoreticalPrice;
            Assert.Greater(callPrice1, callPrice2);
        }


        [Test]
        public void GreekApproximationTest()
        {
            const decimal price = 20.00m;
            const decimal underlyingPrice = 190m;
            const decimal underlyingVol = 0.15m;
            var tz = TimeZones.NewYork;
            var evaluationDate = new DateTime(2016, 1, 19);

            var equity = new Equity(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            equity.VolatilityModel = new DummyVolatilityModel(underlyingVol);

            var contract = new OptionContract(Symbols.SPY_P_192_Feb19_2016, Symbols.SPY) { Time = evaluationDate };
            var optionPut = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(
                    typeof(TradeBar),
                    Symbols.SPY_P_192_Feb19_2016,
                    Resolution.Minute,
                    tz,
                    tz,
                    true,
                    false,
                    false
                ),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            optionPut.SetMarketPrice(new Tick { Value = price });
            optionPut.Underlying = equity;

            var priceModel = (QLOptionPriceModel)OptionPriceModels.CrankNicolsonFD();
            priceModel.EnableGreekApproximation = false;

            var results = priceModel.Evaluate(optionPut, null, contract);
            var greeks = results.Greeks;

            Assert.AreEqual(greeks.Theta, 0);
            Assert.AreEqual(greeks.Rho, 0);
            Assert.AreEqual(greeks.Vega, 0);

            priceModel = (QLOptionPriceModel)OptionPriceModels.CrankNicolsonFD();
            priceModel.EnableGreekApproximation = true;

            results = priceModel.Evaluate(optionPut, null, contract);
            greeks = results.Greeks;

            Assert.LessOrEqual(greeks.Theta, 0);
            Assert.AreNotEqual(greeks.Rho, 0);
            Assert.Greater(greeks.Vega, 0);
        }

        /// <summary>
        /// Dummy implementation of volatility model (for tests only)
        /// </summary>
        class DummyVolatilityModel : IVolatilityModel
        {
            private decimal _volatility;

            public DummyVolatilityModel(decimal volatility)
            {
                _volatility = volatility;
            }
            public decimal Volatility
            {
                get
                {
                    return _volatility;
                }
            }

            public IEnumerable<HistoryRequest> GetHistoryRequirements(Security security, DateTime date)
            {
                return Enumerable.Empty<HistoryRequest>();
            }

            public void Update(Security security, BaseData data)
            {
            }
        }
    }
}
