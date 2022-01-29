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

using System.Collections.Generic;
using Newtonsoft.Json;
using QuantConnect.Brokerages.Binance.Messages;
using QuantConnect.Securities;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.Binance
{
    /// <summary>
    /// Binance REST API implementation
    /// </summary>
    public class BinanceCrossMarginRestApiClient : BinanceBaseRestApiClient
    {
        private const string _apiPrefix = "/sapi/v1/margin";
        private const string _wsPrefix = "/sapi/v1";

        public BinanceCrossMarginRestApiClient(
            SymbolPropertiesDatabaseSymbolMapper symbolMapper,
            ISecurityProvider securityProvider,
            string apiKey,
            string apiSecret,
            string restApiUrl
            )
            : base(symbolMapper, securityProvider, apiKey, apiSecret, restApiUrl, _apiPrefix, _wsPrefix)
        {
        }

        protected override JsonConverter CreateAccountConverter()
            => new MarginAccountConverter();

        protected override IDictionary<string, object> CreateOrderBody(Order order)
        {
            var body = base.CreateOrderBody(order);
            body["isisolated"] = "FALSE";
            body["sideEffectType"] = "MARGIN_BUY";

            return body;
        }
    }
}
