/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 * Changes:
 *  * using inline declarations to block-style declaration
*/

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    public sealed partial class AlpacaTradingClient
    {
        /// <summary>
        /// Gets list of available orders from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only list of order information objects.</returns>
        public Task<IReadOnlyList<IOrder>> ListAllOrdersAsync(
            CancellationToken cancellationToken = default(CancellationToken)) =>
            // TODO: olegra - remove this overload after removing old version with separate arguments
            ListOrdersAsync(new ListOrdersRequest(), cancellationToken);

        /// <summary>
        /// Gets list of available orders from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="request">List orders request parameters.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only list of order information objects.</returns>
        public Task<IReadOnlyList<IOrder>> ListOrdersAsync(
            ListOrdersRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            request.EnsureNotNull(nameof(request)).Validate();

            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + "orders",
                Query = new QueryBuilder()
                    .AddParameter("status", request.OrderStatusFilter)
                    .AddParameter("direction", request.OrderListSorting)
                    .AddParameter("until", request.UntilDateTimeExclusive, "O")
                    .AddParameter("after", request.AfterDateTimeExclusive, "O")
                    .AddParameter("limit", request.LimitOrderNumber)
            };

            return _httpClient.GetObjectsListAsync<IOrder, JsonOrder>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }

        /// <summary>
        /// Creates new order for execution using Alpaca REST API endpoint.
        /// </summary>
        /// <param name="request">New order placement request parameters.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only order information object for newly created order.</returns>
        public async Task<IOrder> PostOrderAsync(
            NewOrderRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            request.EnsureNotNull(nameof(request)).Validate();

            var newOrder = new JsonNewOrder
            {
                Symbol = request.Symbol,
                Quantity = request.Quantity,
                OrderSide = request.Side,
                OrderType = request.Type,
                TimeInForce = request.Duration,
                LimitPrice = request.LimitPrice,
                StopPrice = request.StopPrice,
                ClientOrderId = request.ClientOrderId,
                ExtendedHours = request.ExtendedHours,
                OrderClass = request.OrderClass,
                TakeProfit = request.TakeProfitLimitPrice != null
                    ? new JsonNewOrderAdvancedAttributes
                    {
                        LimitPrice = request.TakeProfitLimitPrice
                    }
                    : null,
                StopLoss = request.StopLossStopPrice != null ||
                           request.StopLossLimitPrice != null
                    ? new JsonNewOrderAdvancedAttributes
                    {
                        StopPrice = request.StopLossStopPrice,
                        LimitPrice = request.StopLossLimitPrice
                    }
                    : null
            };

            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + "orders",
                Query = new QueryBuilder()
                    .AddParameter("nested", request.Nested)
            };

            await _alpacaRestApiThrottler.WaitToProceed(cancellationToken).ConfigureAwait(false);

            using (var content = toStringContent(newOrder))
            using (var response = await _httpClient.PostAsync(
                    builder.Uri, content, cancellationToken)
                .ConfigureAwait(false))
            {
                return await response.DeserializeAsync<IOrder, JsonOrder>()
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Updates existing order using Alpaca REST API endpoint.
        /// </summary>
        /// <param name="request">Patch order request parameters.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only order information object for updated order.</returns>
        public async Task<IOrder> PatchOrderAsync(
            ChangeOrderRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            request.EnsureNotNull(nameof(request)).Validate();

            var changeOrder = new JsonChangeOrder
            {
                Quantity = request.Quantity,
                TimeInForce = request.Duration,
                LimitPrice = request.LimitPrice,
                StopPrice = request.StopPrice,
                ClientOrderId = request.ClientOrderId,
            };

            await _alpacaRestApiThrottler.WaitToProceed(cancellationToken).ConfigureAwait(false);

            using (var httpRequest = new HttpRequestMessage(_httpMethodPatch,
                new Uri($"orders/{request.OrderId:D}", UriKind.RelativeOrAbsolute))
            {
                Content = toStringContent(changeOrder)
            })
            using (var response = await _httpClient.SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false))
            {
                return await response.DeserializeAsync<IOrder, JsonOrder>()
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get single order information by client order ID from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="clientOrderId">Client order ID for searching.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only order information object.</returns>
        public Task<IOrder> GetOrderAsync(
            String clientOrderId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + "orders:by_client_order_id",
                Query = new QueryBuilder()
                    .AddParameter("client_order_id", clientOrderId)
            };

            return _httpClient.GetSingleObjectAsync<IOrder, JsonOrder>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }

        /// <summary>
        /// Get single order information by server order ID from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="orderId">Server order ID for searching.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only order information object.</returns>
        public Task<IOrder> GetOrderAsync(
            Guid orderId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + $"orders/{orderId:D}",
            };

            return _httpClient.GetSingleObjectAsync<IOrder, JsonOrder>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }

        /// <summary>
        /// Deletes/cancel order on server by server order ID using Alpaca REST API endpoint.
        /// </summary>
        /// <param name="orderId">Server order ID for cancelling.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns><c>True</c> if order cancellation was accepted.</returns>
        public async Task<Boolean> DeleteOrderAsync(
            Guid orderId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await _alpacaRestApiThrottler.WaitToProceed(cancellationToken).ConfigureAwait(false);

            using (var response = await _httpClient.DeleteAsync(
                    new Uri($"orders/{orderId:D}", UriKind.RelativeOrAbsolute), cancellationToken)
                .ConfigureAwait(false))
            {
                return response.IsSuccessStatusCode;
            }
        }

        /// <summary>
        /// Deletes/cancel all open orders using Alpaca REST API endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>List of order cancellation status objects.</returns>
        public async Task<IReadOnlyList<IOrderActionStatus>> DeleteAllOrdersAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + "orders",
            };

            return await _httpClient.DeleteObjectsListAsync<IOrderActionStatus, JsonOrderActionStatus>(
                    _alpacaRestApiThrottler, builder, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}