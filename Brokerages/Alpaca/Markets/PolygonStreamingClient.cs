/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.5.5
 *
 * Changes made from original:
 *   - Removed Nullable reference type definitions for compatibility with C# 6
 *   - Removed `is null` pattern match
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Provides unified type-safe access for Polygon streaming API via websockets.
    /// </summary>
    public sealed class PolygonStreamingClient : StreamingClientBase<PolygonStreamingClientConfiguration>
    {
        // Available Polygon message types

        private const String TradesChannel = "T";

        private const String QuotesChannel = "Q";

        private const String MinuteAggChannel = "AM";

        private const String SecondAggChannel = "A";

        private const String StatusMessage = "status";

        private readonly IDictionary<String, Action<JToken>> _handlers;

        /// <summary>
        /// Occured when new trade received from stream.
        /// </summary>
        public event Action<IStreamTrade> TradeReceived;

        /// <summary>
        /// Occured when new quote received from stream.
        /// </summary>
        public event Action<IStreamQuote> QuoteReceived;

        /// <summary>
        /// Occured when new bar received from stream.
        /// </summary>
        public event Action<IStreamAgg> MinuteAggReceived;

        /// <summary>
        /// Occured when new bar received from stream.
        /// </summary>
        public event Action<IStreamAgg> SecondAggReceived;

        /// <summary>
        /// Creates new instance of <see cref="PolygonStreamingClient"/> object.
        /// </summary>
        /// <param name="configuration">Configuration parameters object.</param>
        public PolygonStreamingClient(
            PolygonStreamingClientConfiguration configuration)
            : base(configuration.EnsureNotNull(nameof(configuration)))
        {
            _handlers = new Dictionary<String, Action<JToken>>(StringComparer.Ordinal)
            {
                { StatusMessage, handleAuthorization },
                { TradesChannel, handleTradesChannel },
                { QuotesChannel, handleQuotesChannel },
                { MinuteAggChannel, handleMinuteAggChannel },
                { SecondAggChannel, handleSecondAggChannel }
            };
        }

        /// <summary>
        /// Subscribes for the trade updates via <see cref="TradeReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeTrade(
            String symbol) =>
            subscribe(getParams(TradesChannel, symbol));

        /// <summary>
        /// Subscribes for the quote updates via <see cref="QuoteReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeQuote(
            String symbol) =>
            subscribe(getParams(QuotesChannel, symbol));

        /// <summary>
        /// Subscribes for the second bar updates via <see cref="SecondAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeSecondAgg(
            String symbol) =>
            subscribe(getParams(SecondAggChannel, symbol));

        /// <summary>
        /// Subscribes for the minute bar updates via <see cref="MinuteAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeMinuteAgg(
            String symbol) =>
            subscribe(getParams(MinuteAggChannel, symbol));

        /// <summary>
        /// Subscribes for the trade updates via <see cref="TradeReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void SubscribeTrade(
            IEnumerable<String> symbols) =>
            subscribe(getParams(TradesChannel, symbols));

        /// <summary>
        /// Subscribes for the quote updates via <see cref="QuoteReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void SubscribeQuote(
            IEnumerable<String> symbols) =>
            subscribe(getParams(QuotesChannel, symbols));

        /// <summary>
        /// Subscribes for the second bar updates via <see cref="SecondAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void SubscribeSecondAgg(
            IEnumerable<String> symbols) =>
            subscribe(getParams(SecondAggChannel, symbols));

        /// <summary>
        /// Subscribes for the minute bar updates via <see cref="MinuteAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void SubscribeMinuteAgg(
            IEnumerable<String> symbols) =>
            subscribe(getParams(MinuteAggChannel, symbols));

        /// <summary>
        /// Unsubscribes from the trade updates via <see cref="TradeReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeTrade(
            String symbol) =>
            unsubscribe(getParams(TradesChannel, symbol));

        /// <summary>
        /// Unsubscribes from the quote updates via <see cref="QuoteReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeQuote(
            String symbol) =>
            unsubscribe(getParams(QuotesChannel, symbol));

        /// <summary>
        /// Unsubscribes from the second bar updates via <see cref="SecondAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeSecondAgg(
            String symbol) =>
            unsubscribe(getParams(SecondAggChannel, symbol));

        /// <summary>
        /// Unsubscribes from the minute bar updates via <see cref="MinuteAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeMinuteAgg(
            String symbol) =>
            unsubscribe(getParams(MinuteAggChannel, symbol));

        /// <summary>
        /// Unsubscribes from the trade updates via <see cref="TradeReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void UnsubscribeTrade(
            IEnumerable<String> symbols) =>
            unsubscribe(getParams(TradesChannel, symbols));

        /// <summary>
        /// Unsubscribes from the quote updates via <see cref="QuoteReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void UnsubscribeQuote(
            IEnumerable<String> symbols) =>
            unsubscribe(getParams(QuotesChannel, symbols));

        /// <summary>
        /// Unsubscribes from the second bar updates via <see cref="SecondAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void UnsubscribeSecondAgg(
            IEnumerable<String> symbols) =>
            unsubscribe(getParams(SecondAggChannel, symbols));

        /// <summary>
        /// Unsubscribes from the minute bar updates via <see cref="MinuteAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void UnsubscribeMinuteAgg(
            IEnumerable<String> symbols) =>
            unsubscribe(getParams(MinuteAggChannel, symbols));

        /// <inheritdoc/>
        [SuppressMessage(
            "Design", "CA1031:Do not catch general exception types",
            Justification = "Expected behavior - we report exceptions via OnError event.")]
        protected override void OnMessageReceived(
            String message)
        {
            try
            {
                foreach (var token in JArray.Parse(message))
                {
                    var messageType = token["ev"];
                    if (ReferenceEquals(messageType, null))
                    {
                        HandleError(new InvalidOperationException());
                    }
                    else
                    {
                        HandleMessage(_handlers, messageType.ToString(), token);
                    }
                }
            }
            catch (Exception exception)
            {
                HandleError(exception);
            }
        }

        private void handleAuthorization(
            JToken token)
        {
            var connectionStatus = token.ToObject<JsonConnectionStatus>();

            // ReSharper disable once ConstantConditionalAccessQualifier
            switch (connectionStatus?.Status)
            {
                case ConnectionStatus.Connected:
                    SendAsJsonString(new JsonAuthRequest
                    {
                        Action = JsonAction.PolygonAuthenticate,
                        Params = Configuration.KeyId
                    });
                    break;

                case ConnectionStatus.AuthenticationSuccess:
                    OnConnected(AuthStatus.Authorized);
                    break;

                case ConnectionStatus.AuthenticationFailed:
                case ConnectionStatus.AuthenticationRequired:
                    HandleError(new InvalidOperationException(connectionStatus.Message));
                    break;

                case ConnectionStatus.Failed:
                case ConnectionStatus.Success:
                    break;

                default:
                    HandleError(new InvalidOperationException("Unknown connection status"));
                    break;
            }
        }

        private void subscribe(
            String parameters) =>
            SendAsJsonString(new JsonListenRequest
            {
                Action = JsonAction.PolygonSubscribe,
                Params = parameters
            });

        private void unsubscribe(
            String parameters) =>
            SendAsJsonString(new JsonUnsubscribeRequest
            {
                Action = JsonAction.PolygonUnsubscribe,
                Params = parameters
            });

        private static String getParams(
            String channel,
            String symbol) =>
            $"{channel}.{symbol}";

        private static String getParams(
            String channel,
            IEnumerable<String> symbols) =>
            String.Join(",",symbols.Select(symbol => getParams(channel, symbol)));

        private void handleTradesChannel(
            JToken token) =>
            TradeReceived.DeserializeAndInvoke<IStreamTrade, JsonStreamTrade>(token);

        private void handleQuotesChannel(
            JToken token) =>
            QuoteReceived.DeserializeAndInvoke<IStreamQuote, JsonStreamQuote>(token);

        private void handleMinuteAggChannel(
            JToken token) =>
            MinuteAggReceived.DeserializeAndInvoke<IStreamAgg, JsonStreamAgg>(token);

        private void handleSecondAggChannel(
            JToken token) =>
            SecondAggReceived.DeserializeAndInvoke<IStreamAgg, JsonStreamAgg>(token);
    }
}
