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

namespace QuantConnect.Data
{
    /// <summary>
    /// Provides convenient methods for holding several <see cref="SubscriptionDataConfig"/> 
    /// </summary>
    public class SubscriptionDataConfigList : List<SubscriptionDataConfig>
    {
        /// <summary>
        /// <see cref="Symbol"/> for which this class holds <see cref="SubscriptionDataConfig"/>
        /// </summary>
        public Symbol Symbol { get; private set; }

        /// <summary>
        /// Assume that the InternalDataFeed is the same for both <see cref="SubscriptionDataConfig"/>
        /// </summary>
        public bool IsInternalFeed
        {
            get
            {
                var first = this.FirstOrDefault();
                return first != null && first.IsInternalFeed;
            }
        }

        /// <summary>
        /// Default constructor that specifies the <see cref="Symbol"/> that the <see cref="SubscriptionDataConfig"/> represent
        /// </summary>
        /// <param name="symbol"></param>
        public SubscriptionDataConfigList(Symbol symbol)
        {
            Symbol = symbol;
        }

        /// <summary>
        /// Sets the <see cref="DataNormalizationMode"/> for all <see cref="SubscriptionDataConfig"/> contained in the list
        /// </summary>
        /// <param name="normalizationMode"></param>
        public void SetDataNormalizationMode(DataNormalizationMode normalizationMode)
        {
            if (Symbol.SecurityType == SecurityType.Option && normalizationMode != DataNormalizationMode.Raw)
            {
                throw new ArgumentException("DataNormalizationMode.Raw must be used with options");
            }

            foreach (var config in this)
            {
                config.DataNormalizationMode = normalizationMode;
            }
        }
    }
}
