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
using System.Threading;
using CryptoExchange.Net.Objects;
using Exante.Net;
using Exante.Net.Enums;
using Exante.Net.Objects;

namespace QuantConnect.Brokerages.Exante
{
    public class ExanteClientWrapper
    {
        private readonly ExanteClient _client;
        public ExanteStreamClient StreamClient { get; private set; }

        public ExanteClientWrapper(ExanteClientOptions clientOptions)
        {
            _client = new ExanteClient(clientOptions);
            StreamClient = new ExanteStreamClient(clientOptions);
        }

        private void checkIfResponseOk<T>(WebCallResult<T> response, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            if (!(response.ResponseStatusCode == statusCode && response.Success))
            {
                throw new Exception(
                    $"ExanteBrokerage.GetActiveOrders: request failed: [{response.ResponseStatusCode}], Content: {response.Data}, ErrorMessage: {response.Error}");
            }
        }

        public ExanteAccountSummary GetAccountSummary(string accountId, string reportCurrency)
        {
            var response =
                _client.GetAccountSummaryAsync(accountId, reportCurrency).SynchronouslyAwaitTaskResult();
            checkIfResponseOk(response);
            return response.Data;
        }

        public WebCallResult<IEnumerable<ExanteOrder>> PlaceOrder(
            string accountId,
            string symbolId,
            ExanteOrderType type,
            ExanteOrderSide side,
            Decimal quantity,
            ExanteOrderDuration duration,
            Decimal? limitPrice = null,
            Decimal? stopPrice = null,
            Decimal? stopLoss = null,
            Decimal? takeProfit = null,
            int? placeInterval = null,
            string clientTag = null,
            Guid? parentId = null,
            Guid? ocoGroupId = null,
            DateTime? gttExpiration = null,
            int? priceDistance = null,
            Decimal? partQuantity = null,
            CancellationToken ct = default(CancellationToken)
            )
        {
            var response = _client.PlaceOrderAsync(
                accountId,
                symbolId,
                type,
                side,
                quantity,
                duration,
                limitPrice,
                stopPrice,
                stopLoss,
                takeProfit,
                placeInterval,
                clientTag,
                parentId,
                ocoGroupId,
                gttExpiration,
                priceDistance,
                partQuantity,
                ct).SynchronouslyAwaitTaskResult();
            checkIfResponseOk(response, HttpStatusCode.Created);
            return response;
        }

        public WebCallResult<IEnumerable<ExanteOrder>> GetActiveOrders()
        {
            var response = _client.GetActiveOrdersAsync().SynchronouslyAwaitTaskResult();
            checkIfResponseOk(response);
            return response;
        }

        public WebCallResult<IEnumerable<ExanteTick>> GetTicks(
            string symbolId,
            DateTime? from = null,
            DateTime? to = null,
            int limit = 60,
            ExanteTickType tickType = ExanteTickType.Quotes,
            CancellationToken ct = default(CancellationToken)
            )
        {
            var response = _client.GetTicksAsync(
                symbolId,
                from,
                to,
                limit,
                tickType,
                ct
            ).SynchronouslyAwaitTaskResult();
            checkIfResponseOk(response);
            return response;
        }

        public WebCallResult<ExanteSymbol> GetSymbol(
            string symbolId,
            CancellationToken ct = default(CancellationToken)
            )
        {
            var response =
                _client.GetSymbolAsync(symbolId, ct).SynchronouslyAwaitTaskResult();
            checkIfResponseOk(response);
            return response;
        }

        public WebCallResult<ExanteOrder> GetOrder(
            Guid orderId,
            CancellationToken ct = default
            )
        {
            var response =
                _client.GetOrderAsync(orderId, ct).SynchronouslyAwaitTaskResult();
            checkIfResponseOk(response);
            return response;
        }

        public WebCallResult<ExanteOrder> ModifyOrder(
            Guid orderId,
            ExanteOrderAction action,
            Decimal? quantity = null,
            Decimal? stopPrice = null,
            int? priceDistance = null,
            Decimal? limitPrice = null,
            CancellationToken ct = default(CancellationToken)
            )
        {
            var response = _client.ModifyOrderAsync(
                orderId,
                action,
                quantity,
                stopPrice,
                priceDistance,
                limitPrice,
                ct).SynchronouslyAwaitTaskResult();
            checkIfResponseOk(response);
            return response;
        }

        public WebCallResult<IEnumerable<ExanteTickShort>> GetFeedLastQuote(
            IEnumerable<string> symbolIds,
            ExanteQuoteLevel level = ExanteQuoteLevel.BestPrice,
            CancellationToken ct = default(CancellationToken)
            )
        {
            var response =
                _client.GetFeedLastQuoteAsync(symbolIds, level, ct).SynchronouslyAwaitTaskResult();
            checkIfResponseOk(response);
            return response;
        }
    }
}
