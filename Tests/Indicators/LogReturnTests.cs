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
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class LogReturnTests : CommonIndicatorTests<IndicatorDataPoint>
    {
        protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
        {
            return new LogReturn(14);
        }

        protected override string TestFileName => "spy_logr14.txt";

        protected override string TestColumnName => "LOGR14";

        [Test]
        public void LOGRComputesCorrectly()
        {
            var period = 4;
            var logr = new LogReturn(period);
            var data = new[] { 1, 10, 100, 1000, 10000, 1234, 56789 };
            var seen = new List<int>();
            var time = DateTime.Now;

            for (var i = 0; i < data.Length; i++)
            {
                var datum = data[i];
                var value0 = 0.0;

                if (seen.Count >= 0 && seen.Count < period)
                {
                    value0 = data[0];
                }
                else if (seen.Count >= period)
                {
                    value0 = data[i - period];
                }

                var expected = (decimal)Math.Log(datum / value0);

                seen.Add(datum);
                logr.Update(time.AddSeconds(i), datum);
                Assert.AreEqual(expected, logr.Current.Value);
            }
        }
    }
}