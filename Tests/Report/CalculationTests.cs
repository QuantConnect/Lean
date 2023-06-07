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

using Deedle;
using NUnit.Framework;
using QuantConnect.Report;
using System;
using System.Linq;
using System.Collections.Generic;

namespace QuantConnect.Tests.Report
{
    [TestFixture]
    public class CalculationTests
    {
        [TestCase(new double[] { 1, 2, 4, 8 }, new double[] { 1, 1, 1 })]
        [TestCase(new double[] { 0, 4, 5, 2.5 }, new double[] { double.PositiveInfinity, 0.25, -0.5 })]
        public void PercentChangeProducesCorrectValues(double[] inputs, double[] expected)
        {
            var series = (new Series<DateTime, double>(CreateFakeSeries(inputs))).PercentChange();

            Assert.AreEqual(expected, series.Values.ToList());
        }

        [TestCase(new double[] { 1, 2, 3, 4 }, new double[] { 1, 3, 6, 10 })]
        [TestCase(new double[] { 0, 0, 0, 0 }, new double[] { 0, 0, 0, 0 })]
        [TestCase(new double[] { 0.25, 0.5, 0.75, 1}, new double[] { 0.25, 0.75, 1.5, 2.5 })]
        public void CumulativeSumProducesCorrectValues(double[] inputs, double[] expected)
        {
            var series = (new Series<DateTime, double>(CreateFakeSeries(inputs))).CumulativeSum().Values.ToList();

            Assert.AreEqual(expected, series);
        }

        [TestCase(new double[] { 97.85916, 94.16154, 94.30944, 94.34978, 97.10619 },
            new double[] {-1.829905, -3.804112, 0.1581819, 0.04307238, 2.942012 }, 2, new double[]{ 1.0071140181639988, 1.0070287327461955 })]
        [TestCase(new double[] { -97.85916, -94.16154, -94.30944, -94.34978, -97.10619 },
            new double[] { 1.829905, 3.804112, -0.1581819, -0.04307238, -2.942012 }, 2, new double[] { -1.0071140181639988, -1.0070287327461955 })]
        [TestCase(new double[] { -32.85916, 54.16154, -10.30944, -20.34978, -97.10619 },
            new double[] { 1.829905, 3.804112, -0.1581819, -0.04307238, -2.942012 }, 2, new double[] { 0.0005318694569107083, -0.010360916195344518})]
        [TestCase(new double[] { }, new double[] { }, 2, new double[] { })]
        public void BetaProducesCorrectValues(double[] benchmarkValues, double[] performanceValues, int period, double[] expected)
        {
            var benchmarkPoints = new SortedList<DateTime, double>(CreateFakeSeries(benchmarkValues));
            var performancePoints = new SortedList<DateTime, double>(CreateFakeSeries(performanceValues));

            var betaValues = (Rolling.Beta(performancePoints, benchmarkPoints, period)).Values.ToArray();
            for (var index = 0; index < betaValues.Length; index++)
            {
                var betaValue = betaValues[index];
                Assert.AreEqual(expected[index], betaValue, 0.001d);
            }
        }

        private Dictionary<DateTime, double> CreateFakeSeries(double[] inputs)
        {
            var i = 0;
            return inputs.ToList().ToDictionary(item =>
            {
                var time = new DateTime(2000, 1, 1).AddDays(i);
                i++;

                return time;
            }, item => item);
        }
    }
}
