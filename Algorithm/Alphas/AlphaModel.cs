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

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Provides a base class for alpha models.
    /// </summary>
    public class AlphaModel : IAlphaModel, INamedModel
    {
        /// <summary>
        /// Defines a name for a framework model
        /// </summary>
        public virtual string Name { get; set; }

        public AlphaModel()
        {
            Name = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public virtual IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            throw new System.NotImplementedException("Types deriving from 'AlphaModel' must implement the 'IEnumerable<Insight> Update(QCAlgorithm, Slice) method.");
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public virtual void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
        }
    }
}