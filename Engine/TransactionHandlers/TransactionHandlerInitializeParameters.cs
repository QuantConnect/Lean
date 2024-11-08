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

using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;

namespace QuantConnect.Lean.Engine.TransactionHandlers
{
    /// <summary>
    /// DTO parameters class to initialize a transaction handler
    /// </summary>
    public class TransactionHandlerInitializeParameters
    {
        /// <summary>
        /// The algorithm
        /// </summary>
        public IAlgorithm Algorithm { get; set; }

        /// <summary>
        /// Brokerage
        /// </summary>
        public IBrokerage Brokerage { get; set; }

        /// <summary>
        /// The result handler instance
        /// </summary>
        public IResultHandler ResultHandler { get; set; }

        /// <summary>
        /// The universe selection instance
        /// </summary>
        public UniverseSelection UniverseSelection { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public TransactionHandlerInitializeParameters(IAlgorithm algorithm, IBrokerage brokerage, IResultHandler resultHandler,
            UniverseSelection universeSelection)
        {
            Algorithm = algorithm;
            Brokerage = brokerage;
            ResultHandler = resultHandler;
            UniverseSelection = universeSelection;
        }
    }
}
