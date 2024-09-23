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
using System.Linq;
using Moq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Tests.Research;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmIndicatorsTests
    {
        private QCAlgorithm _algorithm;
        private Symbol _equity;
        private Symbol _option;

        [SetUp]
        public void Setup()
        {
            _algorithm = new AlgorithmStub();
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null,
                TestGlobals.DataProvider, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider,
                null, true, new DataPermissionManager(), _algorithm.ObjectStore, _algorithm.Settings));
            _algorithm.SetHistoryProvider(historyProvider);

            _algorithm.SetDateTime(new DateTime(2013, 10, 11, 15, 0, 0));
            _equity = _algorithm.AddEquity("SPY").Symbol;
            _option = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 450m, new DateTime(2023, 9, 1));
            _algorithm.AddOptionContract(_option);
            _algorithm.Settings.AutomaticIndicatorWarmUp = true;
        }

        [Test]
        public void IndicatorsPassSelectorToWarmUp()
        {
            var mockSelector = new Mock<Func<IBaseData, TradeBar>>();
            mockSelector.Setup(_ => _(It.IsAny<IBaseData>())).Returns<TradeBar>(_ => (TradeBar)_);

            var indicator = _algorithm.ABANDS(Symbols.SPY, 20, selector: mockSelector.Object);

            Assert.IsTrue(indicator.IsReady);
            mockSelector.Verify(_ => _(It.IsAny<IBaseData>()), Times.Exactly(indicator.WarmUpPeriod));
        }

        [Test]
        public void SharpeRatioIndicatorUsesAlgorithmsRiskFreeRateModelSetAfterIndicatorRegistration()
        {
            // Register indicator
            var sharpeRatio = _algorithm.SR(Symbols.SPY, 10);

            // Setup risk free rate model
            var interestRateProviderMock = new Mock<IRiskFreeInterestRateModel>();
            var reference = new DateTime(2023, 11, 21, 10, 0, 0);
            interestRateProviderMock.Setup(x => x.GetInterestRate(reference)).Verifiable();

            // Update indicator
            sharpeRatio.Update(new IndicatorDataPoint(Symbols.SPY, reference, 300m));

            // Our interest rate provider shouldn't have been called yet since it's hasn't been set to the algorithm
            interestRateProviderMock.Verify(x => x.GetInterestRate(reference), Times.Never);

            // Set the interest rate provider to the algorithm
            _algorithm.SetRiskFreeInterestRateModel(interestRateProviderMock.Object);

            // Update indicator
            sharpeRatio.Update(new IndicatorDataPoint(Symbols.SPY, reference, 300m));

            // Our interest rate provider should have been called once
            interestRateProviderMock.Verify(x => x.GetInterestRate(reference), Times.Once);
        }

        [TestCase("Span", Language.CSharp)]
        [TestCase("Count", Language.CSharp)]
        [TestCase("StartAndEndDate", Language.CSharp)]
        [TestCase("Span", Language.Python)]
        [TestCase("Count", Language.Python)]
        [TestCase("StartAndEndDate", Language.Python)]
        public void IndicatorsDataPoint(string testCase, Language language)
        {
            var period = 10;
            var indicator = new BollingerBands(period, 2);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));
            int dataCount;

            IndicatorHistory indicatorValues;
            if (language == Language.CSharp)
            {
                if (testCase == "StartAndEndDate")
                {
                    indicatorValues = _algorithm.IndicatorHistory(indicator, _equity, new DateTime(2013, 10, 07), new DateTime(2013, 10, 11), Resolution.Minute);
                }
                else if (testCase == "Span")
                {
                    indicatorValues = _algorithm.IndicatorHistory(indicator, _equity, TimeSpan.FromDays(5), Resolution.Minute);
                }
                else
                {
                    indicatorValues = _algorithm.IndicatorHistory(indicator, _equity, (int)(4 * 60 * 6.5), Resolution.Minute);
                }
                // BollingerBands, upper, lower, mid bands, std, band width, percentB, price
                Assert.AreEqual(8, indicatorValues.First().GetStorageDictionary().Count);
                dataCount = indicatorValues.ToList().Count;
            }
            else
            {
                using (Py.GIL())
                {
                    if (testCase == "StartAndEndDate")
                    {
                        indicatorValues = _algorithm.IndicatorHistory(indicator.ToPython(), _equity.ToPython(), new DateTime(2013, 10, 07), new DateTime(2013, 10, 11), Resolution.Minute);
                    }
                    else if (testCase == "Span")
                    {
                        indicatorValues = _algorithm.IndicatorHistory(indicator.ToPython(), _equity.ToPython(), TimeSpan.FromDays(5), Resolution.Minute);
                    }
                    else
                    {
                        indicatorValues = _algorithm.IndicatorHistory(indicator.ToPython(), _equity.ToPython(), (int)(4 * 60 * 6.5), Resolution.Minute);
                    }
                    dataCount = QuantBookIndicatorsTests.GetDataFrameLength(indicatorValues.DataFrame);
                }
            }

            // the historical indicator current values
            Assert.AreEqual(1550 + period, indicatorValues.Current.Count);
            Assert.AreEqual(1550 + period, indicatorValues["current"].Count);
            Assert.AreEqual(indicatorValues.Current, indicatorValues["current"]);
            Assert.IsNull(indicatorValues["NonExisting"]);

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(1550 + period, dataCount);

            var lastData = indicatorValues.Current.Last();
            Assert.AreEqual(new DateTime(2013, 10, 10, 16, 0, 0), lastData.EndTime);
            Assert.AreEqual(lastData.EndTime, indicatorValues.Last().EndTime);
        }

        [TestCase("Span", Language.CSharp)]
        [TestCase("Count", Language.CSharp)]
        [TestCase("StartAndEndDate", Language.CSharp)]
        [TestCase("Span", Language.Python)]
        [TestCase("Count", Language.Python)]
        [TestCase("StartAndEndDate", Language.Python)]
        public void IndicatorsBar(string testCase, Language language)
        {
            var period = 10;
            var indicator = new AverageTrueRange(period);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));

            IndicatorHistory indicatorValues;
            int dataCount;
            if (language == Language.CSharp)
            {
                if (testCase == "StartAndEndDate")
                {
                    indicatorValues = _algorithm.IndicatorHistory(indicator, _equity, new DateTime(2013, 10, 07), new DateTime(2013, 10, 11), Resolution.Minute);
                }
                else if (testCase == "Span")
                {
                    indicatorValues = _algorithm.IndicatorHistory(indicator, _equity, TimeSpan.FromDays(5), Resolution.Minute);
                }
                else
                {
                    indicatorValues = _algorithm.IndicatorHistory(indicator, _equity, (int)(4 * 60 * 6.5), Resolution.Minute);
                }
                // the TrueRange & the AVGTrueRange
                Assert.AreEqual(2, indicatorValues.First().GetStorageDictionary().Count);
                dataCount = indicatorValues.ToList().Count;
            }
            else
            {
                using (Py.GIL())
                {
                    if (testCase == "StartAndEndDate")
                    {
                        indicatorValues = _algorithm.IndicatorHistory(indicator.ToPython(), _equity.ToPython(), new DateTime(2013, 10, 07), new DateTime(2013, 10, 11), Resolution.Minute);
                    }
                    else if (testCase == "Span")
                    {
                        indicatorValues = _algorithm.IndicatorHistory(indicator.ToPython(), _equity.ToPython(), TimeSpan.FromDays(5), Resolution.Minute);
                    }
                    else
                    {
                        indicatorValues = _algorithm.IndicatorHistory(indicator.ToPython(), _equity.ToPython(), (int)(4 * 60 * 6.5), Resolution.Minute);
                    }
                    dataCount = QuantBookIndicatorsTests.GetDataFrameLength(indicatorValues.DataFrame);
                }
            }

            // the historical indicator current values
            Assert.AreEqual(1550 + period, indicatorValues.Current.Count);
            Assert.AreEqual(1550 + period, indicatorValues["current"].Count);
            Assert.AreEqual(indicatorValues.Current, indicatorValues["current"]);
            Assert.IsNull(indicatorValues["NonExisting"]);

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(1550 + period, dataCount);

            var lastData = indicatorValues.Current.Last();
            Assert.AreEqual(new DateTime(2013, 10, 10, 16, 0, 0), lastData.EndTime);
            Assert.AreEqual(lastData.EndTime, indicatorValues.Last().EndTime);
        }

        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void IndicatorMultiSymbol(Language language)
        {
            var referenceSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            var indicator = new Beta(_equity, referenceSymbol, 10);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));

            int dataCount;
            IndicatorHistory indicatorValues;
            if (language == Language.CSharp)
            {
                indicatorValues = _algorithm.IndicatorHistory(indicator, new[] { _equity, referenceSymbol }, TimeSpan.FromDays(5));
                Assert.AreEqual(1, indicatorValues.First().GetStorageDictionary().Count);
                dataCount = indicatorValues.ToList().Count;
            }
            else
            {
                using (Py.GIL())
                {
                    indicatorValues = _algorithm.IndicatorHistory(indicator.ToPython(), (new[] { _equity, referenceSymbol }).ToPython(), TimeSpan.FromDays(5));
                    dataCount = QuantBookIndicatorsTests.GetDataFrameLength(indicatorValues.DataFrame);
                }
            }

            // the historical indicator current values
            Assert.AreEqual(1560, indicatorValues.Current.Count);
            Assert.AreEqual(1560, indicatorValues["current"].Count);
            Assert.AreEqual(indicatorValues.Current, indicatorValues["current"]);
            Assert.IsNull(indicatorValues["NonExisting"]);

            Assert.AreEqual(1560, dataCount);
            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public void BetaCalculation()
        {
            var referenceSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            var indicator = new Beta(_equity, referenceSymbol, 10);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));

            var indicatorValues = _algorithm.IndicatorHistory(indicator, new[] { _equity, referenceSymbol }, TimeSpan.FromDays(50), Resolution.Daily);
            var lastPoint = indicatorValues.Last();
            Assert.AreEqual(0.477585951081753m, lastPoint.Price);
            Assert.AreEqual(0.477585951081753m, lastPoint.Current.Value);
            Assert.AreEqual(new DateTime(2013, 10, 10, 16, 0, 0), lastPoint.Current.EndTime);
        }

        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void IndicatorsPassingHistory(Language language)
        {
            var period = 10;
            var referenceSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            var indicator = new Beta(_equity, referenceSymbol, period);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));

            var history = _algorithm.History(new[] { _equity, referenceSymbol }, TimeSpan.FromDays(5), Resolution.Minute);
            int dataCount;
            if (language == Language.CSharp)
            {
                var indicatorValues = _algorithm.IndicatorHistory(indicator, history);
                Assert.AreEqual(1, indicatorValues.First().GetStorageDictionary().Count);
                dataCount = indicatorValues.Count;
            }
            else
            {
                using (Py.GIL())
                {
                    var pandasFrame = _algorithm.IndicatorHistory(indicator.ToPython(), history);
                    dataCount = QuantBookIndicatorsTests.GetDataFrameLength(pandasFrame.DataFrame);
                }
            }
            Assert.AreEqual((int)(4 * 60 * 6.5) - period, dataCount);
            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public void PythonIndicatorCanBeWarmedUpWithTimespan()
        {
            var referenceSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            var indicator = new SimpleMovingAverage("SMA", 100);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));
            _algorithm.AddEquity(referenceSymbol);
            using (Py.GIL())
            {
                var pythonIndicator = indicator.ToPython();
                _algorithm.WarmUpIndicator(referenceSymbol, pythonIndicator, TimeSpan.FromMinutes(60));
                Assert.IsTrue(pythonIndicator.GetAttr("is_ready").GetAndDispose<bool>());
                Assert.IsTrue(pythonIndicator.GetAttr("samples").GetAndDispose<int>() >= 100);
            }
        }

        [Test]
        public void IndicatorCanBeWarmedUpWithTimespan()
        {
            var referenceSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            _algorithm.AddEquity(referenceSymbol);
            var indicator = new SimpleMovingAverage("SMA", 100);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));
            _algorithm.WarmUpIndicator(referenceSymbol, indicator, TimeSpan.FromMinutes(60));
            Assert.IsTrue(indicator.IsReady);
            Assert.IsTrue(indicator.Samples >= 100);
        }

        [Test]
        public void PythonCustomIndicatorCanBeWarmedUpWithTimespan()
        {
            var referenceSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            _algorithm.AddEquity(referenceSymbol);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));
            using (Py.GIL())
            {
                var testModule = PyModule.FromString("testModule",
                            @"
from AlgorithmImports import *
from collections import deque

class CustomSimpleMovingAverage(PythonIndicator):
    def __init__(self, name, period):
        super().__init__()
        self.warm_up_period = period
        self.name = name
        self.value = 0
        self.queue = deque(maxlen=period)

    # Update method is mandatory
    def update(self, input):
        self.queue.appendleft(input.value)
        count = len(self.queue)
        self.value = np.sum(self.queue) / count
        return count == self.queue.maxlen");

                var customIndicator = testModule.GetAttr("CustomSimpleMovingAverage").Invoke("custom".ToPython(), 100.ToPython());
                _algorithm.WarmUpIndicator(referenceSymbol, customIndicator, TimeSpan.FromMinutes(60));
                Assert.IsTrue(customIndicator.GetAttr("is_ready").GetAndDispose<bool>());
                Assert.IsTrue(customIndicator.GetAttr("samples").GetAndDispose<int>() >= 100);
            }
        }

        [TestCase("count")]
        [TestCase("StartAndEndDate")]
        public void IndicatorUpdatedWithSymbol(string testCase)
        {
            var time = new DateTime(2014, 06, 07);

            var put = Symbols.CreateOptionSymbol("AAPL", OptionRight.Call, 250m, new DateTime(2016, 01, 15));
            var call = Symbols.CreateOptionSymbol("AAPL", OptionRight.Put, 250m, new DateTime(2016, 01, 15));
            var indicator = new Delta(option: put, mirrorOption: call, optionModel: OptionPricingModelType.BlackScholes, ivModel: OptionPricingModelType.BlackScholes);
            _algorithm.SetDateTime(time);

            IndicatorHistory indicatorValues;
            if (testCase == "count")
            {
                indicatorValues = _algorithm.IndicatorHistory(indicator, new[] { put, call, put.Underlying }, 60 * 10, resolution: Resolution.Minute);
            }
            else
            {
                indicatorValues = _algorithm.IndicatorHistory(indicator, new[] { put, call, put.Underlying }, TimeSpan.FromMinutes(60 * (10 + 2)), resolution: Resolution.Minute);
            }

            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(0.9942984m, indicator.Current.Value);
            Assert.AreEqual(0.3516544m, indicator.ImpliedVolatility.Current.Value);
            Assert.AreEqual(390, indicatorValues.Count);

            var lastData = indicatorValues.Current.Last();
            Assert.AreEqual(new DateTime(2014, 6, 6, 16, 0, 0), lastData.EndTime);
            Assert.AreEqual(lastData.EndTime, indicatorValues.Last().EndTime);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void PythonCustomIndicator(int testCases)
        {
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));
            using (Py.GIL())
            {
                PyModule module;
                if (testCases == 1)
                {
                    module = PyModule.FromString("PythonCustomIndicator",
                        @"
from AlgorithmImports import *
class GoodCustomIndicator(PythonIndicator):
    def __init__(self):
        self.Value = 0
    def Update(self, input):
        self.Value = input.Value
        return True");
                }
                else
                {
                    module = PyModule.FromString("PythonCustomIndicator",
                        @"
from AlgorithmImports import *
class GoodCustomIndicator:
    def __init__(self):
        self.IsReady = True
        self.Value = 0
    def Update(self, input):
        self.Value = input.Value
        return True");
                }

                var goodIndicator = module.GetAttr("GoodCustomIndicator").Invoke();
                var pandasFrame = _algorithm.IndicatorHistory(goodIndicator, _equity.ToPython(), TimeSpan.FromDays(5), Resolution.Minute);
                var dataCount = QuantBookIndicatorsTests.GetDataFrameLength(pandasFrame.DataFrame);

                Assert.IsTrue((bool)((dynamic)goodIndicator).IsReady);
                Assert.AreEqual((int)(4 * 60 * 6.5), dataCount);
            }
        }

        [Test]
        public void SpecificTTypeIndicator()
        {
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));
            var referenceSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            var indicator = new CustomIndicator();
            var result = _algorithm.IndicatorHistory(indicator, referenceSymbol, TimeSpan.FromDays(1), Resolution.Minute).ToList();
            Assert.AreEqual(390, result.Count);
            Assert.IsTrue(indicator.IsReady);
        }

        [TestCase("span", 1)]
        [TestCase("count", 1)]
        [TestCase("span", 2)]
        [TestCase("count", 2)]
        public void SMAAssertDataCount(string testCase, int requestCount)
        {
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));
            var referenceSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            var indicator = new SimpleMovingAverage(10);
            IndicatorHistory result;
            if (testCase == "span")
            {
                result = _algorithm.IndicatorHistory(indicator, referenceSymbol, TimeSpan.FromDays(requestCount), Resolution.Daily);
            }
            else
            {
                result = _algorithm.IndicatorHistory(indicator, referenceSymbol, requestCount, Resolution.Daily);
            }
            Assert.AreEqual(requestCount, result.Count);
            Assert.AreEqual(10 + requestCount - 1, indicator.Samples);
            Assert.IsTrue(indicator.IsReady);
        }


        private class CustomIndicator : IndicatorBase<QuoteBar>, IIndicatorWarmUpPeriodProvider
        {
            private bool _isReady;
            public int WarmUpPeriod => 1;
            public override bool IsReady => _isReady;
            public CustomIndicator() : base("Pepe")
            { }
            protected override decimal ComputeNextValue(QuoteBar input)
            {
                _isReady = true;
                return input.Ask.High;
            }
        }
    }
}
