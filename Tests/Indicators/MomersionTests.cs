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

using NUnit.Framework;
using QuantConnect.Indicators;
using QuantConnect.Logging;
using System;
using System.Collections;
using System.Linq;

namespace QuantConnect.Tests.Indicators
{
    /// <summary>
    /// Result tested vs. Python and Excel available in http://tinyurl.com/ob5tslj
    /// </summary>
    [TestFixture]
    public class MomersionTests
    {
        #region Array input

        // Real AAPL minute data rounded to 2 decimals.
        private readonly decimal[] _prices = {
            125.99m, 125.91m, 125.75m, 125.62m, 125.54m, 125.45m, 125.47m,
            125.4m , 125.43m, 125.45m, 125.42m, 125.36m, 125.23m, 125.32m,
            125.26m, 125.31m, 125.41m, 125.5m , 125.51m, 125.41m, 125.54m,
            125.51m, 125.61m, 125.43m, 125.42m, 125.42m, 125.46m, 125.43m,
            125.4m , 125.35m
        };

        private readonly decimal[] _expectedMinPeriod = {
            50.00m, 50.00m, 50.00m, 50.00m, 50.00m, 50.00m, 50.00m, 50.00m, 57.14m, 62.50m,
            55.56m, 60.00m, 63.64m, 58.33m, 53.85m, 50.00m, 53.33m, 56.25m, 58.82m, 55.56m,
            52.63m, 50.00m, 45.00m, 40.00m, 40.00m, 36.84m, 38.89m, 38.89m, 44.44m, 44.44m
        };

        private readonly decimal[] _expectedFullPeriod = {
            50m, 50m   , 50m  , 50m, 50m, 50m  , 50m, 50m,
            50m, 50m   , 50m  , 50m, 60m, 50m  , 40m, 30m,
            40m, 50m   , 60m  , 50m, 50m, 40m  , 30m, 30m,
            40m, 44.44m, 37.5m, 25m, 25m, 37.5m,
        };

        #endregion Array input

        [TestCase(7, 20)]
        [TestCase(null, 10)]
        public void ComputesCorrectly(int? minPeriod, int fullPeriod)
        {
            var momersion = new MomersionIndicator(minPeriod, fullPeriod);
            var expected = minPeriod.HasValue ? _expectedMinPeriod : _expectedFullPeriod;

            RunTestIndicator(momersion, expected);
        }

        [TestCase(7, 20)]
        [TestCase(null, 10)]
        public void ResetsProperly(int? minPeriod, int fullPeriod)
        {
            var momersion = new MomersionIndicator(minPeriod, fullPeriod);
            var expected = minPeriod.HasValue ? _expectedMinPeriod : _expectedFullPeriod;

            RunTestIndicator(momersion, expected);

            Assert.IsTrue(momersion.IsReady);

            momersion.Reset();

            TestHelper.AssertIndicatorIsInDefaultState(momersion);
        }

        [TestCase(7, 20)]
        [TestCase(null, 10)]
        public void WarmsUpProperly(int? minPeriod, int fullPeriod)
        {
            var momersion = new MomersionIndicator(minPeriod, fullPeriod);
            var period = ((IIndicatorWarmUpPeriodProvider)momersion).WarmUpPeriod;
            var dataStream = TestHelper.GetDataStream(period).ToArray();

            for (var i = 0; i < period; i++)
            {
                momersion.Update(dataStream[i]);
                Assert.AreEqual(i == period - 1, momersion.IsReady);
            }
        }

        private void RunTestIndicator(MomersionIndicator momersion, IEnumerable expected)
        {
            var time = DateTime.Now;
            var actual = new decimal[_prices.Length];

            for (var i = 0; i < _prices.Length; i++)
            {
                momersion.Update(time.AddMinutes(i), _prices[i]);
                actual[i] = Math.Round(momersion.Current.Value, 2);

                Log.Trace($"Bar : {i} | {momersion}, Is ready? {momersion.IsReady}");
            }
            Assert.AreEqual(expected, actual);
        }
    }
}