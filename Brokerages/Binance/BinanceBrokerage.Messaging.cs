using Newtonsoft.Json.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Binance
{
    public partial class BinanceBrokerage
    {
        private readonly ConcurrentQueue<WebSocketMessage> _messageBuffer = new ConcurrentQueue<WebSocketMessage>();
        private volatile bool _streamLocked;
        /// <summary>
        /// Locking object for the Ticks list in the data queue handler
        /// </summary>
        protected readonly object TickLocker = new object();

        /// <summary>
        /// Lock the streaming processing while we're sending orders as sometimes they fill before the REST call returns.
        /// </summary>
        private void LockStream()
        {
            Log.Trace("BinanceBrokerage.Messaging.LockStream(): Locking Stream");
            _streamLocked = true;
        }

        /// <summary>
        /// Unlock stream and process all backed up messages.
        /// </summary>
        private void UnlockStream()
        {
            Log.Trace("BinanceBrokerage.Messaging.UnlockStream(): Processing Backlog...");
            while (_messageBuffer.Any())
            {
                WebSocketMessage e;
                _messageBuffer.TryDequeue(out e);
                OnMessageImpl(this, e);
            }
            Log.Trace("BinanceBrokerage.Messaging.UnlockStream(): Stream Unlocked.");
            // Once dequeued in order; unlock stream.
            _streamLocked = false;
        }

        private void OnMessageImpl(object sender, WebSocketMessage e)
        {
            try
            {
                var token = JToken.Parse(e.Message);
                if (!(token is JObject))
                    throw new Exception("Invalid message format was recieved");

                switch (token.ToObject<Messages.BaseMessage>().Event)
                {
                    case "24hrTicker":
                        var ticker = token.ToObject<Messages.SymbolTicker>();
                        EmitQuoteTick(
                            _symbolMapper.GetLeanSymbol(ticker.Symbol),
                            Time.UnixMillisecondTimeStampToDateTime(ticker.Time),
                            ticker.BestBidPrice,
                            ticker.BestBidSize,
                            ticker.BestAskPrice,
                            ticker.BestAskSize
                        );
                        break;
                    case "trade":
                        var trade = token.ToObject<Messages.Trade>();
                        EmitTradeTick(
                            _symbolMapper.GetLeanSymbol(trade.Symbol),
                            Time.UnixMillisecondTimeStampToDateTime(trade.Time),
                            trade.Price,
                            trade.Quantity
                        );
                        break;
                    default:
                        return;
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        private void EmitQuoteTick(Symbol symbol, DateTime time, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            lock (TickLocker)
            {
                Ticks.Add(new Tick
                {
                    AskPrice = askPrice,
                    BidPrice = bidPrice,
                    Value = (askPrice + bidPrice) / 2m,
                    Time = time,
                    Symbol = symbol,
                    TickType = TickType.Quote,
                    AskSize = askSize,
                    BidSize = bidSize
                });
            }
        }

        private void EmitTradeTick(Symbol symbol, DateTime time, decimal price, decimal quantity)
        {
            lock (TickLocker)
            {
                Ticks.Add(new Tick
                {
                    Symbol = symbol,
                    Value = price,
                    Quantity = Math.Abs(quantity),
                    Time = time,
                    TickType = TickType.Trade
                });
            }
        }
    }
}
