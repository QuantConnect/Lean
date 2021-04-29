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

using NUnit.Framework;
using System;
using QuantConnect.Brokerages.Exante;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;

namespace QuantConnect.Tests.Brokerages.Exante
{
    [TestFixture]
    public class ExanteBrokerageTests : BrokerageTests
    {
        public ExanteBrokerageTests()
        {
            var securityType = SecurityType.Equity;
            var market = "ARCA";
            Market.Add(market, 999);
            SecurityType = securityType;
            Symbol = Symbol.Create("SPY", securityType, market);
        }

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            var clientId = Config.Get("exante-client-id");
            var applicationId = Config.Get("exante-application-id");
            var sharedKey = Config.Get("exante-shared-key");
            var accountId = Config.Get("exante-account-id");
            var platformTypeStr = Config.Get("exante-platform-type");
            var exanteClientOptions =
                ExanteBrokerageFactory.createExanteClientOptions(clientId, applicationId, sharedKey, platformTypeStr);

            var brokerage = new ExanteBrokerage(exanteClientOptions, accountId);

            return brokerage;
        }

        protected override Symbol Symbol { get; }
        protected override SecurityType SecurityType { get; }

        protected override bool IsAsync()
        {
            return true;
        }

        protected override decimal GetAskPrice(Symbol symbol)
        {
            throw new NotImplementedException();
        }
    }
}
