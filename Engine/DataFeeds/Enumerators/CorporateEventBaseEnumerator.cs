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
    /// Base class for corporate event enumerators. Will call the <see cref="CheckNewEvent"/>
    /// implementation each time there is a new tradable day. Will merge new events produced with
    /// underlying enumerator data.
    /// </summary>
    public abstract class CorporateEventBaseEnumerator : IEnumerator<BaseData>
    {
        private readonly IEnumerator<BaseData> _enumerator;
        private readonly Queue<BaseData> _auxiliaryData;
        private bool _underlyingExhausted;
        private bool _weEmittedData;

        /// <summary>
        /// The previous data provided by the underlying enumerator
        /// </summary>
        protected BaseData PreviousUnderlyingData;

        /// <summary>
        ///The Subscription data config used by this enumerator
        /// </summary>
        protected readonly SubscriptionDataConfig SubscriptionDataConfig;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="enumerator">Underlying enumerator</param>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="tradableDayNotifier">Tradable dates provider</param>
        /// <param name="includeAuxiliaryData">True to emit auxiliary data</param>
        public CorporateEventBaseEnumerator(
            IEnumerator<BaseData> enumerator,
            SubscriptionDataConfig config,
            ITradableDatesNotifier tradableDayNotifier,
            bool includeAuxiliaryData)
        {
            SubscriptionDataConfig = config;
            _auxiliaryData = new Queue<BaseData>();
            _enumerator = enumerator;
            tradableDayNotifier.NewTradableDate += (sender, dateTime) =>
            {
                // Call implementation
                var newEvent = CheckNewEvent(dateTime);
                if (newEvent != null && includeAuxiliaryData)
                {
                    _auxiliaryData.Enqueue(newEvent);
                }
            };
        }

        /// <summary>
        /// Called each time there is a new tradable day
        /// </summary>
        /// <param name="date">The new tradable day value</param>
        /// <returns>New corporate event, else Null</returns>
        protected abstract BaseData CheckNewEvent(DateTime date);

        /// <summary>
        /// Advances the enumerator to the next element.
        /// Will merge chronologically new data produced by this enumerator
        /// with underlying enumerator.
        /// </summary>
        /// <returns>
        /// True if the enumerator was successfully advanced to the next element;
        /// </returns>
        public virtual bool MoveNext()
        {
            // save previous value
            PreviousUnderlyingData = _enumerator.Current;

            // if we emitted data we don't have to turn underlying enumerator
            // because we already did in previous step
            if (!_weEmittedData
                && !_underlyingExhausted
                && !_enumerator.MoveNext())
            {
                _underlyingExhausted = true;
            }

            _weEmittedData = false;
            if (_auxiliaryData.Any()
                && (_enumerator.Current == null // if underlying is null but we have data, emit
                    || _auxiliaryData.Peek().EndTime < _enumerator.Current.EndTime
                    || _underlyingExhausted))
            {
                _weEmittedData = true;
                Current = _auxiliaryData.Dequeue();
            }
            else
            {
                Current = _enumerator.Current;
            }

            return _weEmittedData || (!_underlyingExhausted && _enumerator.Current != null);
        }

        /// <summary>
        /// Dispose of the Stream Reader and close out the source stream and file connections.
        /// </summary>
        public void Dispose()
        {
            _enumerator.Dispose();
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
        protected decimal GetRawClose()
        {
            return PreviousUnderlyingData == null
                ? 0m
                : GetRawValue(
                    PreviousUnderlyingData.Value,
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
