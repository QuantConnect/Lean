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
            if (slice.OptionChains.Count < 2)
            {
                throw new RegressionTestException("Expected at least two option chains, one for the mapped symbol and one or more for the filtered symbol");
            }
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 22299;
    }
}
