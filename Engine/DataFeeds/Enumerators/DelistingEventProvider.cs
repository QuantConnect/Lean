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
    /// Event provider who will emit <see cref="Delisting"/> events
    /// </summary>
    public class DelistingEventProvider : ITradableDateEventProvider
    {
        // we'll use these flags to denote we've already fired off the DelistingType.Warning
        // and a DelistedType.Delisted Delisting object, the _delistingType object is save here
        // since we need to wait for the next trading day before emitting
        private bool _delisted;
        private bool _delistedWarning;
        private DateTime _delistingDate;

        private SubscriptionDataConfig _config;

        /// <summary>
        /// Initializes this instance
        /// </summary>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="factorFile">The factor file to use</param>
        /// <param name="mapFile">The <see cref="MapFile"/> to use</param>
        /// <param name="startTime">Start date for the data request</param>
        public void Initialize(
            SubscriptionDataConfig config,
            FactorFile factorFile,
            MapFile mapFile,
            DateTime startTime)
        {
            _config = config;
            // Estimate delisting date.
            switch (config.Symbol.ID.SecurityType)
            {
                case SecurityType.Future:
                    _delistingDate = config.Symbol.ID.Date;
                    break;
                case SecurityType.Option:
                    _delistingDate = OptionSymbol.GetLastDayOfTrading(
                        config.Symbol);
                    break;
                default:
                    _delistingDate = mapFile.DelistingDate;
                    break;
            }
        }

        /// <summary>
        /// Check for delistings
        /// </summary>
        /// <param name="eventArgs">The new tradable day event arguments</param>
        /// <returns>New delisting event if any</returns>
        public IEnumerable<BaseData> GetEvents(NewTradableDateEventArgs eventArgs)
        {
            if (_config.Symbol == eventArgs.Symbol)
            {
                if (!_delistedWarning && eventArgs.Date >= _delistingDate)
                {
                    _delistedWarning = true;
                    var price = eventArgs.LastBaseData?.Price ?? 0;
                    yield return new Delisting(
                        eventArgs.Symbol,
                        eventArgs.Date,
                        price,
                        DelistingType.Warning);
                }
                if (!_delisted && eventArgs.Date > _delistingDate)
                {
                    _delisted = true;
                    var price = eventArgs.LastBaseData?.Price ?? 0;
                    // delisted at EOD
                    yield return new Delisting(
                        eventArgs.Symbol,
                        _delistingDate.AddDays(1),
                        price,
                        DelistingType.Delisted);
                }
            }
        }
    }
}
