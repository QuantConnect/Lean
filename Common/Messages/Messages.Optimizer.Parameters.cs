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

namespace QuantConnect
{
    /// <summary>
    /// Provides user-facing message construction methods and static messages for the <see cref="Optimizer.Parameters"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing messages for the <see cref="Optimizer.Parameters.OptimizationParameterJsonConverter"/> class and its consumers or related classes
        /// </summary>
        public static class OptimizationParameterJsonConverter
        {
            public static string OptimizationParameterNotSpecified = "Optimization parameter name is not specified.";

            public static string OptimizationParameterNotSupported = "Optimization parameter is not currently supported.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Optimizer.Parameters.OptimizationStepParameter"/> class and its consumers or related classes
        /// </summary>
        public static class OptimizationStepParameter
        {
            public static string StepLessThanMinStep = $"step should be great or equal than minStep";

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string InvalidStepRange(decimal min, decimal max)
            {
                return $"Minimum value ({min}) should be less or equal than maximum ({max})";
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string NonPositiveStepValue(string stepVarName, decimal value)
            {
                return $"{stepVarName} should be positive value; but was {value}";
            }
        }
    }
}
