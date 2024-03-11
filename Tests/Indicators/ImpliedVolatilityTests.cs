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
using System.IO;
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class ImpliedVolatilityTests : OptionBaseIndicatorTests<ImpliedVolatility>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
           => new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, 0.053m, 0.0153m);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel)
            => new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, riskFreeRateModel);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)
            => new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, riskFreeRateModel, dividendYieldModel);

        protected override OptionIndicatorBase CreateIndicator(QCAlgorithm algorithm)
            => algorithm.IV(_symbol);

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
            var period = 5;
            var indicator = new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, 0.053m, 0.0153m, period: period);
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
    }
}
