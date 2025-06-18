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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Python.Runtime;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// This is generic rolling window.
    /// </summary>
    public class RollingWindow : RollingWindow<object>
    {
        /// <summary>
        /// Initializes a new RollingWindow with the specified size.
        /// </summary>
        /// <param name="size">The number of elements to store in the window</param>
        public RollingWindow(int size) : base(size)
        {
        }
    }

    /// <summary>
    ///     This is a window that allows for list access semantics,
    ///     where this[0] refers to the most recent item in the
    ///     window and this[Count-1] refers to the last item in the window
    /// </summary>
    /// <typeparam name="T">The type of data in the window</typeparam>
    public class RollingWindow<T> : IReadOnlyWindow<T>
    {
        // the backing list object used to hold the data
        private readonly List<T> _list;
        // read-write lock used for controlling access to the underlying list data structure
        private readonly ReaderWriterLockSlim _listLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        // the most recently removed item from the window (fell off the back)
        private T _mostRecentlyRemoved;
        // the total number of samples taken by this indicator
        private int _samples;
        // used to locate the last item in the window as an indexer into the _list
        private int _tail;
        // the size or capacity of the window
        private int _size;

        /// <summary>
        ///     Initializes a new instance of the RollwingWindow class with the specified window size.
        /// </summary>
        /// <param name="size">The number of items to hold in the window</param>
        public RollingWindow(int size)
        {
            if (size < 1)
            {
                throw new ArgumentException(Messages.RollingWindow.InvalidSize, nameof(size));
            }
            _list = new List<T>(size);
            _size = size;
        }

        /// <summary>
        ///     Gets the size of this window
        /// </summary>
        public int Size
        {
            get
            {
                try
                {
                    _listLock.EnterReadLock();
                    return _size;
                }
                finally
                {
                    _listLock.ExitReadLock();
                }
            }
            set
            {
                Resize(value);
            }
        }

        /// <summary>
        ///     Gets the current number of elements in this window
        /// </summary>
        public int Count
        {
            get
            {
                try
                {
                    _listLock.EnterReadLock();
                    return _list.Count;
                }
                finally
                {
                    _listLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     Gets the number of samples that have been added to this window over its lifetime
        /// </summary>
        public int Samples
        {
            get
            {
                try
                {
                    _listLock.EnterReadLock();
                    return _samples;
                }
                finally
                {
                    _listLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     Gets the most recently removed item from the window. This is the
        ///     piece of data that just 'fell off' as a result of the most recent
        ///     add. If no items have been removed, this will throw an exception.
        /// </summary>
        public T MostRecentlyRemoved
        {
            get
            {
                try
                {
                    _listLock.EnterReadLock();

                    if (_samples <= _size)
                    {
                        throw new InvalidOperationException(Messages.RollingWindow.NoItemsRemovedYet);
                    }
                    return _mostRecentlyRemoved;
                }
                finally
                {
                    _listLock.ExitReadLock();
                }

            }
        }

        /// <summary>
        ///     Indexes into this window, where index 0 is the most recently
        ///     entered value
        /// </summary>
        /// <param name="i">the index, i</param>
        /// <returns>the ith most recent entry</returns>
        public T this[int i]
        {
            get
            {
                try
                {
                    _listLock.EnterReadLock();

                    if (i < 0)
                    {
                        if (_size + i < 0)
                        {
                            throw new ArgumentOutOfRangeException(nameof(i), i, Messages.RollingWindow.IndexOutOfSizeRange);
                        }
                        i = _size + i;
                    }

                    if (i > _list.Count - 1)
                    {
                        if (i > _size - 1)
                        {
                            _listLock.ExitReadLock();
                            Resize(i + 1);
                            _listLock.EnterReadLock();
                        }

                        return default;
                    }

                    return _list[GetListIndex(i, _list.Count, _tail)];
                }
                finally
                {
                    _listLock.ExitReadLock();
                }
            }
            set
            {
                try
                {
                    _listLock.EnterWriteLock();

                    if (i < 0)
                    {
                        if (_size + i < 0)
                        {
                            throw new ArgumentOutOfRangeException(nameof(i), i, Messages.RollingWindow.IndexOutOfSizeRange);
                        }
                        i = _size + i;
                    }

                    if (i > _list.Count - 1)
                    {
                        if (i > _size - 1)
                        {
                            Resize(i + 1);
                        }

                        var count = _list.Count;
                        for (var j = 0; j < i - count + 1; j++)
                        {
                            Add(default);
                        }
                    }

                    _list[GetListIndex(i, _list.Count, _tail)] = value;
                }
                finally
                {
                    _listLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        ///     Gets a value indicating whether or not this window is ready, i.e,
        ///     it has been filled to its capacity
        /// </summary>
        public bool IsReady
        {
            get
            {
                try
                {
                    _listLock.EnterReadLock();
                    return _samples >= _size;
                }
                finally
                {
                    _listLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator()
        {
            try
            {
                _listLock.EnterReadLock();

                // we make a copy on purpose so the enumerator isn't tied
                // to a mutable object, well it is still mutable but out of scope
                var count = _list.Count;
                var temp = new T[count];
                for (int i = 0; i < count; i++)
                {
                    temp[i] = _list[GetListIndex(i, count, _tail)];
                }

                return ((IEnumerable<T>)temp).GetEnumerator();
            }
            finally
            {
                _listLock.ExitReadLock();
            }

        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Adds an item to this window and shifts all other elements
        /// </summary>
        /// <param name="item">The item to be added</param>
        public void Add(T item)
        {
            try
            {
                _listLock.EnterWriteLock();

                _samples++;
                if (_size == _list.Count)
                {
                    // keep track of what's the last element
                    // so we can reindex on this[ int ]
                    _mostRecentlyRemoved = _list[_tail];
                    _list[_tail] = item;
                    _tail = (_tail + 1) % _size;
                }
                else
                {
                    _list.Add(item);
                }
            }
            finally
            {
                _listLock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     Clears this window of all data
        /// </summary>
        public void Reset()
        {
            try
            {
                _listLock.EnterWriteLock();

                _samples = 0;
                _list.Clear();
                _tail = 0;
            }
            finally
            {
                _listLock.ExitWriteLock();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetListIndex(int index, int listCount, int tail)
        {
            return (listCount + tail - index - 1) % listCount;
        }

        private void Resize(int size)
        {
            try
            {
                _listLock.EnterWriteLock();

                _list.EnsureCapacity(size);
                if (size < _list.Count)
                {
                    _list.RemoveRange(0, _list.Count - size);
                }
                _size = size;
            }
            finally
            {
                _listLock.ExitWriteLock();
            }
        }
    }
}
