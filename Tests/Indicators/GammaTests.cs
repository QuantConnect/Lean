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
    public class GammaTests : OptionBaseIndicatorTests<Gamma>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
            => new Gamma("testGammaIndicator", _symbol, 0.0403m, 0.0m);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel)
            => new Gamma("testGammaIndicator", _symbol, riskFreeRateModel);

        protected override OptionIndicatorBase CreateIndicator(IRiskFreeInterestRateModel riskFreeRateModel, IDividendYieldModel dividendYieldModel)
            => new Gamma("testGammaIndicator", _symbol, riskFreeRateModel, dividendYieldModel);

        protected override OptionIndicatorBase CreateIndicator(QCAlgorithm algorithm)
            => algorithm.G(_symbol);

        [SetUp]
        public void SetUp()
        {
            // 2 updates per iteration, 1 for greek, 1 for IV
            RiskFreeRateUpdatesPerIteration = 2;
            DividendYieldUpdatesPerIteration = 2;
        }

        [TestCase("american/third_party_1_greeks.csv", true, 0.12, false)]
        [TestCase("american/third_party_1_greeks.csv", false, 0.12, false)]
        public void ComparesAgainstExternalData(string subPath, bool reset, double errorMargin, bool singleContract, int callColumn = 11, int putColumn = 10)
        {
            var path = Path.Combine("TestData", "greeksindicator", subPath);
            foreach (var line in File.ReadAllLines(path).Skip(3))
            {
                var items = line.Split(',');

                var interestRate = Parse.Decimal(items[^2]);
                var dividendYield = Parse.Decimal(items[^1]);

                var model = ParseSymbols(items, path.Contains("american"), out var call, out var put);

                Gamma callIndicator;
                Gamma putIndicator;
                if (singleContract == true)
                {
                    callIndicator = new Gamma(call, interestRate, dividendYield, optionModel: model);
                    putIndicator = new Gamma(put, interestRate, dividendYield, optionModel: model);
                }
                else
                {
                    callIndicator = new Gamma(call, interestRate, dividendYield, put, optionModel: model);
                    putIndicator = new Gamma(put, interestRate, dividendYield, call, optionModel: model);
                }

                RunTestIndicator(call, put, callIndicator, putIndicator, items, callColumn, putColumn, errorMargin);

                if (reset == true)
                {
                    callIndicator.Reset();
                    putIndicator.Reset();

                    RunTestIndicator(call, put, callIndicator, putIndicator, items, callColumn, putColumn, errorMargin);
                }
            }
        }

        // Reference values from QuantLib
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.0071, OptionStyle.European)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, 0.0042, OptionStyle.European)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.0067, OptionStyle.European)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, 0.0083, OptionStyle.European)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.0136, OptionStyle.European)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, 0.0042, OptionStyle.European)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.0128, OptionStyle.European)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, 0.0059, OptionStyle.European)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.0070, OptionStyle.European)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, 0.0057, OptionStyle.European)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.0193, OptionStyle.European)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, 0.0073, OptionStyle.European)]
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.0071, OptionStyle.American)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, 0.0042, OptionStyle.American)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.0067, OptionStyle.American)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, 0.0083, OptionStyle.American)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.0136, OptionStyle.American)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, 0.0042, OptionStyle.American)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.0129, OptionStyle.American)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, 0.0059, OptionStyle.American)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.0070, OptionStyle.American)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, 0.0058, OptionStyle.American)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.0193, OptionStyle.American)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, 0.0073, OptionStyle.American)]
        public void ComparesAgainstExternalData2(decimal price, decimal spotPrice, OptionRight right, int expiry, double refGamma, OptionStyle style)
        {
            var symbol = Symbol.CreateOption("SPY", Market.USA, style, right, 450m, _reference.AddDays(expiry));
            var model = style == OptionStyle.European ? OptionPricingModelType.BlackScholes : OptionPricingModelType.BinomialCoxRossRubinstein;
            var indicator = new Gamma(symbol, 0.0403m, 0.0m, optionModel: model, ivModel: OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refGamma, (double)indicator.Current.Value, 0.0005d);
        }
    }
}
