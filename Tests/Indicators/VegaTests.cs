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
    public class VegaTests : OptionBaseIndicatorTests<Vega>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
            => new Vega("testVegaIndicator", _symbol, 0.0403m, 0.0m);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel)
            => new Vega("testVegaIndicator", _symbol, riskFreeRateModel);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)
            => new Vega("testVegaIndicator", _symbol, riskFreeRateModel, dividendYieldModel);

        protected override OptionIndicatorBase CreateIndicator(QCAlgorithm algorithm)
            => algorithm.V(_symbol);

        [SetUp]
        public void SetUp()
        {
            // 2 updates per iteration, 1 for greek, 1 for IV
            RiskFreeRateUpdatesPerIteration = 2;
            DividendYieldUpdatesPerIteration = 2;
        }

        [TestCase("american/third_party_1_greeks.csv", true, false, 0.2, 2e-4)]
        [TestCase("american/third_party_1_greeks.csv", false, false, 0.2, 2e-4)]
        // Just placing the test and data here, we are unsure about the smoothing function and not going to reverse engineer
        [TestCase("american/third_party_2_greeks.csv", false, true, 10000)]
        public void ComparesAgainstExternalData(string subPath, bool reset, bool singleContract, double errorRate, double errorMargin = 1e-4, 
            int callColumn = 13, int putColumn = 12)
        {
            var path = Path.Combine("TestData", "greeksindicator", subPath);
            // skip last entry since for deep ITM, IV will not affect much on price. Thus root finding will not be optimizing a non-convex function.
            foreach (var line in File.ReadAllLines(path).Skip(3).SkipLast(1))
            {
                var items = line.Split(',');

                var interestRate = Parse.Decimal(items[^2]);
                var dividendYield = Parse.Decimal(items[^1]);

                var model = ParseSymbols(items, path.Contains("american"), out var call, out var put);

                Vega callIndicator;
                Vega putIndicator;
                if (singleContract)
                {
                    callIndicator = new Vega(call, interestRate, dividendYield, optionModel: model);
                    putIndicator = new Vega(put, interestRate, dividendYield, optionModel: model);
                }
                else
                {
                    callIndicator = new Vega(call, interestRate, dividendYield, put, model);
                    putIndicator = new Vega(put, interestRate, dividendYield, call, model);
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
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.7215, OptionStyle.European)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, 0.7195, OptionStyle.European)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.6705, OptionStyle.European)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, 0.6181, OptionStyle.European)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.5429, OptionStyle.European)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, 0.6922, OptionStyle.European)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 1.1932, OptionStyle.European)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, 1.2263, OptionStyle.European)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 1.0370, OptionStyle.European)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, 0.3528, OptionStyle.European)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.9707, OptionStyle.European)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, 1.1816, OptionStyle.European)]
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.7206, OptionStyle.American)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, 0.7189, OptionStyle.American)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.6791, OptionStyle.American)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, 0.6290, OptionStyle.American)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.5690, OptionStyle.American)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, 0.6921, OptionStyle.American)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 1.1958, OptionStyle.American)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, 1.2248, OptionStyle.American)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 1.0459, OptionStyle.American)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, 0.4350, OptionStyle.American)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 1.0122, OptionStyle.American)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, 1.1852, OptionStyle.American)]
        // No American option Vega from QuantLib
        public void ComparesAgainstExternalData2(decimal price, decimal spotPrice, OptionRight right, int expiry, double refVega, OptionStyle style)
        {
            var symbol = Symbol.CreateOption("SPY", Market.USA, style, right, 450m, _reference.AddDays(expiry));
            var model = style == OptionStyle.European ? OptionPricingModelType.BlackScholes : OptionPricingModelType.BinomialCoxRossRubinstein;
            var indicator = new Vega(symbol, 0.053m, 0.0153m, optionModel: model, ivModel: OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refVega, (double)indicator.Current.Value, 0.0005d);
        }
    }
}
