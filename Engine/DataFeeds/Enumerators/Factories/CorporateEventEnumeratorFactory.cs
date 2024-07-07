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
using QuantConnect.Interfaces;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators.Factories
{
    /// <summary>
    /// Helper class used to create the corporate event providers
    /// <see cref="MappingEventProvider"/>, <see cref="SplitEventProvider"/>,
    /// <see cref="DividendEventProvider"/>, <see cref="DelistingEventProvider"/>
    /// </summary>
    public static class CorporateEventEnumeratorFactory
    {
        /// <summary>
        /// Creates a new <see cref="AuxiliaryDataEnumerator"/> that will hold the
        /// corporate event providers
        /// </summary>
        /// <param name="rawDataEnumerator">The underlying raw data enumerator</param>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="factorFileProvider">Used for getting factor files</param>
        /// <param name="tradableDayNotifier">Tradable dates provider</param>
        /// <param name="mapFileProvider">The <see cref="MapFile"/> provider to use</param>
        /// <param name="startTime">Start date for the data request</param>
        /// <param name="endTime">
        /// End date for the data request.
        /// This will be used for <see cref="DataNormalizationMode.ScaledRaw"/> data normalization mode to adjust prices to the given end date
        /// </param>
        /// <param name="enablePriceScaling">Applies price factor</param>
        /// <returns>The new auxiliary data enumerator</returns>
        public static IEnumerator<BaseData> CreateEnumerators(
            IEnumerator<BaseData> rawDataEnumerator,
            SubscriptionDataConfig config,
            IFactorFileProvider factorFileProvider,
            ITradableDatesNotifier tradableDayNotifier,
            IMapFileProvider mapFileProvider,
            DateTime startTime,
            DateTime endTime,
            bool enablePriceScaling = true
        )
        {
            var tradableEventProviders = new List<ITradableDateEventProvider>();

            if (config.EmitSplitsAndDividends())
            {
                tradableEventProviders.Add(new SplitEventProvider());
                tradableEventProviders.Add(new DividendEventProvider());
            }

            if (config.TickerShouldBeMapped())
            {
                tradableEventProviders.Add(new MappingEventProvider());
            }

            tradableEventProviders.Add(new DelistingEventProvider());

            var enumerator = new AuxiliaryDataEnumerator(
                config,
                factorFileProvider,
                mapFileProvider,
                tradableEventProviders.ToArray(),
                tradableDayNotifier,
                startTime
            );

            // avoid price scaling for backtesting; calculate it directly in worker
            // and allow subscription to extract the the data depending on config data mode
            var dataEnumerator = rawDataEnumerator;
            if (enablePriceScaling && config.PricesShouldBeScaled())
            {
                dataEnumerator = new PriceScaleFactorEnumerator(
                    rawDataEnumerator,
                    config,
                    factorFileProvider,
                    endDate: endTime
                );
            }

            return new SynchronizingBaseDataEnumerator(dataEnumerator, enumerator);
        }
    }
}
