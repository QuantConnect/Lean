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
using NUnit.Framework;
using QLNet;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
            var option = CreateOption(SPY_C_192_Feb19_2016E);

            var equity = GetEquity(spy, underlyingPrice, underlyingVol, tz);

            var contract = new OptionContract(option, Symbols.SPY) { Time = evaluationDate };
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

        /// This test aim to comapre the maximum greek calculation error between models (2dp)
        /// as well as a benchmark of each model to monitor future changes
        [TestCase(OptionStyle.American, "AdditiveEquiprobabilities", 0.01d, 0.03d, 0.01d, 0.03d, 77d)]
        [TestCase(OptionStyle.American, "BaroneAdesiWhaley", 0.01d, 0.03d, 0.01d, 0.03d, 77d)]
        [TestCase(OptionStyle.American, "BinomialCoxRossRubinstein", 0.01d, 0.03d, 0.01d, 0.03d, 77d)]
        [TestCase(OptionStyle.American, "BinomialJarrowRudd", 0.01d, 0.03d, 0.01d, 0.03d, 77d)]
        [TestCase(OptionStyle.American, "BinomialJoshi", 0.01d, 0.03d, 0.01d, 0.03d, 78d)]
        [TestCase(OptionStyle.American, "BinomialLeisenReimer", 0.01d, 0.03d, 0.01d, 0.03d, 78d)]
        [TestCase(OptionStyle.American, "BinomialTian", 0.01d, 0.03d, 0.01d, 0.03d, 77d)]
        [TestCase(OptionStyle.American, "BinomialTrigeorgis", 0.01d, 0.03d, 0.01d, 0.03d, 77d)]
        [TestCase(OptionStyle.American, "BjerksundStensland", 0.01d, 0.03d, 0.01d, 0.03d, 77d)]
        [TestCase(OptionStyle.American, "CrankNicolsonFD", 0.01d, 0.03d, 0.01d, 0.03d, 508d)]
        [TestCase(OptionStyle.European, "AdditiveEquiprobabilities", 0.01d, 0.01d, 0.01d, 0.33d, 724d)]
        [TestCase(OptionStyle.European, "BinomialCoxRossRubinstein", 0.01d, 0.01d, 0.01d, 0.33d, 724d)]
        [TestCase(OptionStyle.European, "BinomialJarrowRudd", 0.01d, 0.01d, 0.01d, 0.33d, 723d)]
        [TestCase(OptionStyle.European, "BinomialJoshi", 0.01d, 0.01d, 0.01d, 0.33d, 731d)]
        [TestCase(OptionStyle.European, "BinomialLeisenReimer", 0.01d, 0.01d, 0.01d, 0.33d, 731d)]
        [TestCase(OptionStyle.European, "BinomialTian", 0.01d, 0.01d, 0.01d, 0.33d, 723d)]
        [TestCase(OptionStyle.European, "BinomialTrigeorgis", 0.01d, 0.01d, 0.01d, 0.33d, 724d)]
        [TestCase(OptionStyle.European, "BlackScholes", 0.01d, 0.01d, 0.01d, 0.21d, 724d)]
        [TestCase(OptionStyle.European, "CrankNicolsonFD", 0.01d, 0.01d, 0.01d, 0.33d, 724d)]
        [TestCase(OptionStyle.European, "Integral", 0.01d, 0.12d, 0.01d, 0.33d, 4882d)]
        public void MatchesIBGreeksNearATMCall(OptionStyle style, string qlModelName, double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        {
            var filename = style == OptionStyle.American ? "SPY230811C00450000" : "SPX230811C04500000";
            var symbol = Symbols.SPY;       // dummy
            var strike = Parse.Decimal(filename[10..]) / 1000m;
            var optionSymbol = GetOptionSymbol(symbol, style, OptionRight.Call, strike, new DateTime(2023, 8, 11));

            MatchesIBGreeksTest(symbol, optionSymbol, filename, qlModelName, errorIV, errorDelta, errorGamma, errorVega, errorTheta);
        }

        /// This test aim to comapre the maximum greek calculation error between models (2dp)
        /// as well as a benchmark of each model to monitor future changes
        [TestCase(OptionStyle.American, "AdditiveEquiprobabilities", 0.03d, 0.05d, 0.01d, 0.02d, 48d)]
        [TestCase(OptionStyle.American, "BaroneAdesiWhaley", 0.03d, 0.05d, 0.01d, 0.02d, 49d)]
        [TestCase(OptionStyle.American, "BinomialCoxRossRubinstein", 0.03d, 0.05d, 0.01d, 0.02d, 48d)]
        [TestCase(OptionStyle.American, "BinomialJarrowRudd", 0.03d, 0.05d, 0.01d, 0.02d, 48d)]
        [TestCase(OptionStyle.American, "BinomialJoshi", 0.03d, 0.05d, 0.01d, 0.02d, 49d)]
        [TestCase(OptionStyle.American, "BinomialLeisenReimer", 0.03d, 0.05d, 0.01d, 0.02d, 49d)]
        [TestCase(OptionStyle.American, "BinomialTian", 0.03d, 0.05d, 0.01d, 0.02d, 48d)]
        [TestCase(OptionStyle.American, "BinomialTrigeorgis", 0.03d, 0.05d, 0.01d, 0.02d, 48d)]
        [TestCase(OptionStyle.American, "BjerksundStensland", 0.03d, 0.05d, 0.01d, 0.02d, 49d)]
        [TestCase(OptionStyle.American, "CrankNicolsonFD", 0.03d, 0.05d, 0.01d, 0.02d, 165d)]
        [TestCase(OptionStyle.European, "AdditiveEquiprobabilities", 0.01d, 0.03d, 0.01d, 0.22d, 450d)]
        [TestCase(OptionStyle.European, "BinomialCoxRossRubinstein", 0.01d, 0.03d, 0.01d, 0.22d, 450d)]
        [TestCase(OptionStyle.European, "BinomialJarrowRudd", 0.01d, 0.03d, 0.01d, 0.22d, 450d)]
        [TestCase(OptionStyle.European, "BinomialJoshi", 0.01d, 0.03d, 0.01d, 0.22d, 455d)]
        [TestCase(OptionStyle.European, "BinomialLeisenReimer", 0.01d, 0.03d, 0.01d, 0.22d, 455d)]
        [TestCase(OptionStyle.European, "BinomialTian", 0.01d, 0.03d, 0.01d, 0.22d, 451d)]
        [TestCase(OptionStyle.European, "BinomialTrigeorgis", 0.01d, 0.03d, 0.01d, 0.22d, 450d)]
        [TestCase(OptionStyle.European, "BlackScholes", 0.01d, 0.03d, 0.01d, 0.14d, 453d)]
        [TestCase(OptionStyle.European, "CrankNicolsonFD", 0.01d, 0.03d, 0.01d, 0.22d, 453d)]
        [TestCase(OptionStyle.European, "Integral", 0.01d, 0.06d, 0.01d, 0.22d, 1555d)]
        public void MatchesIBGreeksFarATMCall(OptionStyle style, string qlModelName, double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        {
            var filename = style == OptionStyle.American ? "SPY230901C00450000" : "SPX230901C04500000";
            var symbol = Symbols.SPY;       // dummy
            var strike = Parse.Decimal(filename[10..]) / 1000m;
            var optionSymbol = GetOptionSymbol(symbol, style, OptionRight.Call, strike, new DateTime(2023, 9, 1));

            MatchesIBGreeksTest(symbol, optionSymbol, filename, qlModelName, errorIV, errorDelta, errorGamma, errorVega, errorTheta);
        }

        /// This test aim to comapre the maximum greek calculation error between models (2dp)
        /// as well as a benchmark of each model to monitor future changes
        [TestCase(OptionStyle.American, "AdditiveEquiprobabilities", 0.02d, 0.02d, 0.01d, 0.03d, 64d)]
        [TestCase(OptionStyle.American, "BaroneAdesiWhaley", 0.02d, 0.08d, 0.05d, 0.03d, 447d)]
        [TestCase(OptionStyle.American, "BinomialCoxRossRubinstein", 0.02d, 0.02d, 0.01d, 0.03d, 64d)]
        [TestCase(OptionStyle.American, "BinomialJarrowRudd", 0.02d, 0.02d, 0.01d, 0.03d, 64d)]
        [TestCase(OptionStyle.American, "BinomialJoshi", 0.02d, 0.02d, 0.01d, 0.03d, 65d)]
        [TestCase(OptionStyle.American, "BinomialLeisenReimer", 0.02d, 0.02d, 0.01d, 0.03d, 65d)]
        [TestCase(OptionStyle.American, "BinomialTian", 0.02d, 0.02d, 0.01d, 0.03d, 64d)]
        [TestCase(OptionStyle.American, "BinomialTrigeorgis", 0.02d, 0.02d, 0.01d, 0.03d, 64d)]
        [TestCase(OptionStyle.American, "BjerksundStensland", 0.02d, 0.08d, 0.05d, 0.03d, 447d)]
        [TestCase(OptionStyle.American, "CrankNicolsonFD", 0.02d, 0.02d, 0.01d, 0.03d, 447d)]
        [TestCase(OptionStyle.European, "AdditiveEquiprobabilities", 0.01d, 0.01d, 0.01d, 0.33d, 641d)]
        [TestCase(OptionStyle.European, "BinomialCoxRossRubinstein", 0.01d, 0.01d, 0.01d, 0.33d, 641d)]
        [TestCase(OptionStyle.European, "BinomialJarrowRudd", 0.01d, 0.01d, 0.01d, 0.33d, 641d)]
        [TestCase(OptionStyle.European, "BinomialJoshi", 0.01d, 0.01d, 0.01d, 0.33d, 649d)]
        [TestCase(OptionStyle.European, "BinomialLeisenReimer", 0.01d, 0.01d, 0.01d, 0.33d, 649d)]
        [TestCase(OptionStyle.European, "BinomialTian", 0.01d, 0.01d, 0.01d, 0.33d, 641d)]
        [TestCase(OptionStyle.European, "BinomialTrigeorgis", 0.01d, 0.01d, 0.01d, 0.33d, 641d)]
        [TestCase(OptionStyle.European, "BlackScholes", 0.01d, 0.01d, 0.01d, 0.13d, 642d)]
        [TestCase(OptionStyle.European, "CrankNicolsonFD", 0.01d, 0.01d, 0.01d, 0.33d, 642d)]
        [TestCase(OptionStyle.European, "Integral", 0.01d, 0.12d, 0.01d, 0.33d, 4622d)]
        public void MatchesIBGreeksNearATMPut(OptionStyle style, string qlModelName, double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        { 
            var filename = style == OptionStyle.American ? "SPY230811P00450000" : "SPX230811P04500000";
            var symbol = Symbols.SPY;       // dummy
            var strike = Parse.Decimal(filename[10..]) / 1000m;
            var optionSymbol = GetOptionSymbol(symbol, style, OptionRight.Put, strike, new DateTime(2023, 8, 11));

            MatchesIBGreeksTest(symbol, optionSymbol, filename, qlModelName, errorIV, errorDelta, errorGamma, errorVega, errorTheta);
        }

        /// This test aim to comapre the maximum greek calculation error between models (2dp)
        /// as well as a benchmark of each model to monitor future changes
        [TestCase(OptionStyle.American, "AdditiveEquiprobabilities", 0.02d, 0.03d, 0.01d, 0.02d, 35d)]
        [TestCase(OptionStyle.American, "BaroneAdesiWhaley", 0.02d, 0.05d, 0.02d, 0.02d, 129d)]
        [TestCase(OptionStyle.American, "BinomialCoxRossRubinstein", 0.02d, 0.03d, 0.01d, 0.02d, 35d)]
        [TestCase(OptionStyle.American, "BinomialJarrowRudd", 0.02d, 0.03d, 0.01d, 0.02d, 35d)]
        [TestCase(OptionStyle.American, "BinomialJoshi", 0.02d, 0.03d, 0.01d, 0.02d, 35d)]
        [TestCase(OptionStyle.American, "BinomialLeisenReimer", 0.02d, 0.03d, 0.01d, 0.02d, 35d)]
        [TestCase(OptionStyle.American, "BinomialTian", 0.02d, 0.03d, 0.01d, 0.02d, 35d)]
        [TestCase(OptionStyle.American, "BinomialTrigeorgis", 0.02d, 0.03d, 0.01d, 0.02d, 35d)]
        [TestCase(OptionStyle.American, "BjerksundStensland", 0.02d, 0.05d, 0.02d, 0.02d, 129d)]
        [TestCase(OptionStyle.American, "CrankNicolsonFD", 0.02d, 0.03d, 0.01d, 0.02d, 129d)]
        [TestCase(OptionStyle.European, "AdditiveEquiprobabilities", 0.01d, 0.02d, 0.01d, 0.22d, 356d)]
        [TestCase(OptionStyle.European, "BinomialCoxRossRubinstein", 0.01d, 0.02d, 0.01d, 0.22d, 356d)]
        [TestCase(OptionStyle.European, "BinomialJarrowRudd", 0.01d, 0.02d, 0.01d, 0.22d, 356d)]
        [TestCase(OptionStyle.European, "BinomialJoshi", 0.01d, 0.02d, 0.01d, 0.22d, 360d)]
        [TestCase(OptionStyle.European, "BinomialLeisenReimer", 0.01d, 0.02d, 0.01d, 0.22d, 360d)]
        [TestCase(OptionStyle.European, "BinomialTian", 0.01d, 0.02d, 0.01d, 0.22d, 357d)]
        [TestCase(OptionStyle.European, "BinomialTrigeorgis", 0.01d, 0.02d, 0.01d, 0.22d, 356d)]
        [TestCase(OptionStyle.European, "BlackScholes", 0.01d, 0.02d, 0.01d, 0.17d, 358d)]
        [TestCase(OptionStyle.European, "CrankNicolsonFD", 0.01d, 0.02d, 0.01d, 0.22d, 359d)]
        [TestCase(OptionStyle.European, "Integral", 0.01d, 0.06d, 0.01d, 0.22d, 1335d)]
        public void MatchesIBGreeksFarATMPut(OptionStyle style, string qlModelName, double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        {
            var filename = style == OptionStyle.American ? "SPY230901P00450000" : "SPX230901P04500000";
            var symbol = Symbols.SPY;       // dummy
            var strike = Parse.Decimal(filename[10..]) / 1000m;
            var optionSymbol = GetOptionSymbol(symbol, style, OptionRight.Put, strike, new DateTime(2023, 9, 1));

            MatchesIBGreeksTest(symbol, optionSymbol, filename, qlModelName, errorIV, errorDelta, errorGamma, errorVega, errorTheta);
        }

        /// This test aim to comapre the maximum greek calculation error between models (2dp)
        /// as well as a benchmark of each model to monitor future changes
        [TestCase(OptionStyle.American, "AdditiveEquiprobabilities", 0.05d, 0.05d, 0.01d, 0.22d, 56d)]
        [TestCase(OptionStyle.American, "BaroneAdesiWhaley", 0.05d, 0.05d, 0.01d, 0.06d, 57d)]
        [TestCase(OptionStyle.American, "BinomialCoxRossRubinstein", 0.05d, 0.05d, 0.01d, 0.22d, 56d)]
        [TestCase(OptionStyle.American, "BinomialJarrowRudd", 0.05d, 0.05d, 0.01d, 0.22d, 56d)]
        [TestCase(OptionStyle.American, "BinomialJoshi", 0.05d, 0.05d, 0.01d, 0.22d, 57d)]
        [TestCase(OptionStyle.American, "BinomialLeisenReimer", 0.05d, 0.05d, 0.01d, 0.22d, 57d)]
        [TestCase(OptionStyle.American, "BinomialTian", 0.05d, 0.05d, 0.01d, 0.22d, 56d)]
        [TestCase(OptionStyle.American, "BinomialTrigeorgis", 0.05d, 0.05d, 0.01d, 0.22d, 56d)]
        [TestCase(OptionStyle.American, "BjerksundStensland", 0.05d, 0.05d, 0.01d, 0.06d, 57d)]
        [TestCase(OptionStyle.American, "CrankNicolsonFD", 0.05d, 0.05d, 0.01d, 0.22d, 916d)]
        [TestCase(OptionStyle.European, "AdditiveEquiprobabilities", 0.02d, 0.02d, 0.01d, 2.21d, 331d)]
        [TestCase(OptionStyle.European, "BinomialCoxRossRubinstein", 0.02d, 0.02d, 0.01d, 2.21d, 330d)]
        [TestCase(OptionStyle.European, "BinomialJarrowRudd", 0.02d, 0.02d, 0.01d, 2.21d, 330d)]
        [TestCase(OptionStyle.European, "BinomialJoshi", 0.02d, 0.02d, 0.01d, 2.21d, 337d)]
        [TestCase(OptionStyle.European, "BinomialLeisenReimer", 0.02d, 0.02d, 0.01d, 2.21d, 337d)]
        [TestCase(OptionStyle.European, "BinomialTian", 0.02d, 0.02d, 0.01d, 2.21d, 330d)]
        [TestCase(OptionStyle.European, "BinomialTrigeorgis", 0.02d, 0.02d, 0.01d, 2.21d, 330d)]
        [TestCase(OptionStyle.European, "BlackScholes", 0.02d, 0.02d, 0.01d, 0.19d, 333d)]
        [TestCase(OptionStyle.European, "CrankNicolsonFD", 0.02d, 0.02d, 0.01d, 2.21d, 333d)]
        [TestCase(OptionStyle.European, "Integral", 0.02d, 0.34d, 0.01d, 2.21d, 7981d)]
        public void MatchesIBGreeksNearITMCall(OptionStyle style, string qlModelName, double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        {
             var filename = style == OptionStyle.American ? "SPY230811C00430000" : "SPX230811C04300000";
            var symbol = Symbols.SPY;       // dummy
            var strike = Parse.Decimal(filename[10..]) / 1000m;
            var optionSymbol = GetOptionSymbol(symbol, style, OptionRight.Call, strike, new DateTime(2023, 8, 11));

            MatchesIBGreeksTest(symbol, optionSymbol, filename, qlModelName, errorIV, errorDelta, errorGamma, errorVega, errorTheta);
        }

        /// This test aim to comapre the maximum greek calculation error between models (2dp)
        /// as well as a benchmark of each model to monitor future changes
        [TestCase(OptionStyle.American, "AdditiveEquiprobabilities", 0.04d, 0.07d, 0.01d, 0.23d, 50d)]
        [TestCase(OptionStyle.American, "BaroneAdesiWhaley", 0.04d, 0.07d, 0.01d, 0.09d, 50d)]
        [TestCase(OptionStyle.American, "BinomialCoxRossRubinstein", 0.04d, 0.07d, 0.01d, 0.23d, 50d)]
        [TestCase(OptionStyle.American, "BinomialJarrowRudd", 0.04d, 0.07d, 0.01d, 0.23d, 50d)]
        [TestCase(OptionStyle.American, "BinomialJoshi", 0.04d, 0.07d, 0.01d, 0.23d, 50d)]
        [TestCase(OptionStyle.American, "BinomialLeisenReimer", 0.04d, 0.07d, 0.01d, 0.23d, 50d)]
        [TestCase(OptionStyle.American, "BinomialTian", 0.04d, 0.07d, 0.01d, 0.23d, 49d)]
        [TestCase(OptionStyle.American, "BinomialTrigeorgis", 0.04d, 0.07d, 0.01d, 0.23d, 50d)]
        [TestCase(OptionStyle.American, "BjerksundStensland", 0.04d, 0.07d, 0.01d, 0.09d, 50d)]
        [TestCase(OptionStyle.American, "CrankNicolsonFD", 0.04d, 0.07d, 0.01d, 0.23d, 226d)]
        [TestCase(OptionStyle.European, "AdditiveEquiprobabilities", 0.02d, 0.04d, 0.01d, 2.25d, 406d)]
        [TestCase(OptionStyle.European, "BinomialCoxRossRubinstein", 0.02d, 0.04d, 0.01d, 2.25d, 406d)]
        [TestCase(OptionStyle.European, "BinomialJarrowRudd", 0.02d, 0.04d, 0.01d, 2.25d, 406d)]
        [TestCase(OptionStyle.European, "BinomialJoshi", 0.02d, 0.04d, 0.01d, 2.25d, 411d)]
        [TestCase(OptionStyle.European, "BinomialLeisenReimer", 0.02d, 0.04d, 0.01d, 2.25d, 411d)]
        [TestCase(OptionStyle.European, "BinomialTian", 0.02d, 0.04d, 0.01d, 2.25d, 406d)]
        [TestCase(OptionStyle.European, "BinomialTrigeorgis", 0.02d, 0.04d, 0.01d, 2.25d, 406d)]
        [TestCase(OptionStyle.European, "BlackScholes", 0.02d, 0.04d, 0.01d, 0.51d, 409d)]
        [TestCase(OptionStyle.European, "CrankNicolsonFD", 0.02d, 0.04d, 0.01d, 2.25d, 409d)]
        [TestCase(OptionStyle.European, "Integral", 0.02d, 0.24d, 0.01d, 2.25d, 2029d)]
        public void MatchesIBGreeksFarITMCall(OptionStyle style, string qlModelName, double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        {
            var filename = style == OptionStyle.American ? "SPY230901C00430000" : "SPX230901C04300000";
            var symbol = Symbols.SPY;       // dummy
            var strike = Parse.Decimal(filename[10..]) / 1000m;
            var optionSymbol = GetOptionSymbol(symbol, style, OptionRight.Call, strike, new DateTime(2023, 9, 1));

            MatchesIBGreeksTest(symbol, optionSymbol, filename, qlModelName, errorIV, errorDelta, errorGamma, errorVega, errorTheta);
        }

        /// This test aim to comapre the maximum greek calculation error between models (2dp)
        /// as well as a benchmark of each model to monitor future changes
        [TestCase(OptionStyle.American, "AdditiveEquiprobabilities", 0.13d, 1.00d, 0.01d, 0.24d, 5.92d)]
        [TestCase(OptionStyle.American, "BaroneAdesiWhaley", 0.13d, 0.39d, 0.01d, 0.24d, 526d)]
        [TestCase(OptionStyle.American, "BinomialCoxRossRubinstein", 0.13d, 0.01d, 0.01d, 0.24d, 5.93d)]
        [TestCase(OptionStyle.American, "BinomialJarrowRudd", 0.13d, 1.05d, 0.01d, 0.24d, 5.93d)]
        [TestCase(OptionStyle.American, "BinomialJoshi", 0.13d, 1.00d, 0.01d, 0.24d, 6.49d)]
        [TestCase(OptionStyle.American, "BinomialLeisenReimer", 0.13d, 1.00d, 0.01d, 0.24d, 6.49d)]
        [TestCase(OptionStyle.American, "BinomialTian", 0.13d, 0.01d, 0.01d, 0.24d, 5.93d)]
        [TestCase(OptionStyle.American, "BinomialTrigeorgis", 0.13d, 1.05d, 0.01d, 0.24d, 5.93d)]
        [TestCase(OptionStyle.American, "BjerksundStensland", 0.13d, 0.39d, 0.01d, 0.24d, 526d)]
        [TestCase(OptionStyle.American, "CrankNicolsonFD", 0.13d, 1.00d, 0.01d, 0.24d, 526d)]
        [TestCase(OptionStyle.European, "AdditiveEquiprobabilities", 0.14d, 1.00d, 0.01d, 0.37d, 0.71d)]
        [TestCase(OptionStyle.European, "BinomialCoxRossRubinstein", 0.14d, 0.03d, 0.01d, 0.37d, 47d)]
        [TestCase(OptionStyle.European, "BinomialJarrowRudd", 0.14d, 1.04d, 0.01d, 0.37d, 0.71d)]
        [TestCase(OptionStyle.European, "BinomialJoshi", 0.14d, 1.00d, 0.01d, 0.37d, 0.71d)]
        [TestCase(OptionStyle.European, "BinomialLeisenReimer", 0.14d, 1.00d, 0.01d, 0.37d, 0.71d)]
        [TestCase(OptionStyle.European, "BinomialTian", 0.14d, 0.03d, 0.01d, 0.37d, 47d)]
        [TestCase(OptionStyle.European, "BinomialTrigeorgis", 0.14d, 1.04d, 0.01d, 0.37d, 0.71d)]
        [TestCase(OptionStyle.European, "BlackScholes", 0.14d, 0.03d, 0.01d, 0.37d, 47d)]
        [TestCase(OptionStyle.European, "CrankNicolsonFD", 0.14d, 0.03d, 0.01d, 0.37d, 47d)]
        [TestCase(OptionStyle.European, "Integral", 0.14d, 0.03d, 0.01d, 0.37d, 47d)]
        public void MatchesIBGreeksNearITMPut(OptionStyle style, string qlModelName, double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        {
            var filename = style == OptionStyle.American ? "SPY230811P00470000" : "SPX230811P04700000";
            var symbol = Symbols.SPY;       // dummy
            var strike = Parse.Decimal(filename[10..]) / 1000m;
            var optionSymbol = GetOptionSymbol(symbol, style, OptionRight.Put, strike, new DateTime(2023, 8, 11));

            MatchesIBGreeksTest(symbol, optionSymbol, filename, qlModelName, errorIV, errorDelta, errorGamma, errorVega, errorTheta);
        }

        /// This test aim to comapre the maximum greek calculation error between models (2dp)
        /// as well as a benchmark of each model to monitor future changes
        [TestCase(OptionStyle.American, "AdditiveEquiprobabilities", 0.11d, 1.00d, 0.02d, 0.37d, 4.66d)]
        [TestCase(OptionStyle.American, "BaroneAdesiWhaley", 0.11d, 0.31d, 0.02d, 0.37d, 90d)]
        [TestCase(OptionStyle.American, "BinomialCoxRossRubinstein", 0.11d, 0.02d, 0.02d, 0.37d, 4.70d)]
        [TestCase(OptionStyle.American, "BinomialJarrowRudd", 0.11d, 1.05d, 0.02d, 0.37d, 4.68d)]
        [TestCase(OptionStyle.American, "BinomialJoshi", 0.11d, 1.00d, 0.01d, 0.37d, 4.89d)]
        [TestCase(OptionStyle.American, "BinomialLeisenReimer", 0.11d, 1.00d, 0.01d, 0.37d, 4.89d)]
        [TestCase(OptionStyle.American, "BinomialTian", 0.11d, 0.02d, 0.05d, 0.37d, 4.71d)]
        [TestCase(OptionStyle.American, "BinomialTrigeorgis", 0.11d, 1.05d, 0.02d, 0.37d, 4.65d)]
        [TestCase(OptionStyle.American, "BjerksundStensland", 0.11d, 0.31d, 0.02d, 0.37d, 90d)]
        [TestCase(OptionStyle.American, "CrankNicolsonFD", 0.11d, 1.00d, 0.01d, 0.37d, 90d)]
        [TestCase(OptionStyle.European, "AdditiveEquiprobabilities", 0.11d, 0.90d, 0.01d, 2.47d, 0.21d)]
        [TestCase(OptionStyle.European, "BinomialCoxRossRubinstein", 0.11d, 0.12d, 0.01d, 2.47d, 47d)]
        [TestCase(OptionStyle.European, "BinomialJarrowRudd", 0.11d, 0.94d, 0.01d, 2.47d, 0.21d)]
        [TestCase(OptionStyle.European, "BinomialJoshi", 0.11d, 0.90d, 0.01d, 2.47d, 0.21d)]
        [TestCase(OptionStyle.European, "BinomialLeisenReimer", 0.11d, 0.90d, 0.01d, 2.47d, 0.21d)]
        [TestCase(OptionStyle.European, "BinomialTian", 0.11d, 0.12d, 0.01d, 2.47d, 47d)]
        [TestCase(OptionStyle.European, "BinomialTrigeorgis", 0.11d, 0.94d, 0.01d, 2.47d, 0.21d)]
        [TestCase(OptionStyle.European, "BlackScholes", 0.11d, 0.12d, 0.01d, 2.47d, 47d)]
        [TestCase(OptionStyle.European, "CrankNicolsonFD", 0.11d, 0.12d, 0.01d, 2.47d, 47d)]
        [TestCase(OptionStyle.European, "Integral", 0.11d, 0.12d, 0.01d, 2.47d, 47d)]
        public void MatchesIBGreeksFarITMPut(OptionStyle style, string qlModelName, double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        {
            var filename = style == OptionStyle.American ? "SPY230901P00470000" : "SPX230901P04700000";
            var symbol = Symbols.SPY;       // dummy
            var strike = Parse.Decimal(filename[10..]) / 1000m;
            var optionSymbol = GetOptionSymbol(symbol, style, OptionRight.Put, strike, new DateTime(2023, 9, 1));

            MatchesIBGreeksTest(symbol, optionSymbol, filename, qlModelName, errorIV, errorDelta, errorGamma, errorVega, errorTheta);
        }

        /// This test aim to comapre the maximum greek calculation error between models (2dp)
        /// as well as a benchmark of each model to monitor future changes
        [TestCase(OptionStyle.American, "AdditiveEquiprobabilities", 0.01d, 0.01d, 0.01d, 0.24d, 5.84d)]
        [TestCase(OptionStyle.American, "BaroneAdesiWhaley", 0.01d, 0.01d, 0.01d, 0.01d, 6.02d)]
        [TestCase(OptionStyle.American, "BinomialCoxRossRubinstein", 0.01d, 0.01d, 0.01d, 0.24d, 5.85d)]
        [TestCase(OptionStyle.American, "BinomialJarrowRudd", 0.01d, 0.01d, 0.01d, 0.24d, 5.85d)]
        [TestCase(OptionStyle.American, "BinomialJoshi", 0.01d, 0.01d, 0.01d, 0.24d, 6.14d)]
        [TestCase(OptionStyle.American, "BinomialLeisenReimer", 0.01d, 0.01d, 0.01d, 0.24d, 6.14d)]
        [TestCase(OptionStyle.American, "BinomialTian", 0.01d, 0.01d, 0.01d, 0.24d, 5.82d)]
        [TestCase(OptionStyle.American, "BinomialTrigeorgis", 0.01d, 0.01d, 0.01d, 0.24d, 5.85d)]
        [TestCase(OptionStyle.American, "BjerksundStensland", 0.01d, 0.01d, 0.01d, 0.01d, 6.02d)]
        [TestCase(OptionStyle.American, "CrankNicolsonFD", 0.01d, 0.01d, 0.01d, 0.24d, 488d)]
        [TestCase(OptionStyle.European, "AdditiveEquiprobabilities", 0.01d, 0.01d, 0.01d, 2.42d, 53d)]
        [TestCase(OptionStyle.European, "BinomialCoxRossRubinstein", 0.01d, 0.01d, 0.01d, 2.42d, 53d)]
        [TestCase(OptionStyle.European, "BinomialJarrowRudd", 0.01d, 0.01d, 0.01d, 2.42d, 53d)]
        [TestCase(OptionStyle.European, "BinomialJoshi", 0.01d, 0.01d, 0.01d, 2.42d, 55d)]
        [TestCase(OptionStyle.European, "BinomialLeisenReimer", 0.01d, 0.01d, 0.01d, 2.42d, 55d)]
        [TestCase(OptionStyle.European, "BinomialTian", 0.01d, 0.01d, 0.01d, 2.42d, 53d)]
        [TestCase(OptionStyle.European, "BinomialTrigeorgis", 0.01d, 0.01d, 0.01d, 2.42d, 53d)]
        [TestCase(OptionStyle.European, "BlackScholes", 0.01d, 0.01d, 0.01d, 0.01d, 54d)]
        [TestCase(OptionStyle.European, "CrankNicolsonFD", 0.01d, 0.01d, 0.01d, 2.42d, 55d)]
        [TestCase(OptionStyle.European, "Integral", 0.01d, 0.38d, 0.01d, 2.42d, 4459d)]
        public void MatchesIBGreeksNearOTMCall(OptionStyle style, string qlModelName, double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        {
            var filename = style == OptionStyle.American ? "SPY230811C00470000" : "SPX230811C04700000";
            var symbol = Symbols.SPY;       // dummy
            var strike = Parse.Decimal(filename[10..]) / 1000m;
            var optionSymbol = GetOptionSymbol(symbol, style, OptionRight.Call, strike, new DateTime(2023, 8, 11));

            MatchesIBGreeksTest(symbol, optionSymbol, filename, qlModelName, errorIV, errorDelta, errorGamma, errorVega, errorTheta);
        }

        /// This test aim to comapre the maximum greek calculation error between models (2dp)
        /// as well as a benchmark of each model to monitor future changes
        [TestCase(OptionStyle.American, "AdditiveEquiprobabilities", 0.01d, 0.01d, 0.01d, 0.24d, 17d)]
        [TestCase(OptionStyle.American, "BaroneAdesiWhaley", 0.01d, 0.01d, 0.01d, 0.24d, 17d)]
        [TestCase(OptionStyle.American, "BinomialCoxRossRubinstein", 0.01d, 0.01d, 0.01d, 0.24d, 17d)]
        [TestCase(OptionStyle.American, "BinomialJarrowRudd", 0.01d, 0.01d, 0.01d, 0.24d, 17d)]
        [TestCase(OptionStyle.American, "BinomialJoshi", 0.01d, 0.01d, 0.01d, 0.24d, 17d)]
        [TestCase(OptionStyle.American, "BinomialLeisenReimer", 0.01d, 0.01d, 0.01d, 0.24d, 17d)]
        [TestCase(OptionStyle.American, "BinomialTian", 0.01d, 0.01d, 0.01d, 0.24d, 17d)]
        [TestCase(OptionStyle.American, "BinomialTrigeorgis", 0.01d, 0.01d, 0.01d, 0.24d, 17d)]
        [TestCase(OptionStyle.American, "BjerksundStensland", 0.01d, 0.01d, 0.01d, 0.24d, 17d)]
        [TestCase(OptionStyle.American, "CrankNicolsonFD", 0.01d, 0.01d, 0.01d, 0.24d, 123d)]
        [TestCase(OptionStyle.European, "AdditiveEquiprobabilities", 0.01d, 0.01d, 0.01d, 2.54d, 164d)]
        [TestCase(OptionStyle.European, "BinomialCoxRossRubinstein", 0.01d, 0.01d, 0.01d, 2.54d, 164d)]
        [TestCase(OptionStyle.European, "BinomialJarrowRudd", 0.01d, 0.01d, 0.01d, 2.54d, 164d)]
        [TestCase(OptionStyle.European, "BinomialJoshi", 0.01d, 0.01d, 0.01d, 2.54d, 167d)]
        [TestCase(OptionStyle.European, "BinomialLeisenReimer", 0.01d, 0.01d, 0.01d, 2.54d, 167d)]
        [TestCase(OptionStyle.European, "BinomialTian", 0.01d, 0.01d, 0.01d, 2.54d, 164d)]
        [TestCase(OptionStyle.European, "BinomialTrigeorgis", 0.01d, 0.01d, 0.01d, 2.54d, 166d)]
        [TestCase(OptionStyle.European, "BlackScholes", 0.01d, 0.01d, 0.01d, 0.02d, 166d)]
        [TestCase(OptionStyle.European, "CrankNicolsonFD", 0.01d, 0.01d, 0.01d, 2.54d, 166d)]
        [TestCase(OptionStyle.European, "Integral", 0.01d, 0.28d, 0.01d, 2.54d, 1173d)]
        public void MatchesIBGreeksFarOTMCall(OptionStyle style, string qlModelName, double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        {
            var filename = style == OptionStyle.American ? "SPY230901C00470000" : "SPX230901C04700000";
            var symbol = Symbols.SPY;       // dummy
            var strike = Parse.Decimal(filename[10..]) / 1000m;
            var optionSymbol = GetOptionSymbol(symbol, style, OptionRight.Call, strike, new DateTime(2023, 9, 1));

            MatchesIBGreeksTest(symbol, optionSymbol, filename, qlModelName, errorIV, errorDelta, errorGamma, errorVega, errorTheta);
        }

        /// This test aim to comapre the maximum greek calculation error between models (2dp)
        /// as well as a benchmark of each model to monitor future changes
        [TestCase(OptionStyle.American, "AdditiveEquiprobabilities", 0.02d, 0.01d, 0.01d, 0.21d, 19d)]
        [TestCase(OptionStyle.American, "BaroneAdesiWhaley", 0.02d, 0.33d, 0.01d, 0.21d, 678d)]
        [TestCase(OptionStyle.American, "BinomialCoxRossRubinstein", 0.02d, 0.01d, 0.01d, 0.21d, 19d)]
        [TestCase(OptionStyle.American, "BinomialJarrowRudd", 0.02d, 0.01d, 0.01d, 0.21d, 19d)]
        [TestCase(OptionStyle.American, "BinomialJoshi", 0.02d, 0.01d, 0.01d, 0.21d, 20d)]
        [TestCase(OptionStyle.American, "BinomialLeisenReimer", 0.02d, 0.01d, 0.01d, 0.21d, 20d)]
        [TestCase(OptionStyle.American, "BinomialTian", 0.02d, 0.01d, 0.01d, 0.21d, 19d)]
        [TestCase(OptionStyle.American, "BinomialTrigeorgis", 0.02d, 0.01d, 0.01d, 0.21d, 19d)]
        [TestCase(OptionStyle.American, "BjerksundStensland", 0.02d, 0.33d, 0.01d, 0.21d, 678d)]
        [TestCase(OptionStyle.American, "CrankNicolsonFD", 0.02d, 0.01d, 0.01d, 0.21d, 678d)]
        [TestCase(OptionStyle.European, "AdditiveEquiprobabilities", 0.01d, 0.01d, 0.01d, 2.14d, 183d)]
        [TestCase(OptionStyle.European, "BinomialCoxRossRubinstein", 0.01d, 0.01d, 0.01d, 2.14d, 183d)]
        [TestCase(OptionStyle.European, "BinomialJarrowRudd", 0.01d, 0.01d, 0.01d, 2.14d, 183d)]
        [TestCase(OptionStyle.European, "BinomialJoshi", 0.01d, 0.01d, 0.01d, 2.14d, 189d)]
        [TestCase(OptionStyle.European, "BinomialLeisenReimer", 0.01d, 0.01d, 0.01d, 2.14d, 189d)]
        [TestCase(OptionStyle.European, "BinomialTian", 0.01d, 0.01d, 0.01d, 2.14d, 183d)]
        [TestCase(OptionStyle.European, "BinomialTrigeorgis", 0.01d, 0.01d, 0.01d, 2.14d, 183d)]
        [TestCase(OptionStyle.European, "BlackScholes", 0.01d, 0.01d, 0.01d, 0.03d, 186d)]
        [TestCase(OptionStyle.European, "CrankNicolsonFD", 0.01d, 0.01d, 0.01d, 2.14d, 185d)]
        [TestCase(OptionStyle.European, "Integral", 0.01d, 0.33d, 0.01d, 2.14d, 6957d)]
        public void MatchesIBGreeksNearOTMPut(OptionStyle style, string qlModelName, double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        {
            var filename = style == OptionStyle.American ? "SPY230811P00430000" : "SPX230811P04300000";
            var symbol = Symbols.SPY;       // dummy
            var strike = Parse.Decimal(filename[10..]) / 1000m;
            var optionSymbol = GetOptionSymbol(symbol, style, OptionRight.Put, strike, new DateTime(2023, 8, 11));

            MatchesIBGreeksTest(symbol, optionSymbol, filename, qlModelName, errorIV, errorDelta, errorGamma, errorVega, errorTheta);
        }

        /// This test aim to comapre the maximum greek calculation error between models (2dp)
        /// as well as a benchmark of each model to monitor future changes
        [TestCase(OptionStyle.American, "AdditiveEquiprobabilities", 0.01d, 0.01d, 0.01d, 0.21d, 29d)]
        [TestCase(OptionStyle.American, "BaroneAdesiWhaley", 0.01d, 0.23d, 0.01d, 0.21d, 169d)]
        [TestCase(OptionStyle.American, "BinomialCoxRossRubinstein", 0.01d, 0.01d, 0.01d, 0.21d, 29d)]
        [TestCase(OptionStyle.American, "BinomialJarrowRudd", 0.01d, 0.01d, 0.01d, 0.21d, 29d)]
        [TestCase(OptionStyle.American, "BinomialJoshi", 0.01d, 0.01d, 0.01d, 0.21d, 29d)]
        [TestCase(OptionStyle.American, "BinomialLeisenReimer", 0.01d, 0.01d, 0.01d, 0.21d, 29d)]
        [TestCase(OptionStyle.American, "BinomialTian", 0.01d, 0.01d, 0.01d, 0.21d, 29d)]
        [TestCase(OptionStyle.American, "BinomialTrigeorgis", 0.01d, 0.01d, 0.01d, 0.21d, 29d)]
        [TestCase(OptionStyle.American, "BjerksundStensland", 0.01d, 0.23d, 0.01d, 0.21d, 169d)]
        [TestCase(OptionStyle.American, "CrankNicolsonFD", 0.01d, 0.01d, 0.01d, 0.21d, 169d)]
        [TestCase(OptionStyle.European, "AdditiveEquiprobabilities", 0.01d, 0.01d, 0.01d, 2.18d, 276d)]
        [TestCase(OptionStyle.European, "BinomialCoxRossRubinstein", 0.01d, 0.01d, 0.01d, 2.18d, 276d)]
        [TestCase(OptionStyle.European, "BinomialJarrowRudd", 0.01d, 0.01d, 0.01d, 2.18d, 276d)]
        [TestCase(OptionStyle.European, "BinomialJoshi", 0.01d, 0.01d, 0.01d, 2.18d, 280d)]
        [TestCase(OptionStyle.European, "BinomialLeisenReimer", 0.01d, 0.01d, 0.01d, 2.18d, 280d)]
        [TestCase(OptionStyle.European, "BinomialTian", 0.01d, 0.01d, 0.01d, 2.18d, 275d)]
        [TestCase(OptionStyle.European, "BinomialTrigeorgis", 0.01d, 0.01d, 0.01d, 2.18d, 276d)]
        [TestCase(OptionStyle.European, "BlackScholes", 0.01d, 0.01d, 0.01d, 0.08d, 278d)]
        [TestCase(OptionStyle.European, "CrankNicolsonFD", 0.01d, 0.01d, 0.01d, 2.18d, 278d)]
        [TestCase(OptionStyle.European, "Integral", 0.01d, 0.23d, 0.01d, 2.18d, 1727d)]
        public void MatchesIBGreeksFarOTMPut(OptionStyle style, string qlModelName, double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        {
            var filename = style == OptionStyle.American ? "SPY230901P00430000" : "SPX230901P04300000";
            var symbol = Symbols.SPY;       // dummy
            var strike = Parse.Decimal(filename[10..]) / 1000m;
            var optionSymbol = GetOptionSymbol(symbol, style, OptionRight.Put, strike, new DateTime(2023, 9, 1));

            MatchesIBGreeksTest(symbol, optionSymbol, filename, qlModelName, errorIV, errorDelta, errorGamma, errorVega, errorTheta);
        }

        private void MatchesIBGreeksTest(Symbol symbol, Symbol optionSymbol, string filename, string qlModelName, 
                                         double errorIV, double errorDelta, double errorGamma, double errorVega, double errorTheta)
        {
            var tz = TimeZones.NewYork;
            var evaluationDate = new DateTime(2023, 8, 4);

            // setting up underlying
            var equity = GetEquity(symbol, 450m, 0.15m, tz);       // dummy non-zero values

            // setting up option
            var contract = GetOptionContract(optionSymbol, symbol, evaluationDate);
            var option = GetOption(optionSymbol, equity, tz);
            var priceModel = (IOptionPriceModel)typeof(OptionPriceModels).GetMethod(qlModelName).Invoke(null, new object[] { });

            // Get test data
            var data = File.ReadAllLines($"TestData/greeks/{filename}.csv")
                .Skip(1)                                            // skip header row
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Split(','));

            // running evaluation: iterate per slice
            foreach (var datum in data)
            {
                equity.SetMarketPrice(new Tick { Value = Parse.Decimal(datum[7]) });
                option.SetMarketPrice(new Tick { Value = Parse.Decimal(datum[1]) });
                var results = priceModel.Evaluate(option, null, contract);

                // Check the option Greeks are valid
                var greeks = results.Greeks;

                // Expect minor error due to interest rate, bid/ask price and dividend yield used in IB
                // And approximation error using Black Calculator if the original pricing model fails
                Assert.AreEqual((double)results.ImpliedVolatility, Parse.Double(datum[2]), errorIV);
                Assert.AreEqual((double)greeks.Delta, Parse.Double(datum[3]), errorDelta);
                Assert.AreEqual((double)greeks.Gamma, Parse.Double(datum[4]), errorGamma);
                Assert.AreEqual((double)greeks.Vega, Parse.Double(datum[5]), errorVega);
                Assert.AreEqual((double)greeks.Theta, Parse.Double(datum[6]), errorTheta);
            }
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

        private static Symbol GetOptionSymbol(Symbol underlying, OptionStyle optionStyle, OptionRight optionRight, decimal strike = 192m, DateTime? expiry = null)
        {
            if (expiry == null)
            {
                expiry = new DateTime(2016, 02, 19);
            }
            return Symbol.CreateOption(underlying.Value, Market.USA, optionStyle, optionRight, strike, (DateTime)expiry);
        }

        private static Option CreateOption(Symbol symbol)
        {
            return new Option(
                        SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                        new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, true),
                        new Cash(Currencies.USD, 0, 1m),
                        new OptionSymbolProperties(SymbolProperties.GetDefault(Currencies.USD)),
                        ErrorCurrencyConverter.Instance,
                        RegisteredSecurityDataTypesProvider.Null
                    )
            { ExerciseSettlement = SettlementType.Cash };
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
            var option = CreateOption(symbol);
            return new OptionContract(option, underlying) { Time = evaluationDate };
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
        }

        class TestOptionPriceModel : QLOptionPriceModel
        {
            public TestOptionPriceModel()
                : base(process => new BinomialVanillaEngine<CoxRossRubinstein>(process, 100), null, null, null)
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
