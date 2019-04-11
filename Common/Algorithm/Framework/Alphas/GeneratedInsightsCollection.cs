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
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Defines a collection of insights that were generated at the same time step
    /// </summary>
    public class GeneratedInsightsCollection
    {
        /// <summary>
        /// The utc date time the insights were generated
        /// </summary>
        public DateTime DateTimeUtc { get; }

        /// <summary>
        /// The generated insights
        /// </summary>
        public List<Insight> Insights { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedInsightsCollection"/> class
        /// </summary>
        /// <param name="dateTimeUtc">The utc date time the sinals were generated</param>
        /// <param name="insights">The generated insights</param>
        /// <param name="clone">Keep a clone of the generated insights</param>
        public GeneratedInsightsCollection(DateTime dateTimeUtc,
            IEnumerable<Insight> insights,
            bool clone = true)
        {
            DateTimeUtc = dateTimeUtc;

            // for performance only call 'ToArray' if not empty enumerable (which is static)
            Insights = insights == Enumerable.Empty<Insight>()
                ? new List<Insight>() : insights.Select(insight => clone ? insight.Clone() : insight).ToList();
        }
    }
}
