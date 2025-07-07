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
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Brokerage interface that defines the operations all brokerages must implement. The IBrokerage implementation
    /// must have a matching IBrokerageFactory implementation.
    /// </summary>
    public interface IBrokerage : IBrokerageCashSynchronizer, IDisposable
    {
        /// <summary>
        /// Event that fires each time the brokerage order id changes
        /// </summary>
        event EventHandler<BrokerageOrderIdChangedEvent> OrderIdChanged;

        /// <summary>
        /// Event that fires each time the status for a list of orders change
        /// </summary>
        event EventHandler<List<OrderEvent>> OrdersStatusChanged;

        /// <summary>
        /// Event that fires each time an order is updated in the brokerage side
        /// </summary>
        /// <remarks>
        /// These are not status changes but mainly price changes, like the stop price of a trailing stop order
        /// </remarks>
        event EventHandler<OrderUpdateEvent> OrderUpdated;

        /// <summary>
        /// Event that fires each time a short option position is assigned
        /// </summary>
        event EventHandler<OrderEvent> OptionPositionAssigned;

        /// <summary>
        /// Event that fires each time an option position has changed
        /// </summary>
        event EventHandler<OptionNotificationEventArgs> OptionNotification;

        /// <summary>
        /// Event that fires each time there's a brokerage side generated order
        /// </summary>
        event EventHandler<NewBrokerageOrderNotificationEventArgs> NewBrokerageOrderNotification;

        /// <summary>
        /// Event that fires each time a delisting occurs
        /// </summary>
        /// <remarks>TODO: Wire brokerages to call this event to process delistings</remarks>
        event EventHandler<DelistingNotificationEventArgs> DelistingNotification;

        /// <summary>
        /// Event that fires each time a user's brokerage account is changed
        /// </summary>
        event EventHandler<AccountEvent> AccountChanged;

        /// <summary>
        /// Event that fires when a message is received from the brokerage
        /// </summary>
        event EventHandler<BrokerageMessageEvent> Message;

        /// <summary>
        /// Gets the name of the brokerage
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets all open orders on the account
        /// </summary>
        /// <returns>The open orders returned from IB</returns>
        List<Order> GetOpenOrders();

        /// <summary>
        /// Gets all holdings for the account
        /// </summary>
        /// <returns>The current holdings from the account</returns>
        List<Holding> GetAccountHoldings();

        /// <summary>
        /// Gets the current cash balance for each currency held in the brokerage account
        /// </summary>
        /// <returns>The current cash balance for each currency available for trading</returns>
        List<CashAmount> GetCashBalance();

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        bool PlaceOrder(Order order);

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        bool UpdateOrder(Order order);

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        bool CancelOrder(Order order);

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Specifies whether the brokerage will instantly update account balances
        /// </summary>
        bool AccountInstantlyUpdated { get; }

        /// <summary>
        /// Returns the brokerage account's base currency
        /// </summary>
        string AccountBaseCurrency { get; }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of bars covering the span specified in the request</returns>
        IEnumerable<BaseData> GetHistory(HistoryRequest request);

        /// <summary>
        /// Enables or disables concurrent processing of messages to and from the brokerage.
        /// </summary>
        bool ConcurrencyEnabled { get; set; }
    }
}
