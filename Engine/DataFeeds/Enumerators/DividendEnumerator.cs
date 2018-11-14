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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Enumerator who will emit <see cref="Dividend"/> events
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
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="factorFile">The factor file to use</param>
        /// <param name="mapFile">The <see cref="MapFile"/> to use</param>
        /// <param name="tradableDayNotifier">Tradable dates provider</param>
        /// <param name="includeAuxiliaryData">True to emit auxiliary data</param>
        public DividendEnumerator(
            SubscriptionDataConfig config,
            FactorFile factorFile,
            MapFile mapFile,
            ITradableDatesNotifier tradableDayNotifier,
            bool includeAuxiliaryData)
            : base(config, tradableDayNotifier, includeAuxiliaryData)
        {
            _mapFile = mapFile;
            _factorFile = factorFile;
        }

        /// <summary>
        /// Check for dividends and emit them into the aux data queue
        /// </summary>
        /// <param name="eventArgs">The new tradable day event arguments</param>
        /// <returns>New Dividend event, else Null</returns>
        protected override IEnumerable<BaseData> GetCorporateEvents(NewTradableDateEventArgs eventArgs)
        {
            if (_mapFile.HasData(eventArgs.Date))
            {
                if (_priceFactorRatio != null)
                {
                    var close = GetRawClose(eventArgs.LastBaseData?.Price ?? 0);
                    var baseData = Dividend.Create(
                        SubscriptionDataConfig.Symbol,
                        eventArgs.Date,
                        close,
                        _priceFactorRatio.Value
                    );
                    // let the config know about it for normalization
                    SubscriptionDataConfig.SumOfDividends += baseData.Distribution;
                    _priceFactorRatio = null;

                    yield return baseData;
                }

                // check the factor file to see if we have a dividend event tomorrow
                decimal priceFactorRatio;
                if (_factorFile.HasDividendEventOnNextTradingDay(eventArgs.Date, out priceFactorRatio))
                {
                    _priceFactorRatio = priceFactorRatio;
                }
            }
        }
    }
}
