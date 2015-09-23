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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Interfaces;
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
        private Resolution _fillForwardResolution;
        private SecurityChanges _changes = SecurityChanges.None;
        private ConcurrentDictionary<SymbolSecurityType, Subscription> _subscriptions;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Event fired when the data feed encounters new fundamental data
        /// </summary>
        public event EventHandler<FundamentalEventArgs> Fundamental;

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
        /// Cross-threading queue so the datafeed pushes data into the queue and the primary algorithm thread reads it out.
        /// </summary>
        public BusyBlockingCollection<TimeSlice> Bridge
        {
            get; private set;
        }

        public void Initialize(IAlgorithm algorithm, AlgorithmNodePacket job, IResultHandler resultHandler)
        {
            if (algorithm.SubscriptionManager.Subscriptions.Count == 0 && algorithm.Universe == null)
            {
                throw new Exception("No subscriptions registered and no universe defined.");
            }

            _algorithm = algorithm;
            _resultHandler = resultHandler;
            _subscriptions = new ConcurrentDictionary<SymbolSecurityType, Subscription>();
            _cancellationTokenSource = new CancellationTokenSource();

            IsActive = true;
            Bridge = new BusyBlockingCollection<TimeSlice>(100);

            // find the minimum resolution, ignoring ticks
            _fillForwardResolution = algorithm.SubscriptionManager.Subscriptions
                .Where(x => x.Resolution != Resolution.Tick)
                .Select(x => x.Resolution)
                .DefaultIfEmpty(algorithm.UniverseSettings.Resolution)
                .Min();

            // initialize the original user defined securities
            foreach (var security in _algorithm.Securities.Values)
            {
                var subscription = CreateSubscription(resultHandler, security, algorithm.StartDate, algorithm.EndDate, _fillForwardResolution, true);
                if (subscription != null)
                {
                    _subscriptions.AddOrUpdate(new SymbolSecurityType(security), subscription);

                    // prime the pump, run method checks current before move next calls
                    PrimeSubscriptionPump(subscription, true);
                }
            }
        }

        private Subscription CreateSubscription(IResultHandler resultHandler, Security security, DateTime start, DateTime end, Resolution fillForwardResolution, bool userDefined)
        {
            var config = security.SubscriptionDataConfig;
            var tradeableDates = Time.EachTradeableDay(security, start.Date, end.Date);

            // ReSharper disable once PossibleMultipleEnumeration
            if (!tradeableDates.Any())
            {
                if (userDefined)
                {
                    _algorithm.Error(string.Format("No data loaded for {0} because there were no tradeable dates for this security.", security.Symbol));
                }
                return null;
            }

            // ReSharper disable once PossibleMultipleEnumeration
            IEnumerator<BaseData> enumerator = new SubscriptionDataReader(config, start, end, resultHandler, tradeableDates, false);

            // optionally apply fill forward logic, but never for tick data
            if (config.FillDataForward && config.Resolution != Resolution.Tick)
            {
                enumerator = new FillForwardEnumerator(enumerator, security.Exchange, fillForwardResolution.ToTimeSpan(), 
                    security.IsExtendedMarketHours, end, config.Resolution.ToTimeSpan());
            }

            // finally apply exchange/user filters
            enumerator = SubscriptionFilterEnumerator.WrapForDataFeed(resultHandler, enumerator, security, end);
            var subscription = new Subscription(security, enumerator, start, end, userDefined, false);
            return subscription;
        }

        /// <summary>
        /// Adds a new subscription to provide data for the specified security.
        /// </summary>
        /// <param name="security">The security to add a subscription for</param>
        /// <param name="utcStartTime">The start time of the subscription</param>
        /// <param name="utcEndTime">The end time of the subscription</param>
        public void AddSubscription(Security security, DateTime utcStartTime, DateTime utcEndTime)
        {
            var subscription = CreateSubscription(_resultHandler, security, utcStartTime, utcEndTime, security.SubscriptionDataConfig.Resolution, false);
            if (subscription == null)
            {
                // subscription will be null when there's no tradeable dates for the security between the requested times, so
                // don't even try to load the data
                return;
            }
            _subscriptions.AddOrUpdate(new SymbolSecurityType(subscription),  subscription);

            // prime the pump, run method checks current before move next calls
            PrimeSubscriptionPump(subscription, true);

            _changes += new SecurityChanges(new List<Security> {security}, new List<Security>());
        }

        /// <summary>
        /// Removes the subscription from the data feed, if it exists
        /// </summary>
        /// <param name="security">The security to remove subscriptions for</param>
        public void RemoveSubscription(Security security)
        {
            Subscription subscription;
            if (!_subscriptions.TryRemove(new SymbolSecurityType(security), out subscription))
            {
                Log.Error("FileSystemDataFeed.RemoveSubscription(): Unable to remove: " + security.Symbol);
            }

            _changes += new SecurityChanges(new List<Security>(), new List<Security> {security});
        }

        /// <summary>
        /// Main routine for datafeed analysis.
        /// </summary>
        /// <remarks>This is a hot-thread and should be kept extremely lean. Modify with caution.</remarks>
        public void Run()
        {
            var universeSelectionMarkets = new List<string> {"usa"};
            var frontier = DateTime.MaxValue;
            try
            {
                // don't initialize universe selection if it's not requested
                if (_algorithm.Universe != null)
                {
                    // initialize subscriptions used for universe selection
                    foreach (var market in universeSelectionMarkets)
                    {
                        AddSubscriptionForUniverseSelectionMarket(market);
                    }
                }

                // compute initial frontier time
                frontier = GetInitialFrontierTime();

                Log.Trace(string.Format("FileSystemDataFeed.Run(): Begin: {0} UTC", frontier));
                // continue to loop over each subscription, enqueuing data in time order
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    // each time step reset our security changes
                    _changes = SecurityChanges.None;
                    var earlyBirdTicks = long.MaxValue;
                    var data = new List<KeyValuePair<Security, List<BaseData>>>();

                    // we union subscriptions with itself so if subscriptions changes on the first
                    // iteration we will pick up those changes in the union call, this is used in
                    // universe selection. an alternative is to extract this into a method and check
                    // to see if changes != SecurityChanges.None, and re-run all subscriptions again,
                    // This was added as quick fix due to an issue found in universe selection regression alg
                    foreach (var subscription in Subscriptions.Union(Subscriptions))
                    {
                        if (subscription.EndOfStream)
                        {
                            // skip subscriptions that are finished
                            continue;
                        }

                        var cache = new KeyValuePair<Security, List<BaseData>>(subscription.Security, new List<BaseData>());
                        data.Add(cache);

                        var configuration = subscription.Configuration;
                        var offsetProvider = subscription.OffsetProvider;
                        var currentOffsetTicks = offsetProvider.GetOffsetTicks(frontier);
                        while (subscription.Current.EndTime.Ticks - currentOffsetTicks <= frontier.Ticks)
                        {
                            // we want bars rounded using their subscription times, we make a clone
                            // so we don't interfere with the enumerator's internal logic
                            var clone = subscription.Current.Clone(subscription.Current.IsFillForward);
                            clone.Time = clone.Time.ExchangeRoundDown(configuration.Increment, subscription.Security.Exchange.Hours, configuration.ExtendedMarketHours);
                            cache.Value.Add(clone);
                            if (!subscription.MoveNext())
                            {
                                Log.Trace("FileSystemDataFeed.Run(): Finished subscription: " + subscription.Security.Symbol + " at " + frontier + " UTC");
                                break;
                            }
                        }

                        // we have new universe data to select based on
                        if (subscription.IsFundamentalSubscription && cache.Value.Count > 0)
                        {
                            // always wait for other thread
                            if (!Bridge.Wait(Timeout.Infinite, _cancellationTokenSource.Token))
                            {
                                break;
                            }
                            
                            OnFundamental(FundamentalType.Coarse, frontier, configuration, cache.Value);
                        }

                        if (subscription.Current != null)
                        {
                            // take the earliest between the next piece of data or the next tz discontinuity
                            earlyBirdTicks = Math.Min(earlyBirdTicks, Math.Min(subscription.Current.EndTime.Ticks - currentOffsetTicks, offsetProvider.GetNextDiscontinuity()));
                        }
                    }

                    if (earlyBirdTicks == long.MaxValue)
                    {
                        // there's no more data to pull off, we're done
                        break;
                    }

                    // enqueue our next time slice and set the frontier for the next
                    Bridge.Add(TimeSlice.Create(frontier, _algorithm.TimeZone, _algorithm.Portfolio.CashBook, data, _changes), _cancellationTokenSource.Token);

                    // never go backwards in time, so take the max between early birds and the current frontier
                    frontier = new DateTime(Math.Max(earlyBirdTicks, frontier.Ticks), DateTimeKind.Utc);
                }

                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    Bridge.CompleteAdding();
                }
            }
            catch (Exception err)
            {
                Log.Error("FileSystemDataFeed.Run(): Encountered an error: " + err.Message); 
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    Bridge.CompleteAdding();
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

        private void AddSubscriptionForUniverseSelectionMarket(string market)
        {
            var exchangeHours = SecurityExchangeHoursProvider.FromDataFolder().GetExchangeHours(market, null, SecurityType.Equity);
            var symbolName = new Symbol(market + "-coarse");
            var subscriptionDataConfig = new SubscriptionDataConfig(typeof (CoarseFundamental), SecurityType.Equity, symbolName, Resolution.Daily, market, exchangeHours.TimeZone,
                true, false, true, false);
            var security = new Security(exchangeHours, subscriptionDataConfig, 1);
            
            var cf = new CoarseFundamental();
            var list = new List<BaseData>();
            foreach (var date in Time.EachTradeableDay(security, _algorithm.StartDate, _algorithm.EndDate))
            {
                var factory = new BaseDataSubscriptionFactory(subscriptionDataConfig, date, false);
                var source = cf.GetSource(subscriptionDataConfig, date, false);
                var coarseFundamentalForDate = factory.Read(source);
                list.AddRange(coarseFundamentalForDate);
            }


            // spoof a subscription for the market that emits at midnight of each tradeable day
            var subscription = new Subscription(security,
                list.GetEnumerator(),
                _algorithm.StartDate.ConvertToUtc(exchangeHours.TimeZone),
                _algorithm.EndDate.ConvertToUtc(exchangeHours.TimeZone),
                false,
                true
                );

            // let user know if we fail to load the universe subscription, very important for when understanding backtest results!
            PrimeSubscriptionPump(subscription, true);
            _subscriptions.AddOrUpdate(new SymbolSecurityType(subscription), subscription);
        }

        /// <summary>
        /// Send an exit signal to the thread.
        /// </summary>
        public void Exit()
        {
            Log.Trace("FileSystemDataFeed.Exit(): Exit triggered.");
            _cancellationTokenSource.Cancel();
            if (Bridge != null)
            {
                Bridge.Dispose();
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
                Log.Error("FileSystemDataFeed.PrimeSubscriptionPump(): Failed to load subscription: " + subscription.Security.Symbol);
                if (messageUser)
                {
                    _algorithm.Error("Failed to load subscription: " + subscription.Security.Symbol);
                }
                _subscriptions.TryRemove(new SymbolSecurityType(subscription), out subscription);
            }
        }

        /// <summary>
        /// Event invocator for the <see cref="Fundamental"/> event
        /// </summary>
        protected virtual void OnFundamental(FundamentalType fundamentalType, DateTime dateTimeUtc, SubscriptionDataConfig configuration, IReadOnlyList<BaseData> data)
        {
            var handler = Fundamental;
            if (handler != null) handler(this, new FundamentalEventArgs(fundamentalType, configuration, dateTimeUtc, data));
        }
    }
}
