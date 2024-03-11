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
using System.IO;
using System.Linq;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class ThetaTests : OptionBaseIndicatorTests<Theta>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
            => new Theta("testThetaIndicator", _symbol, 0.0403m, 0.0m);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel)
            => new Theta("testThetaIndicator", _symbol, riskFreeRateModel);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)
            => new Theta("testThetaIndicator", _symbol, riskFreeRateModel, dividendYieldModel);

        protected override OptionIndicatorBase CreateIndicator(QCAlgorithm algorithm)
            => algorithm.T(_symbol);

        [SetUp]
        public void SetUp()
        {
            // 2 updates per iteration, 1 for greek, 1 for IV
            RiskFreeRateUpdatesPerIteration = 2;
            DividendYieldUpdatesPerIteration = 2;
        }

        // close to expiry prone to vary
        [TestCase("american/third_party_1_greeks.csv", true, false, 0.6, 1.5e-3)]
        [TestCase("american/third_party_1_greeks.csv", false, false, 0.6, 1.5e-3)]
        // Just placing the test and data here, we are unsure about the smoothing function and not going to reverse engineer
        [TestCase("american/third_party_2_greeks.csv", false, true, 10000, 0.03)]
        public void ComparesAgainstExternalData(string subPath, bool reset, bool singleContract, double errorRate, double errorMargin = 1e-4,
            int callColumn = 15, int putColumn = 14)
        {
            var path = Path.Combine("TestData", "greeksindicator", subPath);
            // skip last entry since for deep ITM, IV will not affect much on price. Thus root finding will not be optimizing a non-convex function.
            foreach (var line in File.ReadAllLines(path).Skip(3).SkipLast(1))
            {
                var items = line.Split(',');

                var interestRate = Parse.Decimal(items[^2]);
                var dividendYield = Parse.Decimal(items[^1]);

                var model = ParseSymbols(items, path.Contains("american"), out var call, out var put);

                Theta callIndicator;
                Theta putIndicator;
                if (singleContract)
                {
                    callIndicator = new Theta(call, interestRate, dividendYield, optionModel: model);
                    putIndicator = new Theta(put, interestRate, dividendYield, optionModel: model);
                }
                else
                {
                    callIndicator = new Theta(call, interestRate, dividendYield, put, model);
                    putIndicator = new Theta(put, interestRate, dividendYield, call, model);
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
        [TestCase(23.753, 450.0, OptionRight.Call, 60, -0.2092, OptionStyle.European)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, -0.2835, OptionStyle.European)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, -0.1858, OptionStyle.European)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, -0.0918, OptionStyle.European)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, -0.0712, OptionStyle.European)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, -0.2852, OptionStyle.European)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, -0.0601, OptionStyle.European)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, -0.0483, OptionStyle.European)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, -0.0733, OptionStyle.European)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, -0.0027, OptionStyle.European)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, -0.0274, OptionStyle.European)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, -0.0297, OptionStyle.European)]
        [TestCase(23.753, 450.0, OptionRight.Call, 60, -0.2095, OptionStyle.American)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, -0.2838, OptionStyle.American)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, -0.1886, OptionStyle.American)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, -0.0936, OptionStyle.American)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, -0.0682, OptionStyle.American)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, -0.2859, OptionStyle.American)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, -0.0603, OptionStyle.American)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, -0.0483, OptionStyle.American)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, -0.0730, OptionStyle.American)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, -0.0035, OptionStyle.American)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, -0.0265, OptionStyle.American)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, -0.0300, OptionStyle.American)]
        public void ComparesAgainstExternalData2(decimal price, decimal spotPrice, OptionRight right, int expiry, double refTheta, OptionStyle style)
        {
            var symbol = Symbol.CreateOption("SPY", Market.USA, style, right, 450m, _reference.AddDays(expiry));
            var model = style == OptionStyle.European ? OptionPricingModelType.BlackScholes : OptionPricingModelType.BinomialCoxRossRubinstein;
            var indicator = new Theta(symbol, 0.0403m, 0.0m, optionModel: model, ivModel: OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refTheta, (double)indicator.Current.Value, 0.0005d);
        }
    }
}
