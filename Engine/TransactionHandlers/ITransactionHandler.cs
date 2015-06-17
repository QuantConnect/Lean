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

using System.ComponentModel.Composition;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// Transaction handlers define how the transactions are processed and set the order fill information.
    /// The pass this information back to the algorithm portfolio and ensure the cash and portfolio are synchronized.
    /// </summary>
    [InheritedExport(typeof(ITransactionHandler))]
    public interface ITransactionHandler
    {
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

        /// <summary>
        /// Initializes the transaction handler for the specified algorithm using the specified brokerage implementation
        /// </summary>
        void Initialize(IAlgorithm algorithm, IBrokerage brokerage);

        /// <summary>
        /// Primary thread entry point to launch the transaction thread.
        /// </summary>
        void Run();

        /// <summary>
        /// Signal a end of thread request to stop montioring the transactions.
        /// </summary>
        void Exit();

        /// <summary>
        /// Process any synchronous events from the primary algorithm thread.
        /// </summary>
        void ProcessSynchronousEvents();
    }
}
