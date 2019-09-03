using System;
using System.Collections.Generic;
using QuantConnect.Brokerages;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

namespace QuantConnect.ToolBox.PolygonApi
{
    /// <summary>
    /// An implementation of <see cref="IDataQueueHandler"/> for CoinAPI
    /// </summary>
    public class PolygonApiDataQueueHandler : IDataQueueHandler, IDisposable
    {

        private const string WebSocketUrl = "wss://socket.polygon.io/stocks";

        private readonly string _apiKey = Config.Get("polygon-api-key");
        private readonly WebSocketWrapper _webSocket = new WebSocketWrapper();
        private readonly object _locker = new object();
        private readonly List<Tick> _ticks = new List<Tick>();
        private readonly DefaultConnectionHandler _connectionHandler = new DefaultConnectionHandler();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<BaseData> GetNextTicks()
        {
            throw new NotImplementedException();
        }

        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            throw new NotImplementedException();
        }
    }
}
