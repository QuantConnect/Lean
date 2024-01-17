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
using System.Globalization;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class ImpliedVolatilityTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override string TestColumnName => "ImpliedVolatility";

        private DateTime _reference = new DateTime(2022, 9, 1, 10, 0, 0);
        private Symbol _symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 450m, new DateTime(2023, 9, 1));
        private Symbol _underlying;

        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            var indicator = new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, 0.04m);
            return indicator;
        }

        [SetUp]
        public void SetUp()
        {
            _underlying = _symbol.Underlying;
        }

        // For comparing IB's value
        [TestCase("SPX230811C04300000", 0.2)]
        [TestCase("SPX230811C04500000", 0.005)]
        [TestCase("SPX230811C04700000", 0.01)]
        [TestCase("SPX230811P04300000", 0.02)]
        [TestCase("SPX230811P04500000", 0.01)]
        [TestCase("SPX230811P04700000", 0.08)]
        [TestCase("SPX230901C04300000", 0.01)]
        [TestCase("SPX230901C04500000", 0.005)]
        [TestCase("SPX230901C04700000", 0.001)]
        [TestCase("SPX230901P04300000", 0.005)]
        [TestCase("SPX230901P04500000", 0.005)]
        [TestCase("SPX230901P04700000", 0.01)]
        [TestCase("SPY230811C00430000", 0.05)]
        [TestCase("SPY230811C00450000", 0.02)]
        [TestCase("SPY230811C00470000", 0.01)]
        [TestCase("SPY230811P00430000", 0.02)]
        [TestCase("SPY230811P00450000", 0.01)]
        [TestCase("SPY230811P00470000", 0.08)]
        [TestCase("SPY230901C00430000", 0.02)]
        [TestCase("SPY230901C00450000", 0.01)]
        [TestCase("SPY230901C00470000", 0.005)]
        [TestCase("SPY230901P00430000", 0.001)]
        [TestCase("SPY230901P00450000", 0.001)]
        [TestCase("SPY230901P00470000", 0.04)]
        public void ComparesAgainstExternalData(string fileName, double errorMargin)
        {
            var path = Path.Combine("TestData", "greeks", $"{fileName}.csv");
            var symbol = ParseOptionSymbol(fileName);
            var underlying = symbol.Underlying;

            var indicator = new ImpliedVolatility(symbol, 0.04m);
            RunTestIndicator(path, indicator, symbol, underlying, errorMargin);
        }

        [Test]
        public override void ComparesAgainstExternalData()
        {
            // Not used
        }

        // For comparing IB's value
        [TestCase("SPX230811C04300000", 0.2)]
        [TestCase("SPX230811C04500000", 0.005)]
        [TestCase("SPX230811C04700000", 0.01)]
        [TestCase("SPX230811P04300000", 0.02)]
        [TestCase("SPX230811P04500000", 0.01)]
        [TestCase("SPX230811P04700000", 0.08)]
        [TestCase("SPX230901C04300000", 0.01)]
        [TestCase("SPX230901C04500000", 0.005)]
        [TestCase("SPX230901C04700000", 0.001)]
        [TestCase("SPX230901P04300000", 0.005)]
        [TestCase("SPX230901P04500000", 0.005)]
        [TestCase("SPX230901P04700000", 0.01)]
        [TestCase("SPY230811C00430000", 0.05)]
        [TestCase("SPY230811C00450000", 0.02)]
        [TestCase("SPY230811C00470000", 0.01)]
        [TestCase("SPY230811P00430000", 0.02)]
        [TestCase("SPY230811P00450000", 0.01)]
        [TestCase("SPY230811P00470000", 0.08)]
        [TestCase("SPY230901C00430000", 0.02)]
        [TestCase("SPY230901C00450000", 0.01)]
        [TestCase("SPY230901C00470000", 0.005)]
        [TestCase("SPY230901P00430000", 0.001)]
        [TestCase("SPY230901P00450000", 0.001)]
        [TestCase("SPY230901P00470000", 0.04)]
        public void ComparesAgainstExternalDataAfterReset(string fileName, double errorMargin)
        {
            var path = Path.Combine("TestData", "greeks", $"{fileName}.csv");
            var symbol = ParseOptionSymbol(fileName);
            var underlying = symbol.Underlying;

            var indicator = new ImpliedVolatility(symbol, 0.04m);
            RunTestIndicator(path, indicator, symbol, underlying, errorMargin);

            indicator.Reset();
            RunTestIndicator(path, indicator, symbol, underlying, errorMargin);
        }

        [Test]
        public override void ComparesAgainstExternalDataAfterReset()
        {
            // Not used
        }

        // Reference values from QuantLib
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.307)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, 0.515)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.276)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, 0.205)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.132)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, 0.545)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.093)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, 0.208)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.134)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, 0.056)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.056)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, 0.178)]
        public void ComparesIVOnBSMModel(decimal price, decimal spotPrice, OptionRight right, int expiry, double refIV)
        {
            // Under CRR framework
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, _reference.AddDays(expiry));
            var indicator = new ImpliedVolatility(symbol, 0.04m, optionModel: OptionPricingModelType.BinomialCoxRossRubinstein);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refIV, (double)indicator.Current.Value, 0.005d);
        }

        private Symbol ParseOptionSymbol(string fileName)
        {
            var ticker = fileName.Substring(0, 3);
            var expiry = DateTime.ParseExact(fileName.Substring(3, 6), "yyMMdd", CultureInfo.InvariantCulture);
            var right = fileName[9] == 'C' ? OptionRight.Call : OptionRight.Put;
            var strike = Parse.Decimal(fileName.Substring(10, 8)) / 1000m;
            var style = ticker == "SPY" ? OptionStyle.American : OptionStyle.European;

            return Symbol.CreateOption(ticker, Market.USA, style, right, strike, expiry);
        }

        private void RunTestIndicator(string path, ImpliedVolatility indicator, Symbol symbol, Symbol underlying, double errorMargin)
        {
            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                var items = line.Split(',');

                var time = DateTime.ParseExact(items[0], "yyyyMMdd HH:mm:ss.ffffff", CultureInfo.InvariantCulture);
                var price = Parse.Decimal(items[1]);
                var spotPrice = Parse.Decimal(items[^1]);
                var refIV = Parse.Double(items[2]);

                indicator.Update(new IndicatorDataPoint(symbol, time, price));
                indicator.Update(new IndicatorDataPoint(underlying, time, spotPrice));

                Assert.AreEqual(refIV, (double)indicator.Current.Value, errorMargin);
            }
        }

        [Test]
        public override void ResetsProperly()
        {
            var indicator = new ImpliedVolatility(_symbol, 0.04m);

            for (var i = 0; i < 5; i++)
            {
                var price = 500m;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;
                var time = _reference.AddDays(1 + i);

                indicator.Update(new IndicatorDataPoint(_symbol, time, optionPrice));
                indicator.Update(new IndicatorDataPoint(_underlying, time, price));
            }

            Assert.IsTrue(indicator.IsReady);

            indicator.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(indicator);
        }

        [Test]
        public override void TimeMovesForward()
        {
            var indicator = CreateIndicator();

            for (var i = 10; i > 0; i--)
            {
                var price = 500m;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;
                var time = _reference.AddDays(1 + i);

                indicator.Update(new IndicatorDataPoint(_symbol, time, optionPrice));
                indicator.Update(new IndicatorDataPoint(_underlying, time, price));
            }

            Assert.AreEqual(2, indicator.Samples);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var period = 5;
            var indicator = new ImpliedVolatility("testImpliedVolatilityIndicator", _symbol, period: period);
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

                // At least 2 days data for historical daily volatility
                if (time <= _reference.AddDays(3))
                {
                    Assert.IsFalse(indicator.IsReady);
                }
                else
                {
                    Assert.IsTrue(indicator.IsReady);
                }

            }

            Assert.AreEqual(2 * warmUpPeriod.Value, indicator.Samples);
        }

        [Test]
        public void UsesRiskFreeInterestRateModel()
        {
            const int count = 20;
            var dates = Enumerable.Range(0, count).Select(i => new DateTime(2022, 11, 21, 10, 0, 0) + TimeSpan.FromDays(i)).ToList();
            var interestRateValues = Enumerable.Range(0, count).Select(i => 0m + (10 - 0m) * (i / (count - 1m))).ToList();

            var interestRateProviderMock = new Mock<IRiskFreeInterestRateModel>();

            // Set up
            for (int i = 0; i < count; i++)
            {
                interestRateProviderMock.Setup(x => x.GetInterestRate(dates[i])).Returns(interestRateValues[i]).Verifiable();
            }

            var iv = new ImpliedVolatility(_symbol, interestRateProviderMock.Object);

            for (int i = 0; i < count; i++)
            {
                iv.Update(new IndicatorDataPoint(_symbol, dates[i], 80m + i));
                iv.Update(new IndicatorDataPoint(_underlying, dates[i], 500m + i));
                Assert.AreEqual(interestRateValues[i], iv.RiskFreeRate.Current.Value);
            }

            // Assert
            Assert.IsTrue(iv.IsReady);
            interestRateProviderMock.Verify(x => x.GetInterestRate(It.IsAny<DateTime>()), Times.Exactly(dates.Count * 2));
            for (int i = 0; i < count; i++)
            {
                interestRateProviderMock.Verify(x => x.GetInterestRate(dates[i]), Times.Exactly(2));
            }
        }

        [Test]
        public void UsesPythonDefinedRiskFreeInterestRateModel()
        {
            using var _ = Py.GIL();

            var module = PyModule.FromString(Guid.NewGuid().ToString(), @"
from AlgorithmImports import *

class TestRiskFreeInterestRateModel:
    CallCount = 0

    def GetInterestRate(self, date: datetime) -> float:
        TestRiskFreeInterestRateModel.CallCount += 1
        return 0.5

def getImpliedVolatilityIndicator(symbol: Symbol) -> ImpliedVolatility:
    return ImpliedVolatility(symbol, TestRiskFreeInterestRateModel())
            ");

            var iv = module.GetAttr("getImpliedVolatilityIndicator").Invoke(_symbol.ToPython()).GetAndDispose<ImpliedVolatility>();
            var modelClass = module.GetAttr("TestRiskFreeInterestRateModel");

            var reference = new DateTime(2022, 11, 21, 10, 0, 0);
            for (int i = 0; i < 20; i++)
            {
                iv.Update(new IndicatorDataPoint(_symbol, reference + TimeSpan.FromMinutes(i), 10m + i));
                iv.Update(new IndicatorDataPoint(_underlying, reference + TimeSpan.FromMinutes(i), 1000m + i));
                Assert.AreEqual((i + 1) * 2, modelClass.GetAttr("CallCount").GetAndDispose<int>());
            }
        }

        // Not used
        protected override string TestFileName => string.Empty;
    }
}
