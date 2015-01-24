using System;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Defines an error handler callback function that is used to process errors returned from
    /// the brokerage's server
    /// </summary>
    /// <param name="key">The error code, or key, represened as a string.</param>
    /// <param name="message">The callback to execute upon encountering the error</param>
    public delegate void ErrorHandlerCallback(string key, string message);

    /// <summary>
    /// Brokerage interface that defines the operations all brokerages must implement.
    /// </summary>
    public interface IBrokerage
    {
        /// <summary>
        /// Event that fires each time an order is filled
        /// </summary>
        event EventHandler<OrderEvent> OrderFilled;

        /// <summary>
        /// Event that fires each time portfolio holdings have changed
        /// </summary>
        event EventHandler<PortfolioEvent> PortfolioChanged;

        /// <summary>
        /// Event that fires each time a user's brokerage account is changed
        /// </summary>
        event EventHandler<AccountEvent> AccountChanged;

        /// <summary>
        /// Gets the name of the brokerage
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Defines a handler for a specific type of error. The key here is a string representation
        /// of the error code sent back from the brokerage.
        /// </summary>
        /// <param name="callback">The callback to execute upon encountering the error</param>
        void AddErrorHander(ErrorHandlerCallback callback);

        /// <summary>
        /// Places a new order
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