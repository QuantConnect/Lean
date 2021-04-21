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

        public ExanteClientWrapper(ExanteClient client)
        {
            _client = client;
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

        public IEnumerable<ExanteOrder> PlaceOrder(
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
            return response.Data;
        }

        public IEnumerable<ExanteOrder> GetActiveOrders()
        {
            var response = _client.GetActiveOrdersAsync().SynchronouslyAwaitTaskResult();
            checkIfResponseOk(response);
            return response.Data;
        }

        public IEnumerable<ExanteTick> GetTicks(
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
            return response.Data;
        }

        public ExanteSymbol GetSymbol(
            string symbolId,
            CancellationToken ct = default(CancellationToken)
            )
        {
            var response =
                _client.GetSymbolAsync(symbolId, ct).SynchronouslyAwaitTaskResult();
            checkIfResponseOk(response);
            return response.Data;
        }

        public ExanteOrder ModifyOrder(
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
            return response.Data;
        }
    }
}
