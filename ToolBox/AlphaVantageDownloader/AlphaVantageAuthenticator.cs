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

using RestSharp;
using RestSharp.Authenticators;
using System;

namespace QuantConnect.ToolBox.AlphaVantageDownloader
{
    /// <summary>
    /// Implements authentication for Alpha Vantage API
    /// </summary>
    internal class AlphaVantageAuthenticator : IAuthenticator
    {
        private readonly string _apiKey;

        /// <summary>
        /// Construct authenticator
        /// </summary>
        /// <param name="apiKey">API key</param>
        /// <remarks>See https://www.alphavantage.co/support/#api-key to get a free key.</remarks>
        public AlphaVantageAuthenticator(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            _apiKey = apiKey;
        }

        /// <summary>
        /// Authenticate request
        /// </summary>
        /// <param name="client">The <see cref="IRestClient"/></param>
        /// <param name="request">The <see cref="IRestRequest"/></param>
        public void Authenticate(IRestClient client, IRestRequest request)
            => request.AddOrUpdateParameter("apikey", _apiKey);
    }
}
