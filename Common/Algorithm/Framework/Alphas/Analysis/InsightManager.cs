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
using Python.Runtime;

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Encapsulates the storage of insights.
    /// </summary>
    public class InsightManager : InsightCollection
    {
        private IInsightScoreFunction _insightScoreFunction;

        /// <summary>
        /// Process a new time step handling insights scoring
        /// </summary>
        /// <param name="utcNow">The current utc time</param>
        public void Step(DateTime utcNow)
        {
            _insightScoreFunction?.Score(this, utcNow);
        }

        /// <summary>
        /// Sets the insight score function to use
        /// </summary>
        /// <param name="insightScoreFunction">Model that scores insights</param>
        public void SetInsightScoreFunction(IInsightScoreFunction insightScoreFunction)
        {
            _insightScoreFunction = insightScoreFunction;
        }

        /// <summary>
        /// Sets the insight score function to use
        /// </summary>
        /// <param name="insightScoreFunction">Model that scores insights</param>
        public void SetInsightScoreFunction(PyObject insightScoreFunction)
        {
            IInsightScoreFunction model;
            if (insightScoreFunction.TryConvert(out model))
            {
                SetInsightScoreFunction(model);
            }
            else
            {
                _insightScoreFunction = new InsightScoreFunctionPythonWrapper(insightScoreFunction);
            }
        }
    }
}
