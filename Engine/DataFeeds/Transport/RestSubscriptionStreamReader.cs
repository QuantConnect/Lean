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
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using RestSharp;

namespace QuantConnect.Lean.Engine.DataFeeds.Transport
{
    /// <summary>
    /// Represents a stream reader capable of polling a rest client
    /// </summary>
    public class RestSubscriptionStreamReader : IStreamReader
    {
        private readonly RestClient _client;
        private readonly RestRequest _request;
        private readonly bool _isLiveMode;
        private bool _delivered;

        /// <summary>
        /// Gets whether or not this stream reader should be rate limited
        /// </summary>
        public bool ShouldBeRateLimited => _isLiveMode;

        /// <summary>
        /// Direct access to the StreamReader instance
        /// </summary>
        public StreamReader StreamReader => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestSubscriptionStreamReader"/> class.
        /// </summary>
        /// <param name="source">The source url to poll with a GET</param>
        /// <param name="headers">Defines header values to add to the request</param>
        /// <param name="isLiveMode">True for live mode, false otherwise</param>
        public RestSubscriptionStreamReader(string source, IEnumerable<KeyValuePair<string, string>> headers, bool isLiveMode)
        {
            _client = new RestClient(source);
            _request = new RestRequest(Method.GET);
            _isLiveMode = isLiveMode;
            _delivered = false;

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    _request.AddHeader(header.Key, header.Value);
                }
            }
        }

        /// <summary>
        /// Gets <see cref="SubscriptionTransportMedium.Rest"/>
        /// </summary>
        public SubscriptionTransportMedium TransportMedium
        {
            get { return SubscriptionTransportMedium.Rest; }
        }

        /// <summary>
        /// Gets whether or not there's more data to be read in the stream
        /// </summary>
        public bool EndOfStream
        {
            get { return !_isLiveMode && _delivered; }
        }

        /// <summary>
        /// Gets the next line/batch of content from the stream
        /// </summary>
        public string ReadLine()
        {
            try
            {
                var response = _client.Execute(_request);
                if (response != null)
                {
                    _delivered = true;
                    return response.Content;
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }

            return string.Empty;
        }

        /// <summary>
        /// This stream reader doesn't require disposal
        /// </summary>
        public void Dispose()
        {
        }
    }
}