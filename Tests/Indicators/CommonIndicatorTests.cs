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
using System.Linq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Util;

namespace QuantConnect.Tests.Indicators
{
    public abstract class CommonIndicatorTests<T>
        where T : class, IBaseData
    {
        protected Symbol Symbol { get; set; } = Symbols.SPY;
        protected List<Symbol> SymbolList = new List<Symbol>();
        protected bool ValueCanBeZero { get; set; } = false;

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

        protected QCAlgorithm CreateAlgorithm()
        {
            var algo = new QCAlgorithm();
            algo.SetHistoryProvider(TestGlobals.HistoryProvider);
            algo.SubscriptionManager.SetDataManager(new DataManagerStub(algo));
            return algo;
        }

        [Test]
        public virtual void WarmUpIndicatorProducesConsistentResults()
        {
            var algo = CreateAlgorithm();
            algo.SetStartDate(2020, 1, 1);
            algo.SetEndDate(2021, 2, 1);

            SymbolList = GetSymbols();

            var firstIndicator = CreateIndicator();
            var period = (firstIndicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;
            if (period == null || period == 0)
            {
                Assert.Ignore($"{firstIndicator.Name}, Skipping this test because it's not applicable.");
            }
            // Warm up the first indicator
            algo.WarmUpIndicator(SymbolList, firstIndicator, Resolution.Daily);

            // Warm up the second indicator manually
            var secondIndicator = CreateIndicator();
            var history = algo.History(SymbolList, period.Value, Resolution.Daily).ToList();
            foreach (var slice in history)
            {
                foreach (var symbol in SymbolList)
                {
                    secondIndicator.Update(slice[symbol]);
                }
            }
            SymbolList.Clear();

            // Assert that the indicators are ready
            Assert.IsTrue(firstIndicator.IsReady);
            Assert.IsTrue(secondIndicator.IsReady);
            if (!ValueCanBeZero)
            {
                Assert.AreNotEqual(firstIndicator.Current.Value, 0);
            }

            // Ensure that the first indicator has processed some data
            Assert.AreNotEqual(firstIndicator.Samples, 0);

            // Validate that both indicators have the same number of processed samples
            Assert.AreEqual(firstIndicator.Samples, secondIndicator.Samples);

            // Validate that both indicators produce the same final computed value
            Assert.AreEqual(firstIndicator.Current.Value, secondIndicator.Current.Value);
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

        [Test]
        public virtual void AcceptsRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            if (indicator is IndicatorBase<TradeBar> ||
                indicator is IndicatorBase<IBaseData> ||
                indicator is BarIndicator ||
                indicator is IndicatorBase<IBaseDataBar>)
            {
                var renkoConsolidator = new RenkoConsolidator(RenkoBarSize);
                renkoConsolidator.DataConsolidated += (sender, renkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(renkoBar));
                };

                TestHelper.UpdateRenkoConsolidator(renkoConsolidator, TestFileName);
                Assert.IsTrue(indicator.IsReady);
                Assert.AreNotEqual(0, indicator.Samples);
                IndicatorValueIsNotZeroAfterReceiveRenkoBars(indicator);
                renkoConsolidator.Dispose();
            }
        }

        [Test]
        public virtual void AcceptsVolumeRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            if (indicator is IndicatorBase<TradeBar> ||
                indicator is IndicatorBase<IBaseData> ||
                indicator is BarIndicator ||
                indicator is IndicatorBase<IBaseDataBar>)
            {
                var volumeRenkoConsolidator = new VolumeRenkoConsolidator(VolumeRenkoBarSize);
                volumeRenkoConsolidator.DataConsolidated += (sender, volumeRenkoBar) =>
                {
                    Assert.DoesNotThrow(() => indicator.Update(volumeRenkoBar));
                };

                TestHelper.UpdateRenkoConsolidator(volumeRenkoConsolidator, TestFileName);
                Assert.IsTrue(indicator.IsReady);
                Assert.AreNotEqual(0, indicator.Samples);
                IndicatorValueIsNotZeroAfterReceiveVolumeRenkoBars(indicator);
                volumeRenkoConsolidator.Dispose();
            }
        }

