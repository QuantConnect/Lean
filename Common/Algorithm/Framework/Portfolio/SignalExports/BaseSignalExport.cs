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

using QuantConnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace QuantConnect.Algorithm.Framework.Portfolio.SignalExports
{
    /// <summary>
    /// Base class to send signals to different 3rd party API's
    /// </summary>
    public abstract class BaseSignalExport : ISignalExportTarget
    {
        /// <summary>
        /// Lazy initialization of HttpClient to be used to sent signals to different 3rd party API's
        /// </summary>
        private Lazy<HttpClient> _lazyClient = new Lazy<HttpClient>();

        /// <summary>
        /// Property to access a HttpClient
        /// </summary>
        protected HttpClient HttpClient => _lazyClient.Value;

        /// <summary>
        /// Sends positions to different 3rd party API's
        /// </summary>
        /// <param name="parameters">Holdings the user have defined to be sent to certain 3rd party API and the algorithm being ran</param>
        /// <returns>The message sent to the 3rd party API</returns>
        public abstract string Send(SignalExportTargetParameters parameters);

        /// <summary>
        /// Verifies every holding in the given list is a stock or an index
        /// </summary>
        /// <param name="holdings">A list of holdings from the portfolio,
        /// expected to be sent to CrunchDAO API</param>
        /// <exception cref="ArgumentException">Throws this exception when it finds a holding type different than stock</exception>
        protected static void VerifyTargetsAreStocks(List<PortfolioTarget> holdings)
        {
            foreach (var signal in holdings)
            {
                if (signal.Symbol.SecurityType != SecurityType.Equity && signal.Symbol.SecurityType != SecurityType.Index)
                {
                    throw new ArgumentException($"{signal.Symbol.SecurityType} security type is not implemented: CrunchDao only accepts signals for US Equities");
                }
            }
        }

        /// <summary>
        /// Dispose of HttpClient we used for the requests to the different 3rd party API's
        /// finalization
        /// </summary>
        public void Dispose()
        {
            HttpClient.Dispose();
        }
    }
}
