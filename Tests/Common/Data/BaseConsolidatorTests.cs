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
using Microsoft.FSharp.Core;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public abstract class BaseConsolidatorTests
    {
        protected abstract IDataConsolidator CreateConsolidator();

        protected virtual void AssertConsolidator(IDataConsolidator consolidator, IDataConsolidator previousConsolidator = null)
        {
            if (previousConsolidator == null)
            {
                Assert.IsNull(consolidator.Consolidated);
            }
            else
            {
                Assert.AreEqual(previousConsolidator.Consolidated?.Value, consolidator.Consolidated?.Value);
            }
        }

        protected virtual dynamic GetTestValues()
        {
            var time = new DateTime(2016, 1, 1);
            return new List<IndicatorDataPoint>()
            {
                new IndicatorDataPoint(time, 1.38687m),
                new IndicatorDataPoint(time.AddSeconds(1), 1.38687m),
                new IndicatorDataPoint(time.AddSeconds(2), 1.38688m),
                new IndicatorDataPoint(time.AddSeconds(3), 1.38687m),
                new IndicatorDataPoint(time.AddSeconds(4), 1.38686m),
                new IndicatorDataPoint(time.AddSeconds(5), 1.38685m),
                new IndicatorDataPoint(time.AddSeconds(6), 1.38683m),
                new IndicatorDataPoint(time.AddSeconds(7), 1.38682m),
                new IndicatorDataPoint(time.AddSeconds(8), 1.38682m),
                new IndicatorDataPoint(time.AddSeconds(9), 1.38684m),
                new IndicatorDataPoint(time.AddSeconds(10), 1.38682m),
                new IndicatorDataPoint(time.AddSeconds(11), 1.38680m),
                new IndicatorDataPoint(time.AddSeconds(12), 1.38681m),
                new IndicatorDataPoint(time.AddSeconds(13), 1.38686m),
                new IndicatorDataPoint(time.AddSeconds(14), 1.38688m),
            };
        }

        [Test]
        public void ResetWorksAsExpected()
        {
            // Test Renko bar consistency amongst three consolidators starting at different times

            var time = new DateTime(2016, 1, 1);
            var testValues = GetTestValues();


            var consolidator = CreateConsolidator();
            foreach (var data in testValues)
            {
                consolidator.Update(data);
            }

            var beforeResetConsolidator = consolidator;

            consolidator.Reset();
            AssertConsolidator(consolidator);

            foreach (var data in testValues)
            {
                consolidator.Update(data);
            }

            AssertConsolidator(consolidator, beforeResetConsolidator);

            consolidator.Dispose();
        }
    }
}
