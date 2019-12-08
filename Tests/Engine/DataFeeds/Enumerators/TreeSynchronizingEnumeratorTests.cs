using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class TreeSynchronizingEnumeratorTests
    {
        private Symbol CreateSymbol(int i)
        {
            return Symbol.Create(i.ToStringInvariant(), SecurityType.Equity, Market.USA);
        }

        [Test]
        public void SynchronizesItems()
        {
            var reference = new DateTime(2000, 01, 01);
            var lists = Enumerable.Range(0, 3).Select(i =>
                Enumerable.Range(0, 3).Select(x =>
                    new Tick(reference.AddDays(x + i), CreateSymbol(i), 1, 2)
                ).ToList()
            ).ToList();

            var total = 0;
            var expected = new HashSet<DateTime>();
            var synchronizer = new TreeSynchronizingCollectionEnumerator(1);
            foreach (var list in lists)
            {
                foreach (var tick in list)
                {
                    total++;
                    expected.Add(tick.EndTime);
                }
                synchronizer.Add(list.GetEnumerator());
            }

            var count = 0;
            var actualTotal = 0;
            var expectedCount = expected.Count;
            var current = reference;
            var times = new List<DateTime>();
            while (synchronizer.MoveNext())
            {
                count++;
                actualTotal += synchronizer.Current.Count;
                Assert.IsTrue(expected.Remove(synchronizer.Current[0].EndTime));
                Assert.GreaterOrEqual(synchronizer.Current.Last().EndTime, current);
                Assert.IsTrue(synchronizer.Current.All(c => c.EndTime.Equals(synchronizer.Current[0].EndTime)));
                Console.WriteLine($"Value: {synchronizer.Current.Max(c => c.EndTime):O}: {synchronizer}");
            }

            times.ForEach(t => Console.WriteLine(t.ToStringInvariant("O")));
            Assert.AreEqual(expectedCount, count);
            Assert.AreEqual(total, actualTotal);
        }
    }
}
