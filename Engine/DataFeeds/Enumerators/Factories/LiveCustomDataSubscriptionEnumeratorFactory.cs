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
using Python.Runtime;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Provides an implementation of <see cref="ISubscriptionEnumeratorFactory"/> to handle live custom data.
    /// </summary>
    public class LiveCustomDataSubscriptionEnumeratorFactory : ISubscriptionEnumeratorFactory
    {
        // when the expected universe file is not available yet, we fall back to the backup universe file ("*.backup"),
        // if any, as a last resort, when the market is open or within this time span before the next market open
        private static readonly TimeSpan UniverseFileBackupFallbackWindow =
            TimeSpan.FromMinutes(Config.GetInt("universe-file-backup-fallback-minutes", 30));

        private readonly TimeSpan _minimumIntervalCheck;
        private readonly ITimeProvider _timeProvider;
        private readonly Func<DateTime, DateTime> _dateAdjustment;
        private readonly bool _fallBackToBackupUniverseFiles;
        private readonly IObjectStore _objectStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiveCustomDataSubscriptionEnumeratorFactory"/> class
        /// </summary>
        /// <param name="timeProvider">Time provider from data feed</param>
        /// <param name="objectStore">The object store to use</param>
        /// <param name="dateAdjustment">Func that allows adjusting the datetime to use</param>
        /// <param name="minimumIntervalCheck">Allows specifying the minimum interval between each enumerator refresh and data check, default is 30 minutes</param>
        /// <param name="fallBackToBackupUniverseFiles">Whether to fall back to the backup universe file ("*.backup"), if any, as a last resort
        /// when the expected universe file is not available and the market is open or close to opening.
        /// Only meaningful for universe subscriptions backed by local files</param>
        public LiveCustomDataSubscriptionEnumeratorFactory(ITimeProvider timeProvider, IObjectStore objectStore,
            Func<DateTime, DateTime> dateAdjustment = null, TimeSpan? minimumIntervalCheck = null,
            bool fallBackToBackupUniverseFiles = false)
        {
            _timeProvider = timeProvider;
            _dateAdjustment = dateAdjustment;
            _minimumIntervalCheck = minimumIntervalCheck ?? TimeSpan.FromMinutes(30);
            _fallBackToBackupUniverseFiles = fallBackToBackupUniverseFiles;
            _objectStore = objectStore;
        }

        /// <summary>
        /// Creates an enumerator to read the specified request.
        /// </summary>
        /// <param name="request">The subscription request to be read</param>
        /// <param name="dataProvider">Provider used to get data when it is not present on disk</param>
        /// <returns>An enumerator reading the subscription request</returns>
        public IEnumerator<BaseData> CreateEnumerator(SubscriptionRequest request, IDataProvider dataProvider)
        {
            var config = request.Configuration;

            // frontier value used to prevent emitting duplicate time stamps between refreshed enumerators
            // also provides some immediate fast-forward to handle spooling through remote files quickly
            var frontier = Ref.Create(_dateAdjustment?.Invoke(request.StartTimeLocal) ?? request.StartTimeLocal);
            var lastSourceRefreshTime = DateTime.MinValue;
            var sourceFactory = config.GetBaseDataInstance();
            var sourceAdjustment = _fallBackToBackupUniverseFiles ? GetUniverseFileBackupSourceAdjustment(request, dataProvider) : null;

            // this is refreshing the enumerator stack for each new source
            var refresher = new RefreshEnumerator<BaseData>(() =>
            {
                // rate limit the refresh of this enumerator stack
                var utcNow = _timeProvider.GetUtcNow();
                var minimumTimeBetweenCalls = GetMinimumTimeBetweenCalls(config.Increment, _minimumIntervalCheck);
                if (utcNow - lastSourceRefreshTime < minimumTimeBetweenCalls)
                {
                    return Enumerable.Empty<BaseData>().GetEnumerator();
                }

                lastSourceRefreshTime = utcNow;
                var localDate = _dateAdjustment?.Invoke(utcNow.ConvertFromUtc(config.ExchangeTimeZone).Date) ?? utcNow.ConvertFromUtc(config.ExchangeTimeZone).Date;
                var source = sourceFactory.GetSource(config, localDate, true);
                if (source == null)
                {
                    // a null source is equivalent to an unreachable source: no data this cycle, retry on the next refresh
                    return Enumerable.Empty<BaseData>().GetEnumerator();
                }

                if (sourceAdjustment != null)
                {
                    source = sourceAdjustment(source, utcNow);
                }

                // fetch the new source and enumerate the data source reader
                var enumerator = EnumerateDataSourceReader(config, dataProvider, frontier, source, localDate, sourceFactory);

                if (SourceRequiresFastForward(source))
                {
                    // The FastForwardEnumerator implements these two features:
                    // (1) make sure we never emit past data
                    // (2) data filtering based on a maximum data age
                    // For custom data we don't want feature (2) because we would reject data points emitted later
                    // (e.g. Quandl daily data after a weekend), so we disable it using a huge maximum data age.

                    // apply fast forward logic for file transport mediums
                    var maximumDataAge = GetMaximumDataAge(Time.MaxTimeSpan);
                    enumerator = new FastForwardEnumerator(enumerator, _timeProvider, config.ExchangeTimeZone, maximumDataAge);
                }
                else
                {
                    // rate limit calls to this enumerator stack
                    enumerator = new RateLimitEnumerator<BaseData>(enumerator, _timeProvider, minimumTimeBetweenCalls);
                }

                if (source.Format == FileFormat.UnfoldingCollection)
                {
                    // unroll collections into individual data points after fast forward/rate limiting applied
                    enumerator = enumerator.SelectMany(data =>
                    {
                        var collection = data as BaseDataCollection;
                        IEnumerator<BaseData> collectionEnumerator;
                        if (collection != null)
                        {
                            if (source.TransportMedium == SubscriptionTransportMedium.Rest || source.TransportMedium == SubscriptionTransportMedium.RemoteFile)
                            {
                                // we want to make sure the data points we *unroll* are not past
                                collectionEnumerator = collection.Data
                                    .Where(baseData => baseData.EndTime > frontier.Value)
                                    .GetEnumerator();
                            }
                            else
                            {
                                collectionEnumerator = collection.Data.GetEnumerator();
                            }
                        }
                        else
                        {
                            collectionEnumerator = new List<BaseData> { data }.GetEnumerator();
                        }
                        return collectionEnumerator;
                    });
                }

                return enumerator;
            });

            return refresher;
        }

        private IEnumerator<BaseData> EnumerateDataSourceReader(SubscriptionDataConfig config, IDataProvider dataProvider, Ref<DateTime> localFrontier, SubscriptionDataSource source, DateTime localDate, BaseData baseDataInstance)
        {
            using (var dataCacheProvider = new SingleEntryDataCacheProvider(dataProvider))
            {
                var newLocalFrontier = localFrontier.Value;
                var dataSourceReader = GetSubscriptionDataSourceReader(source, dataCacheProvider, config, localDate, baseDataInstance, dataProvider);
                using var subscriptionEnumerator = SortEnumerator<DateTime>.TryWrapSortEnumerator(source.Sort, dataSourceReader.Read(source));
                foreach (var datum in subscriptionEnumerator)
                {
                    // always skip past all times emitted on the previous invocation of this enumerator
                    // this allows data at the same time from the same refresh of the source while excluding
                    // data from different refreshes of the source
                    if (datum != null && datum.EndTime > localFrontier.Value)
                    {
                        yield return datum;
                    }
                    else if (!SourceRequiresFastForward(source))
                    {
                        // if the 'source' is Rest and there is no new value,
                        // we *break*, else we will be caught in a tight loop
                        // because Rest source never ends!
                        // edit: we 'break' vs 'return null' so that the source is refreshed
                        // allowing date changes to impact the source value
                        // note it will respect 'minimumTimeBetweenCalls'
                        break;
                    }

                    if (datum != null)
                    {
                        newLocalFrontier = Time.Max(datum.EndTime, newLocalFrontier);

                        if (!SourceRequiresFastForward(source))
                        {
                            // if the 'source' is Rest we need to update the localFrontier here
                            // because Rest source never ends!
                            // Should be advance frontier for all source types here?
                            localFrontier.Value = newLocalFrontier;
                        }
                    }
                }

                localFrontier.Value = newLocalFrontier;
            }
        }

        /// <summary>
        /// Gets the <see cref="ISubscriptionDataSourceReader"/> for the specified source
        /// </summary>
        protected virtual ISubscriptionDataSourceReader GetSubscriptionDataSourceReader(SubscriptionDataSource source,
            IDataCacheProvider dataCacheProvider,
            SubscriptionDataConfig config,
            DateTime date,
            BaseData baseDataInstance,
            IDataProvider dataProvider
            )
        {
            return SubscriptionDataSourceReader.ForSource(source, dataCacheProvider, config, date, true, baseDataInstance, dataProvider, _objectStore);
        }

        /// <summary>
        /// Gets a source adjustment for universe files as a safety net for when the expected universe file
        /// is not available yet: when the market is open or close to opening (within <see cref="UniverseFileBackupFallbackWindow"/>
        /// of the next market open), it falls back to the backup universe file ("*.backup") if present, as a last resort.
        /// It is evaluated at the same cadence as the enumerator refreshes
        /// </summary>
        private static Func<SubscriptionDataSource, DateTime, SubscriptionDataSource> GetUniverseFileBackupSourceAdjustment(
            SubscriptionRequest request, IDataProvider dataProvider)
        {
            var exchangeHours = request.Security.Exchange.Hours;
            return (source, utcNow) =>
            {
                if (source.TransportMedium != SubscriptionTransportMedium.LocalFile)
                {
                    return source;
                }

                var localTime = utcNow.ConvertFromUtc(exchangeHours.TimeZone);
                // only fall back when the market is open or close to opening, when the expected universe file should already be available
                if (!exchangeHours.IsOpen(localTime, extendedMarketHours: false)
                    // if the market is closed, GetNextMarketOpen returns the next day open
                    && exchangeHours.GetNextMarketOpen(localTime, extendedMarketHours: false) - localTime > UniverseFileBackupFallbackWindow)
                {
                    return source;
                }

                if (CanFetchDataSource(dataProvider, source))
                {
                    return source;
                }

                var backupSource = new SubscriptionDataSource(source.Source + ".backup", source.TransportMedium, source.Format);
                if (CanFetchDataSource(dataProvider, backupSource))
                {
                    Log.Trace($"LiveCustomDataSubscriptionEnumeratorFactory.GetUniverseFileBackupSourceAdjustment(): universe file '{source.Source}' is not available, " +
                        $"falling back to backup universe file '{backupSource.Source}'");
                    return backupSource;
                }

                return source;
            };
        }

        private static bool CanFetchDataSource(IDataProvider dataProvider, SubscriptionDataSource source)
        {
            using var stream = dataProvider.Fetch(source.Source);
            return stream != null;
        }

        private bool SourceRequiresFastForward(SubscriptionDataSource source)
        {
            return source.TransportMedium == SubscriptionTransportMedium.LocalFile
                || source.TransportMedium == SubscriptionTransportMedium.RemoteFile;
        }

        private static TimeSpan GetMinimumTimeBetweenCalls(TimeSpan increment, TimeSpan minimumInterval)
        {
            return TimeSpan.FromTicks(Math.Min(increment.Ticks, minimumInterval.Ticks));
        }

        private static TimeSpan GetMaximumDataAge(TimeSpan increment)
        {
            return TimeSpan.FromTicks(Math.Max(increment.Ticks, TimeSpan.FromSeconds(5).Ticks));
        }
    }
}
