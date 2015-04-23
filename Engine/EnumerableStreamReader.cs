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

using System.Collections.Generic;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Represents a stream reader that gets its data from an enumerable
    /// </summary>
    public class EnumerableStreamReader : IStreamReader
    {
        private readonly IEnumerator<string> _source;
        private readonly SubscriptionTransportMedium _medium;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableStreamReader"/> class.
        /// </summary>
        /// <param name="medium">The medium where the enumerable comes from</param>
        /// <param name="source">The data to be streamed</param>
        public EnumerableStreamReader(SubscriptionTransportMedium medium, IEnumerable<string> source)
        {
            _medium = medium;
            _source = source.GetEnumerator();
            EndOfStream = _source.MoveNext();
        }

        /// <summary>
        /// Gets the transport medium of this stream reader
        /// </summary>
        public SubscriptionTransportMedium TransportMedium
        {
            get { return _medium; }
        }

        /// <summary>
        /// Gets whether or not there's more data to be read in the stream
        /// </summary>
        public bool EndOfStream
        {
            get; private set;
        }

        /// <summary>
        /// Gets the next line/batch of content from the stream 
        /// </summary>
        public string ReadLine()
        {
            // we started the enumeration in the constructor, so start by grabbing current
            // and then move next and update the end of stream
            var line = _source.Current;
            EndOfStream = _source.MoveNext();
            return line;
        }

        /// <summary>
        /// Closes the stream
        /// </summary>
        public void Close()
        {
        }

        /// <summary>
        /// Disposes of the stream
        /// </summary>
        public void Dispose()
        {
            _source.Dispose();
        }
    }
}
