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
using QuantConnect.Brokerages.Backtesting;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// This transaction handler is used for processing transactions during backtests
    /// </summary>
    public class BacktestingTransactionHandler : BrokerageTransactionHandler
    {
        // save off a strongly typed version of the brokerage
        private BacktestingBrokerage _brokerage;

        /// <summary>
        /// Creates a new BacktestingTransactionHandler using the BacktestingBrokerage
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="brokerage">The BacktestingBrokerage</param>
        /// <param name="resultHandler"></param>
        public override void Initialize(IAlgorithm algorithm, IBrokerage brokerage, IResultHandler resultHandler)
        {
            if (!(brokerage is BacktestingBrokerage))
            {
                throw new ArgumentException("Brokerage must be of type BacktestingBrokerage for use wth the BacktestingTransactionHandler");
            }

            _brokerage = (BacktestingBrokerage) brokerage;

            base.Initialize(algorithm, brokerage, resultHandler);

            // non blocking implementation
            _orderRequestQueue = new BusyCollection<OrderRequest>();
        }

        /// <summary>
        /// Processes all synchronous events that must take place before the next time loop for the algorithm
        /// </summary>
        public override void ProcessSynchronousEvents()
        {
            // we process pending order requests our selves
            Run();

            base.ProcessSynchronousEvents();

            _brokerage.SimulateMarket();
            _brokerage.Scan();
        }

        /// <summary>
        /// Processes asynchronous events on the transaction handler's thread
        /// </summary>
        public override void ProcessAsynchronousEvents()
        {
            base.ProcessAsynchronousEvents();

            _brokerage.SimulateMarket();
            _brokerage.Scan();
        }

        /// <summary>
        /// For backtesting we will submit the order ourselves
        /// </summary>
        /// <param name="ticket">The <see cref="OrderTicket"/> expecting to be submitted</param>
        protected override void WaitForOrderSubmission(OrderTicket ticket)
        {
            // we submit the order request our selves
            Run();

            if (!ticket.OrderSet.WaitOne(0))
            {
                // this could happen if there was some error handling the order
                // and it was not set
                Log.Error("BacktestingTransactionHandler.WaitForOrderSubmission(): " +
                    $"The order request (Id={ticket.OrderId}) was not submitted. " +
                    "See the OrderRequest.Response for more information");
            }
        }

        /// <summary>
        /// For backtesting order requests will be processed by the algorithm thread
        /// sequentially at <see cref="WaitForOrderToBeProcess"/> and <see cref="ProcessSynchronousEvents"/>
        /// </summary>
        protected override void InitializeTransactionThread()
        {
            // nop
        }
    }
}