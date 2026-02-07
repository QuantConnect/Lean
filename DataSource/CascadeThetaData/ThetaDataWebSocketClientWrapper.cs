/*
 * CASCADELABS.IO
 * Cascade Labs LLC
 */

using Newtonsoft.Json;
using QuantConnect.Logging;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Lean.DataSource.CascadeThetaData.Models.Interfaces;

namespace QuantConnect.Lean.DataSource.CascadeThetaData
{
    /// <summary>
    /// Provides a WebSocket client wrapper for Cascade Labs ThetaData endpoint.
    /// Supports Bearer token authentication for remote WebSocket connections.
    /// </summary>
    public class ThetaDataWebSocketClientWrapper : WebSocketClientWrapper
    {
        /// <summary>
        /// Represents the base URL endpoint for receiving stream messages.
        /// Defaults to Cascade Labs endpoint; can be overridden via config.
        /// </summary>
        private static readonly string BaseUrl = Config.Get("thetadata-ws-url", "wss://thetadata.cascadelabs.io/v1/events");

        /// <summary>
        /// Bearer token for authentication with Cascade Labs endpoint.
        /// </summary>
        private static readonly string AuthToken = Config.Get("thetadata-auth-token", "");

        /// <summary>
        /// Represents the array of required subscription channels for receiving real-time market data.
        /// Subscribing to these channels allows access to specific types of data streams from the Options Price Reporting Authority (OPRA) feed.
        /// </summary>
        /// <remarks>
        /// Available Channels:
        ///     - TRADE: This channel provides every trade executed for a specified contract reported on the OPRA feed.
        ///     - QUOTE: This channel provides every National Best Bid and Offer (NBBO) quote for US Options reported on the OPRA feed for the specified contract.
        /// </remarks>
        private static readonly string[] Channels = { "TRADE", "QUOTE" };

        /// <summary>
        /// Represents a method that handles messages received from a WebSocket.
        /// </summary>
        private readonly Action<string> _messageHandler;

        /// <summary>
        /// Provides the ThetaData mapping between Lean symbols and brokerage specific symbols.
        /// </summary>
        private readonly ISymbolMapper _symbolMapper;

        /// <summary>
        /// The maximum number of contracts that can be streamed simultaneously under the subscription plan.
        /// <see cref="ISubscriptionPlan.MaxStreamingContracts"/>
        /// </summary>
        private readonly uint _maxStreamingContracts;

        /// <summary>
        /// Ensures thread-safe synchronization when updating <see cref="_subscribedSymbolCount"/>
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// Represents the current amount of subscribed symbols.
        /// </summary>
        private volatile uint _subscribedSymbolCount;

        /// <summary>
        /// Represents a way of tracking streaming requests made.
        /// The field should be increased for each new stream request made. 
        /// </summary>
        private int _idRequestCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThetaDataWebSocketClientWrapper"/>
        /// </summary>
        /// <param name="symbolMapper">Provides the mapping between Lean symbols and brokerage specific symbols.</param>
        /// <param name="maxStreamingContracts">The maximum number of contracts that can be streamed simultaneously under the subscription plan.</param>
        /// <param name="messageHandler">The method that handles messages received from the WebSocket client.</param>
        public ThetaDataWebSocketClientWrapper(ISymbolMapper symbolMapper, uint maxStreamingContracts, Action<string> messageHandler, EventHandler<WebSocketError> OnError)
        {
            // Initialize with Bearer auth token if configured (for Cascade Labs endpoint)
            // The sessionToken parameter is used as Authorization header
            var authHeader = !string.IsNullOrEmpty(AuthToken) ? $"Bearer {AuthToken}" : null;
            Initialize(BaseUrl, authHeader);

            _symbolMapper = symbolMapper;
            _messageHandler = messageHandler;
            _maxStreamingContracts = maxStreamingContracts;

            Closed += OnClosed;
            Message += OnMessage;
            Error += OnError;

            if (!string.IsNullOrEmpty(AuthToken))
            {
                Log.Trace($"ThetaDataWebSocketClientWrapper: Initialized with Bearer auth for {BaseUrl}");
            }
        }

