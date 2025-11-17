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
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the option chain APIs return consistent values for index options
    /// See <see cref="QCAlgorithm.OptionChain(Symbol)"/> and <see cref="QCAlgorithm.OptionChainProvider"/>
    /// </summary>
    public class IndexOptionChainApisConsistencyRegressionAlgorithm : OptionChainApisConsistencyRegressionAlgorithm
    {
        protected override DateTime TestDate => new DateTime(2021, 1, 4);

        protected override Option GetOption()
        {
            return AddIndexOption("SPX");
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 2862;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 2;
    }
}
