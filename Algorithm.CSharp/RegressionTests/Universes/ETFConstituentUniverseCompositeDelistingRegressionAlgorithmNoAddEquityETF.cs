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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Tests the delisting of the composite Symbol (ETF symbol) and the removal of
    /// the universe and the symbol from the algorithm, without adding a subscription via AddEquity
    /// </summary>
    public class ETFConstituentUniverseCompositeDelistingRegressionAlgorithmNoAddEquityETF : ETFConstituentUniverseCompositeDelistingRegressionAlgorithm
    {
        protected override bool AddETFSubscription { get; set; } = false;

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 511;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;
    }
}
