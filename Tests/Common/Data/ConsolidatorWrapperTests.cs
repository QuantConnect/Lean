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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class ConsolidatorWrapperTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(100)]
        public void InitialScanTime(int seconds)
        {
            var time = new DateTime(2024, 2, 16);
            var timeKeeper = new TimeKeeper(time, TimeZones.NewYork);
            var localtime = timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork);
            var increment = TimeSpan.FromSeconds(seconds);
            using var consolidator = new TestConsolidator();
            using var wrapper = new ConsolidatorWrapper(consolidator, increment, timeKeeper, localtime);

            Assert.AreEqual(time.Add(increment < Time.OneSecond ? Time.OneSecond : increment), wrapper.UtcScanTime);
        }

        [TestCase(2)]
        [TestCase(100)]
        public void ScanTimeAfterScanUtcTimeInPast(int seconds)
        {
            var time = new DateTime(2024, 2, 16);
            var timeKeeper = new TimeKeeper(time, TimeZones.NewYork);
            var localtime = timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork);
            var increment = TimeSpan.FromSeconds(seconds);
            using var consolidator = new TestConsolidator();
            using var wrapper = new ConsolidatorWrapper(consolidator, increment, timeKeeper, localtime);

            var expected = time.Add(increment < Time.OneSecond ? Time.OneSecond : increment);
            Assert.AreEqual(expected, wrapper.UtcScanTime);

            timeKeeper.SetUtcDateTime(time.Add(Time.OneSecond));
            wrapper.Scan();
            Assert.AreEqual(expected, wrapper.UtcScanTime);
        }

        [TestCase(2)]
        [TestCase(100)]
        public void ScanTimeAfterScanUtcTimeInFuture(int seconds)
        {
            var time = new DateTime(2024, 2, 16);
            var timeKeeper = new TimeKeeper(time, TimeZones.NewYork);
            var localtime = timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork);
            var increment = TimeSpan.FromSeconds(seconds);
            using var consolidator = new TestConsolidator();
            using var wrapper = new ConsolidatorWrapper(consolidator, increment, timeKeeper, localtime);

            var expected = time.Add(increment < Time.OneSecond ? Time.OneSecond : increment);
            Assert.AreEqual(expected, wrapper.UtcScanTime);

            timeKeeper.SetUtcDateTime(time.Add(Time.OneDay));

            wrapper.Scan();
            Assert.AreEqual(expected.Add(Time.OneDay), wrapper.UtcScanTime);
        }

        [TestCase(-1, true)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(24, true)]
        [TestCase(-24, true)]
        [TestCase(-1, false)]
        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(24, false)]
        [TestCase(-24, false)]
        public void ScanTimeAfterConsolidationDayLightSavings(int hoursShift, bool savingsStart)
        {
            var tz = TimeZones.NewYork;
            DateTime time;
            if (savingsStart)
            {
                time = new DateTime(2024, 3, 10).AddHours(hoursShift).ConvertToUtc(tz);
            }
            else
            {
                time = new DateTime(2024, 11, 3).AddHours(hoursShift).ConvertToUtc(tz);
            }
            var timeKeeper = new TimeKeeper(time, tz);
            var localtime = timeKeeper.GetLocalTimeKeeper(tz);
            var increment = Time.OneHour;
            using var consolidator = new TestConsolidator();
            using var wrapper = new ConsolidatorWrapper(consolidator, increment, timeKeeper, localtime);

            var expected = time.Add(Time.OneHour);
            Assert.AreEqual(expected, wrapper.UtcScanTime);

            consolidator.Consolidate(new TradeBar { Time = time.AddMinutes(100), Period = Time.OneDay });

            Assert.AreEqual(consolidator.Consolidated.EndTime.ConvertToUtc(tz) + Time.OneDay, wrapper.UtcScanTime);
        }

        [TestCase(-1, true)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(24, true)]
        [TestCase(-24, true)]
        [TestCase(-1, false)]
        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(24, false)]
        [TestCase(-24, false)]
        public void ScanTimeOnWorkingBarDayLightSavings(int hoursShift, bool savingsStart)
        {
            var tz = TimeZones.NewYork;
            DateTime time;
            if (savingsStart)
            {
                time = new DateTime(2024, 3, 10).AddHours(hoursShift).ConvertToUtc(tz);
            }
            else
            {
                time = new DateTime(2024, 11, 3).AddHours(hoursShift).ConvertToUtc(tz);
            }
            var timeKeeper = new TimeKeeper(time, tz);
            var localtime = timeKeeper.GetLocalTimeKeeper(tz);
            var increment = Time.OneHour;
            using var consolidator = new TestConsolidator();
            using var wrapper = new ConsolidatorWrapper(consolidator, increment, timeKeeper, localtime);

            var expected = time.Add(Time.OneHour);
            Assert.AreEqual(expected, wrapper.UtcScanTime);

            timeKeeper.SetUtcDateTime(wrapper.UtcScanTime);

            // set a working bars
            consolidator.WorkingData = new TradeBar { Time = time.AddMinutes(100), Period = Time.OneDay };
            wrapper.Scan();

            // after the scan we adjust the expected end time to the working bar
            Assert.AreEqual(consolidator.WorkingData.EndTime.ConvertToUtc(tz), wrapper.UtcScanTime);
        }

        private class TestConsolidator : IDataConsolidator
        {
            public IBaseData Consolidated { get; set; }

            public IBaseData WorkingData { get; set; }

            public Type InputType => typeof(BaseData);

            public Type OutputType => typeof(BaseData);

            public event DataConsolidatedHandler DataConsolidated;

            public void Dispose()
            {
            }

            public void Scan(DateTime currentLocalTime)
            {
            }

            public void Update(IBaseData data)
            {
            }

            public void Consolidate(BaseData dataPoint)
            {
                Consolidated = dataPoint;
                DataConsolidated?.Invoke(this, dataPoint);
            }
        }
    }
}
