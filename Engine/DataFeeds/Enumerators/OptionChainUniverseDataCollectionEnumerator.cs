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
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Enumerates data into <see cref="OptionChainUniverseDataCollection"/> instances
    /// </summary>
    /// <remarks>Used in backtest mode <see cref="OptionChainUniverseSubscriptionEnumeratorFactory"/></remarks>
    public class OptionChainUniverseDataCollectionEnumerator : BaseDataCollectionAggregatorEnumerator<OptionChainUniverseDataCollection>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChainUniverseDataCollectionEnumerator"/> class
        /// </summary>
        /// <param name="enumerator">The enumerator to aggregate</param>
        /// <param name="symbol">The output data's symbol</param>
        public OptionChainUniverseDataCollectionEnumerator(IEnumerator<BaseData> enumerator, Symbol symbol)
            : base(enumerator, symbol)
        {
        }

        /// <summary>
        /// Adds the specified instance of <see cref="BaseData"/> to the current collection
        /// </summary>
        /// <param name="collection">The collection to be added to</param>
        /// <param name="current">The data to be added</param>
        protected override void Add(OptionChainUniverseDataCollection collection, BaseData current)
        {
            var baseDataCollection = current as BaseDataCollection;
            if (baseDataCollection != null)
            {
                if (baseDataCollection.Data.Count > 1)
                    SetData(collection, baseDataCollection.Data);

                else if (baseDataCollection.Data.Count == 1)
                    collection.Underlying = baseDataCollection.Data[0];
            }
        }

        /// <summary>
        /// Determines if a given data point is valid and can be emitted
        /// </summary>
        /// <param name="collection">The collection to be emitted</param>
        /// <returns>True if its a valid data point</returns>
        protected override bool IsValid(OptionChainUniverseDataCollection collection)
        {
            return collection?.Underlying != null && collection.Data.Count != 0;
        }
    }
}