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

using QuantConnect.Securities.Option;

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Orders.OptionExercise"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Orders.OptionExercise.DefaultExerciseModel"/> class and its consumers or related classes
        /// </summary>
        public static class DefaultExerciseModel
        {
            public static string OptionAssignment = "Option Assignment";

            public static string OptionExercise = "Option Exercise";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ContractHoldingsAdjustmentFillTag(bool inTheMoney, bool isAssignment, Option option)
            {
                var action = isAssignment ? "Assignment" : "Exercise";
                var tag = inTheMoney ? $"Automatic {action}" : "OTM";

                return $"{tag}. Underlying: {option.Underlying.Price.ToStringInvariant()}";
            }
        }
    }
}
