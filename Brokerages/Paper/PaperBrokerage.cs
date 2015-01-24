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
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages.Paper
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Paper Trading Brokerage
    /// </summary>
    public class PaperBrokerage : IBrokerage
    {
        public event EventHandler<OrderEvent> OrderFilled;
        public event EventHandler<PortfolioEvent> PortfolioChanged;
        public event EventHandler<AccountEvent> AccountChanged;

        /// <summary>
        /// Name of our brokerage
        /// </summary>
        public string Name
        {
            get { return "Paper-Trading Brokerage"; }
        }

        public bool IsConnected
        {
            get { return true; }
        }

        public void AddErrorHander(ErrorHandlerCallback callback)
        {
            throw new NotImplementedException();
        }

        public bool PlaceOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public bool UpdateOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public bool CancelOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Refresh the login session with the brokerage.
        /// </summary>
        public bool RefreshSession()
        {
            //No need to refresh session.
            return true;
        }

    } // End of Paper Brokerage:

}
