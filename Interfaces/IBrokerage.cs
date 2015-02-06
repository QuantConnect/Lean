using System;
using System.Collections.Generic;
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
        event EventHandler<OrderEvent> OrderEvent;

        /// <summary>
        /// Event that fires each time portfolio holdings have changed
        /// </summary>
        event EventHandler<PortfolioEvent> PortfolioChanged;

        /// <summary>
        /// Event that fires each time a user's brokerage account is changed
        /// </summary>
        event EventHandler<AccountEvent> AccountChanged;

        /// <summary>
        /// Event that fires when an error is encountered in the brokerage
        /// </summary>
        event EventHandler<Exception> Error;

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
        /// Gets the current USD cash balance in the brokerage account
        /// </summary>
        /// <returns>The current USD cash balance available for trading</returns>
        decimal GetCashBalance();

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
    }
}