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
    public class StreamReaderEnumerable : IEnumerable<string>
    {
        private int _createdEnumerator;
        private readonly StreamReader _reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamReaderEnumerable"/> class
        /// </summary>
        /// <param name="stream">The stream to be read</param>
        public StreamReaderEnumerable(Stream stream)
        {
            _reader = new StreamReader(stream);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamReaderEnumerable"/> class
        /// </summary>
        /// <param name="reader">The stream reader instance to convert to an enumerable of string</param>
        public StreamReaderEnumerable(StreamReader reader)
        {
            _reader = reader;
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

            return new Enumerator(_reader);
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class Enumerator : IEnumerator<string>
        {
            private string _current;
            private readonly StreamReader _reader;

            public Enumerator(StreamReader reader)
            {
                _reader = reader;
            }

            public void Dispose()
            {
                _reader.DisposeSafely();
            }

            public bool MoveNext()
            {
                var line = _reader.ReadLine();
                if (line == null)
                {
                    return false;
                }

                _current = line;
                return true;
            }

            public void Reset()
            {
                if (!_reader.BaseStream.CanSeek)
                {
                    throw new InvalidOperationException("The underlying stream is unseekable");
                }

                _reader.BaseStream.Seek(0, SeekOrigin.Begin);
            }

            public string Current
            {
                get { return _current; }
                private set { _current = value; }
            }

            object IEnumerator.Current => Current;
        }
    }
}
