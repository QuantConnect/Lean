/*
 * The official C# API client for alpaca brokerage
 * Sourced from: https://github.com/alpacahq/alpaca-trade-api-csharp/tree/v3.0.2
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NATS.Client;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.Alpaca.Markets
{
    /// <summary>
    /// Provides unified type-safe access for Polygon streaming API.
    /// </summary>
    internal sealed class NatsClient : IDisposable
    {
        private readonly IDictionary<String, IAsyncSubscription> _subscriptions =
            new Dictionary<String, IAsyncSubscription>(StringComparer.Ordinal);

        private readonly Options _options;

        private IConnection _connection;

        /// <summary>
        /// Creates new instance of <see cref="NatsClient"/> object.
        /// </summary>
        /// <param name="keyId">Application key identifier.</param>
        /// <param name="isStagingEnvironment">If <c>true</c> use staging.</param>
        /// <param name="natsServers">List of NATS servers/ports.</param>
        public NatsClient(
            String keyId,
            Boolean isStagingEnvironment,
            IEnumerable<String> natsServers = null)
        {
            _options = ConnectionFactory.GetDefaultOptions();
            _options.MaxReconnect = 3;

            natsServers = (natsServers ?? new String [0]).ToArray();

            if (!natsServers.Any())
            {
                natsServers = new[]
                {
                    "nats1.polygon.io:31101",
                    "nats2.polygon.io:31102",
                    "nats3.polygon.io:31103"
                };
            }

            if (isStagingEnvironment &&
                !keyId.EndsWith("-staging"))
            {
                keyId += "-staging";
            }

            _options.Servers = natsServers
                .Select(server => $"nats://{keyId}@{server}")
                .ToArray();

            _options.AsyncErrorEventHandler += (sender, args) => OnError?.Invoke(args.Error);
        }

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
        public event Action<IStreamAgg> AggReceived;

        /// <summary>
        /// Occured when any error happened in stream.
        /// </summary>
        public event Action<String> OnError;

        /// <summary>
        /// Opens connection to Polygon streaming API.
        /// </summary>
        public void Open()
        {
            _connection = new ConnectionFactory()
                .CreateConnection(_options);
        }

        /// <summary>
        /// Subscribes for the trade updates via <see cref="TradeReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeTrade(
            String symbol)
        {
            subscribe($"T.{symbol}", handleTradeMessage);
        }

        /// <summary>
        /// Subscribes for the quote updates via <see cref="QuoteReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeQuote(
            String symbol)
        {
            subscribe($"Q.{symbol}", handleQuoteMessage);
        }

        /// <summary>
        /// Subscribes for the second bar updates via <see cref="AggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeSecondAgg(
            String symbol)
        {
            subscribe($"A.{symbol}", handleAggMessage);
        }

        /// <summary>
        /// Subscribes for the minute bar updates via <see cref="AggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void SubscribeMinuteAgg(
            String symbol)
        {
            subscribe($"AM.{symbol}", handleAggMessage);
        }

        /// <summary>
        /// Unsubscribes from the trade updates via <see cref="TradeReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeTrade(
            String symbol)
        {
            unsubscribe($"T.{symbol}");
        }

        /// <summary>
        /// Unsubscribes from the quote updates via <see cref="QuoteReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeQuote(
            String symbol)
        {
            unsubscribe($"Q.{symbol}");
        }

        /// <summary>
        /// Unsubscribes from the second bar updates via <see cref="AggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeSecondAgg(
            String symbol)
        {
            unsubscribe($"A.{symbol}");
        }

        /// <summary>
        /// Unsubscribes from the minute bar updates via <see cref="AggReceived"/>
        /// event for specific asset from Polygon streaming API.
        /// </summary>
        /// <param name="symbol">Asset name for subscription change.</param>
        public void UnsubscribeMinuteAgg(
            String symbol)
        {
            unsubscribe($"AM.{symbol}");
        }

        /// <summary>
        /// Closes connection to Polygon streaming API.
        /// </summary>
        public void Close()
        {
            foreach (var subscription in _subscriptions.Values)
            {
                subscription?.Unsubscribe();
            }

            _connection?.Close();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var subscription in _subscriptions.Values)
            {
                subscription?.Dispose();
            }

            _connection?.Dispose();
        }

        private void subscribe(
            String topic,
            EventHandler<MsgHandlerEventArgs> handler)
        {
            var subscription = _connection?.SubscribeAsync(topic, handler);

            if (subscription != null)
            {
                _subscriptions[topic] = subscription;
            }
        }

        private void unsubscribe(
            String topic)
        {
            IAsyncSubscription subscription;
            if (_subscriptions.TryGetValue(
                topic, out subscription))
            {
                _subscriptions.Remove(topic);
                subscription?.Unsubscribe();
                subscription?.Dispose();
            }
        }

        private void handleTradeMessage(
            Object sender,
            MsgHandlerEventArgs eventArgs)
        {
            var message = deserializeBytes<JsonStreamTrade>(
                eventArgs.Message.Data);
            if (message != null)
            {
                TradeReceived?.Invoke(message);
            }
        }

        private void handleQuoteMessage(
            Object sender,
            MsgHandlerEventArgs eventArgs)
        {
            var message = deserializeBytes<JsonStreamQuote>(
                eventArgs.Message.Data);
            if (message != null)
            {
                QuoteReceived?.Invoke(message);
            }
        }

        private void handleAggMessage(
            Object sender,
            MsgHandlerEventArgs eventArgs)
        {
            var message = deserializeBytes<JsonStreamAgg>(
                eventArgs.Message.Data);
            if (message != null)
            {
                AggReceived?.Invoke(message);
            }
        }

        private T deserializeBytes<T>(
            Byte[] bytes)
        {
            try
            {
                using (var stream = new MemoryStream(bytes))
                using (var textReader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    var serializer = new JsonSerializer();
                    return serializer.Deserialize<T>(jsonReader);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                return default(T);
            }
        }
    }
}
