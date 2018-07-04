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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages.InteractiveBrokers;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture, Ignore("These tests require the IBController and IB TraderWorkstation to be installed.")]
    public class InteractiveBrokersForexOrderTests : BrokerageTests
    {
        // set to true to disable launch of gateway from tests
        private const bool _manualGatewayControl = false;
        private static bool _gatewayLaunched;

        [TestFixtureSetUp]
        public void InitializeBrokerage()
        {
        }

        [TestFixtureTearDown]
        public void DisposeBrokerage()
        {
            InteractiveBrokersGatewayRunner.Stop();
        }

        protected override Symbol Symbol
        {
            get { return Symbols.USDJPY; }
        }

        protected override SecurityType SecurityType
        {
            get { return SecurityType.Forex; }
        }

        protected override decimal HighPrice
        {
            get { return 10000m; }
        }

        protected override decimal LowPrice
        {
            get { return 0.01m; }
        }

        /// <summary>
        /// Returns wether or not the brokers order methods implementation are async
        /// </summary>
        protected override bool IsAsync()
        {
            return true;
        }

        protected override decimal GetAskPrice(Symbol symbol)
        {
            throw new NotImplementedException();
        }

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            if (!_manualGatewayControl && !_gatewayLaunched)
            {
                _gatewayLaunched = true;
                InteractiveBrokersGatewayRunner.Start(Config.Get("ib-controller-dir"),
                    Config.Get("ib-tws-dir"),
                    Config.Get("ib-user-name"),
                    Config.Get("ib-password"),
                    Config.Get("ib-trading-mode"),
                    Config.GetBool("ib-use-tws")
                    );
            }
            return new InteractiveBrokersBrokerage(new QCAlgorithm(), orderProvider, securityProvider);
        }

        protected override void DisposeBrokerage(IBrokerage brokerage)
        {
            if (!_manualGatewayControl && brokerage != null)
            {
                brokerage.Disconnect();
            }
        }
    }
}
