/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 * Changes:
 *  * default literal to default(T)
 *  * inline using declarations to block style using statements
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
        /// Gets account information from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only account information.</returns>
        public Task<IAccount> GetAccountAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + "account",
            };

            return _httpClient.GetSingleObjectAsync<IAccount, JsonAccount>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }

        /// <summary>
        /// Gets account configuration settings from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Mutable version of account configuration object.</returns>
        public Task<IAccountConfiguration> GetAccountConfigurationAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + "account/configurations",
            };

            return _httpClient.GetSingleObjectAsync<IAccountConfiguration, JsonAccountConfiguration>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }

        /// <summary>
        /// Updates account configuration settings using Alpaca REST API endpoint.
        /// </summary>
        /// <param name="accountConfiguration">New account configuration object for updating.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Mutable version of updated account configuration object.</returns>
        public async Task<IAccountConfiguration> PatchAccountConfigurationAsync(
            IAccountConfiguration accountConfiguration,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await _alpacaRestApiThrottler.WaitToProceed(cancellationToken).ConfigureAwait(false);

            using (var request = new HttpRequestMessage(_httpMethodPatch,
                new Uri("account/configurations", UriKind.RelativeOrAbsolute))
            {
                Content = toStringContent(accountConfiguration)
            })
            using (var response = await _httpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false))
            {
                return await response.DeserializeAsync<IAccountConfiguration, JsonAccountConfiguration>()
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets list of account activities from Alpaca REST API endpoint by specific activity.
        /// </summary>
        /// <param name="request">Account activities request parameters.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only list of account activity record objects.</returns>
        public Task<IReadOnlyList<IAccountActivity>> ListAccountActivitiesAsync(
            AccountActivitiesRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            request.EnsureNotNull(nameof(request)).Validate();

            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + $"account/activities",
                Query = new QueryBuilder()
                    .AddParameter("activity_types", request.ActivityTypes)
                    .AddParameter("date", request.Date, DateTimeHelper.DateFormat)
                    .AddParameter("until", request.Until, "O")
                    .AddParameter("after", request.After, "O")
                    .AddParameter("direction", request.Direction)
                    .AddParameter("pageSize", request.PageSize)
                    .AddParameter("pageToken", request.PageToken)
            };

            return _httpClient.GetObjectsListAsync<IAccountActivity, JsonAccountActivity>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }

        /// <summary>
        /// Gets portfolio equity history from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="request">Portfolio history request parameters.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only portfolio history information object.</returns>
        public Task<IPortfolioHistory> GetPortfolioHistoryAsync(
            PortfolioHistoryRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            request.EnsureNotNull(nameof(request)).Validate();

            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + "account/portfolio/history",
                Query = new QueryBuilder()
                    .AddParameter("start_date", request.StartDate, DateTimeHelper.DateFormat)
                    .AddParameter("end_date", request.EndDate, DateTimeHelper.DateFormat)
                    .AddParameter("period", request.Period?.ToString())
                    .AddParameter("timeframe", request.TimeFrame)
                    .AddParameter("extended_hours", request.ExtendedHours)
            };

            return _httpClient.GetSingleObjectAsync<IPortfolioHistory, JsonPortfolioHistory>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }

        /// <summary>
        /// Gets list of all available assets from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only list of asset information objects.</returns>
        public Task<IReadOnlyList<IAsset>> ListAllAssetsAsync(
            CancellationToken cancellationToken = default(CancellationToken)) =>
            // TODO: olegra - remove this overload after removing old version with separate arguments
            ListAssetsAsync(new AssetsRequest(), cancellationToken);

        /// <summary>
        /// Gets list of available assets from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="request">Asset list request parameters.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only list of asset information objects.</returns>
        public Task<IReadOnlyList<IAsset>> ListAssetsAsync(
            AssetsRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            request.EnsureNotNull(nameof(request)).Validate();

            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + "assets",
                Query = new QueryBuilder()
                    .AddParameter("status", request.AssetStatus)
                    .AddParameter("asset_class", request.AssetClass)
            };

            return _httpClient.GetObjectsListAsync<IAsset, JsonAsset>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }

        /// <summary>
        /// Get single asset information by asset name from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="symbol">Asset name for searching.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only asset information.</returns>
        public Task<IAsset> GetAssetAsync(
            String symbol,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + $"assets/{symbol}",
            };

            return _httpClient.GetSingleObjectAsync<IAsset, JsonAsset>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }

        /// <summary>
        /// Gets list of available positions from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only list of position information objects.</returns>
        public Task<IReadOnlyList<IPosition>> ListPositionsAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + "positions",
            };

            return _httpClient.GetObjectsListAsync<IPosition, JsonPosition>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }

        /// <summary>
        /// Gets position information by asset name from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="symbol">Position asset name.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only position information object.</returns>
        public Task<IPosition> GetPositionAsync(
            String symbol,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + $"positions/{symbol}",
            };

            return _httpClient.GetSingleObjectAsync<IPosition, JsonPosition>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }

        /// <summary>
        /// Liquidates all open positions at market price using Alpaca REST API endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>List of position cancellation status objects.</returns>
        public async Task<IReadOnlyList<IPositionActionStatus>> DeleteAllPositionsAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + "positions",
            };

            return await _httpClient.DeleteObjectsListAsync<IPositionActionStatus, JsonPositionActionStatus>(
                    _alpacaRestApiThrottler, builder, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Liquidate an open position at market price using Alpaca REST API endpoint.
        /// </summary>
        /// <param name="symbol">Symbol for liquidation.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns><c>True</c> if position liquidation was accepted.</returns>
        public async Task<Boolean> DeletePositionAsync(
            String symbol,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await _alpacaRestApiThrottler.WaitToProceed(cancellationToken).ConfigureAwait(false);

            using (var response = await _httpClient.DeleteAsync(
                    new Uri($"positions/{symbol}", UriKind.RelativeOrAbsolute), cancellationToken)
                .ConfigureAwait(false))
            {
                return response.IsSuccessStatusCode;
            }
        }

        /// <summary>
        /// Get current time information from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only clock information object.</returns>
        public Task<IClock> GetClockAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + "clock",
            };

            return _httpClient.GetSingleObjectAsync<IClock, JsonClock>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }

        /// <summary>
        /// Gets list of all trading days from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only list of trading date information object.</returns>
        public Task<IReadOnlyList<ICalendar>> ListAllCalendarAsync(
            CancellationToken cancellationToken = default(CancellationToken)) =>
            // TODO: olegra - remove this overload after removing old version with separate arguments
            ListCalendarAsync(new CalendarRequest(), cancellationToken);

        /// <summary>
        /// Gets list of trading days from Alpaca REST API endpoint.
        /// </summary>
        /// <param name="request">Calendar items request parameters.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>Read-only list of trading date information object.</returns>
        public Task<IReadOnlyList<ICalendar>> ListCalendarAsync(
            CalendarRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            request.EnsureNotNull(nameof(request)).Validate();

            var builder = new UriBuilder(_httpClient.BaseAddress)
            {
                Path = _httpClient.BaseAddress.AbsolutePath + "calendar",
                Query = new QueryBuilder()
                    .AddParameter("start", request.StartDateInclusive, DateTimeHelper.DateFormat)
                    .AddParameter("end", request.EndDateInclusive, DateTimeHelper.DateFormat)
            };

            return _httpClient.GetObjectsListAsync<ICalendar, JsonCalendar>(
                _alpacaRestApiThrottler, builder, cancellationToken);
        }
    }
}
