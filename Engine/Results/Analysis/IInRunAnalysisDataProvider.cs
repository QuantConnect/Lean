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
using QuantConnect.Orders;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.Results.Analysis
{
    /// <summary>
    /// Provides the <see cref="InRunResultsAnalyzer"/> access to the data of the running backtest.
    /// Implemented by the result handler, which owns the data and its synchronization, while the
    /// analyzer decides what to read and how much, keeping its incremental consumption state private.
    /// </summary>
    public interface IInRunAnalysisDataProvider
    {
        /// <summary>
        /// Gets the orders placed so far.
        /// </summary>
        IDictionary<int, Order> GetOrders();

        /// <summary>
        /// Gets the order events produced from the given position in the order event stream.
        /// </summary>
        List<OrderEvent> GetOrderEvents(int fromPosition);

        /// <summary>
        /// Gets the log lines produced from the given position in the log stream.
        /// </summary>
        IReadOnlyList<string> GetLogs(int fromPosition);

        /// <summary>
        /// Gets clones of the requested charts, safe to read without further synchronization.
        /// </summary>
        IDictionary<string, Chart> GetChartSnapshots(IReadOnlyList<string> chartNames);

        /// <summary>
        /// Whether the strategy equity chart has samples yet. Until the first sample exists,
        /// the generated statistics are all-zero defaults.
        /// </summary>
        bool HasEquitySamples();

        /// <summary>
        /// Takes a sample of the engine speed counters, or null when the counters should
        /// not be sampled, like while the algorithm warms up.
        /// </summary>
        AlgorithmSpeedSample? TakeSpeedSample();
    }
}
