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
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Indicators;
using System;
using System.Linq;

namespace QuantConnect.Tests.Indicators
{
	[TestFixture]
	public class SharpeRatioTests : CommonIndicatorTests<IndicatorDataPoint>
    {
    	protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
    	{
            return new SharpeRatio("SR", 10);
    	}

        protected override string TestFileName => "spy_sr.txt";

        protected override string TestColumnName => "SR_10";

        [Test]
        public void TestTradeBarsWithSameValue()
        {
    		// With the value not changing, the indicator should return default value 0m.
    		var sr = new SharpeRatio("SR", 10);

    		// push the value 100000 into the indicator 20 times (sharpeRatioPeriod + movingAveragePeriod)
    		for(int i = 0; i < 20; i++) {
    			IndicatorDataPoint point = new IndicatorDataPoint(new DateTime(), 100000m);
    			sr.Update(point);
    		}

    		Assert.AreEqual(sr.Current.Value, 0m);
        }

        [Test]
        public void TestTradeBarsWithDifferingValue()
        {
        	// With the value changing, the indicator should return a value that is not the default 0m.
        	var sr = new SharpeRatio("SR", 10);

    		// push the value 100000 into the indicator 20 times (sharpeRatioPeriod + movingAveragePeriod)
    		for(int i = 0; i < 20; i++) {
    			IndicatorDataPoint point = new IndicatorDataPoint(new DateTime(), 100000m + i);
    			sr.Update(point);
    		}

    		Assert.AreNotEqual(sr.Current.Value, 0m);
        }

        [Test]
        public void TestDivByZero()
        {
        	// With the value changing, the indicator should return a value that is not the default 0m.
        	var sr = new SharpeRatio("SR", 10);

    		// push the value 100000 into the indicator 20 times (sharpeRatioPeriod + movingAveragePeriod)
    		for(int i = 0; i < 20; i++)
            {
    			IndicatorDataPoint point = new IndicatorDataPoint(new DateTime(), 0);
    			sr.Update(point);
    		}

    		Assert.AreEqual(sr.Current.Value, 0m);
        }

        [Test]
        public void UsesRiskFreeInterestRateModel()
        {
            const int count = 20;
            var dates = Enumerable.Range(0, count).Select(i => new DateTime(2023, 11, 21, 10, 0, 0) + TimeSpan.FromMinutes(i)).ToList();
            var interestRateValues = Enumerable.Range(0, count).Select(i => 0m + (10 - 0m) * (i / (count - 1m))).ToList();

            var interestRateProviderMock = new Mock<IRiskFreeInterestRateModel>();

            // Set up
            for (int i = 0; i < count; i++)
            {
                interestRateProviderMock.Setup(x => x.GetInterestRate(dates[i])).Returns(interestRateValues[i]).Verifiable();
            }

            var sr = new TestableSharpeRatio("SR", 10, interestRateProviderMock.Object);

            // Push the value 100000 into the indicator 20 times (sharpeRatioPeriod + movingAveragePeriod)
            for (int i = 0; i < count; i++)
            {
                sr.Update(new IndicatorDataPoint(dates[i], 100000m + i));
                Assert.AreEqual(interestRateValues[i], sr.RiskFreeRatePublic.Current.Value);
            }

            // Assert
            Assert.IsTrue(sr.IsReady);
            interestRateProviderMock.Verify(x => x.GetInterestRate(It.IsAny<DateTime>()), Times.Exactly(dates.Count));
            for (int i = 0; i < count; i++)
            {
                interestRateProviderMock.Verify(x => x.GetInterestRate(dates[i]), Times.Once);
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

def getSharpeRatioIndicator() -> SharpeRatio:
    return SharpeRatio(""SR"", 10, TestRiskFreeInterestRateModel())
            ");

            var sr = module.GetAttr("getSharpeRatioIndicator").Invoke().GetAndDispose<SharpeRatio>();
            var modelClass = module.GetAttr("TestRiskFreeInterestRateModel");

            var reference = new DateTime(2023, 11, 21, 10, 0, 0);
            for (int i = 0; i < 20; i++)
            {
                sr.Update(new IndicatorDataPoint(reference + TimeSpan.FromMinutes(i), 100000m + i));
                Assert.AreEqual(i + 1, modelClass.GetAttr("CallCount").GetAndDispose<int>());
            }
        }

        private class TestableSharpeRatio : SharpeRatio
        {
            public Identity RiskFreeRatePublic => RiskFreeRate;

            public TestableSharpeRatio(string name, int period, IRiskFreeInterestRateModel riskFreeRateModel)
                : base(name, period, riskFreeRateModel)
            {
            }
            public TestableSharpeRatio(int period, decimal riskFreeRate = 0.0m)
                : base(period, riskFreeRate)
            {
            }

            public TestableSharpeRatio(string name, int period, decimal riskFreeRate = 0.0m)
                : base(name, period, riskFreeRate)
            {
            }
        }
    }
}
