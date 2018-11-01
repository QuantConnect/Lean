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
using QuantConnect.Securities.Option;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Enumerator who will emit <see cref="Delisting"/> events, merged with the
    /// underlying enumerator output.
    /// </summary>
    public class DelistingEnumerator : CorporateEventBaseEnumerator
    {
        // we'll use these flags to denote we've already fired off the DelistingType.Warning
        // and a DelistedType.Delisted Delisting object, the _delistingType object is save here
        // since we need to wait for the next trading day before emitting
        private bool _delisted;
        private bool _delistedWarning;
        private DateTime _delistingDate;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="enumerator">Underlying enumerator</param>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="mapFile">The <see cref="MapFile"/> to use</param>
        /// <param name="tradableDayNotifier">Tradable dates provider</param>
        /// <param name="includeAuxiliaryData">True to emit auxiliary data</param>
        public DelistingEnumerator(
            IEnumerator<BaseData> enumerator,
            SubscriptionDataConfig config,
            MapFile mapFile,
            ITradableDatesNotifier tradableDayNotifier,
            bool includeAuxiliaryData)
            : base(enumerator, config, tradableDayNotifier, includeAuxiliaryData)
        {
            // Estimate delisting date.
            switch (SubscriptionDataConfig.Symbol.ID.SecurityType)
            {
                case SecurityType.Future:
                    _delistingDate = SubscriptionDataConfig.Symbol.ID.Date;
                    break;
                case SecurityType.Option:
                    _delistingDate = OptionSymbol.GetLastDayOfTrading(
                        SubscriptionDataConfig.Symbol);
                    break;
                default:
                    _delistingDate = mapFile.DelistingDate;
                    break;
            }
        }

        /// <summary>
        /// Check for delistings
        /// </summary>
        /// <param name="date">The new tradable day value</param>
        /// <returns>New delisting event, else Null</returns>
        protected override BaseData CheckNewEvent(DateTime date)
        {
            if (!_delistedWarning && date >= _delistingDate)
            {
                _delistedWarning = true;
                var price = PreviousUnderlyingData != null ? PreviousUnderlyingData.Price : 0;
                return new Delisting(
                    SubscriptionDataConfig.Symbol,
                    date,
                    price,
                    DelistingType.Warning);
            }
            if (!_delisted && date > _delistingDate)
            {
                _delisted = true;
                var price = PreviousUnderlyingData != null ? PreviousUnderlyingData.Price : 0;
                // delisted at EOD
                return new Delisting(
                    SubscriptionDataConfig.Symbol,
                    _delistingDate.AddDays(1),
                    price,
                    DelistingType.Delisted);
            }
            return null;
        }
    }
}
