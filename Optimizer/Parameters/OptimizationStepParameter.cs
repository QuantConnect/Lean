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

using Newtonsoft.Json;
using System;

namespace QuantConnect.Optimizer.Parameters
{
    /// <summary>
    /// Defines the optimization parameter meta information
    /// </summary>
    public class OptimizationStepParameter : OptimizationParameter
    {
        /// <summary>
        /// Minimum value of optimization parameter, applicable for boundary conditions
        /// </summary>
        [JsonProperty("min")]
        public decimal MinValue { get; }

        /// <summary>
        /// Maximum value of optimization parameter, applicable for boundary conditions
        /// </summary>
        [JsonProperty("max")]
        public decimal MaxValue { get; }

        /// <summary>
        /// Movement, should be positive
        /// </summary>
        [JsonProperty("step")]
        public decimal? Step { get; private set; }

        /// <summary>
        /// Minimal possible movement for current parameter, should be positive
        /// </summary>
        [JsonProperty("min-step")]
        public decimal? MinStep { get; private set; }

        /// <summary>
        /// Create an instance of <see cref="OptimizationParameter"/> based on configuration
        /// </summary>
        /// <param name="name">parameter name</param>
        /// <param name="min">minimal value</param>
        /// <param name="max">maximal value</param>
        public OptimizationStepParameter(string name, decimal min, decimal max)
            : base(name)
        {
            MinValue = Math.Min(min, max);
            MaxValue = Math.Max(min, max);
        }

        /// <summary>
        /// Create an instance of <see cref="OptimizationParameter"/> based on configuration
        /// </summary>
        /// <param name="name">parameter name</param>
        /// <param name="min">minimal value</param>
        /// <param name="max">maximal value</param>
        /// <param name="step">movement</param>
        public OptimizationStepParameter(string name, decimal min, decimal max, decimal step)
            : this(name, min, max, step, step)
        {

        }

        /// <summary>
        /// Create an instance of <see cref="OptimizationParameter"/> based on configuration
        /// </summary>
        /// <param name="name">parameter name</param>
        /// <param name="min">minimal value</param>
        /// <param name="max">maximal value</param>
        /// <param name="step">movement</param>
        /// <param name="minStep">minimal possible movement</param>
        public OptimizationStepParameter(string name, decimal min, decimal max, decimal step, decimal minStep) : this(name, min, max)
        {
            MinStep = minStep != 0
                ? Math.Abs(minStep)
                : 1;
            Step = step != 0
                ? Math.Max(Math.Abs(step), MinStep.Value)
                : 1;
        }

        /// <summary>
        /// Calculate step and min step values based on default number of fragments
        /// </summary>
        /// <param name="defaultSegmentAmount"></param>
        public void CalculateStep(int defaultSegmentAmount)
        {
            if (defaultSegmentAmount < 1)
            {
                throw new ArgumentException(nameof(defaultSegmentAmount), $"Number of segments should be positive number, but specified '{defaultSegmentAmount}'");
            }

            Step = Math.Abs(MaxValue - MinValue) / defaultSegmentAmount;
            MinStep = Step / 10;
        }
    }
}
