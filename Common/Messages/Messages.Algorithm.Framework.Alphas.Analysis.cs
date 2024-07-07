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

using System;
using System.Runtime.CompilerServices;
using static QuantConnect.StringExtensions;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Algorithm.Framework.Alphas.Analysis"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Algorithm.Framework.Alphas.Analysis.InsightManager"/> class and its consumers or related classes
        /// </summary>
        public static class InsightManager
        {
            public static string InvalidExtraAnalysisPeriodRatio =
                "extraAnalysisPeriodRatio must be greater than or equal to zero.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ZeroInitialPriceValue(
                DateTime frontierTimeUtc,
                Algorithm.Framework.Alphas.Insight insight
            )
            {
                return Invariant(
                    $"InsightManager.Step(): Warning {frontierTimeUtc} UTC: insight {insight} initial price value is 0"
                );
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Algorithm.Framework.Alphas.Analysis.ReadOnlySecurityValuesCollection"/> class and its consumers or related classes
        /// </summary>
        public static class ReadOnlySecurityValuesCollection
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string SecurityValuesForSymbolNotFound(QuantConnect.Symbol symbol)
            {
                return Invariant($"SecurityValues for symbol {symbol} was not found");
            }
        }
    }
}
