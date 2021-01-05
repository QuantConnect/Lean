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

namespace QuantConnect.Tests.Report
{
    [TestFixture]
    public class CalculationTests
    {
        [TestCase(new double[] { 1, 2, 4, 8 }, new double[] { 1, 1, 1 })]
        [TestCase(new double[] { 0, 4, 5, 2.5 }, new double[] { double.PositiveInfinity, 0.25, -0.5 })]
        public void PercentChangeProducesCorrectValues(double[] inputs, double[] expected)
        {
            var series = CreateFakeSeries(inputs).PercentChange();

            Assert.AreEqual(expected, series.Values.ToList());
        }

        [TestCase(new double[] { 1, 2, 3, 4 }, new double[] { 1, 3, 6, 10 })]
        [TestCase(new double[] { 0, 0, 0, 0 }, new double[] { 0, 0, 0, 0 })]
        [TestCase(new double[] { 0.25, 0.5, 0.75, 1}, new double[] { 0.25, 0.75, 1.5, 2.5 })]
        public void CumulativeSumProducesCorrectValues(double[] inputs, double[] expected)
        {
            var series = CreateFakeSeries(inputs).CumulativeSum().Values.ToList();

            Assert.AreEqual(expected, series);
        }

        private Series<DateTime, double> CreateFakeSeries(double[] inputs)
        {
            var i = 0;
            return new Series<DateTime, double>(inputs.Select(_ =>
            {
                var time = new DateTime(1, 1, 1).AddDays(i);
                i++;

                return time;
            }).ToList(), inputs);
        }
    }
}
