using Newtonsoft.Json;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Bitfinex WebSockets integration
    /// </summary>
    public partial class BitfinexWebsocketsBrokerage : BitfinexBrokerage, IDataQueueHandler, IDisposable
    {

        #region Declarations
        WebSocket _ws = new WebSocket("wss://api2.bitfinex.com:3000/ws");
        List<Securities.Cash> _cash = new List<Securities.Cash>();
        Dictionary<int, string> _channelId = new Dictionary<int, string>();
        Task _checkConnectionTask = null;
        CancellationTokenSource _checkConnectionToken = new CancellationTokenSource();
        DateTime _heartbeatCounter = DateTime.UtcNow;
        const int _heartBeatTimeout = 30;
        #endregion

        /// <summary>
        /// Create Brokerage instance
        /// </summary>
        public BitfinexWebsocketsBrokerage()
            : base()
        {
        }

        /// <summary>
        /// Add subscription to Websockets service
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        //todo: support other currency. Use symbol supplied here
        public override void Subscribe(Packets.LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            if (!this.IsConnected)
            {
                this.Connect();
            }

            _ws.Send(JsonConvert.SerializeObject(new
            {
                @event = "subscribe",
                channel = "ticker",
                pair = this.symbol.Value
            }));

        }

        /// <summary>
        /// Remove subscription from Websockets service
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public override void Unsubscribe(Packets.LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var id in _channelId)
            {
                Unsubscribe(id.Key);
            }
        }

        private void Unsubscribe(int id)
        {
            _ws.Send(JsonConvert.SerializeObject(new
            {
                @event = "unsubscribe",
                channelId = id,
            }));
        }

        /// <summary>
        /// Returns if wss is connected
        /// </summary>
        public override bool IsConnected
        {
            get { return _ws.IsAlive; }
        }

        /// <summary>
        /// Creates wss connection
        /// </summary>
        public override void Connect()
        {
            _ws.Connect();
            if (this._checkConnectionTask == null || this._checkConnectionTask.IsFaulted)
            {
                this._checkConnectionTask = Task.Run(() => CheckConnection());
            }
            this._channelId.Clear();
            _ws.OnMessage += OnMessage;
            this.Authenticate();
        }

        /// <summary>
        /// Logs out and closes connection
        /// </summary>
        public override void Disconnect()
        {
            this.UnAuthenticate();
            this._ws.Close();
        }

        /// <summary>
        /// Ensures any wss connection or authentication is closed
        /// </summary>
        public void Dispose()
        {
            _checkConnectionToken.Cancel();
            this.Unsubscribe(null, null);
            this.Disconnect();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task CheckConnection()
        {
            while (!_checkConnectionToken.Token.IsCancellationRequested)
            {
                if (!this.IsConnected || (DateTime.UtcNow - _heartbeatCounter).TotalSeconds > _heartBeatTimeout)
                {
                    Log.Trace("Heartbeat timeout. Reconnecting");
                    Reconnect();
                }
                await Task.Delay(TimeSpan.FromSeconds(5), _checkConnectionToken.Token);
            }
        }

        private void Reconnect()
        {
            _ws.Connect();
            this.Authenticate();
            this.Subscribe(null,null);
        }

    }
}
