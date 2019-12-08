using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Util;

namespace QuantConnect.Tests.Engine.DataFeeds.Enumerators
{
    [TestFixture]
    public class BinaryTreeSynchronizingEnumeratorTests
    {
        private static readonly Comparer<int> IntComparer = Comparer<int>.Default;

        //[Test]
        //public void SynchronizesTwoEnumerators()
        //{
        //    var e1 = Enumerable.Range(0, 10).Select(i => 2 * i).GetEnumerator();
        //    var e2 = Enumerable.Range(0, 7).Select(i => 3 * i).GetEnumerator();
        //    var synchronizer = new BinaryTreeSynchronizingEnumerator<int>(e1, e2, IntComparer);

        //    AssertSynchronized(synchronizer, IntComparer, 17);
        //}

        //[Test]
        //public void AddsEnumeratorAndMaintainsSynchronization()
        //{
        //    var e1 = Enumerable.Range(0, 10).Select(i => 2 * i).GetEnumerator();
        //    var e2 = Enumerable.Range(0, 7).Select(i => 3 * i).GetEnumerator();
        //    var synchronizer = new BinaryTreeSynchronizingEnumerator<int>(e1, e2, IntComparer);

        //    var e3 = Enumerable.Range(0, 5).Select(i => 4 * i).GetEnumerator();
        //    synchronizer = synchronizer.Add(e3);

        //    AssertSynchronized(synchronizer, IntComparer, 22);
        //}

        //[Test]
        //public void AddsEnumeratorAfterEnumerationHasStartedAndMaintainsSynchronization()
        //{
        //    // it's important to note the behavior here. adding an enumerator with items 'in the past'
        //    // will cause the forward marching behavior of the synchronizer to appear as broken. this
        //    // is determined to not be an issue due to the intended use case where this enumerator will
        //    // be enumerator by a frontier aware component and will pull all items that are at or before
        //    // the current frontier, so these 'past items' will get grouped along, as is the current behavior
        //    // further, it is common to us a fast forward mechanism to remove the historical data points.

        //    var e1 = Enumerable.Range(0, 10).Select(i => 2 * i).GetEnumerator();
        //    var e2 = Enumerable.Range(0, 7).Select(i => 3 * i).GetEnumerator();
        //    var synchronizer = new BinaryTreeSynchronizingEnumerator<int>(e1, e2, IntComparer);

        //    for (int i = 0; i < 5; i++)
        //    {
        //        Assert.IsTrue(synchronizer.MoveNext());
        //    }

        //    var e3 = Enumerable.Range(0, 5).Select(i => 4 * i).GetEnumerator();
        //    synchronizer = synchronizer.Add(e3);

        //    AssertSynchronized(synchronizer, IntComparer, 17);
        //}

        //[Test]
        //public void WritesComplexBinaryTreeStructure()
        //{
        //    var enumerators = Enumerable.Range(0, 15).Select(i => Enumerable.Range(i, 15).GetEnumerator()).ToList();

        //    var synchronized = new BinaryTreeSynchronizingEnumerator<int>(IntComparer);
        //    for (int i = 0; i < enumerators.Count; i++)
        //    {
        //        synchronized = synchronized.Add(enumerators[i]);
        //    }

        //    synchronized.MoveNext();

        //    synchronized.WriteTo(Console.Out, e => $"node-{e.Value.Current}");
        //}

        [Test, Ignore]
        public void PerformanceProfiler()
        {
            var enumeratorCount = 10;
            var startTime = new DateTime(2000, 01, 03);
            var endTime = startTime.AddDays(2);
            var resolutionChoices = new[] { Resolution.Tick, Resolution.Second, Resolution.Hour, Resolution.Daily };

            // purposefully not materializing here to ensure we have different enumerator instances in each synchronizer
            var lists = Enumerable.Range(0, enumeratorCount).Select(i =>
            {
                var resolution = resolutionChoices[i % resolutionChoices.Length];
                return CreateDataEnumerator(
                    Symbol.Create($"{i}_{resolution.ToLower()}", SecurityType.Equity, Market.USA),
                    startTime,
                    resolution,
                    endTime
                );
            }).ToList();

            var expectedTotal = lists.Sum(list => list.Count);
            var treeEnumerators = lists.Select(list => (IEnumerator<BaseData>) list.GetEnumerator()).ToList();
            var treeSynchronizer = new TreeSynchronizingCollectionEnumerator(10);
            foreach (var enumerator in treeEnumerators)
            {
                treeSynchronizer.Add(enumerator);
            }

            var count = 0;
            while (treeSynchronizer.MoveNext())
            {
                count += treeSynchronizer.Current.Count;
            }

            Console.WriteLine($"Expected: {expectedTotal} Total: {count}");
        }

