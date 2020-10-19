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

using QuantConnect.Brokerages.Binance.Messages;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using System;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json.Linq;
using QuantConnect.Data;

namespace QuantConnect.Brokerages.Binance
{
    public partial class BinanceBrokerage
    {
        private readonly ConcurrentQueue<WebSocketMessage> _messageBuffer = new ConcurrentQueue<WebSocketMessage>();
        private volatile bool _streamLocked;
        private readonly IDataAggregator _aggregator;

        /// <summary>
        /// Locking object for the Ticks list in the data queue handler
        /// </summary>
        protected readonly object TickLocker = new object();

        /// <summary>
        /// Lock the streaming processing while we're sending orders as sometimes they fill before the REST call returns.
        /// </summary>
        private void LockStream()
        {
            _streamLocked = true;
        }

        /// <summary>
        /// Unlock stream and process all backed up messages.
        /// </summary>
        private void UnlockStream()
        {
            while (_messageBuffer.Any())
            {
                WebSocketMessage e;
                _messageBuffer.TryDequeue(out e);

                OnMessageImpl(e);
            }

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

        private void OnMessageImpl(WebSocketMessage e)
        {
            try
            {
                var obj = JObject.Parse(e.Message);

                var objError = obj["error"];
                if (objError != null)
                {
                    var error = objError.ToObject<ErrorMessage>();
                    OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, error.Code, error.Message));
                    return;
                }

                var objData = obj;

                var objEventType = objData["e"];
                if (objEventType != null)
                {
                    var eventType = objEventType.ToObject<string>();

                    switch (eventType)
                    {
                        case "executionReport":
                            var upd = objData.ToObject<Execution>();
                            if (upd.ExecutionType.Equals("TRADE", StringComparison.OrdinalIgnoreCase))
                            {
                                OnFillOrder(upd);
                            }
                            break;

                        case "trade":
                            var trade = objData.ToObject<Trade>();
                            EmitTradeTick(
                                _symbolMapper.GetLeanSymbol(trade.Symbol),
                                Time.UnixMillisecondTimeStampToDateTime(trade.Time),
                                trade.Price,
                                trade.Quantity);
                            break;
                    }
                }
                else if (objData["u"] != null)
                {
                    var quote = objData.ToObject<BestBidAskQuote>();
                    EmitQuoteTick(
                        _symbolMapper.GetLeanSymbol(quote.Symbol),
                        quote.BestBidPrice,
                        quote.BestBidSize,
                        quote.BestAskPrice,
                        quote.BestAskSize);
                }
            }
            catch (Exception exception)
            {
                OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1, $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
                throw;
            }
        }

        private void EmitQuoteTick(Symbol symbol, decimal bidPrice, decimal bidSize, decimal askPrice, decimal askSize)
        {
            var tick = new Tick
            {
                AskPrice = askPrice,
                BidPrice = bidPrice,
                Time = DateTime.UtcNow,
                Symbol = symbol,
                TickType = TickType.Quote,
                AskSize = askSize,
                BidSize = bidSize
            };
            tick.SetValue();

            lock (TickLocker)
            {
                _aggregator.Update(tick);
            }
        }

        private void EmitTradeTick(Symbol symbol, DateTime time, decimal price, decimal quantity)
        {
            var tick = new Tick
            {
                Symbol = symbol,
                Value = price,
                Quantity = Math.Abs(quantity),
                Time = time,
                TickType = TickType.Trade
            };

            lock (TickLocker)
            {
                _aggregator.Update(tick);
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

                var fillPrice = data.LastExecutedPrice;
                var fillQuantity = data.Direction == OrderDirection.Sell ? -data.LastExecutedQuantity : data.LastExecutedQuantity;
                var updTime = Time.UnixMillisecondTimeStampToDateTime(data.TransactionTime);
                var orderFee = new OrderFee(new CashAmount(data.Fee, data.FeeCurrency));
                var status = ConvertOrderStatus(data.OrderStatus);
                var orderEvent = new OrderEvent
                (
                    order.Id, order.Symbol, updTime, status,
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
    }
}
