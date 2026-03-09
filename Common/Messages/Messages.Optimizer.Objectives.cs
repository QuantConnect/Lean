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
    /// Provides user-facing message construction methods and static messages for the <see cref="Optimizer.Objectives"/> namespace
    /// </summary>
    public static partial class Messages
    {
        /// <summary>
        /// Provides user-facing common messages for the <see cref="Optimizer.Objectives"/> namespace classes
        /// </summary>
        public static class OptimizerObjectivesCommon
        {
            /// <summary>
            /// String message saying the backtest result can not be null or empty
            /// </summary>
            public static string NullOrEmptyBacktestResult = "Backtest result can not be null or empty.";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Optimizer.Objectives.Constraint"/> class and its consumers or related classes
        /// </summary>
        public static class Constraint
        {
            /// <summary>
            /// String message saying the constraint target value is not specified
            /// </summary>
            public static string ConstraintTargetValueNotSpecified = "Constraint target value is not specified";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Optimizer.Objectives.ExtremumJsonConverter"/> class and its consumers or related classes
        /// </summary>
        public static class ExtremumJsonConverter
        {
            /// <summary>
            /// String message saying it could not recognize target direction
            /// </summary>
            public static string UnrecognizedTargetDirection = "Could not recognize target direction";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Optimizer.Objectives.Objective"/> class and its consumers or related classes
        /// </summary>
        public static class Objective
        {
            /// <summary>
            /// Null or empty Objective string message
            /// </summary>
            public static string NullOrEmptyObjective = "Objective can not be null or empty";
        }

        /// <summary>
        /// Provides user-facing messages for the <see cref="Optimizer.Objectives.Target"/> class and its consumers or related classes
        /// </summary>
        public static class Target
        {
            /// <summary>
            /// Parses a Target object into a string message
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static string ToString(Optimizer.Objectives.Target instance)
            {
                if (instance.TargetValue.HasValue)
                {
                    return $"Target: {instance.Target} TargetValue: {instance.TargetValue.Value} at: {instance.Current}";
                }
                return $"Target: {instance.Target} at: {instance.Current}";
            }
        }
    }
}
