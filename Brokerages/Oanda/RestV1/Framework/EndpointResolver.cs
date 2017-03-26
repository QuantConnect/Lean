/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2012-2013 OANDA Corporation
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
 * documentation files (the "Software"), to deal in the Software without restriction, including without 
 * limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the 
 * Software, and to permit persons to whom the Software is furnished  to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
 * the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
 * WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;

namespace QuantConnect.Brokerages.Oanda.RestV1.Framework
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
                case Environment.Practice:
                    switch (server)
                    {
                        case Server.Account:
                        case Server.Rates:
                            return "https://api-fxpractice.oanda.com/v1/";

                        case Server.StreamingRates:
                        case Server.StreamingEvents:
                            return "https://stream-fxpractice.oanda.com/v1/";

                        default:
                            goto EnvironmentServerConfigurationNotFound;
                    }

                case Environment.Trade:
                    switch (server)
                    {
                        case Server.Account:
                        case Server.Rates:
                            return "https://api-fxtrade.oanda.com/v1/";

                        case Server.StreamingRates:
                        case Server.StreamingEvents:
                            return "https://stream-fxtrade.oanda.com/v1/";

                        default:
                            goto EnvironmentServerConfigurationNotFound;
                    }
            }

            EnvironmentServerConfigurationNotFound:
                throw new ArgumentException(string.Concat("Unexpected Oanda Environment: ", environment, "; Server: ", server));
        }
    }
}
