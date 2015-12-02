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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Historical datafeed stream reader for processing files on a local disk.
    /// </summary>
    /// <remarks>Filesystem datafeeds are incredibly fast</remarks>
    public class FileSystemDataFeed : IDataFeed
    {
        private IAlgorithm _algorithm;
        private IResultHandler _resultHandler;
        private Ref<TimeSpan> _fillForwardResolution;
        private SecurityChanges _changes = SecurityChanges.None;
        private IMapFileProvider _mapFileProvider;
        private ConcurrentDictionary<Symbol, Subscription> _subscriptions;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private BusyBlockingCollection<TimeSlice> _bridge;

        /// <summary>
        /// Event fired when the data feed encounters a universe selection subscripion
        /// This event should be bound to so consumers can perform the required actions
        /// such as adding and removing subscriptions to the data feed
        /// </summary>
        public event UniverseSelectionHandler UniverseSelection;

        /// <summary>
        /// Gets all of the current subscriptions this data feed is processing
        /// </summary>
        public IEnumerable<Subscription> Subscriptions
        {
            get { return _subscriptions.Select(x => x.Value); }
        }

        /// <summary>
        /// Flag indicating the hander thread is completely finished and ready to dispose.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Initializes the data feed for the specified job and algorithm
        /// </summary>
        public void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler, IMapFileProvider mapFileProvider)
        {
            if (algorithm.SubscriptionManager.Subscriptions.Count == 0 && algorithm.Universes.IsNullOrEmpty())
            {
                throw new Exception("No subscriptions registered and no universe defined.");
            }

            _algorithm = algorithm;
            _resultHandler = resultHandler;
            _mapFileProvider = mapFileProvider;
            _subscriptions = new ConcurrentDictionary<Symbol, Subscription>();
            _cancellationTokenSource = new CancellationTokenSource();

            IsActive = true;
            _bridge = new BusyBlockingCollection<TimeSlice>(100);

            var ffres = Time.OneSecond;
            _fillForwardResolution = Ref.Create(() => ffres, res => ffres = res);

            // find the minimum resolution, ignoring ticks
            ffres = ResolveFillForwardResolution(algorithm);

            // add each universe selection subscription to the feed
            foreach (var universe in _algorithm.Universes)
            {
                var startTimeUtc = _algorithm.StartDate.ConvertToUtc(_algorithm.TimeZone);
                var endTimeUtc = _algorithm.EndDate.ConvertToUtc(_algorithm.TimeZone);
                AddUniverseSubscription(universe, startTimeUtc, endTimeUtc);
            }
        }

        private Subscription CreateSubscription(Universe universe, IResultHandler resultHandler, Security security, DateTime startTimeUtc, DateTime endTimeUtc, IReadOnlyRef<TimeSpan> fillForwardResolution)
        {
            var config = security.SubscriptionDataConfig;
            var localStartTime = startTimeUtc.ConvertFromUtc(security.Exchange.TimeZone);
            var localEndTime = endTimeUtc.ConvertFromUtc(security.Exchange.TimeZone);

            var tradeableDates = Time.EachTradeableDay(security, localStartTime, localEndTime);

            // ReSharper disable once PossibleMultipleEnumeration
            if (!tradeableDates.Any())
            {
                _algorithm.Error(string.Format("No data loaded for {0} because there were no tradeable dates for this security.", security.Symbol));
                return null;
            }

            // get the map file resolver for this market
            var mapFileResolver = MapFileResolver.Empty;
            if (config.SecurityType == SecurityType.Equity) mapFileResolver = _mapFileProvider.Get(config.Market);

            // ReSharper disable once PossibleMultipleEnumeration
            IEnumerator<BaseData> enumerator = new SubscriptionDataReader(config, localStartTime, localEndTime, resultHandler, mapFileResolver, tradeableDates, false);

            // optionally apply fill forward logic, but never for tick data
            if (config.FillDataForward && config.Resolution != Resolution.Tick)
            {
                enumerator = new FillForwardEnumerator(enumerator, security.Exchange, fillForwardResolution,
                    security.IsExtendedMarketHours, localEndTime, config.Resolution.ToTimeSpan());
            }

            // finally apply exchange/user filters
            enumerator = SubscriptionFilterEnumerator.WrapForDataFeed(resultHandler, enumerator, security, localEndTime);
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(security.Exchange.TimeZone, startTimeUtc, endTimeUtc);
            var subscription = new Subscription(universe, security, enumerator, timeZoneOffsetProvider, startTimeUtc, endTimeUtc, false);
            return subscription;
        }

        /// <summary>
        /// Adds a new subscription to provide data for the specified security.
        /// </summary>
        /// <param name="universe">The universe the subscription is to be added to</param>
        /// <param name="security">The security to add a subscription for</param>
        /// <param name="utcStartTime">The start time of the subscription</param>
        /// <param name="utcEndTime">The end time of the subscription</param>
        public bool AddSubscription(Universe universe, Security security, DateTime utcStartTime, DateTime utcEndTime)
        {
            var subscription = CreateSubscription(universe, _resultHandler, security, utcStartTime, utcEndTime, _fillForwardResolution);
            if (subscription == null)
            {
                // subscription will be null when there's no tradeable dates for the security between the requested times, so
                // don't even try to load the data
                return false;
            }

            Log.Trace("FileSystemDataFeed.AddSubscription(): Added " + security.Symbol.ToString());

            _subscriptions.AddOrUpdate(subscription.Security.Symbol,  subscription);

            // prime the pump, run method checks current before move next calls
            PrimeSubscriptionPump(subscription, true);

            _changes += SecurityChanges.Added(security);

            _fillForwardResolution.Value = ResolveFillForwardResolution(_algorithm);

            return true;
        }

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="subscription">The subscription to be removed</param>
        public bool RemoveSubscription(Subscription subscription)
        {
            Subscription sub;
            if (!_subscriptions.TryRemove(subscription.Security.Symbol, out sub))
            {
                Log.Error("FileSystemDataFeed.RemoveSubscription(): Unable to remove: " + subscription.Security.Symbol.ToString());
                return false;
            }

            Log.Trace("FileSystemDataFeed.RemoveSubscription(): Removed " + subscription.Security.Symbol.ToString());

            _changes += SecurityChanges.Removed(sub.Security);

            _fillForwardResolution.Value = ResolveFillForwardResolution(_algorithm);
            return true;
        }

        /// <summary>
        /// Main routine for datafeed analysis.
        /// </summary>
        /// <remarks>This is a hot-thread and should be kept extremely lean. Modify with caution.</remarks>
        public void Run()
        {
            var frontier = DateTime.MaxValue;
            try
            {
                // compute initial frontier time
                frontier = GetInitialFrontierTime();

                var subscriptionSyncer = new SubscriptionSyncer();
                subscriptionSyncer.SubscriptionFinished += (sender, args) => _subscriptions.TryRemove(args.Security.Symbol, out args);
                subscriptionSyncer.UniverseSelection += (sender, args) =>
                {
                    // always wait for other thread
                    if (!_bridge.Wait(Timeout.Infinite, _cancellationTokenSource.Token))
                    {
                        return SecurityChanges.None;
                    }

                    return OnUniverseSelection(args);
                };

                Log.Trace(string.Format("FileSystemDataFeed.Run(): Begin: {0} UTC", frontier));
                // continue to loop over each subscription, enqueuing data in time order
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _changes = SecurityChanges.None;

                    // we union subscriptions with itself so if subscriptions changes on the first
                    // iteration we will pick up those changes in the union call, this is used in
                    // universe selection. an alternative is to extract this into a method and check
                    // to see if changes != SecurityChanges.None, and re-run all subscriptions again,
                    // This was added as quick fix due to an issue found in universe selection regression alg
                    var subscriptions = Subscriptions.Union(Subscriptions);

                    DateTime nextFrontier;
                    var timeSlice = subscriptionSyncer.Sync(frontier, subscriptions, _algorithm.TimeZone, _algorithm.Portfolio.CashBook, out nextFrontier);

                    if (nextFrontier == DateTime.MaxValue)
                    {
                        if (_changes == SecurityChanges.None)
                        {
                            // there's no more data to pull off, we're done
                            break;
                        }
                    }
                    else
                    {
                        // enqueue our next time slice and set the frontier for the next
                        _bridge.Add(timeSlice, _cancellationTokenSource.Token);
                        frontier = nextFrontier;
                    }
                }

                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _bridge.CompleteAdding();
                }
            }
            catch (Exception err)
            {
                Log.Error("FileSystemDataFeed.Run(): Encountered an error: " + err.Message); 
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _bridge.CompleteAdding();
                    _cancellationTokenSource.Cancel();
                }
            }
            finally
            {
                Log.Trace(string.Format("FileSystemDataFeed.Run(): Data Feed Completed at {0} UTC", frontier));
                
                //Close up all streams:
                foreach (var subscription in Subscriptions)
                {
                    subscription.Dispose();
                }

                Log.Trace("FileSystemDataFeed.Run(): Ending Thread... ");
                IsActive = false;
            }
        }

        private DateTime GetInitialFrontierTime()
        {
            var frontier = DateTime.MaxValue;
            foreach (var subscription in Subscriptions)
            {
                var current = subscription.Current;
                if (current == null)
                {
                    continue;
                }

                // we need to initialize both the frontier time and the offset provider, in order to do
                // this we'll first convert the current.EndTime to UTC time, this will allow us to correctly
                // determine the offset in ticks using the OffsetProvider, we can then use this to recompute
                // the UTC time. This seems odd, but is necessary given Noda time's lenient mapping, the
                // OffsetProvider exists to give forward marching mapping

                // compute the initial frontier time
                var currentEndTimeUtc = current.EndTime.ConvertToUtc(subscription.TimeZone);
                var endTime = current.EndTime.Ticks - subscription.OffsetProvider.GetOffsetTicks(currentEndTimeUtc);
                if (endTime < frontier.Ticks)
                {
                    frontier = new DateTime(endTime);
                }
            }

            if (frontier == DateTime.MaxValue)
            {
                frontier = _algorithm.StartDate.ConvertToUtc(_algorithm.TimeZone);
            }
            return frontier;
        }

        /// <summary>
        /// Adds a new subscription for universe selection
        /// </summary>
        /// <param name="universe">The universe to add a subscription for</param>
        /// <param name="startTimeUtc">The start time of the subscription in utc</param>
        /// <param name="endTimeUtc">The end time of the subscription in utc</param>
        public void AddUniverseSubscription(Universe universe, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            // TODO : Consider moving the creating of universe subscriptions to a separate, testable class

            // grab the relevant exchange hours
            var config = universe.Configuration;

            var exchangeHours = MarketHoursDatabase.FromDataFolder().GetExchangeHours(config);

            // create a canonical security object
            var security = new Security(exchangeHours, config, universe.SubscriptionSettings.Leverage);

            var localStartTime = startTimeUtc.ConvertFromUtc(security.Exchange.TimeZone);
            var localEndTime = endTimeUtc.ConvertFromUtc(security.Exchange.TimeZone);

            // define our data enumerator
            IEnumerator<BaseData> enumerator;

            var tradeableDates = Time.EachTradeableDay(security, localStartTime, localEndTime);

            var userDefined = universe as UserDefinedUniverse;
            if (userDefined != null)
            {
                // spoof a tick on the requested interval to trigger the universe selection function
                enumerator = LinqExtensions.Range(localStartTime, localEndTime, dt => dt + userDefined.Interval)
                    .Where(dt => security.Exchange.IsOpenDuringBar(dt, dt + userDefined.Interval, config.ExtendedMarketHours))
                    .Select(dt => new Tick {Time = dt}).GetEnumerator();
            }
            else
            {
                // normal reader for all others
                enumerator = new SubscriptionDataReader(config, localStartTime, localEndTime, _resultHandler, MapFileResolver.Empty, tradeableDates, false);
            }

            // create the subscription
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(security.Exchange.TimeZone, startTimeUtc, endTimeUtc);
            var subscription = new Subscription(universe, security, enumerator, timeZoneOffsetProvider, startTimeUtc, endTimeUtc, true);

            // only message the user if it's one of their universe types
            var messageUser = subscription.Configuration.Type != typeof(CoarseFundamental);
            PrimeSubscriptionPump(subscription, messageUser);
            _subscriptions.AddOrUpdate(subscription.Security.Symbol, subscription);
        }

        /// <summary>
        /// Send an exit signal to the thread.
        /// </summary>
        public void Exit()
        {
            Log.Trace("FileSystemDataFeed.Exit(): Exit triggered.");
            _cancellationTokenSource.Cancel();
            if (_bridge != null)
            {
                _bridge.Dispose();
            }
        }

        /// <summary>
        /// Calls move next on the subscription and logs if we didn't get any data (load failure)
        /// </summary>
        /// <param name="subscription">The subscription to prime</param>
        /// <param name="messageUser">True to send an algorithm.Error to the user</param>
        private void PrimeSubscriptionPump(Subscription subscription, bool messageUser)
        {
            if (!subscription.MoveNext())
            {
                Log.Error("FileSystemDataFeed.PrimeSubscriptionPump(): Failed to load subscription: " + subscription.Security.Symbol.ToString());
                if (messageUser)
                {
                    _algorithm.Error("Failed to load subscription: " + subscription.Security.Symbol.ToString());
                }
                _subscriptions.TryRemove(subscription.Security.Symbol, out subscription);
            }
        }

        /// <summary>
        /// Event invocator for the <see cref="UniverseSelection"/> event
        /// </summary>
        protected virtual SecurityChanges OnUniverseSelection(UniverseSelectionEventArgs universeSelectionEventArgs)
        {
            var changes = SecurityChanges.None;
            if (UniverseSelection != null)
            {
                var multicast = (MulticastDelegate) UniverseSelection;
                foreach (UniverseSelectionHandler handler in multicast.GetInvocationList())
                {
                    changes += handler(this, universeSelectionEventArgs);
                }
            }
            return changes;
        }


        private static TimeSpan ResolveFillForwardResolution(IAlgorithm algorithm)
        {
            return algorithm.SubscriptionManager.Subscriptions
                .Where(x => !x.IsInternalFeed)
                .Select(x => x.Resolution)
                .Union(algorithm.Universes.Select(x => x.SubscriptionSettings.Resolution))
                .Where(x => x != Resolution.Tick)
                .DefaultIfEmpty(Resolution.Second)
                .Min().ToTimeSpan();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<TimeSlice> GetEnumerator()
        {
            return _bridge.GetConsumingEnumerable(_cancellationTokenSource.Token).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
