using Newtonsoft.Json;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Packets;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace QuantConnect.Brokerages
{
    public abstract class BaseWebsocketsBrokerage : Brokerage
    {

        #region Declarations
        public List<Tick> Ticks = new List<Tick>();
        protected IWebSocket WebSocket;
        protected IRestClient RestClient;
        protected JsonSerializerSettings JsonSettings = new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal };
        public ConcurrentDictionary<int, Orders.Order> CachedOrderIDs = new ConcurrentDictionary<int, Orders.Order>();
        protected Dictionary<string, Channel> ChannelList = new Dictionary<string, Channel>();
        private string _market { get; set; }
        protected string ApiSecret;
        protected string ApiKey;

        protected DateTime LastHeartbeatUtcTime = DateTime.UtcNow;
        const int _heartbeatTimeout = 300;
        Thread _connectionMonitorThread;
        CancellationTokenSource _cancellationTokenSource;
        private readonly object _lockerConnectionMonitor = new object();
        private volatile bool _connectionLost;
        #endregion

        public BaseWebsocketsBrokerage(string wssUrl, IWebSocket websocket, IRestClient restClient, string apiKey, string apiSecret, string market, string name) : base(name)
        {
            WebSocket = websocket;
            WebSocket.Initialize(wssUrl);
            RestClient = restClient;
            _market = market;
            ApiSecret = apiSecret;
            ApiKey = apiKey;
        }

        public abstract void OnMessage(object sender, MessageEventArgs e);

        /// <summary>
        /// Creates wss connection
        /// </summary>
        public override void Connect()
        {
            WebSocket.OnMessage += OnMessage;
            WebSocket.OnError += OnError;

            WebSocket.Connect();
            _cancellationTokenSource = new CancellationTokenSource();
            _connectionMonitorThread = new Thread(() =>
            {
                var nextReconnectionAttemptUtcTime = DateTime.UtcNow;
                double nextReconnectionAttemptSeconds = 1;

                lock (_lockerConnectionMonitor)
                {
                    LastHeartbeatUtcTime = DateTime.UtcNow;
                }

                try
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {

                        TimeSpan elapsed;
                        lock (_lockerConnectionMonitor)
                        {
                            elapsed = DateTime.UtcNow - LastHeartbeatUtcTime;
                        }

                        if (!_connectionLost && elapsed > TimeSpan.FromSeconds(_heartbeatTimeout))
                        {
                            _connectionLost = true;
                            nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);

                            OnMessage(BrokerageMessageEvent.Disconnected("Connection with server lost. " +
                                                                         "This could be because of internet connectivity issues. "));
                        }
                        else if (_connectionLost)
                        {
                            try
                            {
                                if (elapsed <= TimeSpan.FromSeconds(_heartbeatTimeout))
                                {
                                    _connectionLost = false;
                                    nextReconnectionAttemptSeconds = 1;

                                    OnMessage(BrokerageMessageEvent.Reconnected("Connection with server restored."));
                                }
                                else
                                {
                                    if (DateTime.UtcNow > nextReconnectionAttemptUtcTime)
                                    {
                                        try
                                        {
                                            Reconnect();
                                        }
                                        catch (Exception)
                                        {
                                            // double the interval between attempts (capped to 1 minute)
                                            nextReconnectionAttemptSeconds = Math.Min(nextReconnectionAttemptSeconds * 2, 60);
                                            nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);
                                        }
                                    }
                                }
                            }
                            catch (Exception exception)
                            {
                                Log.Error(exception);
                            }
                        }

                        Thread.Sleep(10000);
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                }
            });
            _connectionMonitorThread.Start();
            while (!_connectionMonitorThread.IsAlive)
            {
                Thread.Sleep(1);
            }
        }

        public void OnError(object sender, ErrorEventArgs e)
        {
            Log.Debug(e.Message);
        }

        protected virtual void Reconnect()
        {
            var subscribed = GetSubscribed();

            WebSocket.OnError -= this.OnError;
            try
            {
                //try to clean up state
                if (IsConnected)
                {
                    WebSocket.Close();
                }
                if (!IsConnected)
                {
                    WebSocket.Connect();
                }
            }
            finally
            {
                WebSocket.OnError += this.OnError;
                this.Subscribe(null, subscribed);
            }
        }

        public abstract void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols);

        protected virtual IList<Symbol> GetSubscribed()
        {
            IList<Symbol> list = new List<Symbol>();
            lock (ChannelList)
            {
                foreach (var item in ChannelList)
                {
                    list.Add(Symbol.Create(item.Value.Symbol, SecurityType.Forex, _market));
                }
            }
            return list;
        }

        protected class Channel
        {
            public string Name { get; set; }
            public string Symbol { get; set; }
        }

    }

}
