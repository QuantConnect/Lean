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
 *
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Base class for corporate event enumerators. Will call the <see cref="GetCorporateEvents"/>
    /// implementation each time there is a new tradable day.
    /// </summary>
    public abstract class CorporateEventBaseEnumerator : IEnumerator<BaseData>
    {
        private readonly Queue<BaseData> _auxiliaryData;

        /// <summary>
        ///The Subscription data config used by this enumerator
        /// </summary>
        protected readonly SubscriptionDataConfig SubscriptionDataConfig;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="tradableDayNotifier">Tradable dates provider</param>
        /// <param name="includeAuxiliaryData">True to emit auxiliary data</param>
        protected CorporateEventBaseEnumerator(
            SubscriptionDataConfig config,
            ITradableDatesNotifier tradableDayNotifier,
            bool includeAuxiliaryData)
        {
            SubscriptionDataConfig = config;
            _auxiliaryData = new Queue<BaseData>();
            tradableDayNotifier.NewTradableDate += (sender, eventArgs) =>
            {
                // Call implementation
                var newEvents = GetCorporateEvents(eventArgs);
                if (includeAuxiliaryData)
                {
                    foreach (var newEvent in newEvents)
                    {
                        _auxiliaryData.Enqueue(newEvent);
                    }
                }
            };
        }

        /// <summary>
        /// Called each time there is a new tradable day
        /// </summary>
        /// <param name="eventArgs">The new tradable day event arguments</param>
        /// <returns>New corporate event, else Null</returns>
        protected abstract IEnumerable<BaseData> GetCorporateEvents(NewTradableDateEventArgs eventArgs);

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns>Always true</returns>
        public virtual bool MoveNext()
        {
            Current = _auxiliaryData.Any() ? _auxiliaryData.Dequeue() : null;
            return true;
        }

        /// <summary>
        /// Dispose of the Stream Reader and close out the source stream and file connections.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Reset the IEnumeration
        /// </summary>
        /// <remarks>Not used</remarks>
        public void Reset()
        {
            throw new NotImplementedException("Reset method not implemented. Assumes loop will only be used once.");
        }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Last read BaseData object from this type and source
        /// </summary>
        public BaseData Current
        {
            get;
            private set;
        }

        /// <summary>
        /// Un-normalizes the PreviousUnderlyingData.Value
        /// </summary>
        protected decimal GetRawClose(decimal price)
        {
            return GetRawValue(price,
                    SubscriptionDataConfig.SumOfDividends,
                    SubscriptionDataConfig.PriceScaleFactor);
        }

        /// <summary>
        /// Un-normalizes a price
        /// </summary>
        private decimal GetRawValue(decimal price, decimal sumOfDividends, decimal priceScaleFactor)
        {
            switch (SubscriptionDataConfig.DataNormalizationMode)
            {
                case DataNormalizationMode.Raw:
                    break;

                case DataNormalizationMode.SplitAdjusted:
                case DataNormalizationMode.Adjusted:
                    // we need to 'unscale' the price
                    price = price / priceScaleFactor;
                    break;

                case DataNormalizationMode.TotalReturn:
                    // we need to remove the dividends since we've been accumulating them in the price
                    price = (price - sumOfDividends) / priceScaleFactor;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return price;
        }
    }
}
