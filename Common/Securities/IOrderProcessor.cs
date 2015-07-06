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

using System.Threading;
using QuantConnect.Orders;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a type capable of processing orders
    /// </summary>
    public interface IOrderProcessor : IOrderProvider
    {
        /// <summary>
        /// Reset event that signals when this order processor is not busy processing orders
        /// </summary>
        ManualResetEventSlim ProcessingCompletedEvent { get; }

        /// <summary>
        /// Adds an order request 
        /// </summary>
        /// <param name="orderRequest">The order request to be processed</param>
        void Process(OrderRequest orderRequest);

        /// <summary>
        /// Attempts to remove submit request from request queue
        /// </summary>
        /// <param name="orderId">OrderId of submit request</param>
        /// <returns>True if cancel succeeded</returns>
        bool TryCancelSubmitRequest(int orderId);
    }
}