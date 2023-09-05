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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidTargetPercent(IAlgorithm algorithm, decimal percent)
            {
                return Invariant($@"The portfolio target percent: {
                    percent}, does not comply with the current 'Algorithm.Settings' 'MaxAbsolutePortfolioTargetPercentage': {
                    algorithm.Settings.MaxAbsolutePortfolioTargetPercentage} or 'MinAbsolutePortfolioTargetPercentage': {
                    algorithm.Settings.MinAbsolutePortfolioTargetPercentage}. Skipping");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SymbolNotFound(QuantConnect.Symbol symbol)
            {
                return Invariant($"{symbol} not found in portfolio. Request this data when initializing the algorithm.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnableToComputeOrderQuantityDueToNullResult(QuantConnect.Symbol symbol, GetMaximumLotsResult result)
            {
                return Invariant($"Unable to compute order quantity of {symbol}. Reason: {result.Reason} Returning null.");
            }

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
