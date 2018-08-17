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
using QuantConnect.Interfaces;

namespace QuantConnect.Benchmarks
{
    /// <summary>
    /// Creates a benchmark defined by the compounding annual return on capital
    /// </summary>
    public class CompoundingAnnualReturnBenchmark : FuncBenchmark
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundingAnnualReturnBenchmark"/> class
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="annualReturnPercentage">The compounding annual return percentage</param>
        public CompoundingAnnualReturnBenchmark(IAlgorithm algorithm, decimal annualReturnPercentage = 0.02m)
            : this(algorithm.StartDate, algorithm.Portfolio.TotalPortfolioValue, annualReturnPercentage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundingAnnualReturnBenchmark"/> class
        /// </summary>
        /// <param name="startDate">The starting date</param>
        /// <param name="startingCapital">The initial capital</param>
        /// <param name="annualReturnPercentage">The compounding annual return percentage</param>
        public CompoundingAnnualReturnBenchmark(DateTime startDate, decimal startingCapital, decimal annualReturnPercentage = 0.02m)
            : base(dt =>
            {
                var years = (dt - startDate).TotalDays / 365.25;
                return startingCapital * (decimal)Math.Exp((double)annualReturnPercentage * years);
            })
        {
        }
    }
}

