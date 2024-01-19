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
    public class OptionDeltaTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        private DateTime _reference = new DateTime(2023, 8, 1, 10, 0, 0);
        private Symbol _symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 450m, new DateTime(2023, 9, 1));
        private Symbol _underlying;

        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            var indicator = new OptionDelta("testOptionDeltaIndicator", _symbol, 0.04m);
            return indicator;
        }

        [SetUp]
        public void SetUp()
        {
            _underlying = _symbol.Underlying;
        }

        [TestCase("SPX230811C04300000")]
        [TestCase("SPX230811C04500000")]
        [TestCase("SPX230811C04700000")]
        [TestCase("SPX230811P04300000")]
        [TestCase("SPX230811P04500000")]
        [TestCase("SPX230811P04700000")]
        [TestCase("SPX230901C04300000")]
        [TestCase("SPX230901C04500000")]
        [TestCase("SPX230901C04700000")]
        [TestCase("SPX230901P04300000")]
        [TestCase("SPX230901P04500000")]
        [TestCase("SPX230901P04700000")]
        [TestCase("SPY230811C00430000")]
        [TestCase("SPY230811C00450000")]
        [TestCase("SPY230811C00470000")]
        [TestCase("SPY230811P00430000")]
        [TestCase("SPY230811P00450000")]
        [TestCase("SPY230811P00470000", 0.07)]
        [TestCase("SPY230901C00430000")]
        [TestCase("SPY230901C00450000")]
        [TestCase("SPY230901C00470000")]
        [TestCase("SPY230901P00430000")]
        [TestCase("SPY230901P00450000")]
        [TestCase("SPY230901P00470000", 0.17)]

        public void ComparesAgainstExternalData(string fileName, double errorMargin = 0.03333)
        {
            var path = Path.Combine("TestData", "greeks", $"{fileName}.csv");
            var symbol = ParseOptionSymbol(fileName);
            var underlying = symbol.Underlying;

            var indicator = new OptionDelta(symbol, 0.04m);
            RunTestIndicator(path, indicator, symbol, underlying, errorMargin);
        }

        [Test]
        public override void ComparesAgainstExternalData()
        {
            // Not used
        }

        [TestCase("SPX230811C04300000")]
        [TestCase("SPX230811C04500000")]
        [TestCase("SPX230811C04700000")]
        [TestCase("SPX230811P04300000")]
        [TestCase("SPX230811P04500000")]
        [TestCase("SPX230811P04700000")]
        [TestCase("SPX230901C04300000")]
        [TestCase("SPX230901C04500000")]
        [TestCase("SPX230901C04700000")]
        [TestCase("SPX230901P04300000")]
        [TestCase("SPX230901P04500000")]
        [TestCase("SPX230901P04700000")]
        [TestCase("SPY230811C00430000")]
        [TestCase("SPY230811C00450000")]
        [TestCase("SPY230811C00470000")]
        [TestCase("SPY230811P00430000")]
        [TestCase("SPY230811P00450000")]
        [TestCase("SPY230811P00470000", 0.07)]
        [TestCase("SPY230901C00430000")]
        [TestCase("SPY230901C00450000")]
        [TestCase("SPY230901C00470000")]
        [TestCase("SPY230901P00430000")]
        [TestCase("SPY230901P00450000")]
        [TestCase("SPY230901P00470000", 0.17)]

        public void ComparesAgainstExternalDataAfterReset(string fileName, double errorMargin = 0.03333)
        {
            var path = Path.Combine("TestData", "greeks", $"{fileName}.csv");
            var symbol = ParseOptionSymbol(fileName);
            var underlying = symbol.Underlying;

            var indicator = new OptionDelta(symbol, 0.04m);
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
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.5458)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, -0.4460)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.6928)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, -0.2602)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.2425)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, -0.5261)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.6311)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, -0.4176)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.7639)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, -0.05187)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.2628)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, -0.5570)]
        public void ComparesDeltaOnBSMModel(decimal price, decimal spotPrice, OptionRight right, int expiry, double refDelta)
        {
            // Under CRR framework
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, _reference.AddDays(expiry));
            var indicator = new OptionDelta(symbol, 0.04m, optionModel: OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refDelta, (double)indicator.Current.Value, 0.005d);
        }

        // Reference values from QuantLib
        [TestCase(23.753, 450.0, OptionRight.Call, 60, 0.6041)]
        [TestCase(35.830, 450.0, OptionRight.Put, 60, -0.4167)]
        [TestCase(33.928, 470.0, OptionRight.Call, 60, 0.7400)]
        [TestCase(6.428, 470.0, OptionRight.Put, 60, -0.2458)]
        [TestCase(3.219, 430.0, OptionRight.Call, 60, 0.2612)]
        [TestCase(47.701, 430.0, OptionRight.Put, 60, -0.5117)]
        [TestCase(16.528, 450.0, OptionRight.Call, 180, 0.6587)]
        [TestCase(21.784, 450.0, OptionRight.Put, 180, -0.4184)]
        [TestCase(35.207, 470.0, OptionRight.Call, 180, 0.7991)]
        [TestCase(0.409, 470.0, OptionRight.Put, 180, -0.0571)]
        [TestCase(2.642, 430.0, OptionRight.Call, 180, 0.2773)]
        [TestCase(27.772, 430.0, OptionRight.Put, 180, -0.6066)]
        public void ComparesDeltaOnCRRModel(decimal price, decimal spotPrice, OptionRight right, int expiry, double refDelta)
        {
            // Under CRR framework
            var symbol = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, right, 450m, _reference.AddDays(expiry));
            var indicator = new OptionDelta(symbol, 0.04m, 
                    optionModel: OptionPricingModelType.BinomialCoxRossRubinstein,
                    ivModel: OptionPricingModelType.BlackScholes);

            var optionDataPoint = new IndicatorDataPoint(symbol, _reference, price);
            var spotDataPoint = new IndicatorDataPoint(symbol.Underlying, _reference, spotPrice);
            indicator.Update(optionDataPoint);
            indicator.Update(spotDataPoint);

            Assert.AreEqual(refDelta, (double)indicator.Current.Value, 0.005d);
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

        private void RunTestIndicator(string path, OptionDelta indicator, Symbol symbol, Symbol underlying, double errorMargin)
        {
            foreach (var line in File.ReadAllLines(path).Skip(1))
            {
                var items = line.Split(',');

                var time = DateTime.ParseExact(items[0], "yyyyMMdd HH:mm:ss.ffffff", CultureInfo.InvariantCulture);
                var price = Parse.Decimal(items[1]);
                var spotPrice = Parse.Decimal(items[^1]);
                var refDelta = Parse.Double(items[3]);

                var optionTradeBar = new IndicatorDataPoint(symbol, time, price);
                var spotTradeBar = new IndicatorDataPoint(underlying, time, spotPrice);
                indicator.Update(optionTradeBar);
                indicator.Update(spotTradeBar);

                // We're not sure IB's parameters and models
                Assert.AreEqual(refDelta, (double)indicator.Current.Value, errorMargin);
            }
        }

        [Test]
        public override void ResetsProperly()
        {
            var indicator = new OptionDelta(_symbol, 0.04m);

            for (var i = 0; i < 5; i++)
            {
                var price = 500m;
                var optionPrice = Math.Max(price - 450, 0) * 1.1m;

                indicator.Update(new IndicatorDataPoint(_symbol, _reference.AddDays(1 + i), optionPrice));
                indicator.Update(new IndicatorDataPoint(_underlying, _reference.AddDays(1 + i), price));
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

                indicator.Update(new IndicatorDataPoint(_symbol, _reference.AddDays(1 + i), optionPrice));
                indicator.Update(new IndicatorDataPoint(_underlying, _reference.AddDays(1 + i), price));
            }

            Assert.AreEqual(2, indicator.Samples);
        }

        [Test]
        public override void WarmsUpProperly()
        {
            var indicator = new OptionDelta("testOptionDeltaIndicator", _symbol);
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

            var delta = new OptionDelta(_symbol, interestRateProviderMock.Object);

            for (int i = 0; i < count; i++)
            {
                delta.Update(new IndicatorDataPoint(_symbol, dates[i], 80m + i));
                delta.Update(new IndicatorDataPoint(_underlying, dates[i], 500m + i));
                Assert.AreEqual(interestRateValues[i], delta.RiskFreeRate.Current.Value);
            }

            // Assert
            // every updates will call 3 times (2 for Delta)
            Assert.IsTrue(delta.IsReady);
            interestRateProviderMock.Verify(x => x.GetInterestRate(It.IsAny<DateTime>()), Times.Exactly(dates.Count * 3));
            for (int i = 0; i < count; i++)
            {
                interestRateProviderMock.Verify(x => x.GetInterestRate(dates[i]), Times.Exactly(3));
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

def getOptionDeltaIndicator(symbol: Symbol) -> OptionDelta:
    return OptionDelta(symbol, TestRiskFreeInterestRateModel())
            ");

            var delta = module.GetAttr("getOptionDeltaIndicator").Invoke(_symbol.ToPython()).GetAndDispose<OptionDelta>();
            var modelClass = module.GetAttr("TestRiskFreeInterestRateModel");

            var reference = new DateTime(2022, 11, 21, 10, 0, 0);
            for (int i = 0; i < 20; i++)
            {
                delta.Update(new IndicatorDataPoint(_symbol, reference + TimeSpan.FromMinutes(i), 10m + i));
                delta.Update(new IndicatorDataPoint(_underlying, reference + TimeSpan.FromMinutes(i), 1000m + i));
                // every updates will call 3 times (2 for Delta)
                Assert.AreEqual((i + 1) * 3, modelClass.GetAttr("CallCount").GetAndDispose<int>());
            }
        }

        // Not used
        protected override string TestColumnName => String.Empty;
        protected override string TestFileName => string.Empty;
    }
}
