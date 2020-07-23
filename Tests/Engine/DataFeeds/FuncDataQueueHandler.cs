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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    /// <summary>
    /// Provides an implementation of <see cref="IDataQueueHandler"/> that can be specified
    /// via a function
    /// </summary>
    public class FuncDataQueueHandler : IDataQueueHandler
    {
        private readonly HashSet<SubscriptionDataConfig> _subscriptions;
        private readonly AggregationManager _aggregationManager;
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Gets the subscriptions configurations currently being managed by the queue handler
        /// </summary>
        public List<SubscriptionDataConfig> SubscriptionDataConfigs
        {
            get { lock (_subscriptions) return _subscriptions.ToList(); }
        }

        /// <summary>
        /// Gets the subscriptions Symbols currently being managed by the queue handler
        /// </summary>
        public List<Symbol> Subscriptions
        {
            get { lock (_subscriptions) return _subscriptions.Select(config => config.Symbol).Distinct().ToList(); }
        }

        /// <summary>
        /// Returns whether the data provider is connected
        /// </summary>
        /// <returns>true if the data provider is connected</returns>
        public bool IsConnected => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncDataQueueHandler"/> class
        /// </summary>
        /// <param name="getNextTicksFunction">The functional implementation to get ticks function</param>
        /// <param name="timeProvider">The time provider to use</param>
        public FuncDataQueueHandler(Func<FuncDataQueueHandler, IEnumerable<BaseData>> getNextTicksFunction, ITimeProvider timeProvider)
        {
            _subscriptions = new HashSet<SubscriptionDataConfig>();
            _cancellationTokenSource = new CancellationTokenSource();
            _aggregationManager = new TestAggregationManager(timeProvider);
            Task.Factory.StartNew(() =>
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        foreach (var baseData in getNextTicksFunction(this))
                        {
                            _aggregationManager.Update(baseData);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (exception is ObjectDisposedException)
                        {
                            return;
                        }
                        Log.Error(exception);
                    }
                    Thread.Sleep(1);
                }
            });
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            lock (_subscriptions) _subscriptions.Add(dataConfig);
            return _aggregationManager.Add(dataConfig, newDataAvailableHandler);
        }

        /// <summary>
        /// Removes the specified symbols to the subscription
        /// </summary>
        /// <param name="dataConfig">Job we're processing.</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            lock (_subscriptions) _subscriptions.Remove(dataConfig);
            _aggregationManager.Remove(dataConfig);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _aggregationManager.Dispose();
            _cancellationTokenSource.Dispose();
        }

        private class TestAggregationManager : AggregationManager
        {
            public TestAggregationManager(ITimeProvider timeProvider)
            {
                TimeProvider = timeProvider;
            }
        }
    }
}