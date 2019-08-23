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
using System.Collections.Generic;
using QuantConnect.Orders;
using QuantConnect.Packets;

namespace QuantConnect
{
    /// <summary>
    /// Base class for backtesting and live results that packages result data.
    /// <see cref="LiveResult"/>
    /// <see cref="BacktestResult"/>
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Contains population averages scores over the life of the algorithm
        /// </summary>
        public AlphaRuntimeStatistics AlphaRuntimeStatistics;

        /// <summary>
        /// Charts updates for the live algorithm since the last result packet
        /// </summary>
        public IDictionary<string, Chart> Charts = new Dictionary<string, Chart>();

        /// <summary>
        /// Order updates since the last result packet
        /// </summary>
        public IDictionary<int, Order> Orders = new Dictionary<int, Order>();

        /// <summary>
        /// Trade profit and loss information since the last algorithm result packet
        /// </summary>
        public IDictionary<DateTime, decimal> ProfitLoss = new Dictionary<DateTime, decimal>();

        /// <summary>
        /// Statistics information sent during the algorithm operations.
        /// </summary>
        /// <remarks>Intended for update mode -- send updates to the existing statistics in the result GUI. If statistic key does not exist in GUI, create it</remarks>
        public IDictionary<string, string> Statistics = new Dictionary<string, string>();

        /// <summary>
        /// Runtime banner/updating statistics in the title banner of the live algorithm GUI.
        /// </summary>
        public IDictionary<string, string> RuntimeStatistics = new Dictionary<string, string>();
    }
}