        /// <summary>
        /// Wraps the Close method to handle the closing of the WebSocket connection and
        /// ensures any ongoing streaming subscriptions are stopped before closing.
        /// </summary>
        public void CloseWebSocketConnection()
        {
            if (IsOpen)
            {
                SendStopPreviousStreamingSubscriptions();
                Close();
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        /// <param name="isReSubscribeProcess">Indicates whether the subscription process is a resubscription, to prevent an increase in the <see cref="_subscribedSymbolCount"/> count.</param>
        public bool Subscribe(IEnumerable<Symbol> symbols, bool isReSubscribeProcess = false)
        {
            if (!IsOpen)
            {
                Connect();
            }

            foreach (var symbol in symbols)
            {
                lock (_lock)
                {
                    // constantly following of current amount of subscribed symbols (post increment!)
                    if (!isReSubscribeProcess && ++_subscribedSymbolCount > _maxStreamingContracts)
                    {
                        throw new ArgumentException($"{nameof(ThetaDataWebSocketClientWrapper)}.{nameof(Subscribe)}: Subscription Limit Exceeded. The number of symbols you're trying to subscribe to exceeds the maximum allowed limit of {_maxStreamingContracts}. Please adjust your subscription quantity or upgrade your plan accordingly. Current subscription count: {_subscribedSymbolCount}");
                    }
                }

                foreach (var jsonMessage in GetContractSubscriptionMessage(true, symbol))
                {
                    SendMessage(jsonMessage);
                }
            }

            return true;
        }

        private IEnumerable<string> GetContractSubscriptionMessage(bool isSubscribe, Symbol symbol)
        {
            var brokerageSymbol = _symbolMapper.GetBrokerageSymbol(symbol).Split(',');

            switch (symbol.SecurityType)
            {
                case SecurityType.Equity:
                    foreach (var channel in Channels)
                    {
                        yield return GetMessage(isSubscribe, channel, brokerageSymbol[0], symbol.SecurityType);
                    }
                    break;
                case SecurityType.Index:
                    yield return GetMessage(isSubscribe, "TRADE", brokerageSymbol[0], symbol.SecurityType);
                    break;
                case SecurityType.Option:
                case SecurityType.IndexOption:
                    foreach (var channel in Channels)
                    {
                        yield return GetMessageOption(isSubscribe, channel, brokerageSymbol[0], brokerageSymbol[1], brokerageSymbol[2], brokerageSymbol[3]);
                    }
                    break;
            }
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public bool Unsubscribe(IEnumerable<Symbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                lock (_lock)
                {
                    _subscribedSymbolCount--;
                }

                foreach (var jsonMessage in GetContractSubscriptionMessage(false, symbol))
                {
                    SendMessage(jsonMessage);
                }
            }
            return true;
        }

        /// <summary>
        /// Constructs a message for subscribing or unsubscribing to an option contract on a specified channel.
        /// </summary>
        /// <param name="isSubscribe">A boolean value indicating whether to subscribe (true) or unsubscribe (false).</param>
        /// <param name="channelName">The name of the channel to subscribe or unsubscribe from. <see cref="Channels"/></param>
        /// <param name="ticker">The ticker symbol of the financial instrument.</param>
        /// <param name="expirationDate">The expiration date of the option contract.</param>
        /// <param name="strikePrice">The strike price of the option contract.</param>
        /// <param name="optionRight">The option type, either "C" for call or "P" for put.</param>
        /// <returns>A JSON string representing the constructed message.</returns>
        private string GetMessageOption(bool isSubscribe, string channelName, string ticker, string expirationDate, string strikePrice, string optionRight)
        {
            return JsonConvert.SerializeObject(new
            {
                msg_type = "STREAM",
                sec_type = "OPTION",
                req_type = channelName,
                add = isSubscribe,
                id = _idRequestCount,
                contract = new
                {
                    root = ticker,
                    expiration = expirationDate,
                    strike = strikePrice,
                    right = optionRight
                }
            });
        }

        /// <summary>
        /// Constructs a message for subscribing or unsubscribing to a financial instrument on a specified channel.
        /// </summary>
        /// <param name="isSubscribe">A boolean value indicating whether to subscribe (true) or unsubscribe (false).</param>
        /// <param name="channelName">The name of the channel to subscribe or unsubscribe from. <see cref="Channels"/></param>
        /// <param name="ticker">The ticker symbol of the financial instrument.</param>
        /// <param name="securityType">The type of the security.</param>
        /// <returns>A JSON string representing the constructed message.</returns>
        /// <exception cref="NotSupportedException">Thrown when the security type is not supported.</exception>
        private string GetMessage(bool isSubscribe, string channelName, string ticker, SecurityType securityType)
        {
            var sec_type = securityType switch
            {
                SecurityType.Equity => "STOCK",
                SecurityType.Index => "INDEX",
                _ => throw new NotSupportedException($"{nameof(ThetaDataWebSocketClientWrapper)}.{nameof(GetMessage)}: Security type {securityType} is not supported.")
            };

            return JsonConvert.SerializeObject(new
            {
                msg_type = "STREAM",
                sec_type = sec_type,
                req_type = channelName,
                add = isSubscribe,
                id = _idRequestCount,
                contract = new { root = ticker }
            });
        }

        /// <summary>
        /// Event handler for processing WebSocket messages.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="webSocketMessage">The WebSocket message received.</param>
        private void OnMessage(object? sender, WebSocketMessage webSocketMessage)
        {
            var e = (TextMessage)webSocketMessage.Data;

            _messageHandler?.Invoke(e.Message);
        }

        /// <summary>
        /// Event handler for processing WebSocket close data.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="webSocketCloseData">The WebSocket Close Data received.</param>
        private void OnClosed(object? sender, WebSocketCloseData webSocketCloseData)
        {
            Log.Trace($"{nameof(ThetaDataWebSocketClientWrapper)}.{nameof(OnClosed)}: {webSocketCloseData.Reason}");
        }

        /// <summary>
        /// Wraps the send method to send a JSON message over the WebSocket connection
        /// and increments the request count.
        /// </summary>
        /// <param name="jsonMessage">The JSON message to be sent.</param>
        private void SendMessage(string jsonMessage)
        {
            Send(jsonMessage);
            Interlocked.Increment(ref _idRequestCount);
        }

        /// <summary>
        /// Sends a request to stop all previous streaming subscriptions.
        /// This is crucial to avoid any conflicts or unexpected behavior from previous sessions.
        /// For more details, refer to the official documentation:
        /// https://http-docs.thetadata.us/docs/theta-data-rest-api-v2/a017d29vrw1q0-stop-all-streams
        /// </summary>
        private void SendStopPreviousStreamingSubscriptions()
        {
            Log.Debug($"{nameof(ThetaDataWebSocketClientWrapper)}.{nameof(SendStopPreviousStreamingSubscriptions)}: Sending request to stop all previous streaming subscriptions to avoid conflicts and ensure clean state for new sessions.");
            Send(JsonConvert.SerializeObject(new { msg_type = "STOP" }));
        }
    }
}
