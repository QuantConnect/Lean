
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

using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// In this algorithm we submit/update/cancel each order type
    /// </summary>
    /// <meta name="tag" content="trading and orders" />
    /// <meta name="tag" content="placing orders" />
    /// <meta name="tag" content="managing orders" />
    /// <meta name="tag" content="order tickets" />
    /// <meta name="tag" content="updating orders" />
    public class OrderTicketDemoAlgorithm : QCAlgorithm
    {
        private const string symbol = "SPY";
        private readonly List<OrderTicket> _openMarketOnOpenOrders = new List<OrderTicket>();
        private readonly List<OrderTicket> _openMarketOnCloseOrders = new List<OrderTicket>();
        private readonly List<OrderTicket> _openLimitOrders = new List<OrderTicket>();
        private readonly List<OrderTicket> _openStopMarketOrders = new List<OrderTicket>();
        private readonly List<OrderTicket> _openStopLimitOrders = new List<OrderTicket>();

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 7);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, symbol, Resolution.Minute);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            // MARKET ORDERS

            MarketOrders();

            // LIMIT ORDERS

            LimitOrders();

            // STOP MARKET ORDERS

            StopMarketOrders();

            // STOP LIMIT ORDERS

            StopLimitOrders();

            // MARKET ON OPEN ORDERS

            MarketOnOpenOrders();

            // MARKET ON CLOSE ORDERS

            MarketOnCloseOrders();
        }

        /// <summary>
        /// MarketOrders are the only orders that are processed synchronously by default, so
        /// they'll fill by the next line of code. This behavior equally applies to live mode.
        /// You can opt out of this behavior by specifying the 'asynchronous' parameter as true.
        /// </summary>
        private void MarketOrders()
        {
            if (TimeIs(7, 9, 31))
            {
                Log("Submitting MarketOrder");

                // submit a market order to buy 10 shares, this function returns an OrderTicket object
                // we submit the order with asynchronous:false, so it block until it is filled
                var newTicket = MarketOrder(symbol, 10, asynchronous: false);
                if (newTicket.Status != OrderStatus.Filled)
                {
                    Log("Synchronous market order was not filled synchronously!");
                    Quit();
                }

                // we can also submit the ticket asynchronously. In a backtest, we'll still perform
                // the fill before the next time events for your algorithm. here we'll submit the order
                // asynchronously and try to cancel it, sometimes it will, sometimes it will be filled
                // first.
                newTicket = MarketOrder(symbol, 10, asynchronous: true);
                var response = newTicket.Cancel("Attempt to cancel async order");
                if (response.IsSuccess)
                {
                    Log("Successfully canceled async market order: " + newTicket.OrderId);
                }
                else
                {
                    Log("Unable to cancel async market order: " + response.ErrorCode);
                }
            }
        }

        /// <summary>
        /// LimitOrders are always processed asynchronously. Limit orders are used to
        /// set 'good' entry points for an order. For example, you may wish to go
        /// long a stock, but want a good price, so can place a LimitOrder to buy with
        /// a limit price below the current market price. Likewise the opposite is true
        /// when selling, you can place a LimitOrder to sell with a limit price above the
        /// current market price to get a better sale price.
        /// You can submit requests to update or cancel the LimitOrder at any time.
        /// The 'LimitPrice' for an order can be retrieved from the ticket using the
        /// OrderTicket.Get(OrderField) method, for example:
        /// <code>
        /// var currentLimitPrice = orderTicket.Get(OrderField.LimitPrice);
        /// </code>
        /// </summary>
        private void LimitOrders()
        {
            if (TimeIs(7, 12, 0))
            {
                Log("Submitting LimitOrder");

                // submit a limit order to buy 10 shares at .1% below the bar's close
                var close = Securities[symbol].Close;
                var newTicket = LimitOrder(symbol, 10, close * .999m);
                _openLimitOrders.Add(newTicket);

                // submit another limit order to sell 10 shares at .1% above the bar's close
                newTicket = LimitOrder(symbol, -10, close * 1.001m);
                _openLimitOrders.Add(newTicket);
            }

            // when we submitted new limit orders we placed them into this list,
            // so while there's two entries they're still open and need processing
            if (_openLimitOrders.Count == 2)
            {
                var openOrders = _openLimitOrders;

                // check if either is filled and cancel the other
                var longOrder = openOrders[0];
                var shortOrder = openOrders[1];
                if (CheckPairOrdersForFills(longOrder, shortOrder))
                {
                    _openLimitOrders.Clear();
                    return;
                }

                // if niether order has filled, bring in the limits by a penny

                var newLongLimit = longOrder.Get(OrderField.LimitPrice) + 0.01m;
                var newShortLimit = shortOrder.Get(OrderField.LimitPrice) - 0.01m;
                Log($"Updating limits - Long: {newLongLimit.ToStringInvariant("0.00")} Short: {newShortLimit.ToStringInvariant("0.00")}");

                longOrder.Update(new UpdateOrderFields
                {
                    // we could change the quantity, but need to specify it
                    //Quantity =
                    LimitPrice = newLongLimit,
                    Tag = "Update #" + (longOrder.UpdateRequests.Count + 1)
                });
                shortOrder.Update(new UpdateOrderFields
                {
                    LimitPrice = newShortLimit,
                    Tag = "Update #" + (shortOrder.UpdateRequests.Count + 1)
                });
            }
        }

        /// <summary>
        /// StopMarketOrders work in the opposite way that limit orders do.
        /// When placing a long trade, the stop price must be above current
        /// market price. In this way it's a 'stop loss' for a short trade.
        /// When placing a short trade, the stop price must be below current
        /// market price. In this way it's a 'stop loss' for a long trade.
        /// You can submit requests to update or cancel the StopMarketOrder at any time.
        /// The 'StopPrice' for an order can be retrieved from the ticket using the
        /// OrderTicket.Get(OrderField) method, for example:
        /// <code>
        /// var currentStopPrice = orderTicket.Get(OrderField.StopPrice);
        /// </code>
        /// </summary>
        private void StopMarketOrders()
        {
            if (TimeIs(7, 12 + 4, 0))
            {
                Log("Submitting StopMarketOrder");

                // a long stop is triggered when the price rises above the value
                // so we'll set a long stop .25% above the current bar's close

                var close = Securities[symbol].Close;
                var stopPrice = close * 1.0025m;
                var newTicket = StopMarketOrder(symbol, 10, stopPrice);
                _openStopMarketOrders.Add(newTicket);

                // a short stop is triggered when the price falls below the value
                // so we'll set a short stop .25% below the current bar's close

                stopPrice = close * .9975m;
                newTicket = StopMarketOrder(symbol, -10, stopPrice);
                _openStopMarketOrders.Add(newTicket);
            }

            // when we submitted new stop market orders we placed them into this list,
            // so while there's two entries they're still open and need processing
            if (_openStopMarketOrders.Count == 2)
            {
                // check if either is filled and cancel the other
                var longOrder = _openStopMarketOrders[0];
                var shortOrder = _openStopMarketOrders[1];
                if (CheckPairOrdersForFills(longOrder, shortOrder))
                {
                    _openStopMarketOrders.Clear();
                    return;
                }

                // if niether order has filled, bring in the stops by a penny

                var newLongStop = longOrder.Get(OrderField.StopPrice) - 0.01m;
                var newShortStop = shortOrder.Get(OrderField.StopPrice) + 0.01m;
                Log($"Updating stops - Long: {newLongStop.ToStringInvariant("0.00")} Short: {newShortStop.ToStringInvariant("0.00")}");

                longOrder.Update(new UpdateOrderFields
                {
                    // we could change the quantity, but need to specify it
                    //Quantity =
                    StopPrice = newLongStop,
                    Tag = "Update #" + (longOrder.UpdateRequests.Count + 1)
                });
                shortOrder.Update(new UpdateOrderFields
                {
                    StopPrice = newShortStop,
                    Tag = "Update #" + (shortOrder.UpdateRequests.Count + 1)
                });
            }
        }

        /// <summary>
        /// StopLimitOrders work as a combined stop and limit order. First, the
        /// price must pass the stop price in the same way a StopMarketOrder works,
        /// but then we're also gauranteed a fill price at least as good as the
        /// limit price. This order type can be beneficial in gap down scenarios
        /// where a StopMarketOrder would have triggered and given the not as beneficial
        /// gapped down price, whereas the StopLimitOrder could protect you from
        /// getting the gapped down price through prudent placement of the limit price.
        /// You can submit requests to update or cancel the StopLimitOrder at any time.
        /// The 'StopPrice' or 'LimitPrice' for an order can be retrieved from the ticket
        /// using the OrderTicket.Get(OrderField) method, for example:
        /// <code>
        /// var currentStopPrice = orderTicket.Get(OrderField.StopPrice);
        /// var currentLimitPrice = orderTicket.Get(OrderField.LimitPrice);
        /// </code>
        /// </summary>
        private void StopLimitOrders()
        {
            if (TimeIs(8, 12, 1))
            {
                Log("Submitting StopLimitOrder");

                // a long stop is triggered when the price rises above the value
                // so we'll set a long stop .25% above the current bar's close
                // now we'll also be setting a limit, this means we are gauranteed
                // to get at least the limit price for our fills, so make the limit
                // price a little softer than the stop price

                var close = Securities[symbol].Close;
                var stopPrice = close * 1.001m;
                var limitPrice = close - 0.03m;
                var newTicket = StopLimitOrder(symbol, 10, stopPrice, limitPrice);
                _openStopLimitOrders.Add(newTicket);

                // a short stop is triggered when the price falls below the value
                // so we'll set a short stop .25% below the current bar's close
                // now we'll also be setting a limit, this means we are gauranteed
                // to get at least the limit price for our fills, so make the limit
                // price a little softer than the stop price

                stopPrice = close * .999m;
                limitPrice = close + 0.03m;
                newTicket = StopLimitOrder(symbol, -10, stopPrice, limitPrice);
                _openStopLimitOrders.Add(newTicket);
            }

            // when we submitted new stop limit orders we placed them into this list,
            // so while there's two entries they're still open and need processing
            if (_openStopLimitOrders.Count == 2)
            {
                // check if either is filled and cancel the other
                var longOrder = _openStopLimitOrders[0];
                var shortOrder = _openStopLimitOrders[1];
                if (CheckPairOrdersForFills(longOrder, shortOrder))
                {
                    _openStopLimitOrders.Clear();
                    return;
                }

                // if niether order has filled, bring in the stops/limits in by a penny

                var newLongStop = longOrder.Get(OrderField.StopPrice) - 0.01m;
                var newLongLimit = longOrder.Get(OrderField.LimitPrice) + 0.01m;
                var newShortStop = shortOrder.Get(OrderField.StopPrice) + 0.01m;
                var newShortLimit = shortOrder.Get(OrderField.LimitPrice) - 0.01m;
                Log($"Updating stops  - Long: {newLongStop.ToStringInvariant("0.00")} Short: {newShortStop.ToStringInvariant("0.00")}");
                Log($"Updating limits - Long: {newLongLimit.ToStringInvariant("0.00")} Short: {newShortLimit.ToStringInvariant("0.00")}");

                longOrder.Update(new UpdateOrderFields
                {
                    // we could change the quantity, but need to specify it
                    //Quantity =
                    StopPrice = newLongStop,
                    LimitPrice = newLongLimit,
                    Tag = "Update #" + (longOrder.UpdateRequests.Count + 1)
                });
                shortOrder.Update(new UpdateOrderFields
                {
                    StopPrice = newShortStop,
                    LimitPrice = newShortLimit,
                    Tag = "Update #" + (shortOrder.UpdateRequests.Count + 1)
                });
            }
        }

        /// <summary>
        /// MarketOnCloseOrders are always executed at the next market's closing
        /// price. The only properties that can be updated are the quantity and
        /// order tag properties.
        /// </summary>
        private void MarketOnCloseOrders()
        {
            if (TimeIs(9, 12, 0))
            {
                Log("Submitting MarketOnCloseOrder");

                // open a new position or triple our existing position
                var qty = Portfolio[symbol].Quantity;
                qty = qty == 0 ? 100 : 2*qty;

                var newTicket = MarketOnCloseOrder(symbol, qty);
                _openMarketOnCloseOrders.Add(newTicket);
            }

            if (_openMarketOnCloseOrders.Count == 1 && Time.Minute == 59)
            {
                var ticket = _openMarketOnCloseOrders[0];
                // check for fills
                if (ticket.Status == OrderStatus.Filled)
                {
                    _openMarketOnCloseOrders.Clear();
                    return;
                }

                var quantity = ticket.Quantity + 1;
                Log("Updating quantity  - New Quantity: " + quantity);

                // we can update the quantity and tag
                ticket.Update(new UpdateOrderFields
                {
                    Quantity = quantity,
                    Tag = "Update #" + (ticket.UpdateRequests.Count + 1)
                });
            }

            if (TimeIs(EndDate.Day, 12 + 3, 45))
            {
                Log("Submitting MarketOnCloseOrder to liquidate end of algorithm");

                MarketOnCloseOrder(symbol, -Portfolio[symbol].Quantity, "Liquidate end of algorithm");
            }
        }

        /// <summary>
        /// MarketOnOpenOrders are always executed at the next market's opening
        /// price. The only properties that can be updated are the quantity and
        /// order tag properties.
        /// </summary>
        private void MarketOnOpenOrders()
        {
            if (TimeIs(8, 12 + 2, 0))
            {
                Log("Submitting MarketOnOpenOrder");

                // its EOD, let's submit a market on open order to short even more!
                var newTicket = MarketOnOpenOrder(symbol, 50);
                _openMarketOnOpenOrders.Add(newTicket);
            }

            if (_openMarketOnOpenOrders.Count == 1 && Time.Minute == 59)
            {
                var ticket = _openMarketOnOpenOrders[0];

                // check for fills
                if (ticket.Status == OrderStatus.Filled)
                {
                    _openMarketOnOpenOrders.Clear();
                    return;
                }

                var quantity = ticket.Quantity + 1;
                Log("Updating quantity  - New Quantity: " + quantity);

                // we can update the quantity and tag
                ticket.Update(new UpdateOrderFields
                {
                    Quantity = quantity,
                    Tag = "Update #" + (ticket.UpdateRequests.Count + 1)
                });
            }

        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            var order = Transactions.GetOrderById(orderEvent.OrderId);
            Console.WriteLine("{0}: {1}: {2}", Time, order.Type, orderEvent);
        }

        private bool CheckPairOrdersForFills(OrderTicket longOrder, OrderTicket shortOrder)
        {
            if (longOrder.Status == OrderStatus.Filled)
            {
                Log(shortOrder.OrderType + ": Cancelling short order, long order is filled.");
                shortOrder.Cancel("Long filled.");
                return true;
            }
            if (shortOrder.Status == OrderStatus.Filled)
            {
                Log(longOrder.OrderType + ": Cancelling long order, short order is filled.");
                longOrder.Cancel("Short filled");
                return true;
            }
            return false;
        }

        private bool TimeIs(int day, int hour, int minute)
        {
            return Time.Day == day && Time.Hour == hour && Time.Minute == minute;
        }
    }
}