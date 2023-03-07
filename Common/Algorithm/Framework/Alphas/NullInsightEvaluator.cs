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
using QuantConnect.Algorithm.Framework.Alphas.Analysis;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Null implementation
    /// </summary>
    public class NullInsightEvaluator : IInsightEvaluator
    {
        /// <summary>
        /// Method to evaluate and score insights for each time step
        /// </summary>
        public void Score(IInsightManager insightManager, DateTime utcTime)
        {
        }
    }
}
