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
using QuantConnect.Data;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Data.Market;
using System.Collections.Generic;
using QuantConnect.Data.Auxiliary;

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
        private IMapFileProvider _mapFileProvider;

        /// <summary>
        /// The delisting date
        /// </summary>
        protected ReferenceWrapper<DateTime> DelistingDate { get; set; }

        /// <summary>
        /// The current instance being used
        /// </summary>
        protected MapFile MapFile { get; private set; }

        /// <summary>
        /// The associated configuration
        /// </summary>
        protected SubscriptionDataConfig Config { get; private set; }

        /// <summary>
        /// Initializes this instance
        /// </summary>
        /// <param name="config">The <see cref="SubscriptionDataConfig"/></param>
        /// <param name="factorFileProvider">The factor file provider to use</param>
        /// <param name="mapFileProvider">The <see cref="Data.Auxiliary.MapFile"/> provider to use</param>
        /// <param name="startTime">Start date for the data request</param>
        public virtual void Initialize(
            SubscriptionDataConfig config,
            IFactorFileProvider factorFileProvider,
            IMapFileProvider mapFileProvider,
            DateTime startTime)
        {
            Config = config;
            _mapFileProvider = mapFileProvider;

            InitializeMapFile();
        }

        /// <summary>
        /// Check for delistings
        /// </summary>
        /// <param name="eventArgs">The new tradable day event arguments</param>
        /// <returns>New delisting event if any</returns>
        public virtual IEnumerable<BaseData> GetEvents(NewTradableDateEventArgs eventArgs)
        {
            if (Config.Symbol == eventArgs.Symbol)
            {
                // we send the delisting warning when we reach the delisting date, here we make sure we compare using the date component
                // of the delisting date since for example some futures can trade a few hours in their delisting date, else we would skip on
                // emitting the delisting warning, which triggers us to handle liquidation once delisted
                if (!_delistedWarning && eventArgs.Date >= DelistingDate.Value.Date)
                {
                    _delistedWarning = true;
                    var price = eventArgs.LastBaseData?.Price ?? 0;
                    yield return new Delisting(
                        eventArgs.Symbol,
                        DelistingDate.Value.Date,
                        price,
                        DelistingType.Warning);
                }
                if (!_delisted && eventArgs.Date > DelistingDate.Value)
                {
                    _delisted = true;
                    var price = eventArgs.LastBaseData?.Price ?? 0;
                    // delisted at EOD
                    yield return new Delisting(
                        eventArgs.Symbol,
                        DelistingDate.Value.AddDays(1),
                        price,
                        DelistingType.Delisted);
                }
            }
        }

        /// <summary>
        /// Initializes the factor file to use
        /// </summary>
        protected void InitializeMapFile()
        {
            MapFile = _mapFileProvider.ResolveMapFile(Config);
            DelistingDate = new ReferenceWrapper<DateTime>(Config.Symbol.GetDelistingDate(MapFile));
        }
    }
}
