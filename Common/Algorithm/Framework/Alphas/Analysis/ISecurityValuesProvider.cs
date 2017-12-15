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

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Provides a simple abstraction that returns a security's current price and volatility.
    /// This facilitates testing by removing the dependency of IAlgorithm on the analysis components
    /// </summary>
    public interface ISecurityValuesProvider
    {
        /// <summary>
        /// Gets the current values for the specified symbol (price/volatility)
        /// </summary>
        /// <param name="symbol">The symbol to get price/volatility for</param>
        /// <returns>The alpha target values for the specified symbol</returns>
        SecurityValues GetValues(Symbol symbol);
    }
}