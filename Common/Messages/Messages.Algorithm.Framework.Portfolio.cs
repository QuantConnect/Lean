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

using System.Runtime.CompilerServices;

using QuantConnect.Interfaces;
using QuantConnect.Securities.Positions;

using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Algorithm.Framework.Portfolio"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Algorithm.Framework.Portfolio.PortfolioTarget"/> class and its consumers or related classes
        /// </summary>
        public static class PortfolioTarget
        {
            /// <summary>
            /// Returns a string message saying the portfolio target percent is invalid
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidTargetPercent(IAlgorithm algorithm, decimal percent)
            {
                return Invariant($@"The portfolio target percent: {
                    percent}, does not comply with the current 'Algorithm.Settings' 'MaxAbsolutePortfolioTargetPercentage': {
                    algorithm.Settings.MaxAbsolutePortfolioTargetPercentage} or 'MinAbsolutePortfolioTargetPercentage': {
                    algorithm.Settings.MinAbsolutePortfolioTargetPercentage}. Skipping");
            }

            /// <summary>
            /// Returns a string message saying the given symbol was not found in the portfolio
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SymbolNotFound(QuantConnect.Symbol symbol)
            {
                return Invariant($"{symbol} not found in portfolio. Request this data when initializing the algorithm.");
            }

            /// <summary>
            /// Returns a string message saying it was impossible to compute the order quantity of the given symbol. It also
            /// explains the reason why it was impossible
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToComputeOrderQuantityDueToNullResult(QuantConnect.Symbol symbol, GetMaximumLotsResult result)
            {
                return Invariant($"Unable to compute order quantity of {symbol}. Reason: {result.Reason} Returning null.");
            }

            /// <summary>
            /// Parses the given portfolio target into a string message containing basic information about it
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Algorithm.Framework.Portfolio.PortfolioTarget portfolioTarget)
            {
                var str = Invariant($"{portfolioTarget.Symbol}: {portfolioTarget.Quantity.Normalize()}");
                if (!string.IsNullOrEmpty(portfolioTarget.Tag))
                {
                    str += $" ({portfolioTarget.Tag})";
                }

                return str;
            }
        }
    }
}
