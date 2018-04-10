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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Provides an implementation of <see cref="IAlphaModel"/> that always returns the same insight for each security
    /// </summary>
    public class ConstantAlphaModel : IAlphaModel
    {
        private readonly InsightType _type;
        private readonly InsightDirection _direction;
        private readonly TimeSpan _period;
        private readonly double? _magnitude;
        private readonly double? _confidence;
        private readonly HashSet<Security> _securities;
        private readonly Dictionary<Symbol, DateTime> _insightsTimeBySymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantAlphaModel"/> class
        /// </summary>
        /// <param name="type">The type of insight</param>
        /// <param name="direction">The direction of the insight</param>
        /// <param name="period">The period over which the insight with come to fruition</param>
        public ConstantAlphaModel(InsightType type, InsightDirection direction, TimeSpan period)
            : this(type, direction, period, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantAlphaModel"/> class
        /// </summary>
        /// <param name="type">The type of insight</param>
        /// <param name="direction">The direction of the insight</param>
        /// <param name="period">The period over which the insight with come to fruition</param>
        /// <param name="magnitude">The predicted change in magnitude as a +- percentage</param>
        /// <param name="confidence">The confidence in the insight</param>
        public ConstantAlphaModel(InsightType type, InsightDirection direction, TimeSpan period, double? magnitude, double? confidence)
        {
            _type = type;
            _direction = direction;
            _period = period;

            // Optional
            _magnitude = magnitude;
            _confidence = confidence;

            _securities = new HashSet<Security>();
            _insightsTimeBySymbol = new Dictionary<Symbol, DateTime>();
        }

        /// <summary>
        /// Creates a constant insight for each security as specified via the constructor
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public IEnumerable<Insight> Update(QCAlgorithmFramework algorithm, Slice data)
        {
            foreach (var security in _securities)
            {
                if (ShouldEmitInsight(algorithm.UtcTime, security.Symbol))
                {
                    yield return new Insight(security.Symbol, _period, _type, _direction, _magnitude, _confidence);
                }
            }
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            NotifiedSecurityChanges.UpdateCollection(_securities, changes);

            // this will allow the insight to be re-sent when the security re-joins the universe
            foreach (var removed in changes.RemovedSecurities)
            {
                _insightsTimeBySymbol.Remove(removed.Symbol);
            }
        }

        private bool ShouldEmitInsight(DateTime utcTime, Symbol symbol)
        {
            DateTime generatedTimeUtc;
            if (_insightsTimeBySymbol.TryGetValue(symbol, out generatedTimeUtc))
            {
                // we previously emitted a insight for this symbol, check it's period to see
                // if we should emit another insight
                if (utcTime - generatedTimeUtc < _period)
                {
                    return false;
                }
            }

            // we either haven't emitted a insight for this symbol or the previous
            // insight's period has expired, so emit a new insight now for this symbol
            _insightsTimeBySymbol[symbol] = utcTime;
            return true;
        }
    }
}
