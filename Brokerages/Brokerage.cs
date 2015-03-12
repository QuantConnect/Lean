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
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Represents the base Brokerage implementation. This provides logging on brokerage events.
    /// </summary>
    public abstract class Brokerage : IBrokerage
    {
        /// <summary>
        /// Event that fires each time an order is filled
        /// </summary>
        public event EventHandler<OrderEvent> OrderEvent;

        /// <summary>
        /// Event that fires each time portfolio holdings have changed
        /// </summary>
        public event EventHandler<PortfolioEvent> PortfolioChanged;

        /// <summary>
        /// Event that fires each time a user's brokerage account is changed
        /// </summary>
        public event EventHandler<AccountEvent> AccountChanged;

        /// <summary>
        /// Event that fires when an error is encountered in the brokerage
        /// </summary>
        public event EventHandler<Exception> Error;

        /// <summary>
        /// Gets the name of the brokerage
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns true if we're currently connected to the broker
        /// </summary>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// Creates a new Brokerage instance with the specified name
        /// </summary>
        /// <param name="name">The name of the brokerage</param>
        protected Brokerage(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Places a new order and assigns a new broker ID to the order
        /// </summary>
        /// <param name="order">The order to be placed</param>
        /// <returns>True if the request for a new order has been placed, false otherwise</returns>
        public abstract bool PlaceOrder(Order order);

        /// <summary>
        /// Updates the order with the same id
        /// </summary>
        /// <param name="order">The new order information</param>
        /// <returns>True if the request was made for the order to be updated, false otherwise</returns>
        public abstract bool UpdateOrder(Order order);

        /// <summary>
        /// Cancels the order with the specified ID
        /// </summary>
        /// <param name="order">The order to cancel</param>
        /// <returns>True if the request was made for the order to be canceled, false otherwise</returns>
        public abstract bool CancelOrder(Order order);

        /// <summary>
        /// Connects the client to the broker's remote servers
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Disconnects the client from the broker's remote servers
        /// </summary>
        public abstract void Disconnect();

        /// <summary>
        /// Event invocator for the OrderFilled event
        /// </summary>
        /// <param name="e">The OrderEvent</param>
        protected virtual void OnOrderEvent(OrderEvent e)
        {
            try
            {
                Log.Trace("Brokerage.OnOrderEvent(): " + e);

                var handler = OrderEvent;
                if (handler != null) handler(this, e);
            }
            catch (Exception error)
            {
                Log.Error("Brokerage.OnOrderEvent(): Caught Error: " + error.Message);
            }
        }

        /// <summary>
        /// Event invocator for the PortfolioChanged event
        /// </summary>
        /// <param name="e">The PortfolioEvent</param>
        protected virtual void OnPortfolioChanged(PortfolioEvent e)
        {
            try
            {
                Log.Trace("Brokerage.OnPortfolioChanged(): " + e);

                var handler = PortfolioChanged;
                if (handler != null) handler(this, e);
            }
            catch (Exception error)
            {
                Log.Error("Brokerage.OnPortfolioChanged(): Caught Error: " + error.Message);
            }
        }

        /// <summary>
        /// Event invocator for the AccountChanged event
        /// </summary>
        /// <param name="e">The AccountEvent</param>
        protected virtual void OnAccountChanged(AccountEvent e)
        {
            try
            {
                Log.Trace("Brokerage.OnAccountChanged(): " + e);

                var handler = AccountChanged;
                if (handler != null) handler(this, e);
            }
            catch (Exception error)
            {
                Log.Error("Brokerage.OnAccountChanged(): Caught Error: " + error.Message);
            }
        }

        /// <summary>
        /// Event invocator for the Error event
        /// </summary>
        /// <param name="e">The error</param>
        protected virtual void OnError(Exception e)
        {
            try
            {
                Log.Error("Brokerage.OnError(): " + e.Message);

                var handler = Error;
                if (handler != null) handler(this, e);
            }
            catch (Exception ex)
            {
                Log.Error("Brokerage.OnError(): Caught Error: " + ex.Message);
            }
        }
    }
}
