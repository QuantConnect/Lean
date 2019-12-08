using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    public class TreeSynchronizingCollectionEnumerator : IEnumerator<List<BaseData>>
    {
        object IEnumerator.Current => Current;
        public List<BaseData> Current { get; private set; }

        public int GetEnumeratorCount()
        {
            return count + (left?.GetEnumeratorCount() ?? 0) + (right?.GetEnumeratorCount() ?? 0);
        }

        private int count;
        private long frontier;
        private long localFrontier;
        private readonly Enumerator[] enumerators;
        private TreeSynchronizingCollectionEnumerator left;
        private TreeSynchronizingCollectionEnumerator right;

        public TreeSynchronizingCollectionEnumerator(int count)
        {
            frontier = long.MaxValue;
            localFrontier = long.MaxValue;
            enumerators = new Enumerator[count];
        }

        public void Add(IEnumerator<BaseData> enumerator)
        {
            if (count < enumerators.Length)
            {
                var e = new Enumerator(enumerator);
                enumerators[count] = e;
                localFrontier = Math.Min(localFrontier, e.current);
                count++;
            }
            else  if (left != null && right != null)
            {
                if (left.GetEnumeratorCount() <= right.GetEnumeratorCount())
                {
                    left.Add(enumerator);
                    frontier = Math.Min(frontier, left.frontier);
                }
                else
                {
                    right.Add(enumerator);
                    frontier = Math.Min(frontier, right.frontier);
                }
            }
            else if (left == null)
            {
                left = new TreeSynchronizingCollectionEnumerator(enumerators.Length);
                left.Add(enumerator);
                frontier = Math.Min(frontier, left.frontier);
            }
            else if (right == null)
            {
                right = new TreeSynchronizingCollectionEnumerator(enumerators.Length);
                right.Add(enumerator);
                frontier = Math.Min(frontier, right.frontier);
            }
            else
            {
                throw new NotImplementedException("This else case should never be executed.");
            }

            frontier = Math.Min(
                localFrontier,
                Math.Min(
                    left?.frontier ?? long.MaxValue,
                    right?.frontier ?? long.MaxValue
                )
            );
        }

        public bool MoveNext()
        {
            var items = new List<BaseData>();
            if (frontier == localFrontier)
            {
                var nextLocalFrontier = long.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    BaseData next;
                    var e = enumerators[i];
                    while (e.TryTakeOnOrBeforeFrontier(frontier, out next))
                    {
                        items.Add(next);
                    }

                    if (e.current == long.MaxValue)
                    {
                        count--;
                        enumerators[i].enumerator.Dispose();
                        enumerators[i].enumerator = null;
                        if (i < enumerators.Length - 1)
                        {
                            for (int j = i + 1; j < count + 1; j++)
                            {
                                enumerators[j - 1] = enumerators[j];
                            }
                        }

                        i--;
                        continue;
                    }

                    nextLocalFrontier = Math.Min(nextLocalFrontier, e.current);
                }

                localFrontier = nextLocalFrontier;
            }

            if (left?.frontier == frontier)
            {
                if (left.MoveNext())
                {
                    items.AddRange(left.Current);
                }
            }

            if (right?.frontier == frontier)
            {
                if (right.MoveNext())
                {
                    items.AddRange(right.Current);
                }
            }

            frontier = Math.Min(
                localFrontier,
                Math.Min(
                    left?.frontier ?? long.MaxValue,
                    right?.frontier ?? long.MaxValue
                )
            );

            Current = items;
            return items.Count > 0;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private struct Enumerator
        {
            public long current;
            public IEnumerator<BaseData> enumerator;

            public Enumerator(IEnumerator<BaseData> enumerator)
            {
                current = long.MaxValue;
                this.enumerator = enumerator;
                current = this.enumerator.MoveNext()
                    ? this.enumerator.Current.EndTime.Ticks
                    : long.MaxValue;
            }

            public bool TryTakeOnOrBeforeFrontier(long frontier, out BaseData next)
            {
                if (current <= frontier)
                {
                    next = enumerator.Current;
                    current = enumerator.MoveNext()
                        ? enumerator.Current.EndTime.Ticks
                        : long.MaxValue;
                    return true;
                }

                next = null;
                return false;
            }
        }

        public override string ToString()
        {
            return $"Frontier: {longToDateTime(frontier)} " +
                $"[local: {longToDateTime(localFrontier)} " +
                $"(Left: {left}) " +
                $"(Right: {right})";
        }

        private string longToDateTime(long val)
        {
            if (val <= DateTime.MinValue.Ticks)
            {
                return "MinValue";
            }

            if (val >= DateTime.MaxValue.Ticks)
            {
                return "MaxValue";
            }

            return $"{new DateTime(val):MM-dd:hh:mm:sss}";
        }
    }

    public class TreeSynchronizingEnumerator : IEnumerator<BaseData>
    {
        private static int indent = 0;
        public BaseData Current { get; private set; }
        object IEnumerator.Current => Current;

        public int EnumeratorCount
        {
            get
            {
                var count = 0;
                foreach (var e in enumerators)
                {
                    if (e._enumerator != null)
                    {
                        count++;
                    }
                }

                if (left != null)
                {
                    count += left.EnumeratorCount;
                }

                if (right != null)
                {
                    count += right.EnumeratorCount;
                }

                return count;
            }
        }

        private int enumeratorsCount;

        private long frontier = long.MaxValue;
        private long localFrontier = long.MaxValue;
        private readonly Enumerator[] enumerators;

        private TreeSynchronizingEnumerator left;
        private TreeSynchronizingEnumerator right;

        public TreeSynchronizingEnumerator(int count)
        {
            enumerators = new Enumerator[count];
        }

        public bool Add(IEnumerator<BaseData> enumerator, bool prime = true)
        {
            // prime everyone
            if (prime)
            {
                if (!enumerator.MoveNext())
                {
                    return false;
                }
            }

            frontier = Math.Min(enumerator.Current.EndTime.Ticks, frontier);

            for (int i = 0; i < enumerators.Length; i++)
            {
                if (enumerators[i]._enumerator == null)
                {
                    enumeratorsCount++;
                    localFrontier = Math.Min(localFrontier, enumerators[i].current);
                    enumerators[i] = new Enumerator(enumerator);
                    return true;
                }
            }

            if (left == null)
            {
                left = new TreeSynchronizingEnumerator(enumerators.Length);
            }

            if (right == null)
            {
                right = new TreeSynchronizingEnumerator(enumerators.Length);
            }

            var leftCount = left.EnumeratorCount;
            var rightCount = right.EnumeratorCount;
            if (leftCount <= rightCount)
            {
                left.Add(enumerator, false);
                frontier = Math.Min(left.frontier, frontier);
            }
            else
            {
                right.Add(enumerator, false);
                frontier = Math.Min(right.frontier, frontier);
            }

            return true;
        }

        private bool TryMoveNext(out BaseData next)
        {
            next = null;
            Log("SELF", $"Matching on: {new DateTime(frontier):O}");
            var nextLocalFrontier = long.MaxValue;
            for (int i = 0; i < enumeratorsCount; i++)
            {
                bool finished;
                var enumerator = enumerators[i];
                if (enumerator.TryMoveNext(frontier, out next, out finished))
                {
                    if (finished)
                    {
                        // shift remaining to the front of the array
                        for (int j = i + 1; j < enumeratorsCount; j++)
                        {
                            enumerators[j - 1] = enumerators[j];
                        }

                        enumeratorsCount--;
                        i--;

                        continue;
                    }
                }
                else
                {
                    nextLocalFrontier = Math.Min(nextLocalFrontier, enumerator.current);
                }
            }

            localFrontier = nextLocalFrontier;
            UpdateFrontier();

            return next != null;
        }

        private DateTime frontierDate => frontier == long.MaxValue ? DateTime.MaxValue : new DateTime(frontier);
        private DateTime localDate => localFrontier == long.MaxValue ? DateTime.MaxValue : new DateTime(localFrontier);

        public bool MoveNext()
        {
            if (frontier == long.MaxValue)
            {
                return false;
            }

            Log("MoveNext", $"START: EnumeratorCount: {enumeratorsCount} {this}");

            if (localFrontier == frontier)
            {
                Log("MoveNext", $"LocalMatch: {localDate:hhmm}");
                BaseData next;
                if (TryMoveNext(out next))
                {
                    Current = next;
                    Log("MoveNext", $"Self: ACCEPTED: {Current.EndTime:O} New local frontier: {localDate:hhmm}");
                    UpdateFrontier();
                    if (frontier != long.MaxValue)
                    {
                        Log("MoveNext", $"New Frontier: {new DateTime(frontier):O}");
                    }
                    else
                    {
                        Log("MoveNext", "Frontier max value: completed");
                    }
                    return true;
                }

                Log("MoveNext", "Local match but failed to pull from local");
            }

            if (left?.frontier == frontier)
            {
                Log("MoveNext", $"Left Not Finished: Left Frontier: {new DateTime(left.frontier):O}");
                if (left.frontier <= frontier)
                {
                    indent++;
                    if (left.MoveNext())
                    {
                        Current = left.Current;
                        Log("MoveNext", $"Left: ACCEPTED: {Current.EndTime:O}");
                        UpdateFrontier();
                        if (left.frontier != long.MaxValue)
                        {
                            Log("MoveNext", $"Left moved next: Left Frontier: {new DateTime(left.frontier):O}");
                        }
                    }

                    indent--;

                    return true;
                }
            }

            if (right?.frontier == frontier)
            {
                Log("MoveNext", $"Right Not Finished: Right Frontier: {new DateTime(right.frontier):O}");
                if (right.frontier <= frontier)
                {
                    indent++;
                    if (right.MoveNext())
                    {
                        Current = right.Current;
                        Log("MoveNext", $"Right: ACCEPTED: {Current.EndTime:O}");
                        UpdateFrontier();
                        if (right.frontier != long.MaxValue)
                        {
                            Log("MoveNext", $"Right moved next: Right Frontier: {new DateTime(right.frontier):O}");
                        }
                    }

                    indent--;

                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private struct Enumerator
        {
            internal long current;
            internal readonly IEnumerator<BaseData> _enumerator;

            public Enumerator(IEnumerator<BaseData> enumerator)
            {
                _enumerator = enumerator;
                current = enumerator.Current.EndTime.Ticks;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryMoveNext(long frontier, out BaseData next, out bool finished)
            {
                if (current <= frontier)
                {
                    Log("Enumerator", "Selected");
                    next = _enumerator.Current;
                    if (!_enumerator.MoveNext())
                    {
                        Log("Enumerator", "Selected and finished");
                        current = long.MaxValue;
                        finished = true;
                    }
                    else
                    {
                        finished = false;
                        current = _enumerator.Current.EndTime.Ticks;
                        Log("Enumerator", $"Selected and next:{new DateTime(current):O}");
                    }

                    return true;
                }

                Log("Enumerator", $"Not selected frontier: {new DateTime(frontier):O} actual: {new DateTime(current):O} Delta: {TimeSpan.FromTicks(frontier-current).TotalMinutes}");
                finished = false;
                next = null;
                return false;
            }
        }

        private void UpdateFrontier()
        {
            var str = string.Empty;
            var nextFrontier = localFrontier;
            if (localFrontier != long.MaxValue)
            {
                str += $"LocalFrontier: {new DateTime(localFrontier):O} ";
            }
            else
            {
                str += $"LocalFrontier: MaxValue ";
            }

            if (left != null)
            {
                nextFrontier = Math.Min(nextFrontier, left.frontier);
                if (left.frontier != long.MaxValue)
                {
                    str += $"Left.Frontier: {new DateTime(left.frontier):O} ";
                }
                else
                {
                    str += $"Left.Frontier: MaxValue ";
                }
            }

            if (right != null)
            {
                nextFrontier = Math.Min(nextFrontier, right.frontier);
                if (right.frontier != long.MaxValue)
                {
                    str += $"Right.Frontier: {new DateTime(right.frontier):O} ";
                }
                else
                {
                    str += $"Right.Frontier: MaxValue ";
                }
            }

            frontier = nextFrontier;
            if(frontier != long.MaxValue)
            {
                str += $"Selected: {new DateTime(frontier):O}";
            }
            else
            {
                str += $"Selected: MaxValue";
            }
            Log("UpdateFrontier", str);
        }

        private static void Log(string locator, string str)
        {
            var ind = indent == 0 ? string.Empty : new string(' ', 4 * indent);
            Console.WriteLine($"{ind}{locator}: {str}");
        }

        public override string ToString()
        {
            if (frontier == long.MaxValue)
            {
                return $"Frontier: MaxValue";
            }

            var local = localFrontier == long.MaxValue ? "MaxValue" : $"{new DateTime(localFrontier):hhmm}";
            return $"Frontier: {new DateTime(frontier):hhmm} [(Local: {local}) (Left: {left}) (Right: {right})]";
        }
    }
}