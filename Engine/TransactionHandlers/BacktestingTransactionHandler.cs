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

using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// This transaction handler is used for processing transactions during backtests
    /// </summary>
    public class BacktestingTransactionHandler : BrokerageTransactionHandler
    {
        // save off a strongly typed version of the brokerage
        private readonly BacktestingBrokerage _brokerage;

        /// <summary>
        /// Creates a new BacktestingTransactionHandler using the BacktestingBrokerage
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="brokerage">The BacktestingBrokerage</param>
        public BacktestingTransactionHandler(IAlgorithm algorithm, BacktestingBrokerage brokerage) 
            : base(algorithm, brokerage)
        {
            _brokerage = brokerage;
        }

        /// <summary>
        /// Processes all synchronous events that must take place before the next time loop for the algorithm
        /// </summary>
        public override void ProcessSynchronousEvents()
        {
            base.ProcessSynchronousEvents();
            
            _brokerage.Scan();
        }
    }
}