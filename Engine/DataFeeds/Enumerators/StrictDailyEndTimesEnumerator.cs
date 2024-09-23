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
        public BaseData Current { get; private set; }

        object IEnumerator.Current => Current;

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
            Current = null;
            bool result;
            do
            {
                result = _underlying.MoveNext();
                if (!result || !LeanData.UseDailyStrictEndTimes(_underlying.Current?.GetType()))
                {
                    break;
                }

                // before setting the strict daily end times, let's clone it because underlying enumerator (SubscriptionDataReader) might be using it
                var pontentialNewBar = _underlying.Current.Clone();
                if (LeanData.SetStrictEndTimes(pontentialNewBar, _securityExchange) && pontentialNewBar.EndTime >= _localStartTime)
                {
                    Current = pontentialNewBar;
                    break;
                }
            }
            while (true);

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
