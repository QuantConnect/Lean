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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Signals;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of <see cref="IPortfolioConstructionModel"/> that gives equal weighting to all
    /// securities. The target percent holdings of each security is 1/N where N is the number of securities. For
    /// signals of direction <see cref="SignalDirection.Up"/>, long targets are returned and for signals of direction
    /// <see cref="SignalDirection.Down"/>, short targets are returned.
    /// </summary>
    public class SimplePortfolioConstructionModel : IPortfolioConstructionModel
    {
        private readonly HashSet<Security> _securities = new HashSet<Security>();

        /// <summary>
        /// Create portfolio targets from the specified signals
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="signals">The signals to create portoflio targets from</param>
        /// <returns>An enumerable of portoflio targets to be sent to the execution model</returns>
        public IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithmFramework algorithm, IEnumerable<Signal> signals)
        {
            if (_securities.Count == 0)
            {
                return Enumerable.Empty<IPortfolioTarget>();
            }

            // give equal weighting to each security
            var percent = 1m / _securities.Count;
            return signals.Select(signal =>
                PortfolioTarget.Percent(algorithm, signal.Symbol, (int) signal.Direction * percent)
            );
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            NotifiedSecurityChanges.UpdateCollection(_securities, changes);
        }
    }
}