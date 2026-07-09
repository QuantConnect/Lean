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

using System.Linq;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that the future security cache open interest is set from the chain universe data open interest
    /// </summary>
    public class FutureUniverseOpenInterestRegressionAlgorithm : OptionUniverseOpenInterestRegressionAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 8);
            SetEndDate(2013, 10, 8);
            SetCash(100000);

            var future = AddFuture(Futures.Indices.SP500EMini, Resolution.Minute);
            future.SetFilter(universe => universe.Contracts(contracts => contracts.Where(x => x.OpenInterest != 0)));
        }

        /// <summary>
        /// Gets the chain universe data point stored in the given security cache if any
        /// </summary>
        protected override BaseChainUniverseData GetChainUniverseData(Security security)
        {
            return security.Cache.GetData<FutureUniverse>();
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 8494;
    }
}
