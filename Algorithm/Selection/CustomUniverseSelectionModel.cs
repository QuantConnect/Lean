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
*/

using System;
using System.Collections.Generic;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Provides an implementation of <see cref="IUniverseSelectionModel"/> that simply
    /// subscribes to the specified set of symbols
    /// </summary>
    public class CustomUniverseSelectionModel : UniverseSelectionModel
    {
        private static readonly MarketHoursDatabase MarketHours = MarketHoursDatabase.FromDataFolder();

        private readonly Symbol _symbol;
        private readonly Func<DateTime, IEnumerable<string>> _selector;
        private readonly UniverseSettings _universeSettings;
        private readonly TimeSpan _interval;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomUniverseSelectionModel"/> class
        /// for <see cref="Market.USA"/> and <see cref="SecurityType.Equity"/>
        /// using the algorithm's universe settings
        /// </summary>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="selector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        public CustomUniverseSelectionModel(string name, Func<DateTime, IEnumerable<string>> selector)
            : this(SecurityType.Equity, name, Market.USA, selector, null, Time.OneDay)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomUniverseSelectionModel"/> class
        /// for <see cref="Market.USA"/> and <see cref="SecurityType.Equity"/>
        /// using the algorithm's universe settings
        /// </summary>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="selector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        public CustomUniverseSelectionModel(string name, PyObject selector)
            : this(SecurityType.Equity, name, Market.USA, selector, null, Time.OneDay)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="securityType">The security type of the universe</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="market">The market of the universe</param>
        /// <param name="selector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        /// <param name="universeSettings">The settings used when adding symbols to the algorithm, specify null to use algorithm.UniverseSettings</param>
        public CustomUniverseSelectionModel(SecurityType securityType, string name, string market, Func<DateTime, IEnumerable<string>> selector, UniverseSettings universeSettings, TimeSpan interval)
        {
            _interval = interval;
            _selector = selector;
            _universeSettings = universeSettings;
            _symbol = Symbol.Create($"{name}-{securityType}-{market}", securityType, market);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="securityType">The security type of the universe</param>
        /// <param name="name">A unique name for this universe</param>
        /// <param name="market">The market of the universe</param>
        /// <param name="selector">Function delegate that accepts a DateTime and returns a collection of string symbols</param>
        /// <param name="universeSettings">The settings used when adding symbols to the algorithm, specify null to use algorithm.UniverseSettings</param>
        public CustomUniverseSelectionModel(SecurityType securityType, string name, string market, PyObject selector, UniverseSettings universeSettings, TimeSpan interval)
            : this(
                securityType,
                name,
                market,
                selector.ConvertToDelegate<Func<DateTime, object>>().ConvertToUniverseSelectionStringDelegate(),
                universeSettings,
                interval
            )
        {
        }

        /// <summary>
        /// Creates the universes for this algorithm. Called at algorithm start.
        /// </summary>
        /// <returns>The universes defined by this model</returns>
        public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
        {
            var universeSettings = _universeSettings ?? algorithm.UniverseSettings;
            var entry = MarketHours.GetEntry(_symbol.ID.Market, (string) null, _symbol.SecurityType);

            var config = new SubscriptionDataConfig(
                universeSettings.Resolution == Resolution.Tick ? typeof(Tick) : typeof(TradeBar),
                _symbol,
                universeSettings.Resolution,
                entry.DataTimeZone,
                entry.ExchangeHours.TimeZone,
                universeSettings.FillForward,
                universeSettings.ExtendedMarketHours,
                true
            );

            yield return new CustomUniverse(config, universeSettings, _interval, dt => Select(algorithm, dt));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="algorithm"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public virtual IEnumerable<string> Select(QCAlgorithm algorithm, DateTime date)
        {
            if (_selector == null)
            {
                throw new ArgumentNullException(nameof(_selector));
            }

            return _selector(date);
        }

        /// <summary>
        /// Returns a string that represents the current object
        /// </summary>
        public override string ToString() => _symbol.Value;
    }
}