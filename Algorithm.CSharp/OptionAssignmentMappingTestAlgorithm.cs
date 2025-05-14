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
using QuantConnect.Data;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Test algorithm to assert we receive option chains for the canonical option symbol.
    /// This algorithm will be used to run a unit test instead of a regression test, and
    /// it will be used to assert the correct underlying SID is generated with the correct date
    /// for the backtest current date.
    /// </summary>
    public class OptionAssignmentMappingTestAlgorithm : OptionAssignmentRegressionAlgorithm
    {
        private bool _chainsChecked;

        private Option _canonicalOption;

        public override void Initialize()
        {
            base.Initialize();

            _canonicalOption = AddOption(Stock.Symbol, Resolution.Minute);
        }

        public override void OnData(Slice slice)
        {
            base.OnData(slice);

            if (slice.OptionChains.TryGetValue(_canonicalOption.Symbol, out var chain) && chain.Count > 0)
            {
                _chainsChecked = true;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            base.OnEndOfAlgorithm();

            if (!_chainsChecked)
            {
                throw new RegressionTestException("No options chains received");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public override bool CanRunLocally { get; } = false;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };
    }
}
