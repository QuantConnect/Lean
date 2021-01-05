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
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    public abstract class CommonIndicatorTests<T>
        where T : IBaseData
    {
        [Test]
        public virtual void ComparesAgainstExternalData()
        {
            var indicator = CreateIndicator();
            RunTestIndicator(indicator);
        }

        [Test]
        public virtual void ComparesAgainstExternalDataAfterReset()
        {
            var indicator = CreateIndicator();
            RunTestIndicator(indicator);
            indicator.Reset();
            RunTestIndicator(indicator);
        }

        [Test]
        public virtual void ResetsProperly()
        {
            var indicator = CreateIndicator();
            if (indicator is IndicatorBase<IndicatorDataPoint>)
                TestHelper.TestIndicatorReset(indicator as IndicatorBase<IndicatorDataPoint>, TestFileName);
            else if (indicator is IndicatorBase<IBaseDataBar>)
                TestHelper.TestIndicatorReset(indicator as IndicatorBase<IBaseDataBar>, TestFileName);
            else if (indicator is IndicatorBase<TradeBar>)
                TestHelper.TestIndicatorReset(indicator as IndicatorBase<TradeBar>, TestFileName);
            else
                throw new NotSupportedException("ResetsProperly: Unsupported indicator data type: " + typeof(T));
        }

        [Test]
        public virtual void WarmsUpProperly()
        {
            var indicator = CreateIndicator();
            var period = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (!period.HasValue)
            {
                Assert.Ignore($"{indicator.Name} is not IIndicatorWarmUpPeriodProvider");
                return;
            }

            var startDate = new DateTime(2019, 1, 1);

            for (var i = 0; i < period.Value; i++)
            {
                var input = GetInput(startDate, i);
                indicator.Update(input);
                Assert.AreEqual(i == period.Value - 1, indicator.IsReady);
            }

            Assert.AreEqual(period.Value, indicator.Samples);
        }

        [Test]
        public virtual void TimeMovesForward()
        {
            var indicator = CreateIndicator();
            var startDate = new DateTime(2019, 1, 1);

            for (var i = 10; i > 0; i--)
            {
                var input = GetInput(startDate, i);
                indicator.Update(input);
            }
            
            Assert.AreEqual(1, indicator.Samples);
        }

        protected static IBaseData GetInput(DateTime startDate, int value) => GetInput(Symbols.SPY, startDate, value);

        protected static IBaseData GetInput(Symbol symbol, DateTime startDate, int value)
        {
            if (typeof(T) == typeof(IndicatorDataPoint))
            {
                return new IndicatorDataPoint(startDate.AddDays(value), 100m);
            }

            return new TradeBar(
                startDate.AddDays(value),
                symbol,
                100m + value,
                105m + value,
                95m + value,
                100m + value,
                100m,
                Time.OneDay
            );
        }

        public PyObject GetIndicatorAsPyObject()
        {
            using (Py.GIL())
            {
                return Indicator.ToPython();
            }
        }

        public IndicatorBase<T> Indicator => CreateIndicator();

        /// <summary>
        /// Executes a test of the specified indicator
        /// </summary>
        protected virtual void RunTestIndicator(IndicatorBase<T> indicator)
        {
            if (indicator is IndicatorBase<IndicatorDataPoint>)
                TestHelper.TestIndicator(
                    indicator as IndicatorBase<IndicatorDataPoint>,
                    TestFileName,
                    TestColumnName,
                    Assertion as Action<IndicatorBase<IndicatorDataPoint>, double>
                );
            else if (indicator is IndicatorBase<IBaseDataBar>)
                TestHelper.TestIndicator(
                    indicator as IndicatorBase<IBaseDataBar>,
                    TestFileName,
                    TestColumnName,
                    Assertion as Action<IndicatorBase<IBaseDataBar>, double>
                );
            else if (indicator is IndicatorBase<TradeBar>)
                TestHelper.TestIndicator(
                    indicator as IndicatorBase<TradeBar>,
                    TestFileName,
                    TestColumnName,
                    Assertion as Action<IndicatorBase<TradeBar>, double>
                );
            else
                throw new NotSupportedException("RunTestIndicator: Unsupported indicator data type: " + typeof(T));
        }

        /// <summary>
        /// Returns a custom assertion function, parameters are the indicator and the expected value from the file
        /// </summary>
        protected virtual Action<IndicatorBase<T>, double> Assertion
        {
            get { return (indicator, expected) => Assert.AreEqual(expected, (double) indicator.Current.Value, 1e-3); }
        }

        /// <summary>
        /// Returns a new instance of the indicator to test
        /// </summary>
        protected abstract IndicatorBase<T> CreateIndicator();

        /// <summary>
        /// Returns the CSV file name containing test data for the indicator
        /// </summary>
        protected abstract string TestFileName { get; }

        /// <summary>
        /// Returns the name of the column of the CSV file corresponding to the pre-calculated data for the indicator
        /// </summary>
        protected abstract string TestColumnName { get; }
    }
}