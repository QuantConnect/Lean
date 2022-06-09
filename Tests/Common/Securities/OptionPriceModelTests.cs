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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using QLNet;
using Cash = QuantConnect.Securities.Cash;
using Option = QuantConnect.Securities.Option.Option;
using Log = QuantConnect.Logging.Log;

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
            var spy = Symbols.SPY;
            var SPY_C_192_Feb19_2016E = GetOptionSymbol(spy, OptionStyle.European, OptionRight.Call);
            var SPY_P_192_Feb19_2016E = GetOptionSymbol(spy, OptionStyle.European, OptionRight.Put);

            // setting up underlying
            var equity = GetEquity(spy, underlyingPrice, underlyingVol, tz);

            // setting up European style call option
            var contractCall = GetOptionContract(SPY_C_192_Feb19_2016E, spy, evaluationDate);
            var optionCall = GetOption(SPY_C_192_Feb19_2016E, equity, tz);

            // setting up European style put option
            var contractPut = GetOptionContract(SPY_P_192_Feb19_2016E, spy, evaluationDate);
            var optionPut = GetOption(SPY_P_192_Feb19_2016E, equity, tz);

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
            var spy = Symbols.SPY;
            var SPY_C_192_Feb19_2016E = GetOptionSymbol(spy, OptionStyle.European, OptionRight.Call);

            // setting up underlying
            var equity = GetEquity(spy, underlyingPrice, underlyingVol, tz);

            // setting up European style call option
            var contract = GetOptionContract(SPY_C_192_Feb19_2016E, spy, evaluationDate);
            var optionCall = GetOption(SPY_C_192_Feb19_2016E, equity, tz);
            optionCall.SetMarketPrice(new Tick { Value = price });

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
            var spy = Symbols.SPY;
            var evaluationDate = new DateTime(2015, 2, 19);
            var SPY_C_192_Feb19_2016E = GetOptionSymbol(spy, OptionStyle.American, OptionRight.Call);

            var equity = GetEquity(spy, underlyingPrice, underlyingVol, tz);

            var contract = new OptionContract(Symbols.SPY_C_192_Feb19_2016, Symbols.SPY) { Time = evaluationDate };
            var optionCall = GetOption(SPY_C_192_Feb19_2016E, equity, tz);
            optionCall.SetMarketPrice(new Tick { Value = price });

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

            Assert.GreaterOrEqual(Math.Round(leftPart, 4), Math.Round(rightPart, 4));
        }

        [Test]
        public void EvaluationDateWorksInPortfolioTest()
        {
            const decimal price = 30.00m;
            const decimal underlyingPrice = 200m;
            const decimal underlyingVol = 0.25m;
            var tz = TimeZones.NewYork;
            var spy = Symbols.SPY;
            var evaluationDate1 = new DateTime(2015, 2, 19);
            var evaluationDate2 = new DateTime(2015, 2, 20);
            var SPY_C_192_Feb19_2016E = GetOptionSymbol(spy, OptionStyle.American, OptionRight.Call);

            var equity = GetEquity(spy, underlyingPrice, underlyingVol, tz);

            var contract = GetOptionContract(SPY_C_192_Feb19_2016E, spy, evaluationDate1);
            var optionCall = GetOption(SPY_C_192_Feb19_2016E, equity, tz);
            optionCall.SetMarketPrice(new Tick { Value = price });

            var priceModel = OptionPriceModels.BaroneAdesiWhaley();
            var results = priceModel.Evaluate(optionCall, null, contract);

            var callPrice1 = results.TheoreticalPrice;

            contract.Time = evaluationDate2;
            results = priceModel.Evaluate(optionCall, null, contract);

            var callPrice2 = results.TheoreticalPrice;
            Assert.Greater(callPrice1, callPrice2);
        }

        [Test]
        [TestCase("BaroneAdesiWhaleyApproximationEngine")]
        [TestCase("QLNet.BaroneAdesiWhaleyApproximationEngine")]
        public void CreatesOptionPriceModelByName(string priceEngineName)
        {
            IOptionPriceModel priceModel = null;
            Assert.DoesNotThrow(() =>
            {
                priceModel = OptionPriceModels.Create(priceEngineName, 0.01m);
            });

            Assert.NotNull(priceModel);
            Assert.IsInstanceOf<QLOptionPriceModel>(priceModel);
        }

        [Test]
        public void GreekApproximationTest()
        {
            const decimal price = 20.00m;
            const decimal underlyingPrice = 190m;
            const decimal underlyingVol = 0.15m;
            var tz = TimeZones.NewYork;
            var evaluationDate = new DateTime(2016, 1, 19);
            var spy = Symbols.SPY;

            var equity = GetEquity(spy, underlyingPrice, underlyingVol, tz);

            var contract = GetOptionContract(Symbols.SPY_P_192_Feb19_2016, spy, evaluationDate);

            var optionPut = GetOption(Symbols.SPY_P_192_Feb19_2016, equity, tz);
            optionPut.SetMarketPrice(new Tick { Value = price });

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

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void HasBeenWarmedUp(bool warmUp)
        {
            var volatilityModel = new Mock<IQLUnderlyingVolatilityEstimator>();
            volatilityModel.SetupGet(s => s.IsReady).Returns(warmUp);
            var priceModel = new QLOptionPriceModel(
                process => new IntegralEngine(process),
                volatilityModel.Object,
                null,
                null);

            Assert.AreEqual(warmUp, priceModel.VolatilityEstimatorWarmedUp);
        }

        [Test]
        public void ReturnsNoneIfNotWarmedUp()
        {
            const decimal underlyingPrice = 200m;
            const decimal underlyingVol = 0.15m;
            var tz = TimeZones.NewYork;
            var evaluationDate = new DateTime(2015, 2, 19);
            var spy = Symbols.SPY;
            var SPY_C_192_Feb19_2016E = GetOptionSymbol(spy, OptionStyle.European, OptionRight.Call);
            var SPY_P_192_Feb19_2016E = GetOptionSymbol(spy, OptionStyle.European, OptionRight.Put);

            // setting up underlying
            var equity = GetEquity(spy, underlyingPrice, underlyingVol, tz);

            // setting up European style call option
            var contractCall = GetOptionContract(SPY_C_192_Feb19_2016E, spy, evaluationDate);
            var optionCall = GetOption(SPY_C_192_Feb19_2016E, equity, tz);

            // setting up European style put option
            var contractPut = GetOptionContract(SPY_P_192_Feb19_2016E, spy, evaluationDate);
            var optionPut = GetOption(SPY_P_192_Feb19_2016E, equity, tz);

            // running evaluation
            var volatilityModel = new Mock<IQLUnderlyingVolatilityEstimator>();
            volatilityModel.SetupGet(s => s.IsReady).Returns(false);
            var priceModel = new QLOptionPriceModel(process => new AnalyticEuropeanEngine(process),
                volatilityModel.Object,
                null,
                null);
            var resultsCall = priceModel.Evaluate(optionCall, null, contractCall);
            var resultsPut = priceModel.Evaluate(optionPut, null, contractPut);

            Assert.AreEqual(OptionPriceModelResult.None, resultsCall);
            Assert.AreEqual(OptionPriceModelResult.None, resultsCall);
        }

        [Test]
        [TestCase("BlackScholes", OptionStyle.American, true)]
        [TestCase("BlackScholes", OptionStyle.European, false)]
        [TestCase("Integral", OptionStyle.American, true)]
        [TestCase("Integral", OptionStyle.European, false)]
        [TestCase("BaroneAdesiWhaley", OptionStyle.American, false)]
        [TestCase("BaroneAdesiWhaley", OptionStyle.European, true)]
        [TestCase("BjerksundStensland", OptionStyle.American, false)]
        [TestCase("BjerksundStensland", OptionStyle.European, true)]
        public void ThrowsIfOptionStyleIsNotSupportedByQLPricingModel(string qlModelName, OptionStyle optionStyle, bool shouldThrow)
        {
            const decimal underlyingPrice = 200m;
            const decimal underlyingVol = 0.15m;
            var tz = TimeZones.NewYork;
            var evaluationDate = new DateTime(2015, 2, 19);
            var spy = Symbols.SPY;
            var SPY_C_192_Feb19_2016E = GetOptionSymbol(spy, optionStyle, OptionRight.Call);
            var SPY_P_192_Feb19_2016E = GetOptionSymbol(spy, optionStyle, OptionRight.Put);

            // setting up underlying
            var equity = GetEquity(spy, underlyingPrice, underlyingVol, tz);

            // setting up European style call option
            var contractCall = GetOptionContract(SPY_C_192_Feb19_2016E, spy, evaluationDate);
            var optionCall = GetOption(SPY_C_192_Feb19_2016E, equity, tz);

            // setting up European style put option
            var contractPut = GetOptionContract(SPY_P_192_Feb19_2016E, spy, evaluationDate);
            var optionPut = GetOption(SPY_P_192_Feb19_2016E, equity, tz);

            // running evaluation
            var priceModel = (IOptionPriceModel)typeof(OptionPriceModels).GetMethod(qlModelName).Invoke(null, new object[]{});
            TestDelegate call = () => priceModel.Evaluate(optionCall, null, contractCall);
            TestDelegate put = () => priceModel.Evaluate(optionPut, null, contractPut);

            if (shouldThrow)
            {
                Assert.Throws<ArgumentException>(call);
                Assert.Throws<ArgumentException>(put);
            }
            else
            {
                Assert.DoesNotThrow(call);
                Assert.DoesNotThrow(put);

                var results = priceModel.Evaluate(optionCall, null, contractCall);
                var greeks = results.Greeks;

                Assert.That(greeks.Delta, Is.InRange(0, 1m));
                Assert.Less(greeks.Theta, 0);
                Assert.Greater(greeks.Rho, 0m);
                Assert.Greater(greeks.Vega, 0m);

                results = priceModel.Evaluate(optionPut, null, contractPut);
                greeks = results.Greeks;

                Assert.That(greeks.Delta, Is.InRange(-1m, 0));
                Assert.Less(greeks.Theta, 0);
                Assert.Less(greeks.Rho, 0m);
                Assert.Greater(greeks.Vega, 0m);
            }
        }

        private Symbol GetOptionSymbol(Symbol underlying, OptionStyle optionStyle, OptionRight optionRight)
        {
            return Symbol.CreateOption(underlying.Value, Market.USA, optionStyle, optionRight, 192m, new DateTime(2016, 02, 19));
        }

        private Equity GetEquity(Symbol symbol, decimal underlyingPrice, decimal underlyingVol, NodaTime.DateTimeZone tz)
        {
            var equity = new Equity(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            equity.VolatilityModel = new DummyVolatilityModel(underlyingVol);

            return equity;
        }

        public OptionContract GetOptionContract(Symbol symbol, Symbol underlying, DateTime evaluationDate)
        {
            return new OptionContract(symbol, underlying) { Time = evaluationDate };
        }

        public Option GetOption(Symbol symbol, Equity underlying, NodaTime.DateTimeZone tz)
        {
            var option = new Option(
                SecurityExchangeHours.AlwaysOpen(tz),
                new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, tz, tz, true, false, false),
                new Cash(Currencies.USD, 0, 1m),
                new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null
            );
            option.Underlying = underlying;

            return option;
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