        [Test, Ignore]
        public void BenchmarkAgainstBruteForceSynchronizingEnumerator()
        {
            for (int ei = 500; ei < 502; ei+=10)
            {
                var startTime = new DateTime(2000, 01, 03);
                var endTime = startTime.AddMonths(1);
                var resolutionChoices = new[] {Resolution.Tick, Resolution.Second, Resolution.Hour, Resolution.Daily};

                // purposefully not materializing here to ensure we have different enumerator instances in each synchronizer
                var lists = Enumerable.Range(0, ei).Select(i =>
                {
                    var resolution = resolutionChoices[i % resolutionChoices.Length];
                    return CreateDataEnumerator(
                        Symbol.Create($"{i}_{resolution.ToLower()}", SecurityType.Equity, Market.USA),
                        startTime,
                        resolution,
                        endTime
                    );
                }).ToList();

                var expectedTotal = lists.Sum(l => l.Count);

                var bfsEnumerator = lists.Select(list => (IEnumerator<BaseData>) list.GetEnumerator()).ToList();
                var treeEnumerator = lists.Select(list => (IEnumerator<BaseData>) list.GetEnumerator()).ToList();
                var bruteForceSynchronizer = new BruteForceSynchronizingCollectionEnumerator(bfsEnumerator);
                var treeSynchronizer = new TreeSynchronizingCollectionEnumerator(20);
                foreach (var enumerator in treeEnumerator)
                {
                    treeSynchronizer.Add(enumerator);
                }

                var sw = Stopwatch.StartNew();
                treeSynchronizer.MoveNext();
                sw.Stop();
                Console.WriteLine($"Took {sw.Elapsed.TotalMilliseconds:0.0}ms to initialize");

                try
                {
                    Console.WriteLine($"Running test with {ei} enumerators");

                    // run JIT
                    bruteForceSynchronizer.MoveNext();
                    treeSynchronizer.MoveNext();

                    // run test
                    var bruteForceCount = 0;
                    var bruteForceStart = DateTime.UtcNow;
                    while (bruteForceSynchronizer.MoveNext())
                    {
                        bruteForceCount += bruteForceSynchronizer.Current.Count;
                    }

                    var bruteForceStop = DateTime.UtcNow;

                    var binaryTreeCount = 0;
                    var binaryTreeStart = DateTime.UtcNow;
                    while (treeSynchronizer.MoveNext())
                    {
                        binaryTreeCount += treeSynchronizer.Current.Count;
                    }

                    var binaryTreeStop = DateTime.UtcNow;

                    Console.WriteLine($"Expected Total: {expectedTotal}");
                    Console.WriteLine(
                        $"BruteForce: Count: {bruteForceCount} Elapsed: {(bruteForceStop - bruteForceStart).TotalMilliseconds:0.0}ms"
                    );

                    Console.WriteLine(
                        $"BinaryTree: Count: {binaryTreeCount} Elapsed: {(binaryTreeStop - binaryTreeStart).TotalMilliseconds:0.0}ms"
                    );
                }
                catch (Exception error)
                {
                    Console.WriteLine(error);
                }
            }
        }

        private class BaseDataEndTimeComparer : IComparer<BaseData>
        {
            public int Compare(BaseData x, BaseData y)
            {
                return x.EndTime.CompareTo(y.EndTime);
            }
        }

        private List<BaseData> CreateDataEnumerator(Symbol symbol, DateTime startTime, Resolution resolution, DateTime endTime)
        {
            var random = new Random();
            var time = startTime;
            var increment = resolution.ToTimeSpan();
            if (increment == TimeSpan.Zero)
            {
                increment = TimeSpan.FromMilliseconds(800);
            }

            var list = new List<BaseData>();
            while (time < endTime)
            {
                if (random.Next(0, 10) < 9)
                {
                    var data = new Tick(time, symbol, 1, 2);
                    list.Add(data);
                }

                time += increment;

                if (time.TimeOfDay.TotalHours >= 16)
                {
                    // market closed, move to ned day
                    time = time.RoundUp(Time.OneDay).AddHours(9.5);
                }

                if (time.DayOfWeek == DayOfWeek.Saturday)
                {
                    //  would be sat morning at 9:30, jump to monday at 9:30
                    time = time.AddDays(2);
                }
            }

            return list;
        }

        private void AssertSynchronized<T>(IEnumerator<T> enumerator, Comparer<T> comparer, int expectedCount, T? initialValue = null)
            where T : struct
        {
            T current;
            var count = 0;
            if (initialValue == null)
            {
                count++;
                if (!enumerator.MoveNext())
                {
                    throw new InvalidOperationException("Provided enumerator yielded zero items.");
                }

                current = enumerator.Current;
            }
            else
            {
                current = initialValue.Value;
            }

            while (enumerator.MoveNext())
            {
                count++;
                var comparison = comparer.Compare(current, enumerator.Current);
                Assert.GreaterOrEqual(0, comparison);
            }

            Assert.AreEqual(expectedCount, count);
        }

