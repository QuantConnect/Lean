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
using QuantConnect.Orders;
using QuantConnect.Securities;
using System.Collections.Generic;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Defines the parameters for <see cref="LiveResult"/>
    /// </summary>
    public class LiveResultParameters : BaseResultParameters
    {
        /// <summary>
        /// Holdings dictionary of algorithm holdings information
        /// </summary>
        public IDictionary<string, Holding> Holdings { get; set; }

        /// <summary>
        /// Cashbook for the algorithm's live results.
        /// </summary>
        public CashBook CashBook { get; set; }

        /// <summary>
        /// Server status information, including CPU/RAM usage, ect...
        /// </summary>
        public IDictionary<string, string> ServerStatistics { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public LiveResultParameters(IDictionary<string, Chart> charts,
            IDictionary<int, Order> orders,
            IDictionary<DateTime, decimal> profitLoss,
            IDictionary<string, Holding> holdings,
            CashBook cashBook,
            IDictionary<string, string> statistics,
            IDictionary<string, string> runtimeStatistics,
            List<OrderEvent> orderEvents,
            IDictionary<string, string> serverStatistics = null,
            AlgorithmConfiguration algorithmConfiguration = null,
            IDictionary<string, string> state = null)
            : base(charts, orders, profitLoss, statistics, runtimeStatistics, orderEvents, algorithmConfiguration, state)
        {
            Holdings = holdings;
            CashBook = cashBook;
            ServerStatistics = serverStatistics ?? OS.GetServerStatistics();
        }
    }
}
