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
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;
using System;
using System.IO;
using System.Linq;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class DeltaTests : OptionBaseIndicatorTests<Delta>
    {
        protected override IndicatorBase<IBaseData> CreateIndicator()
            => new Delta("testDeltaIndicator", _symbol, 0.0403m, 0.0m);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel)
            => new Delta("testDeltaIndicator", _symbol, riskFreeRateModel);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)
        {
            var symbol = (SymbolList.Count > 0) ? SymbolList[0] : _symbol;
            return new Delta("testDeltaIndicator", symbol, riskFreeRateModel, dividendYieldModel);
        }

        protected override OptionIndicatorBase CreateIndicator(QCAlgorithm algorithm)
            => algorithm.D(_symbol);

        [SetUp]
        public void SetUp()
        {
            // 2 updates per iteration, 1 for greek, 1 for IV
            RiskFreeRateUpdatesPerIteration = 2;
            DividendYieldUpdatesPerIteration = 2;
        }

        [TestCase("american/third_party_1_greeks.csv", true, false, 0.03)]
        [TestCase("american/third_party_1_greeks.csv", false, false, 0.03)]
        // Just placing the test and data here, we are unsure about the smoothing function and not going to reverse engineer
        [TestCase("american/third_party_2_greeks.csv", false, true, 10000)]
        public void ComparesAgainstExternalData(string subPath, bool reset, bool singleContract, double errorRate, double errorMargin = 1e-4,
            int callColumn = 9, int putColumn = 8)
        {
            var path = Path.Combine("TestData", "greeksindicator", subPath);
            // skip last entry since for deep ITM, IV will not affect much on price. Thus root finding will not be optimizing a non-convex function.
            foreach (var line in File.ReadAllLines(path).Skip(3).SkipLast(1))
            {
                var items = line.Split(',');

                var interestRate = Parse.Decimal(items[^2]);
                var dividendYield = Parse.Decimal(items[^1]);

                var model = ParseSymbols(items, path.Contains("american"), out var call, out var put);

                Delta callIndicator;
                Delta putIndicator;
                if (singleContract)
                {
                    callIndicator = new Delta(call, interestRate, dividendYield, optionModel: model);
                    putIndicator = new Delta(put, interestRate, dividendYield, optionModel: model);
                }
                else
                {
                    callIndicator = new Delta(call, interestRate, dividendYield, put, model);
                    putIndicator = new Delta(put, interestRate, dividendYield, call, model);
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

        // Reference values from QuantLib
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.546, OptionStyle.European)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, -0.446, OptionStyle.European)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.693, OptionStyle.European)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, -0.260, OptionStyle.European)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.243, OptionStyle.European)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, -0.526, OptionStyle.European)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.632, OptionStyle.European)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, -0.417, OptionStyle.European)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.765, OptionStyle.European)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, -0.052, OptionStyle.European)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.263, OptionStyle.European)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, -0.556, OptionStyle.European)]
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.546, OptionStyle.American)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, -0.446, OptionStyle.American)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.693, OptionStyle.American)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, -0.260, OptionStyle.American)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.243, OptionStyle.American)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, -0.526, OptionStyle.American)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.632, OptionStyle.American)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, -0.417, OptionStyle.American)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.765, OptionStyle.American)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, -0.052, OptionStyle.American)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.264, OptionStyle.American)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, -0.556, OptionStyle.American)]
        public void ComparesAgainstExternalData2(decimal price, decimal spotPrice, OptionRight right, int expiry, double refDelta, OptionStyle style)
        {
            var symbol = Symbol.CreateOption("SPY", Market.USA, style, right, 450m, _reference.AddDays(expiry));
            var model = style == OptionStyle.European ? OptionPricingModelType.BlackScholes : OptionPricingModelType.BinomialCoxRossRubinstein;
            var indicator = new Delta(symbol, 0.0403m, 0.0m, optionModel: model, ivModel: OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refDelta, (double)indicator.Current.Value, 0.0017d);
        }

        [TestCase(0.5, 470.0, OptionRight.Put, 0)]
        [TestCase(0.5, 470.0, OptionRight.Put, 5)]
        [TestCase(0.5, 470.0, OptionRight.Put, 10)]
        [TestCase(0.5, 470.0, OptionRight.Put, 15)]
        [TestCase(15, 450.0, OptionRight.Call, 0)]
        [TestCase(15, 450.0, OptionRight.Call, 5)]
        [TestCase(15, 450.0, OptionRight.Call, 10)]
        [TestCase(0.5, 450.0, OptionRight.Call, 15)]
        public void CanComputeOnExpirationDate(decimal price, decimal spotPrice, OptionRight right, int hoursAfterExpiryDate)
        {
            var expiration = new DateTime(2024, 12, 6);
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, expiration);
            var indicator = new Delta(symbol, 0.0403m, 0.0m,
                optionModel: OptionPricingModelType.BinomialCoxRossRubinstein, ivModel: OptionPricingModelType.BlackScholes);

            var currentTime = expiration.AddHours(hoursAfterExpiryDate);

            var optionDataPoint = new IndicatorDataPoint(symbol, currentTime, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, currentTime, spotPrice);

            Assert.IsFalse(indicator.Update(optionDataPoint));
            Assert.IsTrue(indicator.Update(spotDataPoint));

            Assert.AreNotEqual(0, indicator.Current.Value);
        }
    }
}
