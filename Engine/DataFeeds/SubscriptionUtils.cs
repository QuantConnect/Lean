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
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Lean.Engine.DataFeeds.WorkScheduling;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Utilities related to data <see cref="Subscription"/>
    /// </summary>
    public static class SubscriptionUtils
    {
        /// <summary>
        /// Creates a new <see cref="Subscription"/> which will directly consume the provided enumerator
        /// </summary>
        /// <param name="request">The subscription data request</param>
        /// <param name="enumerator">The data enumerator stack</param>
        /// <returns>A new subscription instance ready to consume</returns>
        public static Subscription Create(
            SubscriptionRequest request,
            IEnumerator<BaseData> enumerator,
            bool dailyStrictEndTimeEnabled)
        {
            if (enumerator == null)
            {
                return GetEndedSubscription(request);
            }
            var exchangeHours = request.Security.Exchange.Hours;
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.Configuration.ExchangeTimeZone, request.StartTimeUtc, request.EndTimeUtc);
            var dataEnumerator = new SubscriptionDataEnumerator(
                request.Configuration,
                exchangeHours,
                timeZoneOffsetProvider,
                enumerator,
                request.IsUniverseSubscription,
                dailyStrictEndTimeEnabled
            );
            return new Subscription(request, dataEnumerator, timeZoneOffsetProvider);
        }

        /// <summary>
        /// Setups a new <see cref="Subscription"/> which will consume a blocking <see cref="EnqueueableEnumerator{T}"/>
        /// that will be feed by a worker task
        /// </summary>
        /// <param name="request">The subscription data request</param>
        /// <param name="enumerator">The data enumerator stack</param>
        /// <param name="factorFileProvider">The factor file provider</param>
        /// <param name="enablePriceScale">Enables price factoring</param>
        /// <returns>A new subscription instance ready to consume</returns>
        public static Subscription CreateAndScheduleWorker(
            SubscriptionRequest request,
            IEnumerator<BaseData> enumerator,
            IFactorFileProvider factorFileProvider,
            bool enablePriceScale,
            bool dailyStrictEndTimeEnabled)
        {
            if(enumerator == null)
            {
                return GetEndedSubscription(request);
            }
            var exchangeHours = request.Security.Exchange.Hours;
            var enqueueable = new EnqueueableEnumerator<SubscriptionData>(true);
            var timeZoneOffsetProvider = new TimeZoneOffsetProvider(request.ExchangeHours.TimeZone, request.StartTimeUtc, request.EndTimeUtc);
            var subscription = new Subscription(request, enqueueable, timeZoneOffsetProvider);
            var config = subscription.Configuration;
            enablePriceScale = enablePriceScale && config.PricesShouldBeScaled();
            var lastTradableDate = DateTime.MinValue;

            ScheduleEnumerator(config.Symbol, enumerator, enqueueable, data =>
            {
                // Use our config filter to see if we should emit this
                // This currently catches Auxiliary data that we don't want to emit
                if (data != null && !config.ShouldEmitData(data, request.IsUniverseSubscription))
                {
                    return null;
                }

                // In the event we have "Raw" configuration, we will force our subscription data
                // to precalculate adjusted data. The data will still be emitted as raw, but
                // if the config is changed at any point it can emit adjusted data as well
                // See SubscriptionData.Create() and PrecalculatedSubscriptionData for more
                var requestMode = config.DataNormalizationMode;
                if (config.SecurityType == SecurityType.Equity)
                {
                    requestMode = requestMode != DataNormalizationMode.Raw ? requestMode : DataNormalizationMode.Adjusted;
                }

                var priceScaleFrontierDate = data.GetUpdatePriceScaleFrontier().Date;

                // We update our price scale factor when the date changes for non fill forward bars or if we haven't initialized yet.
                // We don't take into account auxiliary data because we don't scale it and because the underlying price data could be fill forwarded
                if (enablePriceScale && priceScaleFrontierDate > lastTradableDate && data.DataType != MarketDataType.Auxiliary && (!data.IsFillForward || lastTradableDate == DateTime.MinValue))
                {
                    var factorFile = factorFileProvider.Get(request.Configuration.Symbol);
                    lastTradableDate = priceScaleFrontierDate;
                    request.Configuration.PriceScaleFactor = factorFile.GetPriceScale(lastTradableDate, requestMode, config.ContractDepthOffset, config.DataMappingMode);
                }

                return SubscriptionData.Create(dailyStrictEndTimeEnabled,
                    config,
                    exchangeHours,
                    subscription.OffsetProvider,
                    data,
                    requestMode,
                    enablePriceScale ? request.Configuration.PriceScaleFactor : null);
            });

            return subscription;
        }

        /// <summary>
        /// Setups a worker task to enumerate the given enumerator, feeding a blocking <see cref="EnqueueableEnumerator{T}"/> which is returned.
        /// Useful for enumerators which perform blocking IO, like the live warmup enumerators performing history requests, so that they run
        /// concurrently on the workers instead of sequentially driven by the consuming thread, which will only block until the next data
        /// point of each enumerator is available
        /// </summary>
        /// <param name="symbol">The symbol associated with this work</param>
        /// <param name="enumerator">The data enumerator stack to enumerate</param>
        /// <returns>A blocking enumerator providing the data</returns>
        public static IEnumerator<BaseData> ScheduleEnumerator(Symbol symbol, IEnumerator<BaseData> enumerator)
        {
            var enqueueable = new EnqueueableEnumerator<BaseData>(blocking: true);
            ScheduleEnumerator(symbol, enumerator, enqueueable, data => data, endOnNullDataPoint: true);
            return enqueueable;
        }

        /// <summary>
        /// Setups a worker task to enumerate the given enumerator, processing each data point and feeding the given enumerator
        /// </summary>
        /// <param name="endOnNullDataPoint">When true, a null data point is taken as the enumerator having ended, since in live mode
        /// enumerators will return true with a null current value when they have no more data. Else null data points are forwarded</param>
        /// <remarks>A null processed non null data point is skipped</remarks>
        private static void ScheduleEnumerator<T>(Symbol symbol, IEnumerator<BaseData> enumerator, EnqueueableEnumerator<T> enqueueable,
            Func<BaseData, T> processDataPoint, bool endOnNullDataPoint = false)
            where T : class
        {
            Func<int, bool> produce = (workBatchSize) =>
            {
                try
                {
                    var count = 0;
                    while (enumerator.MoveNext())
                    {
                        // the consumer has been removed, no need to continue enumerating
                        if (enqueueable.HasFinished)
                        {
                            enumerator.DisposeSafely();
                            return false;
                        }

                        var data = enumerator.Current;
                        if (data == null && endOnNullDataPoint)
                        {
                            break;
                        }

                        var processedDataPoint = processDataPoint(data);
                        if (data != null && processedDataPoint == null)
                        {
                            // filtered out
                            continue;
                        }

                        // drop the data into the back of the enqueueable
                        enqueueable.Enqueue(processedDataPoint);

                        count++;
                        // stop executing if added more data than the work batch size, we don't want to fill the ram
                        if (count > workBatchSize)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception, $"Enumerator worker task exception {symbol}.");
                }

                // we made it here because MoveNext returned false or we exploded, stop the enqueueable
                enqueueable.Stop();
                // we have to dispose of the enumerator
                enumerator.DisposeSafely();
                return false;
            };

            WeightedWorkScheduler.Instance.QueueWork(symbol, produce,
                // if the enumerator finished we return 0, so the work is prioritized and gets removed
                () =>
                {
                    if (enqueueable.HasFinished)
                    {
                        return 0;
                    }
                    return enqueueable.Count;
                }
            );
        }

        /// <summary>
        /// Return an ended subscription so it doesn't blow up at runtime on the data worker, this can happen if there's no tradable date
        /// </summary>
        private static Subscription GetEndedSubscription(SubscriptionRequest request)
        {
            var result = new Subscription(request, null, null);
            // set subscription as ended
            result.Dispose();
            return result;
        }
    }
}
