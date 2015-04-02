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
using OANDARestLibrary;
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
        private static int    _account     = 0;
        private static string _accessToken = null;
        //Credentials _oandaCredentials = null;

        // All Implementations
        private static bool   _isValid     = false;
        private static string _lastError   = "Pending Validation";

        /// <summary>
        /// Constructor
        /// </summary>
        public OandaBrokerageAuthentication(out Dictionary<string, string> parameters)
        {
            parameters = new Dictionary<string, string>();
            // todo: implementation
            parameters.Add("APIServer", @"[""Practice"", ""Sandbox"", ""Trade""]");
            parameters.Add("Account", "");
            parameters.Add("AccessToken", "");
        }

        /// <summary>
        /// Oanda Parameter Validation
        /// </summary>
        /// <returns>true for OK (ie. no error)</returns>
        public override bool Validate()
        {
            
            // Check APIServer

            // Check Account

            // Check Access Token

            // For OANDA a full call to the API server with the supplied
            // values may be appropriate to validate the input (since this
            // is a RestAPI, where another broker may provide some form
            // of validation directly in their local API library.
            switch (this.APIServer)
            {
                case "Practice":
                    Credentials.SetCredentials(EEnvironment.Practice, this.AccessToken, this.Account);
                    break;
                case "Sandbox":
                    Credentials.SetCredentials(EEnvironment.Sandbox, this.AccessToken, this.Account);
                    break;
                case "Trade":
                    Credentials.SetCredentials(EEnvironment.Trade, this.AccessToken, this.Account);
                    break;
                default:
                    _isValid   = true;
                    _lastError = "Invalid API Server Value";
                    return _isValid;
            }

            // Verify the Connection
            string request = Rest.Server(EServer.Account) + "accounts";
            string response = MakeRequest(request);

            // todo: perform test

            // If all tests pass:
            _isValid   = true;
            _lastError = "";

            // If tests fail:
            // _isValid = false;
            // _lastError = <Error Message>
            
            // On no error return String.Empty
            return _isValid; // todo: - implementation

        }
        
        /// <summary>
        /// Result of Parameter Validation
        /// </summary>
        public bool IsValid
        {
            get { return _isValid;  }
        }

        /// <summary>
        /// Last Error Message
        /// </summary>
        public string LastError
        {
            get { return _lastError;  }
        }

        // **** Oanda Specific Properties & Methods ****

        /// <summary>
        /// send a request and retrieve the response
        /// </summary>
        /// <param name="requestString">the request to send</param>
        /// <returns>the response string</returns>
        private static string MakeRequest(string requestString, string method = "GET", string postData = null)
        {
            var request = WebRequest.CreateHttp(requestString);
            /*
            // for non-sandbox requests
            var accessToken = "<your access token here>";
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            */
            request.Method = method;
            if (method == "POST")
            {
                var data = Encoding.UTF8.GetBytes(postData);
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            using (var response = request.GetResponse())
            {
                // todo: need to deal with this result without using System.IO
                //       maybe implement a simple 'Connection Test' in the reference implementation
                //using (var reader = new StreamReader(response.GetResponseStream()))
                //{
                //    string responseString = reader.ReadToEnd().Trim();
                //    return responseString;
                //}
                throw new NotImplementedException();
            }
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
        public int Account
        {
            get { return Convert.ToInt32(_account) ; }
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
