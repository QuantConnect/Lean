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
using System.Globalization;
using System.Linq;
using Accord.Statistics;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class AutoregressiveIntegratedMovingAverageTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        private static List<decimal> betweenMethods;
        private double _ssIndicator;
        private double _ssTest;

        protected override string TestFileName => "spy_arima.csv";
        protected override string TestColumnName => "ARIMA";

        [Test]
        public override void ComparesAgainstExternalData()
        {
            var ARIMA = CreateIndicator();
            TestHelper.TestIndicator(ARIMA, TestFileName, TestColumnName,
                (ind, expected) => Assert.AreEqual(expected, (double) ARIMA.Current.Value, 10d));
        }

        [Test]
        public override void ComparesAgainstExternalDataAfterReset()
        {
            var ARIMA = CreateIndicator();
            TestHelper.TestIndicator(ARIMA, TestFileName, TestColumnName,
                (ind, expected) => Assert.AreEqual(expected, (double) ARIMA.Current.Value, 10d));
            ARIMA.Reset();
            TestHelper.TestIndicator(ARIMA, TestFileName, TestColumnName,
                (ind, expected) => Assert.AreEqual(expected, (double) ARIMA.Current.Value, 10d));
        }

        [Test]
        public void PredictionErrorAgainstExternalData()
        {
            if (betweenMethods == null)
            {
                betweenMethods = FillDataPerMethod();
            }
            
            // Testing predictive performance vs. external.
            Assert.LessOrEqual(_ssIndicator, _ssTest);
        }

        [Test]
        public override void WarmsUpProperly() // Overridden in order to ensure matrix inversion during ARIMA fitting.
        {
            var indicator = CreateIndicator();
            var period = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;
            if (!period.HasValue)
            {
                Assert.Ignore($"{indicator.Name} is not IIndicatorWarmUpPeriodProvider");
                return;
            }

            var startDate = new DateTime(2019, 1, 1);
            for (decimal i = 0; i < period.Value; i++)
            {
                indicator.Update(startDate, 100m * (1m + 0.05m * i)); // Values should be sufficiently different, now.
                Assert.AreEqual(i == period.Value - 1, indicator.IsReady);
            }

            Assert.AreEqual(period.Value, indicator.Samples);
        }

        [Test]
        public void ExpectedDifferenceFromExternal()
        {
            if (betweenMethods == null)
            {
                betweenMethods = FillDataPerMethod();
            }
            
            Assert.LessOrEqual(1.39080827453985, betweenMethods.Average()); // Mean difference
            Assert.LessOrEqual(1.19542348709062, betweenMethods.ToDoubleArray().StandardDeviation()); // Std. Dev
        }

        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            var ARIMA = new AutoRegressiveIntegratedMovingAverage("ARIMA", 1, 0, 1, 50);
            return ARIMA;
        }

        private List<decimal> FillDataPerMethod()
        {
            var ARIMA = CreateIndicator();
            var realValues = new List<decimal>();
            var testValues = new List<decimal[]>();
            var betweenMethods = new List<decimal>();
            var data = TestHelper.GetCsvFileStream(TestFileName);
            foreach (var val in data)
            {
                if (val["Close"] != string.Empty)
                {
                    var close = val["Close"];
                    realValues.Add(decimal.Parse(val["Close"], new NumberFormatInfo()));
                    ARIMA.Update(new IndicatorDataPoint(Convert.ToDateTime(val["Date"], new DateTimeFormatInfo()),
                        Convert.ToDecimal(close, new NumberFormatInfo())));
                }

                if (val[TestColumnName] != string.Empty)
                {
                    var fromTest = decimal.Parse(val[TestColumnName], new NumberFormatInfo());
                    testValues.Add(new[] {ARIMA.Current.Value, fromTest});
                }
            }

            _ssIndicator = 0d;
            _ssTest = 0d;
            for (var i = 51; i < realValues.Count; i++)
            {
                var test = realValues[i];
                var arimas = testValues[i - 50];
                _ssIndicator += Math.Pow((double) (arimas[0] - test), 2);
                _ssTest += Math.Pow((double) (arimas[1] - test), 2);
                betweenMethods.Add(Math.Abs(arimas[0] - arimas[1]));
            }

            return betweenMethods;
        }
    }
}
