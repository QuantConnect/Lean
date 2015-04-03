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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OANDARestLibrary;
using OANDARestLibrary.TradeLibrary.DataTypes;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    public class OandaBrokerageAuthentication : BrokerageAuthentication
    {
        // These are specific to Oanda
        private static string _apiServer   = null;
        private static int    _accountId   = 0;
        private static string _accessToken = null;

        // All Implementations
        private static bool   _isValid     = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public OandaBrokerageAuthentication(out Dictionary<string, string> parameters)
        {
            string apiServerPrompt   = "Enter the Oanda API Server (Sandbox, Practice, or Trade)";
            string accountIdPrompt   = "Enter your Oanda Account Number for this Algoritm";
            string accessTokenPrompt = "Enter your Oanda API Access Token";
            
            parameters = new Dictionary<string, string>();
            parameters.Add("APIServer", apiServerPrompt);
            parameters.Add("AccountID", accountIdPrompt);
            parameters.Add("AccessToken", accessTokenPrompt);
        }

        /// <summary>
        /// Oanda Parameter Validation
        /// </summary>
        /// <param name="parameters">Oanda Brokerage Parameters</param>
        /// <param name="error">Error Message</param>
        /// <returns>Error Status</returns>
        public override bool Validate(Dictionary<string, string> parameters, out StringBuilder messages)
        {
            string apiServer   = parameters["APIServer"];
            int accountId = 0; 
            Int32.TryParse(parameters["AccountID"], out accountId);
            string accessToken = parameters["AccessToken"];

            messages = new StringBuilder();

            // Assumes that validation will fail unless tests pass
            _isValid = false;

            // APIServer
            if (String.IsNullOrEmpty(apiServer))
            {
                messages.AppendLine("Input Error: No API Server Provided");
            }

            string[] environments = new string[] { "sandbox", "practice", "trade" };
            if (Array.IndexOf(environments, apiServer.ToLower()) < 0)
            {
                messages.AppendLine("Input Error: Invalid API Environment Specified");
            }
            
            // Account
            if (accountId <= 0)
            {
                messages.AppendLine("Input Error: No Account Number Provided");
            }

            // Access Token
            if (String.IsNullOrEmpty(accessToken))
            {
                messages.AppendLine("Input Error: No Access Token Provided");
            }

            // Authentication with Special Handling for Sandbox Environment
            string sandboxUser = null;
            if (OandaBrokerage.GetEnvironment(apiServer) == EEnvironment.Sandbox)
            {
                Credentials.SetCredentials(OandaBrokerage.GetEnvironment(apiServer), "", 0);

                var t1 = OandaBrokerage.CreateSandboxAccount();
                t1.Wait();

                Dictionary<string, string> sandboxParams = t1.Result;

                Int32.TryParse(sandboxParams["AccountId"], out accountId);
                sandboxUser = sandboxParams["Username"];

                Credentials.SetSandboxCredentials(accountId, sandboxUser);

                accessToken = "";
            }
            else
                Credentials.SetCredentials(OandaBrokerage.GetEnvironment(apiServer), accessToken, accountId);

            // Load Account Information
            var t2 = OandaBrokerage.GetAccountInfo(accountId, sandboxUser);
            Account account = null;
            try
            {
                t2.Wait();
                account = t2.Result;
            }
            catch (Exception ex)
            {
                messages.AppendLine(ex.Message);
                if (ex.InnerException != null)
                    messages.AppendLine(ex.InnerException.Message);
            }

            // Check the Response
            if (account != null)
                if (accountId == account.accountId)
                {
                    _isValid     = true;
                    _apiServer   = apiServer;
                    _accountId   = accountId;
                    _accessToken = accessToken;
                }
            
            // Return Results
            return _isValid;
        }
        
        /// <summary>
        /// Result of Parameter Validation
        /// </summary>
        public override bool IsValid()
        {
            return _isValid;
        }

        /// <summary>
        /// Oanda API Server
        /// </summary>
        public string APIServer
        {
            get { return _apiServer; }
        }

        /// <summary>
        /// Oanda Account Number
        /// </summary>
        public int AccountID
        {
            get { return Convert.ToInt32(_accountId) ; }
        }

        /// <summary>
        /// Oanda Access Token
        /// </summary>
        public string AccessToken
        {
            get { return _accessToken; }
        }
    }
}
