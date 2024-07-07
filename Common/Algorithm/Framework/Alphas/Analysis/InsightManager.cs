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
using Python.Runtime;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.Framework.Alphas.Analysis
{
    /// <summary>
    /// Encapsulates the storage of insights.
    /// </summary>
    public class InsightManager : InsightCollection
    {
        private readonly IAlgorithm _algorithm;
        private IInsightScoreFunction _insightScoreFunction;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="algorithm">The associated algorithm instance</param>
        public InsightManager(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

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

        /// <summary>
        /// Expire the insights of the given symbols
        /// </summary>
        /// <param name="symbols">Symbol we want to expire insights for</param>
        public void Expire(IEnumerable<Symbol> symbols)
        {
            if (symbols == null)
            {
                return;
            }

            foreach (var symbol in symbols)
            {
                if (TryGetValue(symbol, out var insights))
                {
                    Expire(insights);
                }
            }
        }

        /// <summary>
        /// Cancel the insights of the given symbols
        /// </summary>
        /// <param name="symbols">Symbol we want to cancel insights for</param>
        public void Cancel(IEnumerable<Symbol> symbols)
        {
            Expire(symbols);
        }

        /// <summary>
        /// Expire the given insights
        /// </summary>
        /// <param name="insights">Insights to expire</param>
        public void Expire(IEnumerable<Insight> insights)
        {
            if (insights == null)
            {
                return;
            }

            var currentUtcTime = _algorithm.UtcTime;
            foreach (var insight in insights)
            {
                insight.Expire(currentUtcTime);
            }
        }

        /// <summary>
        /// Cancel the given insights
        /// </summary>
        /// <param name="insights">Insights to cancel</param>
        public void Cancel(IEnumerable<Insight> insights)
        {
            Expire(insights);
        }
    }
}
