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

using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.Results;
using System.Collections.Generic;

namespace QuantConnect.Tests
{
    /// <summary>
    /// Container class for results generated during an algorithm's execution in <see cref="AlgorithmRunner"/>
    /// </summary>
    public class AlgorithmRunnerResults
    {
        /// <summary>
        /// Algorithm name
        /// </summary>
        public readonly string Algorithm;

        /// <summary>
        /// Algorithm language (C#, Python)
        /// </summary>
        public readonly Language Language;

        /// <summary>
        /// AlgorithmManager instance that is used to run the algorithm
        /// </summary>
        public readonly AlgorithmManager AlgorithmManager;

        /// <summary>
        /// Algorithm results containing all of the sampled series
        /// </summary>
        public readonly BacktestingResultHandler Results;

        /// <summary>
        /// The logs generated during the algorithm's execution
        /// </summary>
        public readonly List<string> Logs;

        public AlgorithmRunnerResults(
            string algorithm,
            Language language,
            AlgorithmManager manager,
            BacktestingResultHandler results,
            List<string> logs)
        {
            Algorithm = algorithm;
            Language = language;
            AlgorithmManager = manager;
            Results = results;
            Logs = logs;
        }
    }
}
