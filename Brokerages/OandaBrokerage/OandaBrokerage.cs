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
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    class OandaBrokerage : Brokerage
    {
        BrokerageAuthentication _credentials = null;

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public override bool IsConnected
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Broker Server/API Credentials 
        /// </summary>
        public override BrokerageAuthentication Credentials
        {
            get
            {
                return _credentials;
            }
        }


        public OandaBrokerage(BrokerageAuthentication credentials)
            : base("Oanda Brokerage")
        {
            _credentials = credentials;

            throw new NotImplementedException();
        }


        public override bool PlaceOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override bool CancelOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public override void Connect()
        {
            throw new NotImplementedException();
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        protected override void OnOrderEvent(OrderEvent e)
        {
            base.OnOrderEvent(e);
        }

        protected override void OnPortfolioChanged(PortfolioEvent e)
        {
            base.OnPortfolioChanged(e);
        }

        protected override void OnAccountChanged(AccountEvent e)
        {
            base.OnAccountChanged(e);
        }

        protected override void OnError(Exception e)
        {
            base.OnError(e);
        }


    }
}
