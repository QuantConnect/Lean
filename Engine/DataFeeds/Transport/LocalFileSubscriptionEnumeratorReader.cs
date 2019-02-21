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
using System.IO;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;

namespace QuantConnect.Lean.Engine.DataFeeds.Transport
{
    /// <summary>
    /// Implements a data point reader over local file subscription with enumerator
    /// </summary>
    public class LocalFileSubscriptionEnumeratorReader : IStreamReader
    {
        /// <summary>
        /// The enumerator of data points
        /// </summary>
        private readonly DataPointEnumerator _lineEnumerator;

        /// <summary>
        /// The subscription data config
        /// </summary>
        private readonly SubscriptionDataConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileSubscriptionEnumeratorReader"/> class.
        /// </summary>
        /// <param name="dataCacheProvider">The <see cref="IDataCacheProvider"/> used to retrieve a stream of data</param>
        /// <param name="config">The subscription config</param>
        /// <param name="source">The local file to be read</param>
        /// <param name="entryName">Specifies the zip entry to be opened. Leave null if not applicable,
        /// or to open the first zip entry found regardless of name</param>
        /// <param name="startDate">The start of data range</param>
        /// <param name="endDate">The end of data range</param>
        public LocalFileSubscriptionEnumeratorReader(
            IDataCacheProvider dataCacheProvider,
            SubscriptionDataConfig config,
            string source,
            string entryName = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            _config = config;
            _lineEnumerator = dataCacheProvider
                .FetchEnumerator(source, _config, startDate, endDate) as DataPointEnumerator;
        }

        /// <summary>
        /// Gets the transport medium of this subscription
        /// </summary>
        public SubscriptionTransportMedium TransportMedium
        {
            get { return SubscriptionTransportMedium.LocalFile; }
        }

        /// <summary>
        /// Gets if it is end of stream(enumerator)
        /// </summary>
        public bool EndOfStream
        {
            get
            {
                return _lineEnumerator == null || _lineEnumerator.EndOfStream;
            }
        }

        /// <summary>
        /// Dispose the enumerator
        /// </summary>
        public void Dispose()
        {
            _lineEnumerator?.Dispose();
        }

        /// <summary>
        /// Wrapped enumerator as ReadLine() in stream
        /// </summary>
        /// <returns>A line of data point</returns>
        public string ReadLine()
        {
            if (EndOfStream)
            {
                throw new EndOfStreamException("Line enumerator is exhausted.");
            }

            _lineEnumerator.MoveNext();
            return _lineEnumerator.Current;
        }
    }
}
