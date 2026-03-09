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
using QuantConnect.Util;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Base class for security databases, including market hours and symbol properties.
    /// </summary>
    public abstract class BaseSecurityDatabase<T, TEntry>
        where T : BaseSecurityDatabase<T, TEntry>
    {
        /// <summary>
        /// The database instance loaded from the data folder
        /// </summary>
        protected static T DataFolderDatabase { get; set; }

        /// <summary>
        /// Lock object for the data folder database
        /// </summary>
        protected static readonly object DataFolderDatabaseLock = new object();

        /// <summary>
        /// The database entries
        /// </summary>
        protected Dictionary<SecurityDatabaseKey, TEntry> Entries { get; set; }

        /// <summary>
        /// Custom entries set by the user.
        /// </summary>
        protected HashSet<SecurityDatabaseKey> CustomEntries { get; }

        // _loadFromFromDataFolder and _updateEntry are used to load the database from
        // the data folder and update an entry respectively.
        // These are not abstract or virtual methods because they might be static methods.
        private readonly Func<T> _loadFromFromDataFolder;
        private readonly Action<TEntry, TEntry> _updateEntry;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseSecurityDatabase{T, TEntry}"/> class
        /// </summary>
        /// <param name="entries">The full listing of exchange hours by key</param>
        /// <param name="fromDataFolder">Method to load the database form the data folder</param>
        /// <param name="updateEntry">Method to update a database entry</param>
        protected BaseSecurityDatabase(Dictionary<SecurityDatabaseKey, TEntry> entries,
            Func<T> fromDataFolder, Action<TEntry, TEntry> updateEntry)
        {
            Entries = entries;
            CustomEntries = new();
            _loadFromFromDataFolder = fromDataFolder;
            _updateEntry = updateEntry;
        }

        /// <summary>
        /// Resets the database, forcing a reload when reused.
        /// Called in tests where multiple algorithms are run sequentially,
        /// and we need to guarantee that every test starts with the same environment.
        /// </summary>
#pragma warning disable CA1000 // Do not declare static members on generic types
        public static void Reset()
#pragma warning restore CA1000 // Do not declare static members on generic types
        {
            lock (DataFolderDatabaseLock)
            {
                DataFolderDatabase = null;
            }
        }

        /// <summary>
        /// Reload entries dictionary from file and merge them with previous custom ones
        /// </summary>
        internal void UpdateDataFolderDatabase()
        {
            lock (DataFolderDatabaseLock)
            {
                Reset();
                var newDatabase = _loadFromFromDataFolder();
                Merge(newDatabase, resetCustomEntries: false);
                // Make sure we keep this as the data folder database
                DataFolderDatabase = (T)this;
            }
        }

        /// <summary>
        /// Updates the entries dictionary with the new entries from the specified database
        /// </summary>
        internal virtual void Merge(T newDatabase, bool resetCustomEntries)
        {
            var newEntries = new List<KeyValuePair<SecurityDatabaseKey, TEntry>>();

            foreach (var newEntry in newDatabase.Entries)
            {
                if (Entries.TryGetValue(newEntry.Key, out var entry))
                {
                    if (resetCustomEntries || !CustomEntries.Contains(newEntry.Key))
                    {
                        _updateEntry(entry, newEntry.Value);
                    }
                }
                else
                {
                    newEntries.Add(KeyValuePair.Create(newEntry.Key, newEntry.Value));
                }
            }

            Entries = Entries
                .Where(kvp => (!resetCustomEntries && CustomEntries.Contains(kvp.Key)) || newDatabase.Entries.ContainsKey(kvp.Key))
                .Concat(newEntries)
                .ToDictionary();

            if (resetCustomEntries)
            {
                CustomEntries.Clear();
            }
        }

        /// <summary>
        /// Determines if the database contains the specified key
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <returns>True if an entry is found, otherwise false</returns>
        protected bool ContainsKey(SecurityDatabaseKey key)
        {
            return Entries.ContainsKey(key);
        }

        /// <summary>
        /// Check whether an entry exists for the specified market/symbol/security-type
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded</param>
        /// <param name="securityType">The security type of the symbol</param>
        public bool ContainsKey(string market, string symbol, SecurityType securityType)
        {
            return ContainsKey(new SecurityDatabaseKey(market, symbol, securityType));
        }

        /// <summary>
        /// Check whether an entry exists for the specified market/symbol/security-type
        /// </summary>
        /// <param name="market">The market the exchange resides in, i.e, 'usa', 'fxcm', ect...</param>
        /// <param name="symbol">The particular symbol being traded (Symbol class)</param>
        /// <param name="securityType">The security type of the symbol</param>
        public bool ContainsKey(string market, Symbol symbol, SecurityType securityType)
        {
            return ContainsKey(
                market,
                GetDatabaseSymbolKey(symbol),
                securityType);
        }

        /// <summary>
        /// Gets the correct string symbol to use as a database key
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The symbol string used in the database ke</returns>
#pragma warning disable CA1000 // Do not declare static members on generic types
        public static string GetDatabaseSymbolKey(Symbol symbol)
#pragma warning restore CA1000 // Do not declare static members on generic types
        {
            string stringSymbol;
            if (symbol == null)
            {
                stringSymbol = string.Empty;
            }
            else
            {
                switch (symbol.ID.SecurityType)
                {
                    case SecurityType.Option:
                        stringSymbol = symbol.HasUnderlying ? symbol.Underlying.Value : string.Empty;
                        break;
                    case SecurityType.IndexOption:
                    case SecurityType.FutureOption:
                        stringSymbol = symbol.HasUnderlying ? symbol.ID.Symbol : string.Empty;
                        break;
                    case SecurityType.Base:
                    case SecurityType.Future:
                        stringSymbol = symbol.ID.Symbol;
                        break;
                    default:
                        stringSymbol = symbol.Value;
                        break;
                }
            }

            return stringSymbol;
        }
    }
}
