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
using QuantConnect.Data;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Securities.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Implements a wrapper around a base data enumerator to provide a final filtering step
    /// </summary>
    public class SubscriptionFilterEnumerator : IEnumerator<BaseData>
    {
        /// <summary>
        /// Fired when there's an error executing a user's data filter
        /// </summary>
        public event EventHandler<Exception> DataFilterError;

        private readonly bool _liveMode;
        private readonly Security _security;
        private readonly DateTime _endTime;
        private readonly bool _extendedMarketHours;
        private readonly SecurityExchange _exchange;
        private readonly ISecurityDataFilter _dataFilter;
        private readonly IEnumerator<BaseData> _enumerator;

        /// <summary>
        /// Convenience method to wrap the enumerator and attach the data filter event to log and alery users of errors
        /// </summary>
        /// <param name="resultHandler">Result handler reference used to send errors</param>
        /// <param name="enumerator">The source enumerator to be wrapped</param>
        /// <param name="security">The security who's data is being enumerated</param>
        /// <param name="endTime">The end time of the subscription</param>
        /// <param name="extendedMarketHours">True if extended market hours are enabled</param>
        /// <param name="liveMode">True if live mode</param>
        /// <returns>A new instance of the <see cref="SubscriptionFilterEnumerator"/> class that has had it's <see cref="DataFilterError"/>
        /// event subscribed to to send errors to the result handler</returns>
        public static SubscriptionFilterEnumerator WrapForDataFeed(IResultHandler resultHandler, IEnumerator<BaseData> enumerator, Security security, DateTime endTime, bool extendedMarketHours, bool liveMode)
        {
            var filter = new SubscriptionFilterEnumerator(enumerator, security, endTime, extendedMarketHours, liveMode);
            filter.DataFilterError += (sender, exception) =>
            {
                Log.Error(exception, "WrapForDataFeed");
                resultHandler.RuntimeError("Runtime error applying data filter. Assuming filter pass: " + exception.Message, exception.StackTrace);
            };
            return filter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionFilterEnumerator"/> class
        /// </summary>
        /// <param name="enumerator">The source enumerator to be wrapped</param>
        /// <param name="security">The security containing an exchange and data filter</param>
        /// <param name="endTime">The end time of the subscription</param>
        /// <param name="extendedMarketHours">True if extended market hours are enabled</param>
        /// <param name="liveMode">True if live mode</param>
        public SubscriptionFilterEnumerator(IEnumerator<BaseData> enumerator, Security security, DateTime endTime, bool extendedMarketHours, bool liveMode)
        {
            _liveMode = liveMode;
            _enumerator = enumerator;
            _security = security;
            _endTime = endTime;
            _exchange = _security.Exchange;
            _dataFilter = _security.DataFilter;
            _extendedMarketHours = extendedMarketHours;
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public BaseData Current
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            while (_enumerator.MoveNext())
            {
                var current = _enumerator.Current;
                if (current != null)
                {
                    try
                    {
                        // execute user data filters
                        if (current.DataType != MarketDataType.Auxiliary && !_dataFilter.Filter(_security, current))
                        {
                            continue;
                        }
                    }
                    catch (Exception err)
                    {
                        OnDataFilterError(err);
                        continue;
                    }

                    // verify that the bar is within the exchange's market hours
                    if (current.DataType != MarketDataType.Auxiliary && !_exchange.IsOpenDuringBar(current.Time, current.EndTime, _extendedMarketHours))
                    {
                        if (_liveMode && !current.IsFillForward)
                        {
                            // TODO: replace for setting security.RealTimePrice not to modify security cache data directly
                            _security.SetMarketPrice(current);
                        }
                        continue;
                    }

                    // make sure we haven't passed the end
                    if (current.Time > _endTime)
                    {
                        return false;
                    }
                }

                Current = current;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            _enumerator.Dispose();
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
        public void Reset()
        {
            _enumerator.Reset();
        }

        /// <summary>
        /// Event invocated for the <see cref="DataFilterError"/> event
        /// </summary>
        /// <param name="exception">The exception that was thrown when trying to perform data filtering</param>
        private void OnDataFilterError(Exception exception)
        {
            var handler = DataFilterError;
            if (handler != null) handler(this, exception);
        }
    }
}