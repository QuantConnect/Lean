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
using System.Collections.Generic;

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
        public decimal? Step { get; private set; }

        /// <summary>
        /// Minimal possible movement for current parameter, should be positive
        /// </summary>
        /// <remarks>Used by <see cref="Strategies.EulerSearchOptimizationStrategy"/> to determine when this parameter can no longer be optimized</remarks>
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
            var absStep = Math.Abs(step);
            var absMinStep = Math.Abs(minStep);
            var actual = Math.Max(absStep, absMinStep);
            
            Step = actual != 0 
                ? actual 
                : 1;

            MinStep = absMinStep != 0
                ? absMinStep
                : Step;
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

        public override IEnumerator<string> GetEnumerator() => new OptimizationStepParameterEnumerator(this);

        /// <summary>
        /// Calculates number od data points for step based optimization parameter based on min/max and step values
        /// </summary>
        /// <returns></returns>
        public override int Estimate()
        {
            if (!Step.HasValue)
            {
                throw new InvalidOperationException("Optimization parameter cannot be estimated due to step value is not initialized");
            }
            return (int) Math.Floor((MaxValue - MinValue) / Step.Value) + 1;
        }
    }
}
