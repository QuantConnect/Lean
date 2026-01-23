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

using QuantConnect.Indicators;
using QuantConnect.Securities.Option;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to override the option pricing model with the
    /// <see cref="IndicatorBasedOptionPriceModel"/> for a given index option security.
    /// </summary>
    public class IndicatorBasedOptionPricingModelIndexOptionRegressionAlgorithm : IndicatorBasedOptionPricingModelRegressionAlgorithm
    {
        protected override DateTime TestStartDate => new(2021, 1, 4);

        protected override DateTime TestEndDate => new(2021, 1, 4);

        protected override Option GetOption()
        {
            var index = AddIndex("SPX");
            var indexOption = AddIndexOption(index.Symbol);
            indexOption.SetFilter(u => u.CallsOnly());
            return indexOption;
        }

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 4806;
    }
}
