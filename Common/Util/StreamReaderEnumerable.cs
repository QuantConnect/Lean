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
using System.IO;
using System.Threading;

namespace QuantConnect.Util
{
    /// <summary>
    /// Converts a <see cref="StreamReader"/> into an enumerable of string
    /// </summary>
    public class StreamReaderEnumerable : IEnumerable<string>, IDisposable
    {
        private int _disposed;
        private int _createdEnumerator;
        private readonly StreamReader _reader;
        private readonly IDisposable[] _disposables;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamReaderEnumerable"/> class
        /// </summary>
        /// <param name="stream">The stream to be read</param>
        /// <param name="disposables">Allows specifying other resources that should be disposed when this instance is disposed</param>
        public StreamReaderEnumerable(Stream stream, params IDisposable[] disposables)
        {
            _disposables = disposables;

            // this StreamReader constructor gives ownership of the stream to the StreamReader
            // which is mediated by the LeaveOpen property, so when _reader is disposed, stream
            // will also be disposed
            _reader = new StreamReader(stream);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamReaderEnumerable"/> class
        /// </summary>
        /// <param name="reader">The stream reader instance to convert to an enumerable of string</param>
        /// <param name="disposables">Allows specifying other resources that should be disposed when this instance is disposed</param>
        public StreamReaderEnumerable(StreamReader reader, params IDisposable[] disposables)
        {
            _reader = reader;
            _disposables = disposables;
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<string> GetEnumerator()
        {
            // can't share the underlying stream instance -- barf
            if (Interlocked.CompareExchange(ref _createdEnumerator, 1, 0) == 1)
            {
                throw new InvalidOperationException("A StreamReaderEnumerable may only be enumerated once. Consider using memoization or materialization.");
            }

            return new Enumerator(this);
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            _reader.DisposeSafely();
            if (_disposables != null)
            {
                foreach (var disposable in _disposables)
                {
                    disposable.DisposeSafely();
                }
            }
        }

        /// <summary>Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.</summary>
        ~StreamReaderEnumerable()
        {
            // be sure to clean up unmanaged resources via finalizer if
            // dispose wasn't explicitly called by consuming code
            Dispose();
        }

        private class Enumerator : IEnumerator<string>
        {
            private readonly StreamReaderEnumerable _enumerable;

            public string Current { get; private set; }

            object IEnumerator.Current => Current;

            public Enumerator(StreamReaderEnumerable enumerable)
            {
                _enumerable = enumerable;
            }

            public bool MoveNext()
            {
                var line = _enumerable._reader.ReadLine();
                if (line == null)
                {
                    return false;
                }

                Current = line;
                return true;
            }

            public void Reset()
            {
                if (!_enumerable._reader.BaseStream.CanSeek)
                {
                    throw new InvalidOperationException("The underlying stream is unseekable");
                }

                _enumerable._reader.BaseStream.Seek(0, SeekOrigin.Begin);
            }

            public void Dispose()
            {
                _enumerable.Dispose();
            }

            ~Enumerator()
            {
                _enumerable.Dispose();
            }
        }
    }
}
