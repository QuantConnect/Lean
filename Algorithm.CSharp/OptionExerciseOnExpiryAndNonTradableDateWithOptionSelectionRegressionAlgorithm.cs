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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm asserting that options are automatically exercised on expiry regardless on whether
    /// the day after expiration is tradable or not.
    /// This specific algorithm works with contracts added by selection using the option security filter.
    /// </summary>
    public class OptionExerciseOnExpiryAndNonTradableDateWithOptionSelectionRegressionAlgorithm
        : OptionExerciseOnExpiryAndNonTradableDateRegressionAlgorithm
    {
        protected override void InitializeOptions(Symbol underlying, Symbol[] options)
        {
            AddIndexOption(underlying, options[0].ID.Symbol)
                .SetFilter(u => u.IncludeWeeklys().Contracts(contracts => options));
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 16649;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public override int AlgorithmHistoryDataPoints => 0;
    }
}
