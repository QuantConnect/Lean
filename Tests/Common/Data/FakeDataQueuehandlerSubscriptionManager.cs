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

using QuantConnect.Data;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common.Data
{
    public class FakeDataQueuehandlerSubscriptionManager : DataQueueHandlerSubscriptionManager
    {
        private Func<TickType, string> _getChannelName;

        public FakeDataQueuehandlerSubscriptionManager(Func<TickType, string> getChannelName)
        {
            _getChannelName = getChannelName;
        }

        protected override bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType) => true;

        protected override bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType) => true;

        protected override string ChannelNameFromTickType(TickType tickType) => _getChannelName(tickType);
    }
}
