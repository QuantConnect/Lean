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
    /// Defines the step based optimization parameter
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
        public decimal? Step { get; set; }

        /// <summary>
        /// Minimal possible movement for current parameter, should be positive
        /// </summary>
        /// <remarks>Used by <see cref="Strategies.EulerSearchOptimizationStrategy"/> to determine when this parameter can no longer be optimized</remarks>
        [JsonProperty("min-step")]
        public decimal? MinStep { get; set; }

        /// <summary>
        /// Create an instance of <see cref="OptimizationParameter"/> based on configuration
        /// </summary>
        /// <param name="name">parameter name</param>
        /// <param name="min">minimal value</param>
        /// <param name="max">maximal value</param>
        public OptimizationStepParameter(string name, decimal min, decimal max)
            : base(name)
        {
            if (min > max)
            {
                throw new ArgumentException($"Minimum value ({min}) should be less or equal than maximum ({max})");
            }

            MinValue = min;
            MaxValue = max;
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
            // with zero step algorithm can go to infinite loop, use default step value
            if (step <= 0)
            {
                throw new ArgumentException($"Step should be positive value; but was {step}");
            }

            // EulerSearch algorithm can go to infinite range division if Min step is not provided, use Step as default 
            if (minStep <= 0)
            {
                throw new ArgumentException($"MinStep should be positive value; but was {minStep}");
            }

            if (step < minStep)
            {
                throw new ArgumentException($"Step should be great or equal than MinStep");
            }

            Step = step;
            MinStep = minStep;
        }
    }
}
