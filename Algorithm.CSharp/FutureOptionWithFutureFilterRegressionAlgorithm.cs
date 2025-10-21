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

using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm that validates that when using a Future with filter
    /// the option chains are correctly populated and are unique
    /// </summary>
    public class FutureOptionWithFutureFilterRegressionAlgorithm : FutureOptionContinuousFutureRegressionAlgorithm
    {
        public override void SetFilter()
        {
            Future.SetFilter(0, 368);
        }

        public override void ValidateOptionChains(Slice slice)
        {
            var futureContractsWithOptionChains = 0;
            foreach (var futureChain in slice.FutureChains.Values)
            {
                foreach (var futureContract in futureChain)
                {
                    // Not all future contracts have option chains, so we need to check if the contract is in the option chain
                    if (slice.OptionChains.ContainsKey(futureContract.Symbol))
                    {
                        var chain = slice.OptionChains[futureContract.Symbol];
                        if (chain.Count == 0)
                        {
                            throw new RegressionTestException($"Expected at least one option contract for {chain.Symbol}");
                        }
                        futureContractsWithOptionChains++;
                    }
                }

            }
            if (futureContractsWithOptionChains < 1)
            {
                throw new RegressionTestException($"Expected at least two future contracts with option chains, but found {futureContractsWithOptionChains}");
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 29701;
    }
}
