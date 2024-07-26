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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class ImpliedVolatilityTests : OptionBaseIndicatorTests<ImpliedVolatility>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
           => new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, 0.053m, 0.0153m, optionModel: OptionPricingModelType.BlackScholes);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel)
            => new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, riskFreeRateModel, optionModel: OptionPricingModelType.BlackScholes);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)
            => new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, riskFreeRateModel, dividendYieldModel,
                optionModel: OptionPricingModelType.BlackScholes);

        protected override OptionIndicatorBase CreateIndicator(QCAlgorithm algorithm)
            => algorithm.IV(_symbol, optionModel: OptionPricingModelType.BlackScholes);

        [SetUp]
        public void SetUp()
        {
            RiskFreeRateUpdatesPerIteration = 1;
            DividendYieldUpdatesPerIteration = 1;
        }

        [TestCase("american/third_party_1_greeks.csv", true, false, 0.08)]
        [TestCase("american/third_party_1_greeks.csv", false, false, 0.08)]
        // Just placing the test and data here, we are unsure about the smoothing function and not going to reverse engineer
        [TestCase("american/third_party_2_greeks.csv", false, true, 10000)]
        public void ComparesAgainstExternalData(string subPath, bool reset, bool singleContract, double errorRate, double errorMargin = 1e-4,
            int callColumn = 7, int putColumn = 6)
        {
            var path = Path.Combine("TestData", "greeksindicator", subPath);
            // skip last entry since for deep ITM, IV will not affect much on price. Thus root finding will not be optimizing a non-convex function.
            foreach (var line in File.ReadAllLines(path).Skip(3).SkipLast(1))
            {
                var items = line.Split(',');

                var interestRate = Parse.Decimal(items[^2]);
                var dividendYield = Parse.Decimal(items[^1]);

                var model = ParseSymbols(items, path.Contains("american"), out var call, out var put);

                ImpliedVolatility callIndicator;
                ImpliedVolatility putIndicator;
                if (singleContract)
                {
                    callIndicator = new ImpliedVolatility(call, interestRate, dividendYield, optionModel: model);
                    putIndicator = new ImpliedVolatility(put, interestRate, dividendYield, optionModel: model);
                }
                else
                {
                    callIndicator = new ImpliedVolatility(call, interestRate, dividendYield, put, model);
                    putIndicator = new ImpliedVolatility(put, interestRate, dividendYield, call, model);
                }

                RunTestIndicator(call, put, callIndicator, putIndicator, items, callColumn, putColumn, errorRate, errorMargin);

                if (reset == true)
                {
                    callIndicator.Reset();
                    putIndicator.Reset();

                    RunTestIndicator(call, put, callIndicator, putIndicator, items, callColumn, putColumn, errorRate, errorMargin);
                }
            }
        }

        [TestCase(23.753, 27.651, 450.0, OptionRight.Call, 60, 0.309, 0.309)]
        [TestCase(33.928, 5.564, 470.0, OptionRight.Call, 60, 0.191, 0.279)]
        [TestCase(47.701, 10.213, 430.0, OptionRight.Put, 60, 0.247, 0.545)]
        public void SetSmoothingFunction(decimal price, decimal mirrorPrice, decimal spotPrice, OptionRight right, int expiry, double refIV1, double refIV2)
        {
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, _reference.AddDays(expiry));
            var mirrorSymbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right == OptionRight.Call ? OptionRight.Put : OptionRight.Call,
                450m, _reference.AddDays(expiry));
            var indicator = new ImpliedVolatility(symbol, 0.0530m, 0.0153m, mirrorSymbol, OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var mirrorOptionDataPoint = new IndicatorDataPoint(mirrorSymbol, _reference, mirrorPrice);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(mirrorOptionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refIV1, (double)indicator.Current.Value, 0.001d);

            indicator.SetSmoothingFunction((iv, mirrorIv) => iv);

            optionDataPoint = new IndicatorDataPoint(symbol, _reference.AddMilliseconds(1), price);
            mirrorOptionDataPoint = new IndicatorDataPoint(mirrorSymbol, _reference.AddMilliseconds(1), mirrorPrice);
            spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference.AddMilliseconds(1), spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(mirrorOptionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refIV2, (double)indicator.Current.Value, 0.001d);
        }

        [TestCase(23.753, 27.651, 450.0, OptionRight.Call, 60, 0.309, 0.309)]
        [TestCase(33.928, 5.564, 470.0, OptionRight.Call, 60, 0.191, 0.279)]
        [TestCase(47.701, 10.213, 430.0, OptionRight.Put, 60, 0.247, 0.545)]
        public void SetPythonSmoothingFunction(decimal price, decimal mirrorPrice, decimal spotPrice, OptionRight right, int expiry, double refIV1, double refIV2)
        {
            using var _ = Py.GIL();
            var module = PyModule.FromString(Guid.NewGuid().ToString(), $@"
def TestSmoothingFunction(iv: float, mirror_iv: float) -> float:
    return iv");
            var pythonSmoothingFunction = module.GetAttr("TestSmoothingFunction");

            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, _reference.AddDays(expiry));
            var mirrorSymbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right == OptionRight.Call ? OptionRight.Put : OptionRight.Call,
                450m, _reference.AddDays(expiry));
            var indicator = new ImpliedVolatility(symbol, 0.0530m, 0.0153m, mirrorSymbol, OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var mirrorOptionDataPoint = new IndicatorDataPoint(mirrorSymbol, _reference, mirrorPrice);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(mirrorOptionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refIV1, (double)indicator.Current.Value, 0.001d);

            indicator.SetSmoothingFunction(pythonSmoothingFunction);

            optionDataPoint = new IndicatorDataPoint(symbol, _reference.AddMilliseconds(1), price);
            mirrorOptionDataPoint = new IndicatorDataPoint(mirrorSymbol, _reference.AddMilliseconds(1), mirrorPrice);
            spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference.AddMilliseconds(1), spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(mirrorOptionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refIV2, (double)indicator.Current.Value, 0.001d);
        }

        // Reference values from QuantLib
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.309)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, 0.515)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.279)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, 0.205)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.133)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, 0.545)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.097)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, 0.207)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.140)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, 0.055)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.057)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, 0.177)]
        public void ComparesAgainstExternalData2(decimal price, decimal spotPrice, OptionRight right, int expiry, double refIV)
        {
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, _reference.AddDays(expiry));
            var indicator = new ImpliedVolatility(symbol, 0.0530m, 0.0153m, optionModel: OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refIV, (double)indicator.Current.Value, 0.001d);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, 0.053m, 0.0153m,
                optionModel: OptionPricingModelType.BlackScholes);
            var warmUpPeriod = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (!warmUpPeriod.HasValue)
            {
                Assert.Ignore($"{indicator.Name} is not IIndicatorWarmUpPeriodProvider");
                return;
            }

            // warmup period is 5 + 1
            for (var i = 1; i <= warmUpPeriod.Value; i++)
            {
                var time = _reference.AddDays(i);
                var price = 500m;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;

                indicator.Update(new IndicatorDataPoint(_symbol, time, optionPrice));

                Assert.IsFalse(indicator.IsReady);

                indicator.Update(new IndicatorDataPoint(_underlying, time, price));

                Assert.IsTrue(indicator.IsReady);
            }

            Assert.AreEqual(2 * warmUpPeriod.Value, indicator.Samples);
        }

        [TestCase(OptionPricingModelType.BinomialCoxRossRubinstein, 3.0, 380.0, OptionRight.Call, 60)]
        [TestCase(OptionPricingModelType.BinomialCoxRossRubinstein, 23.0, 400.0, OptionRight.Call, 60, true)]
        [TestCase(OptionPricingModelType.BinomialCoxRossRubinstein, 33.0, 420.0, OptionRight.Call, 60, true)]
        [TestCase(OptionPricingModelType.BinomialCoxRossRubinstein, 50.0, 380.0, OptionRight.Put, 60, true)]
        [TestCase(OptionPricingModelType.BinomialCoxRossRubinstein, 35.0, 400.0, OptionRight.Put, 60, true)]
        [TestCase(OptionPricingModelType.BinomialCoxRossRubinstein, 6.0, 420.0, OptionRight.Put, 60)]
        [TestCase(OptionPricingModelType.BinomialCoxRossRubinstein, 3.0, 380.0, OptionRight.Call, 180)]
        [TestCase(OptionPricingModelType.BinomialCoxRossRubinstein, 15.0, 400.0, OptionRight.Call, 180, true)]
        [TestCase(OptionPricingModelType.BinomialCoxRossRubinstein, 35.0, 420.0, OptionRight.Call, 180, true)]
        [TestCase(OptionPricingModelType.BinomialCoxRossRubinstein, 30.0, 380.0, OptionRight.Put, 180, true)]
        [TestCase(OptionPricingModelType.BinomialCoxRossRubinstein, 20.0, 400.0, OptionRight.Put, 180, true)]
        [TestCase(OptionPricingModelType.BinomialCoxRossRubinstein, 1.0, 420.0, OptionRight.Put, 180)]
        [TestCase(OptionPricingModelType.ForwardTree, 3.0, 380.0, OptionRight.Call, 60)]
        [TestCase(OptionPricingModelType.ForwardTree, 23.0, 400.0, OptionRight.Call, 60)]
        [TestCase(OptionPricingModelType.ForwardTree, 33.0, 420.0, OptionRight.Call, 60)]
        [TestCase(OptionPricingModelType.ForwardTree, 50.0, 380.0, OptionRight.Put, 60)]
        [TestCase(OptionPricingModelType.ForwardTree, 35.0, 400.0, OptionRight.Put, 60)]
        [TestCase(OptionPricingModelType.ForwardTree, 6.0, 420.0, OptionRight.Put, 60)]
        [TestCase(OptionPricingModelType.ForwardTree, 3.0, 380.0, OptionRight.Call, 180)]
        [TestCase(OptionPricingModelType.ForwardTree, 15.0, 400.0, OptionRight.Call, 180)]
        [TestCase(OptionPricingModelType.ForwardTree, 35.0, 420.0, OptionRight.Call, 180)]
        [TestCase(OptionPricingModelType.ForwardTree, 30.0, 380.0, OptionRight.Put, 180)]
        [TestCase(OptionPricingModelType.ForwardTree, 20.0, 400.0, OptionRight.Put, 180)]
        [TestCase(OptionPricingModelType.ForwardTree, 1.0, 420.0, OptionRight.Put, 180)]
        public void LessStepsGiveSameResultAtBetterTimes(OptionPricingModelType model, double price, double spotPrice, OptionRight right, int expiry, bool fails = false)
        {
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 400m, _reference.AddDays(expiry));
            var stopWatch = new Stopwatch();

            var stepsList = new int[] { 100, 150, 200 };
            var times = new List<long>();
            var results = new List<decimal>();

            var csvLine = $"{model},{right},{symbol.ID.StrikePrice},{spotPrice},{expiry}";

            for (var i = 0; i < stepsList.Length; i++)
        {
                var steps = stepsList[i];

                stopWatch.Restart();

                var result = 0m;
                for (var j = 0; j < 100; j++)
                {
                    var indicator = new TestableImpliedVolatility(symbol, 0.0530m, 0.0153m, optionModel: model);
                    indicator.SetSteps(steps);

                    var optionDataPoint = new IndicatorDataPoint(symbol, _reference.AddDays(1), (decimal)price);
                    var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference.AddDays(1), (decimal)spotPrice);

            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

                    result = indicator.Current.Value;
                }

                var elapsed = stopWatch.ElapsedMilliseconds;
                times.Add(elapsed);
                results.Add(result);

                csvLine += $",{steps},{result},{elapsed}";
            }

            Console.WriteLine(csvLine);

            if (fails)
            {
                // TODO: Especial cases for the BinomialCoxRossRubinstein model, where the model is not converging
                //       or the result is zero. Check the model if necessary to make sure it is working as expected,
                //       since ForwardTree is able to calculate the IV in all cases.
                foreach (var result in results)
                {
                    Assert.AreEqual(result, 0);
                }
            }
            else
            {
                for (var i = 0; i < stepsList.Length; i++)
                {
                    for (var j = i + 1; j < stepsList.Length; j++)
                    {
                        var diff = Math.Abs(results[i] - results[j]);
                        var percentDiff = diff / results[i] * 100;
                        Assert.Less(percentDiff, 3);
                    }
                }
            }

            for (var i = 0; i < times.Count - 1; i++)
            {
                Assert.LessOrEqual(times[i], times[i + 1]);
            }
        }

        private class TestableImpliedVolatility : ImpliedVolatility
        {
            private int _steps;

            protected override int Steps => _steps;

            public TestableImpliedVolatility(Symbol option, decimal riskFreeRate = 0.05m, decimal dividendYield = 0.0m, Symbol mirrorOption = null,
                OptionPricingModelType optionModel = OptionPricingModelType.ForwardTree)
                : base(option, riskFreeRate, dividendYield, mirrorOption, optionModel)
            {

            }

            public void SetSteps(int steps)
            {
                _steps = steps;
            }
        }
    }
}
