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

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Provides helper methods for the Random Data Generator
    /// </summary>
    public static class RandomDataGeneratorHelper
    {
        /// <summary>
        /// Calculates the progress percentage of the current time between a start and end time.
        /// </summary>
        /// <param name="start">The start time of the process.</param>
        /// <param name="end">The end time of the process.</param>
        /// <param name="currentTime">The current time to evaluate progress.</param>
        /// <returns>The progress as a percentage, rounded to two decimal places.</returns>
        public static double GetProgressAsPercentage(DateTime start, DateTime end, DateTime currentTime)
        {
            var totalDuration = end - start;
            return Math.Round((currentTime - start).TotalMilliseconds * 1.0 / totalDuration.TotalMilliseconds * 100, 2);
        }
    }
}