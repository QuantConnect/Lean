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
using QuantConnect.Data.Market;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Enumerator who will emit <see cref="Dividend"/> events, merged with the
    /// underlying enumerator output
    /// </summary>
    public class DividendEnumerator : CorporateEventBaseEnumerator
    {
        private readonly FactorFile _factorFile;
        private readonly MapFile _mapFile;
        // we set the price factor ratio when we encounter a dividend in the factor file
        // and on the next trading day we use this data to produce the dividend instance
        private decimal? _priceFactorRatio;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="enumerator">Underlying enumerator</param>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="factorFile">The factor file to use</param>
        /// <param name="mapFile">The <see cref="MapFile"/> to use</param>
        /// <param name="tradableDayNotifier">Tradable dates provider</param>
        /// <param name="includeAuxiliaryData">True to emit auxiliary data</param>
        public DividendEnumerator(
            IEnumerator<BaseData> enumerator,
            SubscriptionDataConfig config,
            FactorFile factorFile,
            MapFile mapFile,
            ITradableDatesNotifier tradableDayNotifier,
            bool includeAuxiliaryData)
            : base(enumerator, config, tradableDayNotifier, includeAuxiliaryData)
        {
            _mapFile = mapFile;
            _factorFile = factorFile;
        }

        /// <summary>
        /// Check for dividends and emit them into the aux data queue
        /// </summary>
        protected override BaseData CheckNewEvent(DateTime date)
        {
            BaseData baseData = null;
            if (_mapFile.HasData(date))
            {
                if (_priceFactorRatio != null)
                {
                    var close = GetRawClose();
                    baseData = Dividend.Create(
                        SubscriptionDataConfig.Symbol,
                        date,
                        close,
                        _priceFactorRatio.Value
                    );
                    // let the config know about it for normalization
                    SubscriptionDataConfig.SumOfDividends += ((Dividend) baseData).Distribution;
                    _priceFactorRatio = null;
                }

                // check the factor file to see if we have a dividend event tomorrow
                decimal priceFactorRatio;
                if (_factorFile.HasDividendEventOnNextTradingDay(date, out priceFactorRatio))
                {
                    _priceFactorRatio = priceFactorRatio;
                }
            }

            return baseData;
        }
    }
}