        [Test]
        public virtual void TracksPreviousState()
        {
            var indicator = CreateIndicator();
            var period = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            var startDate = new DateTime(2024, 1, 1);
            var previousValue = indicator.Current.Value;

            // Update the indicator and verify the previous values
            for (var i = 0; i < 2 * period; i++)
            {
                indicator.Update(GetInput(startDate, i));

                // Verify the previous value matches the indicator's previous value
                Assert.AreEqual(previousValue, indicator.Previous.Value);

                // Update previousValue to the current value for the next iteration
                previousValue = indicator.Current.Value;
            }
        }

        [Test]
        public virtual void WorksWithLowValues()
        {
            var indicator = CreateIndicator();
            var period = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            var random = new Random();
            var time = new DateTime(2023, 5, 28);
            for (int i = 0; i < 2 * period; i++)
            {
                var value = (decimal)(random.NextDouble() * 0.000000000000000000000000000001);
                Assert.DoesNotThrow(() => indicator.Update(GetInput(Symbol, time, i, value, value, value, value)));
            }
        }

        [Test]
        public virtual void IndicatorShouldHaveSymbolAfterUpdates()
        {
            var indicator = CreateIndicator();
            var period = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            var startDate = new DateTime(2024, 1, 1);

            for (var i = 0; i < 2 * period; i++)
            {
                // Feed input data to the indicator, each input uses Symbol.SPY
                indicator.Update(GetInput(startDate, i));

                // The indicator should retain the symbol from the input (SPY)
                Assert.AreEqual(Symbols.SPY, indicator.Current.Symbol);
            }
        }

        protected virtual void IndicatorValueIsNotZeroAfterReceiveRenkoBars(IndicatorBase indicator)
        {
            Assert.AreNotEqual(0, indicator.Current.Value);
        }

        protected virtual void IndicatorValueIsNotZeroAfterReceiveVolumeRenkoBars(IndicatorBase indicator)
        {
            Assert.AreNotEqual(0, indicator.Current.Value);
        }

        protected static IBaseData GetInput(DateTime startDate, int days) => GetInput(Symbols.SPY, startDate, days);

        protected static IBaseData GetInput(Symbol symbol, DateTime startDate, int days) => GetInput(symbol, startDate, days, 100m + days, 105m + days, 95m + days, 100 + days);

        protected static IBaseData GetInput(Symbol symbol, DateTime startDate, int days, decimal open, decimal high, decimal low, decimal close)
        {
            if (typeof(T) == typeof(IndicatorDataPoint))
            {
                return new IndicatorDataPoint(symbol, startDate.AddDays(days), close);
            }

            return new TradeBar(
                startDate.AddDays(days),
                symbol,
                open,
                high,
                low,
                close,
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
                    Assertion as Action<IndicatorBase<TradeBar>, double>);
            else
                throw new NotSupportedException("RunTestIndicator: Unsupported indicator data type: " + typeof(T));
        }

        /// <summary>
        /// Returns a custom assertion function, parameters are the indicator and the expected value from the file
        /// </summary>
        protected virtual Action<IndicatorBase<T>, double> Assertion
        {
            get
            {
                return (indicator, expected) =>
                {
                    Assert.AreEqual(expected, (double)indicator.Current.Value, 1e-3);

                    var relativeDifference = Math.Abs(((double)indicator.Current.Value - expected) / expected);
                    Assert.LessOrEqual(relativeDifference, 1); // less than 1% error rate
                };
            }
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

        /// <summary>
        /// Returns the list of symbols used for testing, defaulting to SPY.
        /// </summary>
        protected virtual List<Symbol> GetSymbols() => [Symbols.SPY];

        /// <summary>
        /// Returns the BarSize for the RenkoBar test, namely, AcceptsRenkoBarsAsInput()
        /// </summary>
        protected decimal RenkoBarSize { get; set; } = 10m;

        /// <summary>
        /// Returns the BarSize for the VolumeRenkoBar test, namely, AcceptsVolumeRenkoBarsAsInput()
        /// </summary>
        protected decimal VolumeRenkoBarSize { get; set; } = 500000m;
    }
}
