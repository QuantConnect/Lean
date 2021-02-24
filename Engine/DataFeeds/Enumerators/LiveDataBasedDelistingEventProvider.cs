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
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Delisting event provider implementation which will source the delisting date based on the incoming data point
    /// </summary>
    /// <remarks>This is useful for equities for which we don't know the delisting date upfront</remarks>
    public class LiveDataBasedDelistingEventProvider : DelistingEventProvider, IDisposable
    {
        private readonly SubscriptionDataConfig _dataConfig;
        private readonly IDataQueueHandler _dataQueueHandler;
        private readonly IEnumerator<BaseData> _delistingEnumerator;
        
        /// <summary>
        /// Creates a new instance
        /// </summary>
        public LiveDataBasedDelistingEventProvider(SubscriptionDataConfig dataConfig, IDataQueueHandler dataQueueHandler)
        {
            _dataConfig = new SubscriptionDataConfig(dataConfig, typeof(Delisting));

            _dataQueueHandler = dataQueueHandler;
            _delistingEnumerator = dataQueueHandler.Subscribe(_dataConfig, (sender, args) =>
            {
                if (_delistingEnumerator != null && _delistingEnumerator.MoveNext())
                {
                    // live trading always returns true but could be null
                    if (_delistingEnumerator.Current != null)
                    {
                        var delisting = _delistingEnumerator.Current as Delisting;
                        if (delisting != null)
                        {
                            // we set the delisting date!
                            DelistingDate = new ReferenceWrapper<DateTime>(delisting.Time);
                        }
                        else
                        {
                            Log.Error($"LiveDataBasedDelistingEventProvider(): Current is not a {nameof(Delisting)} event: {_delistingEnumerator.Current?.GetType()}");
                        }
                    }
                }
                else
                {
                    Log.Error("LiveDataBasedDelistingEventProvider(): new data available triggered with no data");
                }
            });
        }

        /// <summary>
        /// Clean up
        /// </summary>
        public void Dispose()
        {
            _dataQueueHandler.Unsubscribe(_dataConfig);
            _delistingEnumerator.DisposeSafely();
        }
    }
}
