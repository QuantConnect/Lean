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

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System;
using System.IO;
using QuantConnect.Logging;
using RestSharp;

namespace QuantConnect.Lean.Engine
{
    /********************************************************
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Wrapper on stream reader to make it compatible with reading real files, or calling live REST endpoints.
    /// </summary>
    /// <remarks>
    ///     BaseData class can accept live REST endpoints as data sources which are polled on the frequency
    ///     specified in the custom data resolution.
    /// </remarks>
    public class SubscriptionStreamReader : IStreamReader
    {
        /********************************************************
        * CLASS PRIVATE VARIABLES
        *********************************************************/
        private StreamReader _sr = null;
        private RestClient _client = new RestClient();
        private RestRequest _request = new RestRequest();
        private DataFeedEndpoint _dataFeed = DataFeedEndpoint.Backtesting;

        /********************************************************
        * CLASS PUBLIC PROPERTIES:
        *********************************************************/
        /// <summary>
        /// End of stream boolean flag.
        /// </summary>
        /// <remarks>Files EOS is based on the underlying stream reader, but a REST API is always open.</remarks>
        public bool EndOfStream
        {
            get
            {
                var eos = false;
                switch (_dataFeed)
                {
                    case DataFeedEndpoint.FileSystem:
                    case DataFeedEndpoint.Backtesting:
                        eos = _sr.EndOfStream;
                        break;

                    case DataFeedEndpoint.LiveTrading:
                        eos = false;
                        break;
                }
                return eos;
            }
        }

        /********************************************************
        * CLASS CONSTRUCTOR
        *********************************************************/
        /// <summary>
        /// REST Streaming Reader. Each call will call to rest API again
        /// </summary>
        /// <param name="source">String location of the REST Endpoint</param>
        /// <param name="datafeed">DataEndpoint</param>
        public SubscriptionStreamReader(string source, DataFeedEndpoint datafeed)
        {
            //This is a physical file location, create a stream:
            if (datafeed == DataFeedEndpoint.Backtesting || datafeed == DataFeedEndpoint.FileSystem)
            {
                _sr = new StreamReader(source);
            }
            else
            {
                _client = new RestClient(source);
                _request = new RestRequest(Method.GET);
            }
            _dataFeed = datafeed;
        }

        /// <summary>
        /// Construct this from a normal stream reader
        /// </summary>
        /// <param name="sr"></param>
        /// <param name="datafeed"></param>
        public SubscriptionStreamReader(StreamReader sr, DataFeedEndpoint datafeed)
        {
            _sr = sr;
            _dataFeed = datafeed;
        }

        /********************************************************
        * CLASS METHODS
        *********************************************************/
        /// <summary>
        /// ReadLine wrapper on stream reader to read a line from the file or a REST API endpoint.
        /// </summary>
        /// <returns>string response from REST request</returns>
        public string ReadLine()
        {
            var line = "";

            switch (_dataFeed)
            {
                case DataFeedEndpoint.FileSystem:
                case DataFeedEndpoint.Backtesting:
                    line = _sr.ReadLine();
                    break;

                case DataFeedEndpoint.LiveTrading:
                    try
                    {
                        var response = _client.Execute(_request);
                        line = response.Content;
                    }
                    catch (Exception err)
                    {
                        Log.Error("SubscriptionStreamReader.ReadLine(): " + err.Message);
                    }
                    break;
            }
            return line;
        }

        /// <summary>
        /// Close old stream reader.
        /// </summary>
        public void Close()
        {
            if (_sr != null)
            {
                _sr.Close();
            }
        }

        /// <summary>
        /// Dispose of old stream reader.
        /// </summary>
        public void Dispose()
        {
            if (_sr != null)
            {
                _sr.Dispose();
            }
        }

    } // End of Class


    /// <summary>
    /// IStream Reader for enchancing the basic SR classes to include REST calls.
    /// </summary>
    public interface IStreamReader
    {
        /// IStream Reader Implementation - End of Stream.
        bool EndOfStream { get; }

        /// IStream Reader Implementation - Read Line.
        string ReadLine();

        /// IStream Reader Implementation - Close Reader.
        void Close();

        /// IStream Reader Implementation - Dispose Reader.
        void Dispose();
    }
} // End of Namespace
