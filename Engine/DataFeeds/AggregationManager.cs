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

using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    public class AggregationManager : IDataAggregator
    {
        private bool _isDisposed = false;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ConcurrentDictionary<Symbol, ConcurrentDictionary<SubscriptionDataConfig, IDataConsolidator>> _consolidators = new ConcurrentDictionary<Symbol, ConcurrentDictionary<SubscriptionDataConfig, IDataConsolidator>>();
        private readonly ITimeProvider _timeProvider;

        public AggregationManager()
        {
            _timeProvider = GetTimeProvider();
        }

        public IEnumerator<BaseData> Add(SubscriptionDataConfig config, EventHandler newDataAvailableHandler)
        {
            IDataConsolidator consolidator;
            var period = config.Resolution.ToTimeSpan();
            switch (config.Type.Name)
            {
                case nameof(QuoteBar):
                    consolidator = new TickQuoteBarConsolidator(period);
                    break;

                case nameof(TradeBar):
                    consolidator = new TickConsolidator(period);
                    break;

                case nameof(OpenInterest):
                    consolidator = new OpenInterestConsolidator(period);
                    break;

                case nameof(Tick):
                default:
                    // tick or streaming custom data subscriptions can pass right through
                    consolidator = new IdentityDataConsolidator<BaseData>();
                    break;
            }

            ScannableEnumerator<BaseData> enumerator = new ScannableEnumerator<BaseData>(
                consolidator, 
                config.DataTimeZone, 
                _timeProvider,
                newDataAvailableHandler);

            _consolidators.AddOrUpdate(
                config.Symbol,
                new ConcurrentDictionary<SubscriptionDataConfig, IDataConsolidator>() { [config] = consolidator },
                (k, v) => { v.AddOrUpdate(config, consolidator); return v; });

            return enumerator;
        }

        /// <summary>
        /// Removes the handler with the specified identifier
        /// </summary>
        /// <param name="dataConfig">Subscription data configuration to be removed</param>
        public bool Remove(SubscriptionDataConfig dataConfig)
        {
            ConcurrentDictionary<SubscriptionDataConfig, IDataConsolidator> consolidators;
            if (_consolidators.TryGetValue(dataConfig.Symbol, out consolidators))
            {
                if (consolidators.Count == 1)
                {
                    ConcurrentDictionary<SubscriptionDataConfig, IDataConsolidator> output;
                    return _consolidators.TryRemove(dataConfig.Symbol, out output);
                }
                else
                {
                    IDataConsolidator output;
                    return consolidators.TryRemove(dataConfig, out output);
                }
            }
            else
            {
                Log.Trace($"AggregationManager.Update(): IDataConsolidator for symbol ({dataConfig.Symbol.Value}) was not found.");
                return false;
            }
        }

        public void Update(Tick input)
        {
            ConcurrentDictionary<SubscriptionDataConfig, IDataConsolidator> consolidators;
            if (_consolidators.TryGetValue(input.Symbol, out consolidators))
            {
                foreach (var kvp in consolidators)
                {
                    if (kvp.Key.TickType == input.TickType)
                    {
                        var consolidator = kvp.Value;
                        consolidator.Update(input);
                    }
                }
            }
            else
            {
                Log.Trace($"AggregationManager.Update(): IDataConsolidator fot symbol ({input.Symbol.Value}) was not found.");
                return;
            }
        }

        protected virtual ITimeProvider GetTimeProvider()
        {
            return new RealTimeProvider();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}
