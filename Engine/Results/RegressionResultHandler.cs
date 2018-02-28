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

using System;
using System.IO;
using QuantConnect.Configuration;
using QuantConnect.Orders;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.Results
{
    /// <summary>
    /// Provides a wrapper over the <see cref="BacktestingResultHandler"/> that logs all order events
    /// to a separate file
    /// </summary>
    public class RegressionResultHandler : BacktestingResultHandler
    {
        private string AlgorithmTypeName => Algorithm.GetType().Name;
        private Language Language => Config.GetValue<Language>("algorithm-language");
        private readonly Lazy<StreamWriter> OrdersLogStreamWriter;

        /// <summary>
        /// Gets the path used for logging all order events
        /// </summary>
        public string OrdersLogFilePath => $"./regression/{AlgorithmTypeName}.{Language.ToLower()}.orders.log";

        /// <summary>
        /// Initializes a new instance of the <see cref="RegressionResultHandler"/> class
        /// </summary>
        public RegressionResultHandler()
        {
            OrdersLogStreamWriter = new Lazy<StreamWriter>(() =>
            {
                var fileInfo = new FileInfo(OrdersLogFilePath);
                Directory.CreateDirectory(fileInfo.DirectoryName);
                if (fileInfo.Exists) fileInfo.Delete();
                return new StreamWriter(OrdersLogFilePath);
            });
        }

        /// <summary>
        /// Log the order and order event to the dedicated log file for this regression algorithm
        /// </summary>
        /// <remarks>In backtesting the order events are not sent because it would generate a high load of messaging.</remarks>
        /// <param name="newEvent">New order event details</param>
        public override void OrderEvent(OrderEvent newEvent)
        {
            // log order events to a separate file for easier diffing of regression runs
            var order = Algorithm.Transactions.GetOrderById(newEvent.OrderId);
            OrdersLogStreamWriter.Value.WriteLine($"{Algorithm.UtcTime}: Order: {order}  OrderEvent: {newEvent}");

            base.OrderEvent(newEvent);
        }

        /// <summary>
        /// Terminate the result thread and apply any required exit procedures.
        /// Save orders log files to disk.
        /// </summary>
        public override void Exit()
        {
            base.Exit();
            if (OrdersLogStreamWriter.IsValueCreated)
            {
                OrdersLogStreamWriter.Value.DisposeSafely();
            }
        }
    }
}