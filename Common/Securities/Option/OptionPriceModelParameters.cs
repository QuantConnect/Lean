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
using QuantConnect.Data.Market;

namespace QuantConnect.Securities.Option
{
    /// <summary>
    /// Defines the parameters for <see cref="IOptionPriceModel.Evaluate"/>
    /// </summary>
    public class OptionPriceModelParameters
    {
        /// <summary>
        /// Gets the option security object
        /// </summary>
        public Security Security { get; set; }

        /// <summary>
        /// Gets the current data slice
        /// </summary>
        public Slice Slice { get; set; }

        /// <summary>
        /// Gets the option contract to evaluate
        /// </summary>
        public OptionContract Contract { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionPriceModelParameters"/> class
        /// </summary>
        /// <param name="security">The option security object</param>
        /// <param name="slice">The current data slice</param>
        /// <param name="contract">The option contract to evaluate</param>
        public OptionPriceModelParameters(Security security = null, Slice slice = null, OptionContract contract = null)
        {
            Security = security;
            Slice = slice;
            Contract = contract;
        }
    }
}