/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.Binance.Messages;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;

namespace QuantConnect.Brokerages.Binance
{
    public partial class BinanceBrokerage
    {
        private readonly ConcurrentQueue<WebSocketMessage> _messageBuffer = new ConcurrentQueue<WebSocketMessage>();
        private volatile bool _streamLocked;
        private readonly ConcurrentDictionary<Symbol, BinanceOrderBook> _orderBooks = new ConcurrentDictionary<Symbol, BinanceOrderBook>();
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
                OnMessageImpl(this, e, (msg) =>
                {
                    switch (msg.Event)
                    {
                        case EventType.Execution:
                            OnUserMessageImpl(msg);
                            break;
                        case EventType.Trade:
                        case EventType.OrderBook:
                            OnStreamMessageImpl(msg);
                            break;
                        default:
                            break;
                    }
                });
            }
            Log.Trace("BinanceBrokerage.Messaging.UnlockStream(): Stream Unlocked.");
            // Once dequeued in order; unlock stream.
            _streamLocked = false;
        }

        private void WithLockedStream(Action code)
        {
            try
            {
                LockStream();
                code();
            }
            finally
            {
                UnlockStream();
            }
        }

        private void OnMessageImpl(object sender, WebSocketMessage e, Action<BaseMessage> handler)
        {
            try
            {
                var msg = Messages.BaseMessage.Parse(e.Message);
                if (msg != null)
                {
                    handler(msg);
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        private void OnUserMessageImpl(BaseMessage message)
        {
            switch (message.Event)
            {
                case EventType.Execution:
                    var upd = message.ToObject<Messages.Execution>();
                    if (upd.ExecutionType.Equals("TRADE", StringComparison.OrdinalIgnoreCase))
                    {
                        OnFillOrder(upd);
                    }
                    break;
                default:
                    return;
            }
        }

        private void OnStreamMessageImpl(BaseMessage message)
        {
            switch (message.Event)
            {
                case EventType.OrderBook:
                    var updates = message.ToObject<Messages.OrderBookUpdateMessage>();
                    OnOrderBookUpdate(updates);
                    break;
                case EventType.Trade:
                    var trade = message.ToObject<Messages.Trade>();
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

        private void OnOrderBookUpdate(OrderBookUpdateMessage ticker)
        {
            try
            {
                var symbol = _symbolMapper.GetLeanSymbol(ticker.Symbol);
                BinanceOrderBook orderBook = null;
                if (_orderBooks.ContainsKey(symbol))
                {
                    orderBook = _orderBooks[symbol] as BinanceOrderBook;
                }
                else
                {
                    orderBook = new BinanceOrderBook(symbol);
                    _orderBooks.AddOrUpdate(symbol, orderBook);
                }

                //take snapshot
                if (orderBook.LastUpdateId == 0)
                {
                    FetchOrderBookSnapshot(symbol, orderBook);
                }

                // check incoming events order
                // new event should start from (last_final + 1)
                if (ticker.FirstUpdate - orderBook.LastUpdateId > 1)
                {
                    orderBook.Reset();
                    return;
                }

                // ignore event from the past
                if (ticker.FinalUpdate < orderBook.LastUpdateId)
                {
                    return;
                }

                ProcessOrderBookEvents(orderBook, ticker.Bids, ticker.Asks);

                orderBook.LastUpdateId = ticker.FinalUpdate;
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private void OnBestBidAskUpdated(object sender, BestBidAskUpdatedEventArgs e)
        {
            EmitQuoteTick(e.Symbol, e.BestBidPrice, e.BestBidSize, e.BestAskPrice, e.BestAskSize);
        }

        private void EmitQuoteTick(Symbol symbol, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            lock (TickLocker)
            {
                Ticks.Add(new Tick
                {
                    AskPrice = askPrice,
                    BidPrice = bidPrice,
                    Value = (askPrice + bidPrice) / 2m,
                    Time = DateTime.UtcNow,
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

        private void OnFillOrder(Execution data)
        {
            try
            {
                var order = FindOrderByExternalId(data.OrderId);
                if (order == null)
                {
                    // not our order, nothing else to do here
                    return;
                }

                var symbol = _symbolMapper.GetLeanSymbol(data.Symbol);
                var fillPrice = data.LastExecutedPrice;
                var fillQuantity = data.LastExecutedQuantity;
                var updTime = Time.UnixMillisecondTimeStampToDateTime(data.TransactionTime);
                var orderFee = new OrderFee(new CashAmount(data.Fee, data.FeeCurrency));
                var status = ConvertOrderStatus(data.OrderStatus);
                var orderEvent = new OrderEvent
                (
                    order.Id, symbol, updTime, status,
                    data.Direction, fillPrice, fillQuantity,
                    orderFee, $"Binance Order Event {data.Direction}"
                );

                if (status == OrderStatus.Filled)
                {
                    Orders.Order outOrder;
                    CachedOrderIDs.TryRemove(order.Id, out outOrder);
                }

                OnOrderEvent(orderEvent);
            }
            catch (Exception e)
            {
                Log.Error(e);
                throw;
            }
        }

        private Orders.Order FindOrderByExternalId(string brokerId)
        {
            var order = CachedOrderIDs
                    .FirstOrDefault(o => o.Value.BrokerId.Contains(brokerId))
                    .Value;
            if (order == null)
            {
                order = _algorithm.Transactions.GetOrderByBrokerageId(brokerId);
            }

            return order;
        }

        private void FetchOrderBookSnapshot(Symbol symbol, BinanceOrderBook orderBook)
        {
            WithLockedStream(() =>
            {
                var snapshot = _apiClient.FetchOrderBookSnapshot(symbol);

                orderBook.BestBidAskUpdated -= OnBestBidAskUpdated;
                orderBook.LastUpdateId = snapshot.LastUpdateId;
                ProcessOrderBookEvents(orderBook, snapshot.Bids, snapshot.Asks);

                EmitQuoteTick(
                    symbol,
                    orderBook.BestBidPrice,
                    orderBook.BestBidSize,
                    orderBook.BestAskPrice,
                    orderBook.BestAskSize);
                orderBook.BestBidAskUpdated += OnBestBidAskUpdated;
            });
        }

        private void ProcessOrderBookEvents(DefaultOrderBook orderBook, object[][] bids, object[][] asks)
        {
            foreach (var item in bids)
            {
                var price = (item[0] as string).ToDecimal();
                var quantity = (item[1] as string).ToDecimal();
                if (quantity == 0)
                    orderBook.RemoveBidRow(price);
                else
                    orderBook.UpdateBidRow(price, quantity);
            }

            foreach (var item in asks)
            {
                var price = (item[0] as string).ToDecimal();
                var quantity = (item[1] as string).ToDecimal();
                if (quantity == 0)
                    orderBook.RemoveAskRow(price);
                else
                    orderBook.UpdateAskRow(price, quantity);
            }
        }
    }
}
