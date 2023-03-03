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

using Moq;
using NodaTime;
using NUnit.Framework;
using QLNet;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cash = QuantConnect.Securities.Cash;
using Option = QuantConnect.Securities.Option.Option;

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
            optionCall.SetMarketPrice(new Tick { Value = 17m });  // dummy non-zero price
            // setting up European style put option
            var contractPut = GetOptionContract(SPY_P_192_Feb19_2016E, spy, evaluationDate);
            var optionPut = GetOption(SPY_P_192_Feb19_2016E, equity, tz);
            optionPut.SetMarketPrice(new Tick { Value = 7m });  // dummy non-zero price

            // running evaluation
            var priceModel = OptionPriceModels.BlackScholes();
            var resultsCall = priceModel.Evaluate(optionCall, null, contractCall);
            var resultsPut = priceModel.Evaluate(optionPut, null, contractPut);
            var callPrice = resultsCall.TheoreticalPrice;
            var putPrice = resultsPut.TheoreticalPrice;

            // Put-call parity equation
            var rightPart = putPrice + underlyingPrice; // no yield
            var leftPart = callPrice + contractCall.Strike * (decimal)Math.Exp((double)-riskFreeRate);

            Assert.AreEqual((double)leftPart, (double)rightPart, (double)rightPart * 0.001);
        }

        [Test]
        public void ExpirationDate()
        {
            const decimal price = 20.00m;
            const decimal underlyingPrice = 200m;
            const decimal underlyingVol = 0.15m;
            var tz = TimeZones.NewYork;
            var spy = Symbols.SPY;
            var SPY_C_192_Feb19_2016E = GetOptionSymbol(spy, OptionStyle.European, OptionRight.Call);

            // setting up underlying
            var equity = GetEquity(spy, underlyingPrice, underlyingVol, tz);

            // setting up European style call option
            var contract = GetOptionContract(SPY_C_192_Feb19_2016E, spy, DateTime.MinValue);
            var optionCall = GetOption(SPY_C_192_Feb19_2016E, equity, tz);
            optionCall.SetMarketPrice(new Tick { Value = price });

            // running evaluation
            var priceModel = OptionPriceModels.BlackScholes();

            OptionPriceModelResult results;
            foreach (var date in new[] { optionCall.Expiry.AddDays(-1), optionCall.Expiry })
            {
                contract.Time = date;
                results = priceModel.Evaluate(optionCall, null, contract);

                Assert.AreNotEqual(0, results.TheoreticalPrice);
                Assert.AreNotEqual(0, results.Greeks.Gamma);
                Assert.AreNotEqual(0, results.Greeks.Vega);
                Assert.AreNotEqual(0, results.Greeks.Delta);
                Assert.AreNotEqual(0, results.Greeks.Lambda);
                Assert.AreNotEqual(0, results.Greeks.Theta);
            }

            // and post expiration they are 0
            contract.Time = optionCall.Expiry.AddDays(1);
            results = priceModel.Evaluate(optionCall, null, contract);

            Assert.AreEqual(0, results.TheoreticalPrice);
            Assert.AreEqual(0, results.Greeks.Gamma);
            Assert.AreEqual(0, results.Greeks.Vega);
            Assert.AreEqual(0, results.Greeks.Delta);
            Assert.AreEqual(0, results.Greeks.Lambda);
            Assert.AreEqual(0, results.Greeks.Theta);
        }

        [Test]
        public void ChangesWithEvaluationDate()
        {
            const decimal price = 20.00m;
            const decimal underlyingPrice = 200m;
            const decimal underlyingVol = 0.15m;
             var tz = TimeZones.NewYork;
            var spy = Symbols.SPY;
            var SPY_C_192_Feb19_2016E = GetOptionSymbol(spy, OptionStyle.European, OptionRight.Call);

            // setting up underlying
            var equity = GetEquity(spy, underlyingPrice, underlyingVol, tz);

            // setting up European style call option
            var contract = GetOptionContract(SPY_C_192_Feb19_2016E, spy, DateTime.MinValue);
            var optionCall = GetOption(SPY_C_192_Feb19_2016E, equity, tz);
            optionCall.SetMarketPrice(new Tick { Value = price });

            // running evaluation
            var priceModel = OptionPriceModels.BlackScholes();

            contract.Time = new DateTime(2015, 02, 19);
            var results1 = priceModel.Evaluate(optionCall, null, contract);
            // we need to get the greeks else they will calculated bellow after we change the static evaluation date
            var gamma = results1.Greeks.Gamma;
            var delta = results1.Greeks.Delta;
            var vega = results1.Greeks.Delta;
            var lambda = results1.Greeks.Lambda;
            var theta = results1.Greeks.Theta;

            contract.Time = new DateTime(2015, 12, 4);
            var results2 = priceModel.Evaluate(optionCall, null, contract);

            Assert.AreNotEqual(results1.TheoreticalPrice, results2.TheoreticalPrice);

            Assert.AreNotEqual(gamma, results2.Greeks.Gamma);
            Assert.AreNotEqual(vega, results2.Greeks.Vega);
            Assert.AreNotEqual(delta, results2.Greeks.Delta);
            Assert.AreNotEqual(lambda, results2.Greeks.Lambda);
            Assert.AreNotEqual(theta, results2.Greeks.Theta);
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
            var impliedVol = results.ImpliedVolatility;
            var greeks = results.Greeks;

            // BS equation
            var rightPart = greeks.Theta + riskFreeRate * underlyingPrice * greeks.Delta + 0.5m * impliedVol * impliedVol * underlyingPrice * underlyingPrice * greeks.Gamma;
            var leftPart = riskFreeRate * price;

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

            var rightPart = greeks.Theta + riskFreeRate * underlyingPrice * greeks.Delta + 0.5m * impliedVolatility * impliedVolatility * underlyingPrice * underlyingPrice * greeks.Gamma;
            var leftPart = riskFreeRate * price;
            Assert.AreEqual((double)leftPart, (double)rightPart, 0.0001);
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
            optionCall.SetMarketPrice(new Tick { Value = 17m });  // dummy non-zero price

            // setting up European style put option
            var contractPut = GetOptionContract(SPY_P_192_Feb19_2016E, spy, evaluationDate);
            var optionPut = GetOption(SPY_P_192_Feb19_2016E, equity, tz);
            optionPut.SetMarketPrice(new Tick { Value = 7m });  // dummy non-zero price

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

        [TestCase(OptionRight.Call, 200, 20.80707831, 0.56552801, 0.00787097, -0.02948410, 0.78709695, 0.92298523)]         // ATM
        [TestCase(OptionRight.Call, 250, 6.06011876, 0.23343714, 0.00612336, -0.02208350, 0.61233641, 0.40627309)]          // deep OTM
        [TestCase(OptionRight.Call, 150, 53.94314691, 0.90586737, 0.00335759, -0.01498436, 0.33575894, 1.27230328)]         // deep ITM
        [TestCase(OptionRight.Put, 200, 18.90107225, -0.43447199, 0.00787097, -0.02405917, 0.78709695, -1.05711443)]        // ATM
        [TestCase(OptionRight.Put, 150, 2.45317094, -0.09413263, 0.00335759, -0.01091566, 0.33575894, -0.21277148)]         // deep ITM
        [TestCase(OptionRight.Put, 250, 54.08980489, -0.76656286, 0.00612336, -0.01530234, 0.61233641, -2.07837775)]        // deep OTM
        public void Greeks(OptionRight optionRight, decimal strike, decimal price, decimal ibDelta, decimal ibGamma, decimal ibTheta, decimal ibVega, decimal ibRho)
        {
            const decimal underlyingPrice = 200m;
            const decimal underlyingVol = 0.25m;
            var tz = TimeZones.NewYork;
            var evaluationDate = new DateTime(2015, 2, 19);
            var spy = Symbols.SPY;
            var optionSymbol = GetOptionSymbol(spy, OptionStyle.American, optionRight, strike);

            // setting up
            var equity = GetEquity(spy, underlyingPrice, underlyingVol, tz);
            var contract = GetOptionContract(optionSymbol, spy, evaluationDate);
            var option = GetOption(optionSymbol, equity, tz);
            option.SetMarketPrice(new Tick { Value = price });

            // running evaluation
            var priceModel = OptionPriceModels.BjerksundStensland();
            var results = priceModel.Evaluate(option, null, contract);
            var greeks = results.Greeks;

            // Expect minor error due to interest rate and dividend yield used in IB
            Assert.AreEqual((double)greeks.Delta, (double)ibDelta, 0.005);
            Assert.AreEqual((double)greeks.Gamma, (double)ibGamma, 0.005);
            Assert.AreEqual((double)greeks.Theta / 365.25, (double)ibTheta, 0.005);
            Assert.AreEqual((double)greeks.Vega, (double)ibVega, 0.005);
            Assert.AreEqual((double)greeks.Rho, (double)ibRho, 0.005);
        }

        [TestCase(OptionRight.Call, 200, 24.76, 0.3003)]         // ATM
        [TestCase(OptionRight.Call, 250, 12.33, 0.3430)]         // deep OTM
        [TestCase(OptionRight.Call, 150, 57.24, 0.3323)]         // deep ITM
        [TestCase(OptionRight.Put, 200, 22.02, 0.2907)]          // ATM
        [TestCase(OptionRight.Put, 180, 15.50, 0.3312)]          // deep ITM
        [TestCase(OptionRight.Put, 220, 36.59, 0.3225)]          // deep OTM
        public void ImpliedVolatilityEstimator(OptionRight optionRight, decimal strike, double price, double ibImpliedVol)
        {
            const double underlyingPrice = 200d;
            var evaluationDate = new DateTime(2015, 2, 19);
            var spy = Symbols.SPY;
            var optionSymbol = GetOptionSymbol(spy, OptionStyle.American, optionRight, strike);

            // setting up
            var contract = GetOptionContract(optionSymbol, spy, evaluationDate);
            var payoff = new PlainVanillaPayoff(contract.Right == OptionRight.Call ? QLNet.Option.Type.Call : QLNet.Option.Type.Put, (double)contract.Strike);
            var forwardPrice = underlyingPrice / 0.99d;
            BlackCalculator black = null;

            // running evaluation with 0% dividend yield and 1% interest rate
            var initialGuess = Math.Sqrt(2 * Math.PI) * price / underlyingPrice;
            var priceModel = new TestOptionPriceModel();
            var impliedVolEstimate = priceModel.TestImpliedVolEstimator(price, initialGuess, 1, 0.99d, forwardPrice, payoff, out black);

            // Expect minor error due to interest rate and dividend yield used in IB
            Assert.AreEqual(impliedVolEstimate, ibImpliedVol, 0.001);
        }

        [Test]
        public void PriceModelEvaluateSpeedTest()
        {
            const decimal underlyingPrice = 3820.08m;
            const decimal underlyingVol = 0.2m;
            var tz = TimeZones.NewYork;
            var evaluationDate = new DateTime(2021, 1, 14);
            var spx = Symbols.SPX;
            var optionSymbol = Symbol.CreateOption(spx.Value, spx.ID.Market, OptionStyle.European, OptionRight.Put, 4200,
                new DateTime(2021, 1, 15));

            // setting up
            var equity = GetEquity(spx, underlyingPrice, underlyingVol, tz);
            var contract = GetOptionContract(optionSymbol, spx, evaluationDate);
            var option = GetOption(optionSymbol, equity, tz);
            option.SetMarketPrice(new Tick { Value = 379.45m });

            // running evaluation
            var priceModel = OptionPriceModels.BlackScholes();

            var results = priceModel.Evaluate(option, null, contract);
            var greeks = results.Greeks;
            Assert.IsNotNull(results.ImpliedVolatility);
            Assert.IsNotNull(greeks.Delta);
            Assert.IsNotNull(greeks.Gamma);
            Assert.IsNotNull(greeks.Theta);
            Assert.IsNotNull(greeks.Vega);
            Assert.IsNotNull(greeks.Rho);

            Thread.Sleep(500);

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for (var i = 0; i < 1000; i++)
            {
                results = priceModel.Evaluate(option, null, contract);
                greeks = results.Greeks;

                // Expect minor error due to interest rate and dividend yield used in IB
                Assert.IsNotNull(results.ImpliedVolatility);
                Assert.IsNotNull(greeks.Delta);
                Assert.IsNotNull(greeks.Gamma);
                Assert.IsNotNull(greeks.Theta);
                Assert.IsNotNull(greeks.Vega);
                Assert.IsNotNull(greeks.Rho);
            }
            stopWatch.Stop();
            Assert.Less(stopWatch.ElapsedMilliseconds, 2200);
        }

        private Symbol GetOptionSymbol(Symbol underlying, OptionStyle optionStyle, OptionRight optionRight, decimal strike = 192m)
        {
            return Symbol.CreateOption(underlying.Value, Market.USA, optionStyle, optionRight, strike, new DateTime(2016, 02, 19));
        }

        public static Equity GetEquity(Symbol symbol, decimal underlyingPrice, decimal underlyingVol, NodaTime.DateTimeZone tz)
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

        public static Option GetOption(Symbol symbol, Equity underlying, NodaTime.DateTimeZone tz)
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

            public void ApplyDividend(QuantConnect.Data.Market.Dividend dividend, bool liveMode, DataNormalizationMode dataNormalizationMode)
            {
            }

            public void ApplySplit(Split split, bool liveMode, DataNormalizationMode dataNormalizationMode)
            {
            }

            public void WarmUp(IHistoryProvider historyProvider, Security security, DateTime utcTime, DateTimeZone timeZone)
            {
            }

            public void Reset()
            {
            }
        }

        class TestOptionPriceModel : QLOptionPriceModel
        {
            public TestOptionPriceModel()
                : base(process => new BjerksundStenslandApproximationEngine(process), null, null, null)
            {
            }

            public double TestImpliedVolEstimator(double price, double initialGuess, double timeTillExpiry, double riskFreeDiscount,
                                                  double forwardPrice, PlainVanillaPayoff payoff, out BlackCalculator black)
            {
                return base.ImpliedVolatilityEstimation(price, initialGuess, timeTillExpiry, riskFreeDiscount, forwardPrice, payoff, out black);
            }
        }
    }
}
