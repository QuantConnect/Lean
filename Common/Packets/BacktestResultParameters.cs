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
using QuantConnect.Statistics;
using System.Collections.Generic;
using QuantConnect.Securities.Positions;

namespace QuantConnect.Packets
{
    /// <summary>
    /// Defines the parameters for <see cref="BacktestResult"/>
    /// </summary>
    public class BacktestResultParameters : BaseResultParameters
    {
        /// <summary>
        /// Rolling window detailed statistics.
        /// </summary>
        public Dictionary<string, AlgorithmPerformance> RollingWindow { get; set; }

        /// <summary>
        /// Rolling window detailed statistics.
        /// </summary>
        public AlgorithmPerformance TotalPerformance { get; set; }
        /// <summary>
        /// Creates a new instance
        /// </summary>
        public BacktestResultParameters(IDictionary<string, Chart> charts,
            IDictionary<int, Order> orders,
            IDictionary<DateTime, decimal> profitLoss,
            IDictionary<string, string> statistics,
            IDictionary<string, string> runtimeStatistics,
            Dictionary<string, AlgorithmPerformance> rollingWindow,
            List<OrderEvent> orderEvents,
            AlgorithmPerformance totalPerformance = null,
            AlgorithmConfiguration algorithmConfiguration = null,
            IDictionary<string, string> state = null)
            : base(charts, orders, profitLoss, statistics, runtimeStatistics, orderEvents, algorithmConfiguration, state)
        {
            RollingWindow = rollingWindow;
            TotalPerformance = totalPerformance;
        }
    }
}
