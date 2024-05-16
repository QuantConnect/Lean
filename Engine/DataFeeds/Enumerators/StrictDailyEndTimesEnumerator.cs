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
using QuantConnect.Data;
using QuantConnect.Util;
using System.Collections;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Enumerator that will handle adjusting daily strict end times if appropriate
    /// </summary>
    public class StrictDailyEndTimesEnumerator : IEnumerator<BaseData>
    {
        private static readonly HashSet<Type> _types = new()
        {
            // the underlying could yield auxiliary data which we don't want to change
            typeof(TradeBar), typeof(QuoteBar), typeof(ZipEntryName), typeof(BaseDataCollection)
        };

        private readonly SecurityExchangeHours _securityExchange;
        private readonly IEnumerator<BaseData> _underlying;

        public BaseData Current => _underlying.Current;

        object IEnumerator.Current => _underlying.Current;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public StrictDailyEndTimesEnumerator(IEnumerator<BaseData> underlying, SecurityExchangeHours securityExchangeHours)
        {
            _underlying = underlying;
            _securityExchange = securityExchangeHours;
        }

        public bool MoveNext()
        {
            var result = _underlying.MoveNext();
            if (_underlying.Current != null && _types.Contains(_underlying.Current.GetType()))
            {
                LeanData.SetStrictEndTimes(_underlying.Current, _securityExchange);
            }
            return result;
        }

        public void Reset()
        {
            _underlying.Reset();
        }

        public void Dispose()
        {
            _underlying.Dispose();
        }
    }
}
