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
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.Results.Analysis
{
    /// <summary>
    /// Provides an in-run <see cref="ResultsAnalyzer"/> instance access to the data of the running
    /// backtest that the intermediate results handed to it don't carry. Implemented by the result
    /// handler, which owns the data and its synchronization, while the analyzer decides what to
    /// read and how much, keeping its incremental consumption state private.
    /// </summary>
    public interface IInRunAnalysisDataProvider
    {
        /// <summary>
        /// Gets the log lines produced from the given position in the log stream.
        /// </summary>
        IReadOnlyList<string> GetLogs(int fromPosition);

        /// <summary>
        /// Takes a sample of the engine speed counters, or null when the counters should
        /// not be sampled, like while the algorithm warms up.
        /// </summary>
        AlgorithmSpeedSample? TakeSpeedSample();
    }
}
