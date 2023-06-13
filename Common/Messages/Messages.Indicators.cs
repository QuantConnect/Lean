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
    /// Provides user-facing message construction methods and static messages for the <see cref="Indicators"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Indicators.IndicatorDataPoint"/> class and its consumers or related classes
        /// </summary>
        public static class IndicatorDataPoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidObjectTypeToCompareTo(Type type)
            {
                return $"Object must be of type {type.GetBetterTypeName()}";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Indicators.IndicatorDataPoint instance)
            {
                return Invariant($"{instance.Time.ToStringInvariant("s")} - {instance.Value}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string UnsupportedMethod(string methodName)
            {
                return $"IndicatorDataPoint does not support the {methodName} function. This function should never be called on this type.";
            }
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Indicators.RollingWindow{T}"/> class and its consumers or related classes
        /// </summary>
        public static class RollingWindow
        {
            public static string InvalidSize = "RollingWindow must have size of at least 1.";

            public static string NoItemsRemovedYet = "No items have been removed yet!";

            public static string IndexOutOfSizeRange = "Index must be a non-negative integer";
        }
    }
}
