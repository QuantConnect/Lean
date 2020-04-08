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
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    [TestFixture]
    public class GeneratedInsightsCollectionTests
    {
        [Test]
        public void CheckCloneRespectsDerivedTypes()
        {
            var insights = new List<DerivedInsight>
            {
                new DerivedInsight(Symbol.Empty, TimeSpan.Zero, InsightType.Price, InsightDirection.Flat),
                new DerivedInsight(Symbol.Empty, TimeSpan.Zero, InsightType.Price, InsightDirection.Flat),
                new DerivedInsight(Symbol.Empty, TimeSpan.Zero, InsightType.Price, InsightDirection.Flat),
                new DerivedInsight(Symbol.Empty, TimeSpan.Zero, InsightType.Price, InsightDirection.Flat),
            };

            var generatedInsightsCollection = new GeneratedInsightsCollection(DateTime.UtcNow, insights, clone: true);

            Assert.True(generatedInsightsCollection.Insights.TrueForAll(x => x.GetType() == typeof(DerivedInsight)));
        }

        private class DerivedInsight : Insight
        {
            public DerivedInsight(Symbol symbol, TimeSpan period, InsightType type, InsightDirection direction)
                : base(symbol, period, type, direction)
            {
            }

            public override Insight Clone()
            {
                return new DerivedInsight(Symbol, Period, Type, Direction);
            }
        }
    }
}