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
    /// Enumerator who will emit <see cref="Split"/> events, merged with the
    /// underlying enumerator output
    /// </summary>
    public class SplitEnumerator : CorporateEventBaseEnumerator
    {
        private readonly FactorFile _factorFile;
        private readonly MapFile _mapFile;
        // we set the split factor when we encounter a split in the factor file
        // and on the next trading day we use this data to produce the split instance
        private decimal? _splitFactor;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="enumerator">Underlying enumerator</param>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="factorFile">The factor file to use</param>
        /// <param name="mapFile">The <see cref="MapFile"/> to use</param>
        /// <param name="tradableDayNotifier">Tradable dates provider</param>
        /// <param name="includeAuxiliaryData">True to emit auxiliary data</param>
        public SplitEnumerator(
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
        /// Check for new splits
        /// </summary>
        /// <param name="date">The new tradable day value</param>
        /// <returns>New split event, else Null</returns>
        protected override BaseData CheckNewEvent(DateTime date)
        {
            BaseData baseData = null;
            if (_mapFile.HasData(date))
            {
                var factor = _splitFactor;
                if (factor != null)
                {
                    var close = GetRawClose();
                    baseData = new Split(
                        SubscriptionDataConfig.Symbol,
                        date,
                        close,
                        factor.Value,
                        SplitType.SplitOccurred);
                    _splitFactor = null;
                }

                decimal splitFactor;
                if (_factorFile.HasSplitEventOnNextTradingDay(date, out splitFactor))
                {
                    _splitFactor = splitFactor;
                    baseData = new Split(
                        SubscriptionDataConfig.Symbol,
                        date,
                        GetRawClose(),
                        splitFactor,
                        SplitType.Warning);
                }
            }

            return baseData;
        }
    }
}