        /// <summary>
        /// Provides an implementation used as a comparison for benchmarking. This implementation follows the
        /// methodology of synchronization used in the <seealso cref="SubscriptionSynchronizer"/>, with the only
        /// difference being grouping items into a list.
        /// </summary>
        private class BruteForceSynchronizingEnumerator : IEnumerator<BaseData>
        {
            object IEnumerator.Current => Current;
            public BaseData Current { get; private set; }

            private long frontier;
            private int finished;
            private readonly List<IEnumerator<BaseData>> _enumerators;

            public BruteForceSynchronizingEnumerator(List<IEnumerator<BaseData>> enumerators)
            {
                finished = -1;
                Current = null;
                frontier = long.MaxValue;
                _enumerators = enumerators;
            }

            public bool MoveNext()
            {
                if (frontier == long.MaxValue)
                {
                    foreach (var enumerator in _enumerators)
                    {
                        enumerator.MoveNext();
                        frontier = Math.Min(frontier, enumerator.Current.EndTime.Ticks);
                    }
                }

                BaseData next = null;
                var nextFrontier = long.MaxValue;
                for (var i = 0; i < _enumerators.Count; i++)
                {
                    var enumerator = _enumerators[i];
                    var ticks = enumerator.Current.EndTime.Ticks;
                    if (ticks <= frontier)
                    {
                        if (next == null)
                        {
                            next = enumerator.Current;
                            if (!enumerator.MoveNext())
                            {
                                finished = i;
                            }

                            continue;
                        }
                    }

                    if (ticks <= nextFrontier)
                    {
                        nextFrontier = ticks;
                    }
                }

                if (finished != -1)
                {
                    _enumerators.RemoveAt(finished);
                    finished = -1;
                }

                Current = next;
                frontier = nextFrontier;
                return nextFrontier != long.MaxValue;
            }

            public void Reset()
            {
                foreach (var enumerator in _enumerators)
                {
                    enumerator.Reset();
                }
            }

            public void Dispose()
            {
                foreach (var enumerator in _enumerators)
                {
                    enumerator.Dispose();
                }
            }
        }

        /// <summary>
        /// Provides an implementation used as a comparison for benchmarking. This implementation follows the
        /// methodology of synchronization used in the <seealso cref="SubscriptionSynchronizer"/>, with the only
        /// difference being grouping items into a list.
        /// </summary>
        private class BruteForceSynchronizingCollectionEnumerator : IEnumerator<List<BaseData>>
        {
            object IEnumerator.Current => Current;
            public List<BaseData> Current { get; private set; }

            private long frontier;
            private List<int> finished;
            private readonly List<IEnumerator<BaseData>> _enumerators;

            public BruteForceSynchronizingCollectionEnumerator(List<IEnumerator<BaseData>> enumerators)
            {
                Current = null;
                frontier = long.MaxValue;
                finished = new List<int>();
                _enumerators = enumerators;
            }

            public bool MoveNext()
            {
                if (frontier == long.MaxValue)
                {
                    for (var i = 0; i < _enumerators.Count; i++)
                    {
                        var enumerator = _enumerators[i];
                        if (enumerator.MoveNext())
                        {
                            frontier = Math.Min(frontier, enumerator.Current.EndTime.Ticks);
                        }
                        else
                        {
                            finished.Add(i);
                        }
                    }

                    if (finished.Count > 0)
                    {
                        for (int i = finished.Count - 1; i > -1; i--)
                        {
                            _enumerators.RemoveAt(finished[i]);
                        }

                        finished.Clear();
                    }
                }

                var next = new List<BaseData>();
                var nextFrontier = long.MaxValue;
                for (var i = 0; i < _enumerators.Count; i++)
                {
                    var enumerator = _enumerators[i];
                    var ticks = enumerator.Current.EndTime.Ticks;
                    while (ticks <= frontier)
                    {
                        next.Add(enumerator.Current);
                        if (!enumerator.MoveNext())
                        {
                            finished.Add(i);
                        }

                        break;
                    }

                    nextFrontier = Math.Min(nextFrontier, ticks);
                }

                if (finished.Count > 0)
                {
                    for (int i = finished.Count - 1; i > -1; i--)
                    {
                        _enumerators.RemoveAt(finished[i]);
                    }

                    finished.Clear();
                }

                Current = next;
                frontier = nextFrontier;
                return nextFrontier != long.MaxValue;
            }

            public void Reset()
            {
                foreach (var enumerator in _enumerators)
                {
                    enumerator.Reset();
                }
            }

            public void Dispose()
            {
                foreach (var enumerator in _enumerators)
                {
                    enumerator.Dispose();
                }
            }
        }
    }
}
