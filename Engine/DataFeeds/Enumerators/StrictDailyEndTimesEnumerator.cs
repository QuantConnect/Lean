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
using QuantConnect.Data.Auxiliary;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Enumerator that will handle adjusting daily strict end times if appropriate
    /// </summary>
    public class StrictDailyEndTimesEnumerator : IEnumerator<BaseData>
    {
        private readonly DateTime _localStartTime;
        private readonly SecurityExchangeHours _securityExchange;
        private readonly IEnumerator<BaseData> _underlying;

        /// <summary>
        /// Current value of the enumerator
        /// </summary>
        public BaseData Current => _underlying.Current;

        object IEnumerator.Current => _underlying.Current;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public StrictDailyEndTimesEnumerator(IEnumerator<BaseData> underlying, SecurityExchangeHours securityExchangeHours, DateTime localStartTime)
        {
            _underlying = underlying;
            _localStartTime = localStartTime;
            _securityExchange = securityExchangeHours;
        }

        /// <summary>
        /// Move to the next date
        /// </summary>
        public bool MoveNext()
        {
            bool result;
            do
            {
                result = _underlying.MoveNext();
                if (LeanData.UseDailyStrictEndTimes(_underlying.Current))
                {
                    if (_underlying.Current.GetType() == typeof(ZipEntryName) && _underlying.Current.Time.Hour == 0)
                    {
                        // zip entry names are emitted point in time for a date, see BaseDataSubscriptionEnumeratorFactory. When setting the strict end times
                        // we will move it to the previous day daily times, because daily market data on disk end time is midnight next day, so here we add 1 day
                        _underlying.Current.Time += Time.OneDay;
                    }
                    LeanData.SetStrictEndTimes(_underlying.Current, _securityExchange);
                }
            }
            while (result && _underlying.Current != null && _localStartTime > _underlying.Current.EndTime);

            return result;
        }

        /// <summary>
        /// Reset the enumerator
        /// </summary>
        public void Reset()
        {
            _underlying.Reset();
        }

        /// <summary>
        /// Dispose the enumerator
        /// </summary>
        public void Dispose()
        {
            _underlying.Dispose();
        }
    }
}
