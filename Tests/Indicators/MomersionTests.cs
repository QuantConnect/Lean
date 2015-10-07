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
using System;

namespace QuantConnect.Tests.Indicators
{
    /// <summary>
    /// Result tested vs. Python and Excel available in http://tinyurl.com/pnqakbm
    /// </summary>
    [TestFixture]
    public class MomersionTests
    {
        #region Array input

        // Real AAPL minute data rounded to 2 decimals.
        private decimal[] prices = new decimal[30]
        {
            125.99m, 125.91m, 125.75m, 125.62m, 125.54m, 125.45m, 125.47m,
            125.4m , 125.43m, 125.45m, 125.42m, 125.36m, 125.23m, 125.32m,
            125.26m, 125.31m, 125.41m, 125.5m , 125.51m, 125.41m, 125.54m,
            125.51m, 125.61m, 125.43m, 125.42m, 125.42m, 125.46m, 125.43m,
            125.4m , 125.35m
        };

        #endregion Array input

        [Test]
        public void OnlyFullPeriodTest()
        {
            int fullPeriod = 10;
            Momersion Momersion = new Momersion(fullPeriod);
            DateTime time = DateTime.Now;

            #region Array input

            decimal[] expected = new decimal[30]
            {
                50m, 50m   , 50m  , 50m, 50m, 50m  , 50m, 50m,
                50m, 50m   , 50m  , 50m, 60m, 50m  , 40m, 30m,
                40m, 50m   , 60m  , 50m, 50m, 40m  , 30m, 30m,
                40m, 44.44m, 37.5m, 25m, 25m, 37.5m,
            };

            #endregion Array input

            decimal[] actual = new decimal[prices.Length];

            for (int i = 0; i < prices.Length; i++)
            {
                Momersion.Update(new IndicatorDataPoint(time, prices[i]));
                decimal momersionValue = Math.Round(Momersion.Current.Value, 2);
                actual[i] = momersionValue;

                Console.WriteLine(string.Format("Bar : {0} | {1}, Is ready? {2}", i, Momersion.ToString(), Momersion.IsReady));
                time = time.AddMinutes(1);
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void MinPeriodTest()
        {
            int minPeriod = 7;
            int fullPeriod = 20;
            Momersion Momersion = new Momersion(minPeriod, fullPeriod);
            DateTime time = DateTime.Now;

            #region Array input

            decimal[] expected = new decimal[30]
            {
                50.00m, 50.00m, 50.00m, 50.00m, 50.00m, 50.00m, 50.00m, 50.00m, 57.14m, 62.50m,
                55.56m, 60.00m, 63.64m, 58.33m, 53.85m, 50.00m, 53.33m, 56.25m, 58.82m, 55.56m,
                52.63m, 50.00m, 45.00m, 40.00m, 40.00m, 36.84m, 38.89m, 38.89m, 44.44m, 44.44m
            };

            #endregion Array input

            decimal[] actual = new decimal[prices.Length];

            for (int i = 0; i < prices.Length; i++)
            {
                Momersion.Update(new IndicatorDataPoint(time, prices[i]));
                decimal momersionValue = Math.Round(Momersion.Current.Value, 2);
                actual[i] = momersionValue;

                Console.WriteLine(string.Format("Bar : {0} | {1}, Is ready? {2}", i, Momersion.ToString(), Momersion.IsReady));
                time = time.AddMinutes(1);
            }
            Assert.AreEqual(expected, actual);
        }
    }
}