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

using QuantConnect.Data.Consolidators;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using NUnit.Framework;
using System;

namespace QuantConnect.Tests.Common.Data
{
    public class ClassicRangeConsolidatorTests : RangeConsolidatorTests
    {
        protected override RangeConsolidator CreateConsolidator(int range)
        {
            return new ClassicRangeConsolidator(range, x => x.Value, x => 10m);
        }

        /// <summary>
        /// This test doesn't work for ClassicRangeConsolidator since this consolidator
        /// doesn't create intermediate/phantom bars
        /// </summary>
        [TestCaseSource(nameof(PriceGapBehaviorIsTheExpectedOneTestCases))]
        public override void PriceGapBehaviorIsTheExpectedOne(Symbol symbol, double minimumPriceVariation, double range)
        {
        }

        [TestCaseSource(nameof(ConsolidatorCreatesExpectedBarsTestCases))]
        public override void ConsolidatorCreatesExpectedBarsInDifferentScenarios(List<decimal> testValues, RangeBar[] expectedBars)
        {
            base.ConsolidatorCreatesExpectedBarsInDifferentScenarios(testValues, expectedBars);
        }

        private static object[] ConsolidatorCreatesExpectedBarsTestCases = new object[]
        {
            new object[] { new List<decimal>(){ 90m, 94.5m }, new RangeBar[] {
                new RangeBar{ Open = 90m, Low = 90m, High = 91m, Close = 91m, Volume = 10m, EndTime = new DateTime(2016, 1, 2) }
            }},
            new object[] { new List<decimal>(){ 94m, 89.5m }, new RangeBar[] {
                new RangeBar { Open = 94m, Low = 93m, High = 94m, Close = 93m, Volume = 10m, EndTime = new DateTime(2016, 1, 2) }
            }},
            new object[] { new List<decimal>{ 90m, 94.5m, 89.5m }, new RangeBar[] {
                new RangeBar { Open = 90m, Low = 90m, High = 91m, Close = 91m, Volume = 10m, EndTime = new DateTime(2016, 1, 2) },
                new RangeBar { Open = 94.5m, Low = 93.50m, High = 94.50m, Close = 93.50m, Volume = 10m, EndTime = new DateTime(2016, 1, 3)}
            }},
            new object[] { new List<decimal>{ 94.5m, 89.5m, 94.5m }, new RangeBar[] {
                new RangeBar { Open = 95m, Low = 94m, High = 95m, Close = 94m, Volume = 10m, EndTime = new DateTime(2016, 1, 2)},
                new RangeBar { Open = 89.50m, Low = 89.50m, High = 90.50m, Close = 90.50m, Volume = 10m , EndTime = new DateTime(2016, 1, 3)}
            }},
        };

        protected override decimal[][] GetRangeConsolidatorExpectedValues()
        {
            return new decimal[][] {
                    new decimal[]{ 90m, 90m, 91m, 91m, 10m },
                    new decimal[]{ 94.5m, 93.5m, 94.5m, 93.5m, 20m},
                    new decimal[]{ 89.5m, 89m, 90m, 90m, 20m},
                    new decimal[]{ 90.5m, 90m, 91m, 91m, 20m},
                    new decimal[]{ 91.5m, 90.5m, 91.5m, 90.5m, 10m},
                    new decimal[]{ 90m, 90m, 91m, 91m, 20m},
                };
        }
    }
}
