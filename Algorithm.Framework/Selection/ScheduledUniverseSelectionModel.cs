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
using NodaTime;
using Python.Runtime;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Scheduling;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Selection
{
    /// <summary>
    /// Defines a universe selection model that invokes a selector function on a specific scheduled given by an <see cref="IDateRule"/> and an <see cref="ITimeRule"/>
    /// </summary>
    public class ScheduledUniverseSelectionModel : UniverseSelectionModel
    {
        private readonly IDateRule _dateRule;
        private readonly ITimeRule _timeRule;
        private readonly Func<DateTime, IEnumerable<Symbol>> _selector;
        private readonly DateTimeZone _timeZone;
        private readonly UniverseSettings _settings;
        private readonly ISecurityInitializer _initializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledUniverseSelectionModel"/> class using the algorithm's time zone
        /// </summary>
        /// <param name="dateRule">Date rule defines what days the universe selection function will be invoked</param>
        /// <param name="timeRule">Time rule defines what times on each day selected by date rule the universe selection function will be invoked</param>
        /// <param name="selector">Selector function accepting the date time firing time and returning the universe selected symbols</param>
        /// <param name="settings">Universe settings for subscriptions added via this universe, null will default to algorithm's universe settings</param>
        /// <param name="initializer">Security initializer for new securities created via this universe, null will default to algorithm's security initializer</param>
        public ScheduledUniverseSelectionModel(IDateRule dateRule, ITimeRule timeRule, Func<DateTime, IEnumerable<Symbol>> selector, UniverseSettings settings = null, ISecurityInitializer initializer = null)
        {
            _dateRule = dateRule;
            _timeRule = timeRule;
            _selector = selector;
            _settings = settings;
            _initializer = initializer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="timeZone">The time zone the date/time rules are in</param>
        /// <param name="dateRule">Date rule defines what days the universe selection function will be invoked</param>
        /// <param name="timeRule">Time rule defines what times on each day selected by date rule the universe selection function will be invoked</param>
        /// <param name="selector">Selector function accepting the date time firing time and returning the universe selected symbols</param>
        /// <param name="settings">Universe settings for subscriptions added via this universe, null will default to algorithm's universe settings</param>
        /// <param name="initializer">Security initializer for new securities created via this universe, null will default to algorithm's security initializer</param>
        public ScheduledUniverseSelectionModel(DateTimeZone timeZone, IDateRule dateRule, ITimeRule timeRule, Func<DateTime, IEnumerable<Symbol>> selector, UniverseSettings settings = null, ISecurityInitializer initializer = null)
        {
            _timeZone = timeZone;
            _dateRule = dateRule;
            _timeRule = timeRule;
            _selector = selector;
            _settings = settings;
            _initializer = initializer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledUniverseSelectionModel"/> class using the algorithm's time zone
        /// </summary>
        /// <param name="dateRule">Date rule defines what days the universe selection function will be invoked</param>
        /// <param name="timeRule">Time rule defines what times on each day selected by date rule the universe selection function will be invoked</param>
        /// <param name="selector">Selector function accepting the date time firing time and returning the universe selected symbols</param>
        /// <param name="settings">Universe settings for subscriptions added via this universe, null will default to algorithm's universe settings</param>
        /// <param name="initializer">Security initializer for new securities created via this universe, null will default to algorithm's security initializer</param>
        public ScheduledUniverseSelectionModel(IDateRule dateRule, ITimeRule timeRule, PyObject selector, UniverseSettings settings = null, ISecurityInitializer initializer = null)
            : this(null, dateRule, timeRule, selector, settings, initializer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledUniverseSelectionModel"/> class
        /// </summary>
        /// <param name="timeZone">The time zone the date/time rules are in</param>
        /// <param name="dateRule">Date rule defines what days the universe selection function will be invoked</param>
        /// <param name="timeRule">Time rule defines what times on each day selected by date rule the universe selection function will be invoked</param>
        /// <param name="selector">Selector function accepting the date time firing time and returning the universe selected symbols</param>
        /// <param name="settings">Universe settings for subscriptions added via this universe, null will default to algorithm's universe settings</param>
        /// <param name="initializer">Security initializer for new securities created via this universe, null will default to algorithm's security initializer</param>
        public ScheduledUniverseSelectionModel(DateTimeZone timeZone, IDateRule dateRule, ITimeRule timeRule, PyObject selector, UniverseSettings settings = null, ISecurityInitializer initializer = null)
        {
            Func<DateTime, object> func;
            selector.TryConvertToDelegate(out func);
            _timeZone = timeZone;
            _dateRule = dateRule;
            _timeRule = timeRule;
            _selector = func.ConvertToUniverseSelectionSymbolDelegate();
            _settings = settings;
            _initializer = initializer;
        }

        /// <summary>
        /// Creates the universes for this algorithm. Called once after <see cref="IAlgorithm.Initialize"/>
        /// </summary>
        /// <param name="algorithm">The algorithm instance to create universes for</param>
        /// <returns>The universes to be used by the algorithm</returns>
        public override IEnumerable<Universe> CreateUniverses(QCAlgorithm algorithm)
        {
            yield return new ScheduledUniverse(
                // by default ITimeRule yields in UTC
                _timeZone ?? TimeZones.Utc,
                _dateRule,
                _timeRule,
                _selector,
                _settings ?? algorithm.UniverseSettings,
                _initializer ?? algorithm.SecurityInitializer
            );
        }
    }
}