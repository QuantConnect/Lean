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
        private static readonly DateTime _referenceTime = new DateTime(2019, 1, 1);

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
        public void HasActiveInsights()
        {
            var collection = new InsightCollection();

            Assert.IsFalse(collection.HasActiveInsights(Symbols.AAPL, DateTime.MinValue));

            collection.AddRange(GetTestInsight());

            Assert.IsFalse(collection.HasActiveInsights(Symbols.AAPL, DateTime.MaxValue));

            Assert.IsTrue(collection.HasActiveInsights(Symbols.AAPL, _referenceTime));
        }

        [Test]
        public void GetNextExpiryTime()
        {
            var collection = new InsightCollection();

            Assert.AreEqual(null, collection.GetNextExpiryTime());

            collection.AddRange(GetTestInsight());

            Assert.AreEqual(_referenceTime, collection.GetNextExpiryTime());

            var nextDay = _referenceTime.AddDays(1);
            Assert.AreEqual(1, collection.RemoveExpiredInsights(nextDay).Count);
            Assert.AreEqual(nextDay, collection.GetNextExpiryTime());
        }

        [Test]
        public void TryGetValue()
        {
            var collection = new InsightCollection();

            Assert.IsFalse(collection.TryGetValue(Symbols.AAPL, out var _));

            collection.AddRange(GetTestInsight());
            Assert.IsTrue(collection.TryGetValue(Symbols.AAPL, out var insights));

            Assert.AreEqual(2, insights.Count);
            Assert.AreEqual(2, insights.Count(insight => insight.Symbol == Symbols.AAPL));
        }

        [Test]
        public void KeyNotFoundException()
        {
            var collection = new InsightCollection();
            Assert.Throws<KeyNotFoundException>(() =>
            {
                var insight = collection[Symbols.AAPL];
            });
        }

        [Test]
        public void Contains()
        {
            var collection = new InsightCollection();
            var insights = GetTestInsight();
            collection.AddRange(insights);

            foreach (var insight in insights)
            {
                Assert.IsTrue(collection.Contains(insight));
                Assert.IsTrue(collection.ContainsKey(insight.Symbol));
            }
            Assert.IsFalse(collection.ContainsKey(Symbols.BTCEUR));

            var anotherInsight = new Insight(Symbols.BTCEUR, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up);
            Assert.IsFalse(collection.Contains(anotherInsight));
        }

        [Test]
        public void Addition()
        {
            var collection = new InsightCollection();
            var insight = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = _referenceTime };
            collection.Add(insight);
            collection.Add(new Insight(Symbols.SPY, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = _referenceTime });
            collection.Add(new Insight(Symbols.IBM, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Down) { CloseTimeUtc = _referenceTime.AddDays(-1) });

            var beforeExpiration = insight.CloseTimeUtc.AddDays(-1);

            Assert.AreEqual(3, collection.Count);
            Assert.IsTrue(collection.TryGetValue(Symbols.AAPL, out var insightInCollection));
            Assert.IsTrue(collection.HasActiveInsights(Symbols.AAPL, beforeExpiration));
            Assert.AreEqual(insight, insightInCollection.Single());
            Assert.AreEqual(insight, collection[Symbols.AAPL].Single());
            Assert.AreEqual(3, collection.Count);
            Assert.AreEqual(3, collection.GetActiveInsights(beforeExpiration).Count);
            Assert.AreEqual(3, collection.GetInsights().Count);
            Assert.AreEqual(insight, collection.GetInsights(x => insight == x).Single());
            Assert.AreEqual(0, collection.GetActiveInsights(_referenceTime.AddYears(1)).Count);
            Assert.AreEqual(3, collection.TotalCount);
        }

        [Test]
        public void GetInsights()
        {
            var collection = new InsightCollection();
            var insights = GetTestInsight();
            collection.AddRange(insights);

            Assert.AreEqual(5, collection.Count);

            collection.RemoveInsights(x => x == insights[0]);
            Assert.AreEqual(4, collection.GetInsights().Count);
            Assert.AreEqual(4, collection.Count);
        }

        [Test]
        public void Removal()
        {
            var collection = new InsightCollection();
            var insights = GetTestInsight();
            collection.AddRange(insights);

            var insightCount = collection.Count;
            foreach (var insight in insights)
            {
                Assert.IsTrue(collection.Remove(insight));
                Assert.AreEqual(--insightCount, collection.Count);
            }

            // readd the first insight
            var firstInsight = insights[0];
            collection.Add(firstInsight);
            Assert.AreEqual(1, collection.Count);

            // we only remove 'firstInsight' from the global collection
            collection.RemoveInsights(x => x == firstInsight);
            Assert.AreEqual(4, collection.GetInsights().Count);
            Assert.AreEqual(0, collection.Count);
            Assert.AreEqual(6, collection.TotalCount);
        }

        [Test]
        public void ExpiredRemoval()
        {
            var collection = new InsightCollection();
            var insights = GetTestInsight();
            collection.AddRange(insights);

            Assert.AreEqual(5, collection.Count);
            Assert.AreEqual(0, collection.RemoveExpiredInsights(_referenceTime.AddDays(-1)).Count);

            // expire 1 insight
            Assert.AreEqual(insights[0], collection.RemoveExpiredInsights(_referenceTime.AddDays(1)).Single());

            // expire 2 insights
            Assert.AreEqual(2, collection.RemoveExpiredInsights(_referenceTime.AddDays(2)).Count);
            Assert.AreEqual(2, collection.Count);
            Assert.AreEqual(5, collection.TotalCount);
        }

        [Test]
        public void IndexAccess()
        {
            var collection = new InsightCollection();
            collection.AddRange(GetTestInsight());

            collection[Symbols.AAPL] = null;
            Assert.AreEqual(3, collection.Count);

            var insight = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 1) };
            var insight2 = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = new DateTime(2019, 1, 2) };
            collection[Symbols.AAPL] = new() { insight, insight2 };
            Assert.AreEqual(5, collection.Count);

            collection[Symbols.AAPL] = null;
            Assert.AreEqual(3, collection.Count);

            Assert.AreEqual(7, collection.TotalCount);
        }

        [Test]
        public void AddRange()
        {
            var collection = new InsightCollection();
            var insights = GetTestInsight();
            collection.AddRange(insights);

            Assert.AreEqual(5, collection.Count);

            foreach (var insight in insights)
            {
                Assert.IsTrue(collection.Contains(insight));
                Assert.IsTrue(collection.ContainsKey(insight.Symbol));
            }
            Assert.AreEqual(5, collection.TotalCount);
        }

        [Test]
        public void ClearSymbols()
        {
            var collection = new InsightCollection();
            collection.AddRange(GetTestInsight());

            collection.Clear(Array.Empty<Symbol>());
            Assert.AreEqual(5, collection.Count);

            collection.Clear(new[] { Symbols.AAPL });
            Assert.AreEqual(3, collection.Count);
            Assert.IsTrue(collection.ContainsKey(Symbols.SPY));
            Assert.IsTrue(collection.ContainsKey(Symbols.IBM));
            Assert.IsFalse(collection.ContainsKey(Symbols.AAPL));
            Assert.AreEqual(5, collection.TotalCount);
        }



        private static List<Insight> GetTestInsight()
        {
            var insight = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = _referenceTime };
            var insight2 = new Insight(Symbols.AAPL, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = _referenceTime.AddDays(1) };
            var insight3 = new Insight(Symbols.SPY, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up) { CloseTimeUtc = _referenceTime.AddDays(1) };

            var insight4 = new Insight(Symbols.SPY, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Down) { CloseTimeUtc = _referenceTime.AddMonths(1) };
            var insight5 = new Insight(Symbols.IBM, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Down) { CloseTimeUtc = _referenceTime.AddMonths(1) };

            return new List<Insight> { insight, insight2, insight3, insight4, insight5 };
        }
    }
}
