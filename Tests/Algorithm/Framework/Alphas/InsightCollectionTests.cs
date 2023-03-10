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
using System.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    [TestFixture]
    public class InsightCollectionTests
    {
        [Test]
        public void InsightCollectionShouldBeAbleToBeConvertedToListWithoutStackOverflow()
        {
            var insightCollection = new InsightCollection
            {
                new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up)
                {
                    CloseTimeUtc = new DateTime(2019, 1, 1),
                },
                new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Volatility, InsightDirection.Up)
                {
                    CloseTimeUtc = new DateTime(2019, 1, 2),
                }
            };

            Assert.DoesNotThrow(() => insightCollection.OrderBy(x => x.CloseTimeUtc).ToList());
        }

        [Test]
        public void Addition()
        {
            var collection = new InsightCollection();
            var insight = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 1) };
            collection.Add(insight);

            var beforeExpiration = insight.CloseTimeUtc.AddDays(-1);

            Assert.AreEqual(1, collection.Count);
            Assert.IsTrue(collection.ContainsKey(Symbols.AAPL));
            Assert.IsTrue(collection.Contains(insight));
            Assert.IsTrue(collection.TryGetValue(Symbols.AAPL, out var insightInCollection));
            Assert.IsTrue(collection.HasActiveInsights(Symbols.AAPL, beforeExpiration));
            Assert.AreEqual(insight, insightInCollection.Single());
            Assert.AreEqual(insight, collection[Symbols.AAPL].Single());
            Assert.AreEqual(insight, collection.Single());
            Assert.AreEqual(insight, collection.GetActiveInsights(beforeExpiration).Single());
            Assert.AreEqual(insight, collection.GetInsights().Single());
        }

        [Test]
        public void GetInsights()
        {
            var collection = new InsightCollection();
            var insight = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 1) };
            collection.Add(insight);
            var insight2 = new Insight(Symbols.SPY, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 2) };
            collection.Add(insight2);

            Assert.AreEqual(2, collection.Count);

            collection.RemoveInsights(x => x == insight);
            Assert.AreEqual(1, collection.GetInsights().Count);
            Assert.AreEqual(1, collection.Count);
        }

        [Test]
        public void Removal()
        {
            var collection = new InsightCollection();
            var insight = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 1) };
            collection.Add(insight);

            Assert.AreEqual(1, collection.Count);
            Assert.IsTrue(collection.Remove(insight));
            Assert.AreEqual(0, collection.Count);

            collection.Add(insight);
            Assert.AreEqual(1, collection.Count);
            collection.RemoveInsights(x => x == insight);
            Assert.AreEqual(0, collection.GetInsights().Count);
            Assert.AreEqual(0, collection.Count);
        }

        [Test]
        public void ExpiredRemoval()
        {
            var collection = new InsightCollection();
            var insight = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 1) };
            collection.Add(insight);

            var beforeExpiration = insight.CloseTimeUtc.AddDays(-1);
            var afterExpiration = insight.CloseTimeUtc.AddDays(1);

            Assert.AreEqual(1, collection.Count);
            Assert.AreEqual(0, collection.RemoveExpiredInsights(beforeExpiration).Count);
            Assert.AreEqual(insight, collection.RemoveExpiredInsights(afterExpiration).Single());
        }

        [Test]
        public void IndexAccess()
        {
            var collection = new InsightCollection();
            var insight = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 1) };
            var insight2 = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 2) };

            collection[Symbols.AAPL] = null;
            Assert.AreEqual(0, collection.Count);
            collection[Symbols.AAPL] = new() { insight, insight2 };

            Assert.AreEqual(2, collection.Count);

            collection[Symbols.AAPL] = null;
            Assert.AreEqual(0, collection.Count);
        }

        [Test]
        public void AddRange()
        {
            var collection = new InsightCollection();
            var insight = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 1) };
            var insight2 = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 2) };
            var insight3 = new Insight(Symbols.SPY, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 2) };

            collection.AddRange(new List<Insight> { insight, insight2, insight3 });
            Assert.AreEqual(3, collection.Count);
        }

        [Test]
        public void ClearSymbols()
        {
            var collection = new InsightCollection();
            var insight = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 1) };
            var insight2 = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 2) };
            var insight3 = new Insight(Symbols.SPY, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 2) };
            collection.AddRange(new List<Insight> { insight, insight2, insight3 });

            collection.Clear(new[] { Symbols.AAPL });
            Assert.AreEqual(1, collection.Count);
            Assert.IsTrue(collection.ContainsKey(Symbols.SPY));
            Assert.IsFalse(collection.ContainsKey(Symbols.AAPL));
        }
    }
}
