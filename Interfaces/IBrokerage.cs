using System;
using System.Collections.Generic;
using QuantConnect.Brokerages;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Brokerage interface that defines the operations all brokerages must implement. The IBrokerage implementation
    /// must have a matching IBrokerageFactory implementation.
    /// </summary>
    public interface IBrokerage
    {
        /// <summary>
        /// Event that fires each time an order is filled
        /// </summary>
        event EventHandler<OrderEvent> OrderStatusChanged;

        /// <summary>
        /// Event that fires each time portfolio holdings have changed
        /// </summary>
        event EventHandler<SecurityEvent> SecurityHoldingUpdated;

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
        List<Cash> GetCashBalance();

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
        /// Determines whether or not this brokerage can process the specified order.
        /// </summary>
        /// <param name="order">The order to check</param>
        /// <returns>True if this brokerage implementation can process the specified order, false otherwise</returns>
        bool CanProcessOrder(Order order);

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        void Disconnect();
    }
}