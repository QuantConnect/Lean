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
using System.Linq;
using Python.Runtime;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// ConstituentsUniverse allows to perform universe selection based on an
    /// already preselected set of <see cref="Symbol"/>.
    /// </summary>
    /// <remarks>Using this class allows a performance improvement, since there is no
    /// runtime logic computation required for selecting the <see cref="Symbol"/></remarks>
    public class ConstituentsUniverse<T> : FuncUniverse<T>
        where T : BaseData
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ConstituentsUniverse"/>
        /// </summary>
        /// <param name="symbol">The universe symbol</param>
        /// <param name="universeSettings">The universe settings to use</param>
        /// <param name="constituentsFilter">User-provided function to filter constituents universe with</param>
        public ConstituentsUniverse(
            Symbol symbol,
            UniverseSettings universeSettings,
            Func<IEnumerable<T>, IEnumerable<Symbol>> constituentsFilter = null
        )
            : this(
                new SubscriptionDataConfig(
                    typeof(T),
                    symbol,
                    Resolution.Daily,
                    TimeZones.NewYork,
                    TimeZones.NewYork,
                    false,
                    false,
                    true,
                    true
                ),
                universeSettings,
                constituentsFilter
            ) { }

        /// <summary>
        /// Creates a new instance of the <see cref="ConstituentsUniverse"/>
        /// </summary>
        /// <param name="symbol">The universe symbol</param>
        /// <param name="universeSettings">The universe settings to use</param>
        /// <param name="constituentsFilter">User-provided function to filter constituents universe with</param>
        public ConstituentsUniverse(
            Symbol symbol,
            UniverseSettings universeSettings,
            PyObject constituentsFilter = null
        )
            : this(
                symbol,
                universeSettings,
                constituentsFilter.ConvertPythonUniverseFilterFunction<T>()
            ) { }

        /// <summary>
        /// Creates a new instance of the <see cref="ConstituentsUniverse"/>
        /// </summary>
        /// <param name="subscriptionDataConfig">The universe configuration to use</param>
        /// <param name="universeSettings">The universe settings to use</param>
        /// <param name="constituentsFilter">User-provided function to filter constituents universe with</param>
        public ConstituentsUniverse(
            SubscriptionDataConfig subscriptionDataConfig,
            UniverseSettings universeSettings,
            Func<IEnumerable<T>, IEnumerable<Symbol>> constituentsFilter = null
        )
            : base(
                subscriptionDataConfig,
                universeSettings,
                constituentsFilter
                    ?? (
                        constituents =>
                        {
                            var symbols = constituents.Select(baseData => baseData.Symbol).ToList();
                            // for performance, just compare to Symbol.None if we have 1 Symbol
                            if (symbols.Count == 1 && symbols[0] == Symbol.None)
                            {
                                // no symbol selected
                                return Enumerable.Empty<Symbol>();
                            }

                            return symbols;
                        }
                    )
            )
        {
            if (!subscriptionDataConfig.IsCustomData)
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name} {nameof(SubscriptionDataConfig)}"
                        + $" only supports custom data property set to 'true'"
                );
            }
        }

        /// <summary>
        /// Constituent universe for a Python function
        /// </summary>
        /// <param name="subscriptionDataConfig">The universe configuration to use</param>
        /// <param name="universeSettings">The universe settings to use</param>
        /// <param name="constituentsFilter">User-provided function to filter constituents universe with</param>
        public ConstituentsUniverse(
            SubscriptionDataConfig subscriptionDataConfig,
            UniverseSettings universeSettings,
            PyObject constituentsFilter = null
        )
            : this(
                subscriptionDataConfig,
                universeSettings,
                constituentsFilter.ConvertPythonUniverseFilterFunction<T>()
            ) { }
    }

    /// <summary>
    /// ConstituentsUniverse allows to perform universe selection based on an
    /// already preselected set of <see cref="Symbol"/>.
    /// </summary>
    /// <remarks>Using this class allows a performance improvement, since there is no
    /// runtime logic computation required for selecting the <see cref="Symbol"/></remarks>
    public class ConstituentsUniverse : ConstituentsUniverse<ConstituentsUniverseData>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ConstituentsUniverse"/>
        /// </summary>
        /// <param name="symbol">The universe symbol</param>
        /// <param name="universeSettings">The universe settings to use</param>
        /// <param name="filterFunc">The constituents filter function</param>
        public ConstituentsUniverse(
            Symbol symbol,
            UniverseSettings universeSettings,
            Func<IEnumerable<ConstituentsUniverseData>, IEnumerable<Symbol>> filterFunc
        )
            : base(symbol, universeSettings, filterFunc) { }

        /// <summary>
        /// Creates a new instance of the <see cref="ConstituentsUniverse"/>
        /// </summary>
        /// <param name="symbol">The universe symbol</param>
        /// <param name="universeSettings">The universe settings to use</param>
        public ConstituentsUniverse(Symbol symbol, UniverseSettings universeSettings)
            : base(
                symbol,
                universeSettings,
                (Func<IEnumerable<ConstituentsUniverseData>, IEnumerable<Symbol>>)null
            ) { }

        /// <summary>
        /// Creates a new instance of the <see cref="ConstituentsUniverse"/>
        /// </summary>
        /// <param name="symbol">The universe symbol</param>
        /// <param name="universeSettings">The universe settings to use</param>
        /// <param name="filterFunc">The constituents filter function</param>
        public ConstituentsUniverse(
            Symbol symbol,
            UniverseSettings universeSettings,
            PyObject filterFunc
        )
            : base(symbol, universeSettings, filterFunc) { }
    }
}
