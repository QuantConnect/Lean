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
                { StatusMessage, HandleAuthorization },
                { TradesChannel, HandleTradesChannel },
                { QuotesChannel, HandleQuotesChannel },
                { MinuteAggChannel, HandleMinuteAggChannel },
                { SecondAggChannel, HandleSecondAggChannel }
            };
        }

        /// <summary>
        /// Subscribes for the trade updates via <see cref="TradeReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeTrade(
            String symbol) =>
            Subscribe(GetParams(TradesChannel, symbol));

        /// <summary>
        /// Subscribes for the quote updates via <see cref="QuoteReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeQuote(
            String symbol) =>
            Subscribe(GetParams(QuotesChannel, symbol));

        /// <summary>
        /// Subscribes for the second bar updates via <see cref="SecondAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeSecondAgg(
            String symbol) =>
            Subscribe(GetParams(SecondAggChannel, symbol));

        /// <summary>
        /// Subscribes for the minute bar updates via <see cref="MinuteAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeMinuteAgg(
            String symbol) =>
            Subscribe(GetParams(MinuteAggChannel, symbol));

        /// <summary>
        /// Subscribes for the trade updates via <see cref="TradeReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void SubscribeTrade(
            IEnumerable<String> symbols) =>
            Subscribe(GetParams(TradesChannel, symbols));

        /// <summary>
        /// Subscribes for the quote updates via <see cref="QuoteReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void SubscribeQuote(
            IEnumerable<String> symbols) =>
            Subscribe(GetParams(QuotesChannel, symbols));

        /// <summary>
        /// Subscribes for the second bar updates via <see cref="SecondAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void SubscribeSecondAgg(
            IEnumerable<String> symbols) =>
            Subscribe(GetParams(SecondAggChannel, symbols));

        /// <summary>
        /// Subscribes for the minute bar updates via <see cref="MinuteAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void SubscribeMinuteAgg(
            IEnumerable<String> symbols) =>
            Subscribe(GetParams(MinuteAggChannel, symbols));

        /// <summary>
        /// Unsubscribes from the trade updates via <see cref="TradeReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeTrade(
            String symbol) =>
            Unsubscribe(GetParams(TradesChannel, symbol));

        /// <summary>
        /// Unsubscribes from the quote updates via <see cref="QuoteReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeQuote(
            String symbol) =>
            Unsubscribe(GetParams(QuotesChannel, symbol));

        /// <summary>
        /// Unsubscribes from the second bar updates via <see cref="SecondAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeSecondAgg(
            String symbol) =>
            Unsubscribe(GetParams(SecondAggChannel, symbol));

        /// <summary>
        /// Unsubscribes from the minute bar updates via <see cref="MinuteAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeMinuteAgg(
            String symbol) =>
            Unsubscribe(GetParams(MinuteAggChannel, symbol));

        /// <summary>
        /// Unsubscribes from the trade updates via <see cref="TradeReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void UnsubscribeTrade(
            IEnumerable<String> symbols) =>
            Unsubscribe(GetParams(TradesChannel, symbols));

        /// <summary>
        /// Unsubscribes from the quote updates via <see cref="QuoteReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void UnsubscribeQuote(
            IEnumerable<String> symbols) =>
            Unsubscribe(GetParams(QuotesChannel, symbols));

        /// <summary>
        /// Unsubscribes from the second bar updates via <see cref="SecondAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void UnsubscribeSecondAgg(
            IEnumerable<String> symbols) =>
            Unsubscribe(GetParams(SecondAggChannel, symbols));

        /// <summary>
        /// Unsubscribes from the minute bar updates via <see cref="MinuteAggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbols">List of asset names for subscription change.</param>
        public void UnsubscribeMinuteAgg(
            IEnumerable<String> symbols) =>
            Unsubscribe(GetParams(MinuteAggChannel, symbols));

        /// <inheritdoc/>
        [SuppressMessage(
            "Design", "CA1031:Do not catch general exception types",
            Justification = "Expected behavior - we report exceptions via OnError event.")]
        protected override void OnMessage(object sender, WebSocketMessage message)
        {
            try
            {
                foreach (var token in JArray.Parse(message.Message))
                {
                    var messageType = token["ev"];
                    if (ReferenceEquals(messageType, null))
                    {
                        var errorMessage = "Null message type.";
                        HandleError(null, new WebSocketError(errorMessage, new InvalidOperationException(errorMessage)));
                    }
                    else
                    {
                        HandleMessage(_handlers, messageType.ToString(), token);
                    }
                }
            }
            catch (Exception exception)
            {
                HandleError(null, new WebSocketError(exception.Message, exception));
            }
        }

        private void HandleAuthorization(
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
                    HandleError(null, new WebSocketError(connectionStatus.Message, new InvalidOperationException(connectionStatus.Message)));
                    break;

                case ConnectionStatus.Failed:
                case ConnectionStatus.Success:
                    break;

                default:
                    var errorMessage = "Unknown connection status.";
                    HandleError(null, new WebSocketError(errorMessage, new InvalidOperationException(errorMessage)));
                    break;
            }
        }

        private void Subscribe(
            String parameters) =>
            SendAsJsonString(new JsonListenRequest
            {
                Action = JsonAction.PolygonSubscribe,
                Params = parameters
            });

        private void Unsubscribe(
            String parameters) =>
            SendAsJsonString(new JsonUnsubscribeRequest
            {
                Action = JsonAction.PolygonUnsubscribe,
                Params = parameters
            });

        private static String GetParams(
            String channel,
            String symbol) =>
            $"{channel}.{symbol}";

        private static String GetParams(
            String channel,
            IEnumerable<String> symbols) =>
            String.Join(",",symbols.Select(symbol => GetParams(channel, symbol)));

        private void HandleTradesChannel(
            JToken token) =>
            TradeReceived.DeserializeAndInvoke<IStreamTrade, JsonStreamTrade>(token);

        private void HandleQuotesChannel(
            JToken token) =>
            QuoteReceived.DeserializeAndInvoke<IStreamQuote, JsonStreamQuote>(token);

        private void HandleMinuteAggChannel(
            JToken token) =>
            MinuteAggReceived.DeserializeAndInvoke<IStreamAgg, JsonStreamAgg>(token);

        private void HandleSecondAggChannel(
            JToken token) =>
            SecondAggReceived.DeserializeAndInvoke<IStreamAgg, JsonStreamAgg>(token);
    }
}
