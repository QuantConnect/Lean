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
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    public class OandaBrokerageAuthentication : BrokerageAuthentication
    {
        private static string _apiServer;
        private static int _account;
        private static string _accessToken;


        public override string IntroMessage
        {
            get
            {
                return "The OANDA brokerage conector is ... and needs to be ..." +
                       "this connected is currently untested code." + Environment.NewLine +
                       "Use at your own risk !!!";
            }
        }

        /// <summary>
        /// Oanda API Server
        /// </summary>
        [WebUIAttribute(Prompt = "Enter the Oanda API Server URL"
            , HelpUrl = "http://developer.oanda.com/rest-live/development-guide/"
            , Mandatory=true)]
        public string APIServer 
        {
            get 
            { 
                return _apiServer; 
            }
            set 
            { 
                _apiServer = value; 
            }
        }

        /// <summary>
        /// Oanda Account
        /// </summary>
        [WebUIAttribute(Prompt = "Enter your Oanda Account Number"
            , Mandatory = true)]
        public int Account
        {
            get
            {
                return _account;
            }
            set
            {
                _account = value;
            }
        }

        /// <summary>
        /// Oanda API Access Token
        /// </summary>
        [WebUIAttribute(Prompt = "Enter your Oanda Access Token"
            , Mandatory = true)]
        public string AccessToken
        {
            get
            {
                return _accessToken;
            }
            set
            {
                _accessToken = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public OandaBrokerageAuthentication()
        { }

        /// <summary>
        /// Validate the Input
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public override bool Validate(out string error)
        {
            error = string.Empty;
            
            // Check APIServer

            // Check Account

            // Check Access Token

            // For OANDA a full call to the API server with the supplied
            // values may be appropriate to validate the input (since this
            // is a RestAPI, where another broker may provide some form
            // of validation directly in their local API library.

            return true; // todo: - implementation

        }
    
    }
}
