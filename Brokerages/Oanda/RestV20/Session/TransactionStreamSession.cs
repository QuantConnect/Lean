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

using System.Net;
using QuantConnect.Brokerages.Oanda;

namespace Oanda.RestV20.Session
{
    /// <summary>
    /// Transaction streaming session
    /// </summary>
    public class TransactionStreamSession : StreamSession
    {
        private readonly OandaRestApiV20 _api;

        /// <summary>
        /// Creates an instance of the <see cref="TransactionStreamSession"/> class
        /// </summary>
        public TransactionStreamSession(OandaRestApiV20 api)
        {
            _api = api;
        }

        /// <summary>
        /// Returns the started session
        /// </summary>
        protected override WebResponse GetSession()
        {
            return _api.StartEventsSession();
        }
    }
}