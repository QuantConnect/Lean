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

namespace QuantConnect.Optimizer.Parameters
{
    /// <summary>
    /// Enumerates all possible values for specific optimization parameter
    /// </summary>
    public class OptimizationStepParameterEnumerator : OptimizationParameterEnumerator<OptimizationStepParameter>
    {
        /// <summary>
        /// Creates an instance of <see cref="OptimizationStepParameterEnumerator"/>
        /// </summary>
        /// <param name="optimizationParameter">Step-based optimization parameter</param>
        public OptimizationStepParameterEnumerator(OptimizationStepParameter optimizationParameter) : base(optimizationParameter)
        {
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        public override string Current
        {
            get
            {
                var value = OptimizationParameter.MinValue + Index * OptimizationParameter.Step;
                if (Index == -1 || value > OptimizationParameter.MaxValue)
                    throw new InvalidOperationException();

                return value.ToStringInvariant();
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns> true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        public override bool MoveNext()
        {
            var value = OptimizationParameter.MinValue + (Index + 1) * OptimizationParameter.Step;
            if (value <= OptimizationParameter.MaxValue)
            {
                Index++;
                return true;
            }

            return false;
        }
    }
}
