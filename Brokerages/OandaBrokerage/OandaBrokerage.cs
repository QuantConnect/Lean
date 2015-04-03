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
using System.Collections.Generic;
using System.Threading.Tasks;
using OANDARestLibrary;
using OANDARestLibrary.TradeLibrary;
using OANDARestLibrary.TradeLibrary.DataTypes;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    class OandaBrokerage : Brokerage
    {
        private static OandaBrokerageAuthentication _brokerageAuthentication;

        private readonly Dictionary<string, string> _accountProperties = new Dictionary<string, string>(); 

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

        public OandaBrokerage(OandaBrokerageAuthentication brokerageAuthentication)
            : base("Oanda Brokerage")
        {
            if (!brokerageAuthentication.IsValid())
            {
                throw new ArgumentException();
            }

            _brokerageAuthentication = brokerageAuthentication;

            throw new NotImplementedException();
        }

        public override bool PlaceOrder(QuantConnect.Orders.Order order)
        {
            throw new NotImplementedException();
        }

        public override bool UpdateOrder(QuantConnect.Orders.Order order)
        {
            throw new NotImplementedException();
        }

        public override bool CancelOrder(QuantConnect.Orders.Order order)
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

        #region Account Management
        /// <summary>
        /// Create a Sandbox Account
        /// </summary>
        /// <returns>AccountId, Username, Password</returns>
        public static async Task<Dictionary<string, string>> CreateSandboxAccount()
        {
            var parameters = new Dictionary<string, string>();

            var response = await Rest.CreateAccount();
            parameters.Add("AccountId", response.accountId.ToString());
            parameters.Add("Username", response.username);
            parameters.Add("Password", response.password);

            return parameters;
        }

        /// <summary>
        /// Retrieves Account Information from Oanda
        /// </summary>
        /// <param name="accountId">Account ID (Number)</param>
        /// <param name="user">Username is required for Sandbox access</param>
        /// <returns>Account</returns>
        public static async Task<Account> GetAccountInfo(int accountId, string user = "")
        {
            List<Account> accountList;
            Account accountInfo = null;

            if (String.IsNullOrEmpty(user))
                accountList = await Rest.GetAccountListAsync();
            else
                accountList = await Rest.GetAccountListAsync(user);

            foreach(var account in accountList)
            {
                if (account.accountId == accountId)
                {
                    accountInfo = await Rest.GetAccountDetailsAsync(account.accountId);
                }
            }

            return accountInfo;
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Look up the Oanda RestAPI Environment
        /// </summary>
        /// <param name="apiServer"></param>
        public static EEnvironment GetEnvironment(string apiServer)
        {
            var environment = EEnvironment.Sandbox;
            switch (apiServer.ToLower())
            {
                case "sandbox":
                    environment = EEnvironment.Sandbox;
                    break;
                case "practice":
                    environment = EEnvironment.Practice;
                    break;
                case "trade":
                    environment = EEnvironment.Trade;
                    break;
            }

            return environment;
        }
        #endregion
    }
}
