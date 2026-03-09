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

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Represents a portfolio target. This may be a percentage of total portfolio value
    /// or it may be a fixed number of shares.
    /// </summary>
    public interface IPortfolioTarget
    {
        /// <summary>
        /// Gets the symbol of this target
        /// </summary>
        Symbol Symbol { get; }

        /// <summary>
        /// Gets the quantity of this symbol the algorithm should hold
        /// </summary>
        decimal Quantity { get; }

        /// <summary>
        /// Portfolio target tag with additional information
        /// </summary>
        string Tag { get; }
    }
}
