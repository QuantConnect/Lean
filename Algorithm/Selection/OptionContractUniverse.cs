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
using System.Linq;
using QuantConnect.Data;
using System.Collections.Generic;
using System.Collections.Specialized;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.Selection
{
    /// <summary>
    /// This universe will hold single option contracts and their underlying, managing removals and additions
    /// </summary>
    public class OptionContractUniverse : UserDefinedUniverse
    {
        private readonly HashSet<Symbol> _symbols;

        /// <summary>
        /// Creates a new empty instance
        /// </summary>
        /// <param name="configuration">The universe configuration to use</param>
        /// <param name="universeSettings">The universe settings to use</param>
        public OptionContractUniverse(SubscriptionDataConfig configuration, UniverseSettings universeSettings)
            : base(configuration, universeSettings, Time.EndOfTimeTimeSpan,
                // Argument isn't used since we override 'SelectSymbols'
                Enumerable.Empty<Symbol>())
        {
            _symbols = new HashSet<Symbol>();
        }

        /// <summary>
        /// Returns the symbols defined by the user for this universe
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="data">The symbols to remain in the universe</param>
        /// <returns>The data that passes the filter</returns>
        public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
        {
            return _symbols;
        }

        /// <summary>
        /// Event invocator for the <see cref="UserDefinedUniverse.CollectionChanged"/> event
        /// </summary>
        /// <param name="args">The notify collection changed event arguments</param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                var removedSymbol = (Symbol)args.OldItems[0];
                _symbols.Remove(removedSymbol);

                // the option has been removed! This can happen when the user manually removed the option contract we remove the underlying
                if (removedSymbol.SecurityType == SecurityType.Option || removedSymbol.SecurityType == SecurityType.FutureOption)
                {
                    Remove(removedSymbol.Underlying);
                }
            }
            else if (args.Action == NotifyCollectionChangedAction.Add)
            {
                // QCAlgorithm.AddOptionContract will add both underlying and option contract
                _symbols.Add((Symbol)args.NewItems[0]);
            }

            base.OnCollectionChanged(args);
        }

        /// <summary>
        /// Creates a user defined universe symbol
        /// </summary>
        /// <param name="market">The market</param>
        /// <param name="securityType">The underlying option security type</param>
        /// <returns>A symbol for user defined universe of the specified security type and market</returns>
        public static Symbol CreateSymbol(string market, SecurityType securityType)
        {
            var ticker = $"qc-universe-optioncontract-{securityType.SecurityTypeToLower()}-{market.ToLowerInvariant()}";
            var underlying = Symbol.Create(ticker, securityType, market);
            var sid = SecurityIdentifier.GenerateOption(SecurityIdentifier.DefaultDate, underlying.ID, market, 0, 0, 0);

            return new Symbol(sid, ticker);
        }
    }
}
