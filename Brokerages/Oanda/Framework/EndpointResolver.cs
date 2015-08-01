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

namespace QuantConnect.Brokerages.Oanda.Framework
{
    /// <summary>
    /// Helper class to resolve the endpoint for the Oanda RESTful call.
    /// </summary>
    public static class EndpointResolver
    {
        /// <summary>
        /// Resolves the endpoint.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        public static string ResolveEndpoint(Environment environment, Server server)
        {
            switch (environment)
            {
                case Environment.Sandbox:
                    switch (server)
                    {
                        case Server.Account:
                            return "http://api-sandbox.oanda.com/v1/";
                        case Server.Rates:
                            return "http://api-sandbox.oanda.com/v1/";
                        case Server.StreamingRates:
                            return "http://stream-sandbox.oanda.com/v1/";
                        case Server.StreamingEvents:
                            return "http://stream-sandbox.oanda.com/v1/";
                        default:
                            goto EnvironmentServerConfigurationNotFound;
                    }

                case Environment.Practice:
                    switch (server)
                    {
                        case Server.Account:
                            return "https://api-fxpractice.oanda.com/v1/";
                        case Server.Rates:
                            return "https://api-fxpractice.oanda.com/v1/";
                        case Server.StreamingRates:
                            return "https://stream-fxpractice.oanda.com/v1/";
                        case Server.StreamingEvents:
                            return "https://stream-fxpractice.oanda.com/v1/";
                        default:
                            goto EnvironmentServerConfigurationNotFound;
                    }

                case Environment.Trade:
                    switch (server)
                    {
                        case Server.Account:
                            return "https://api-fxtrade.oanda.com/v1/";
                        case Server.Rates:
                            return "https://api-fxtrade.oanda.com/v1/";
                        case Server.StreamingRates:
                            return "https://stream-fxtrade.oanda.com/v1/";
                        case Server.StreamingEvents:
                            return "https://stream-fxtrade.oanda.com/v1/";
                        default:
                            goto EnvironmentServerConfigurationNotFound;
                    }
            }

            EnvironmentServerConfigurationNotFound:
                throw new ArgumentException(string.Concat("Unexpected or unexpected Oanda Environment: ", environment , "; Server: ", server));
        }
    }
}
