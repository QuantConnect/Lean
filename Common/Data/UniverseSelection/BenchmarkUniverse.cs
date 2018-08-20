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
using System.Collections.Specialized;
using System.Linq;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Represents the universe containing only the selected benchmark security.
    /// This universe keeps the benchmark security and its subscription separate
    /// from the algorithm securities. Universe selection will fire only once,
    /// when the benchmark security is added to the universe.
    /// </summary>
    public class BenchmarkUniverse : Universe, INotifyCollectionChanged
    {
        /// <summary>
        /// Event fired when a symbol is added or removed from this universe
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Gets the settings used for subscriptions added for this universe (unused in this universe)
        /// </summary>
        public override UniverseSettings UniverseSettings => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="BenchmarkUniverse"/> class
        /// </summary>
        /// <param name="configuration">The configuration used to resolve the data for universe selection</param>
        public BenchmarkUniverse(SubscriptionDataConfig configuration) : base(configuration)
        {
        }

        /// <summary>
        /// Returns the benchmark symbol in this universe
        /// </summary>
        /// <param name="utcTime">The current UTC time</param>
        /// <param name="data">The symbols to remain in the universe</param>
        public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
        {
            return Securities.Select(x => x.Key);
        }

        /// <summary>
        /// Returns an existing security for this universe
        /// </summary>
        public override Security GetSecurity(Symbol symbol, IAlgorithm algorithm)
        {
            return Securities.Select(x => x.Value.Security).FirstOrDefault(x => x.Symbol == symbol);
        }

        /// <summary>
        /// Determines whether or not the specified security can be removed from
        /// this universe. This is useful to prevent securities from being taken
        /// out of a universe before the algorithm has had enough time to make
        /// decisions on the security
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="security">The security to check if its ok to remove</param>
        /// <returns>True if we can remove the security, false otherwise</returns>
        public override bool CanRemoveMember(DateTime utcTime, Security security)
        {
            // never allow removal of the benchmark security
            return false;
        }

        /// <summary>
        /// Adds the specified symbol to this universe
        /// </summary>
        /// <param name="utcTime">The current UTC time</param>
        /// <param name="security">The symbol to be added to this universe</param>
        /// <returns>True if the symbol was added, false if it was already present</returns>
        public bool SetBenchmarkSecurity(DateTime utcTime, Security security)
        {
            if (AddMember(utcTime, security))
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, security.Symbol));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Event invocator for the <see cref="CollectionChanged"/> event
        /// </summary>
        /// <param name="e">The notify collection changed event arguments</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Creates a benchmark universe symbol
        /// </summary>
        /// <param name="securityType">The security</param>
        /// <param name="market">The market</param>
        /// <returns>A symbol for benchmark universe of the specified security type and market</returns>
        public static Symbol CreateSymbol(SecurityType securityType, string market)
        {
            var ticker = $"qc-universe-benchmark-{market.ToLower()}-{securityType}";
            return CreateSymbol(ticker, securityType, market);
        }
    }
}
