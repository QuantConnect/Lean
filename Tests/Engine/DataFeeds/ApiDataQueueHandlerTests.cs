using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine.DataFeeds.Queues;
using QuantConnect.Logging;
using QuantConnect.Packets;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Category("TravisExclude")]
    public class ApiDataQueueHandlerTests
    {
        private WebSocketServer _mockServer;
        private readonly string _liveDataUrl = Config.Get("live-data-url", "wss://www.quantconnect.com/api/v2/live/data");
        private readonly int _liveDataPort = Config.GetInt("live-data-port", 443);
        private ApiDataQueueHandler _dataQueueHandler;
        private List<Symbol> _symbols;
        private MockServerBehavior _mockServerBehavior;

        [TestFixtureSetUp]
        public void Setup()
        {
            _symbols = new List<Symbol>()
            {
                Symbol.Create("AAPL", SecurityType.Equity, Market.USA),
                Symbol.Create("AIG", SecurityType.Equity, Market.USA)
            };

            _mockServerBehavior = new MockServerBehavior();
            _dataQueueHandler = new ApiDataQueueHandler();
            var liveDataUri = new Uri(_liveDataUrl);
            var uriBuilder = new UriBuilder(liveDataUri);
            uriBuilder.Port = _liveDataPort;

            Task.Run(() =>
            {
                _mockServer = new WebSocketServer(uriBuilder.ToString());
                _mockServer.AddWebSocketService("/", () => _mockServerBehavior);
                
                Log.Trace("ApiDataQueueHandlerTests.Setup(): Starting the mock server.");
                _mockServer.Start();

                while (true)
                {
                    Thread.Sleep(1000);
                }
            });
        }

        [Test]
        public void ApiDataQueueHandler_CanSubscribeToSymbols_Successfully()
        {
            _dataQueueHandler.Subscribe(new LiveNodePacket(), _symbols);
            
            // Give ApiDataQueueHandler at least one second to open the connection
            Thread.Sleep(1000);

            Assert.IsTrue(_mockServerBehavior.Subscriptions.Count == _symbols.Count);
            foreach (Symbol sym in _symbols)
            {
                Assert.IsTrue(_mockServerBehavior.Subscriptions.Contains(sym));
            }
        }

        [Test]
        public void ApiDataQueueHandler_CanUnubscribeToSymbols_Successfully()
        {
            ApiDataQueueHandler_CanSubscribeToSymbols_Successfully();
            _dataQueueHandler.Unsubscribe(new LiveNodePacket(), _symbols);

            Thread.Sleep(2000);

            Assert.IsTrue(_mockServerBehavior.Subscriptions.Count == 0);
        }

        [Test]
        public void DataQueueHandler_CanChangeSubscription_Successfully()
        {
            ApiDataQueueHandler_CanSubscribeToSymbols_Successfully();
            var newSymbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            _dataQueueHandler.Subscribe(new LiveNodePacket(), new List<Symbol>() { newSymbol });

            Thread.Sleep(2000);

            Assert.IsTrue(_mockServerBehavior.Subscriptions.Count == _symbols.Count + 1);
            foreach (Symbol sym in _symbols)
            {
                Assert.IsTrue(_mockServerBehavior.Subscriptions.Contains(sym));
            }
            Assert.IsTrue(_mockServerBehavior.Subscriptions.Contains(newSymbol));
        }
    }

    /// <summary>
    /// Mock behavior for server
    /// </summary>
    public class MockServerBehavior : WebSocketBehavior
    {
        /// <summary>
        /// List of Symbols that the client is subscribed to
        /// </summary>
        public readonly List<Symbol> Subscriptions;

        /// <summary>
        /// Default constructor
        /// </summary>
        public MockServerBehavior()
        {
            Subscriptions = new List<Symbol>();
        }
        /// <summary>
        /// Behavior that is invoked when a message is received
        /// </summary>
        protected override void OnMessage(MessageEventArgs s)
        {
            var subscriptionRequest = JsonConvert.DeserializeObject<List<Symbol>>(s.Data);

            // unsubscribe from everything
            Subscriptions.Clear();

            // subscribe
            foreach (var symbol in subscriptionRequest)
            {
                Subscriptions.Add(symbol);
            }
        }
    }
}
