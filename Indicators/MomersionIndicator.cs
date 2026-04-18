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
 *
*/

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Legacy name for the <see cref="Momersion"/> indicator, maintained for backwards compatibility.
    /// This oscillator measures the balance between momentum and mean-reversion over a specified period.
    /// </summary>
    public class MomersionIndicator : Momersion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MomersionIndicator"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="minPeriod">The minimum period.</param>
        /// <param name="fullPeriod">The full period.</param>
        public MomersionIndicator(string name, int? minPeriod, int fullPeriod)
            : base(name, minPeriod, fullPeriod)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MomersionIndicator"/> class.
        /// </summary>
        /// <param name="minPeriod">The minimum period.</param>
        /// <param name="fullPeriod">The full period.</param>
        public MomersionIndicator(int? minPeriod, int fullPeriod)
            : this($"Momersion({minPeriod},{fullPeriod})", minPeriod, fullPeriod)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MomersionIndicator"/> class.
        /// </summary>
        /// <param name="fullPeriod">The full period.</param>
        public MomersionIndicator(int fullPeriod)
            : this(null, fullPeriod)
        {
        }
    }
}