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
using System.Net;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Oanda.DataType;

namespace QuantConnect.Brokerages.Oanda.Session
{
    /// <summary>
    /// Initialise an events sessions for Oanda Brokerage.
    /// </summary>
    public class EventsSession : StreamSession<Event>
    {
        public EventsSession(int accountId)
            : base(accountId)
        {
        }

        protected override async Task<WebResponse> GetSession()
        {
            return await OandaBrokerage.StartEventsSession(new List<int> {_accountId});
        }
    }
}