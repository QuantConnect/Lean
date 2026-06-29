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
 *
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Lean.Engine.Results.Analysis.Analyses;

namespace QuantConnect.Tests.Engine.Results
{
    [TestFixture]
    public class FlatEquityCurveAnalysisTests
    {
        [Test]
        public void NoFlatSegmentsProducesEmptySample()
        {
            // All length-1 runs, so the curve only goes up and never stays flat
            var equityCurve = BuildEquityCurve(1, 1, 1, 1, 1);

            var result = new FlatEquityCurveAnalysis().Run(equityCurve);

            var analysis = result.Single();
            Assert.IsNull(analysis.Sample);
            Assert.IsNull(analysis.Count);
            Assert.IsEmpty(analysis.Solutions);
        }

        [Test]
        public void ReportsAllSegmentsWhenAtCap()
        {
            // Exactly 5 flat segments, so we keep them all and don't report a total count
            var equityCurve = BuildEquityCurve(2, 3, 4, 5, 6);

            var result = new FlatEquityCurveAnalysis().Run(equityCurve);

            var analysis = result.Single();
            var segments = ((IEnumerable)analysis.Sample).Cast<object>().ToList();
            Assert.AreEqual(5, segments.Count);
            Assert.IsNull(analysis.Count);
            Assert.IsNotEmpty(analysis.Solutions);
        }

        [Test]
        public void CapsToFiveBiggestSegmentsAndReportsTotalCount()
        {
            // 8 flat segments: the sample shows only the 5 longest, but Count still reports the real total of 8
            var equityCurve = BuildEquityCurve(2, 9, 3, 8, 4, 7, 5, 6);

            var result = new FlatEquityCurveAnalysis().Run(equityCurve);

            var analysis = result.Single();
            Assert.AreEqual(8, analysis.Count);

            var tradingDays = ((IEnumerable)analysis.Sample)
                .Cast<object>()
                .Select(GetTradingDays)
                .ToList();

            // The 5 longest runs, biggest first
            CollectionAssert.AreEqual(new[] { 9, 8, 7, 6, 5 }, tradingDays);
            Assert.IsNotEmpty(analysis.Solutions);
        }

        private static int GetTradingDays(object segment)
        {
            return (int)segment.GetType().GetProperty("trading_days").GetValue(segment);
        }

        /// <summary>
        /// Builds an equity curve from a list of run lengths. Each length is a run of that many equal
        /// values, and the value bumps between runs so they stay separate. A length of 1 is not flat,
        /// so passing all 1s gives a strictly increasing curve.
        /// </summary>
        private static SortedList<DateTime, decimal> BuildEquityCurve(params int[] segmentLengths)
        {
            var equityCurve = new SortedList<DateTime, decimal>();
            var date = new DateTime(2024, 1, 1);
            var value = 100m;
            foreach (var length in segmentLengths)
            {
                for (var i = 0; i < length; i++)
                {
                    equityCurve[date] = value;
                    date = date.AddDays(1);
                }
                // Bump the value so the next run is a separate segment
                value += 1m;
            }
            return equityCurve;
        }
    }
}
