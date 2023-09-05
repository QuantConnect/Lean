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
    /// Provides user-facing message construction methods and static messages for the <see cref="Algorithm.Framework.Alphas"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Algorithm.Framework.Alphas.Insight"/> class and its consumers or related classes
        /// </summary>
        public static class Insight
        {
            public static string InvalidBarCount = "Insight barCount must be greater than zero.";

            public static string InvalidPeriod = "Insight period must be greater than or equal to 1 second.";

            public static string InvalidCloseTimeUtc = "Insight closeTimeUtc must be greater than generatedTimeUtc.";

            public static string InvalidCloseTimeLocal = "Insight closeTimeLocal must not be in the past.";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string GeneratedTimeUtcNotSet(Algorithm.Framework.Alphas.Insight insight)
            {
                return Invariant($@"The insight's '{nameof(insight.GeneratedTimeUtc)}' property must be set before calling {
                    nameof(insight.SetPeriodAndCloseTime)}.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InsightAlreadyAssignedToAGroup(Algorithm.Framework.Alphas.Insight insight)
            {
                return Invariant($"Unable to set group id on insight {insight} because it has already been assigned to a group.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Algorithm.Framework.Alphas.Insight insight)
            {
                var str = Invariant($"{insight.Id:N}: {insight.Symbol} {insight.Type} {insight.Direction} within {insight.Period}");

                if (insight.Magnitude.HasValue)
                {
                    str += Invariant($" by {insight.Magnitude.Value}%");
                }
                if (insight.Confidence.HasValue)
                {
                    str += Invariant($" with {Math.Round(100 * insight.Confidence.Value, 1)}% confidence");
                }
                if (insight.Weight.HasValue)
                {
                    str += Invariant($" and {Math.Round(100 * insight.Weight.Value, 1)}% weight");
                }

                if (!string.IsNullOrEmpty(insight.Tag))
                {
                    str += Invariant($": {insight.Tag}");
                }

                return str;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ShortToString(Algorithm.Framework.Alphas.Insight insight)
            {
                var str = Invariant($"{insight.Symbol.Value} {insight.Type} {insight.Direction} {insight.Period}");

                if (insight.Magnitude.HasValue)
                {
                    str += Invariant($" M:{insight.Magnitude.Value}%");
                }
                if (insight.Confidence.HasValue)
                {
                    str += Invariant($" C:{Math.Round(100 * insight.Confidence.Value, 1)}%");
                }
                if (insight.Weight.HasValue)
                {
                    str += Invariant($" W:{Math.Round(100 * insight.Weight.Value, 1)}%");
                }
                if (!string.IsNullOrEmpty(insight.Tag))
                {
                    str += Invariant($". {insight.Tag}");
                }

                return str;
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Algorithm.Framework.Alphas.InsightScore"/> class and its consumers or related classes
        /// </summary>
        public static class InsightScore
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Algorithm.Framework.Alphas.InsightScore insightScore)
            {
                return Invariant($@"Direction: {Math.Round(100 * insightScore.Direction, 2)} Magnitude: {
                    Math.Round(100 * insightScore.Magnitude, 2)}");
            }
        }
    }
}
