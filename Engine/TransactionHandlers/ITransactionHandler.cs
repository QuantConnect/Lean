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
 *
*/

/**********************************************************
* USING NAMESPACES
**********************************************************/

using System.Collections.Concurrent;
using System.Collections.Generic;
using QuantConnect.Interfaces;
using QuantConnect.Orders;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// Transaction handlers define how the transactions are processed and set the order fill information.
    /// The pass this information back to the algorithm portfolio and ensure the cash and portfolio are synchronized.
    /// </summary>
    /// <remarks>A new transaction handler is required for each each brokerage endpoint.</remarks>
    public interface ITransactionHandler
    {
        /******************************************************** 
        * INTERFACE PROPERTIES
        *********************************************************/
        /// <summary>
        /// The orders queue holds orders which are sent to exchange, partially filled, completely filled or cancelled.
        /// Once the transaction thread has worked on them they get put here while witing for fill updates.
        /// </summary>
        ConcurrentDictionary<int, Order> Orders
        {
            get;
            set;
        }

        /// <summary>
        /// OrderEvents is an orderid indexed collection of events attached to each order. Because an order might be filled in 
        /// multiple legs it is important to keep a record of each event.
        /// </summary>
        ConcurrentDictionary<int, List<OrderEvent>> OrderEvents
        {
            get;
            set;
        }

        /// <summary>
        /// OrderQueue holds the newly updated orders from the user algorithm waiting to be processed. Once
        /// orders are processed they are moved into the Orders queue awaiting the brokerage response.
        /// </summary>
        ConcurrentQueue<Order> OrderQueue
        {
            get;
            set;
        }

        /// <summary>
        /// Boolean flag indicating the thread is busy. 
        /// False indicates it is completely finished processing and ready to be terminated.
        /// </summary>
        bool IsActive
        {
            get;
        }

        /// <summary>
        /// Boolean flag signalling the handler is ready and all orders have been processed.
        /// </summary>
        bool Ready
        {
            get;
        }
        /******************************************************** 
        * INTERFACE METHODS
        *********************************************************/
        /// <summary>
        /// Primary thread entry point to launch the transaction thread.
        /// </summary>
        void Run();
        
        /// <summary>
        /// Submit a new order to be processed.
        /// </summary>
        /// <param name="order">New order object</param>
        /// <returns>New unique quantconnect order id</returns>
        int NewOrder(Order order);

        /// <summary>
        /// Update and resubmit the order to the OrderQueue for processing.
        /// </summary>
        /// <param name="order">Order we'd like updated</param>
        /// <returns>True if successful, false if already cancelled or filled.</returns>
        bool UpdateOrder(Order order);

        /// <summary>
        /// Cancel the order specified
        /// </summary>
        /// <param name="order">Order we'd like to cancel.</param>
        /// <returns>True if successful, false if its already been cancelled or filled.</returns>
        bool CancelOrder(Order order);

        /// <summary>
        /// Set a local reference to the algorithm instance.
        /// </summary>
        /// <param name="algorithm">IAlgorithm object</param>
        void SetAlgorithm(IAlgorithm algorithm);

        /// <summary>
        /// Signal a end of thread request to stop montioring the transactions.
        /// </summary>
        void Exit();
    }
}
