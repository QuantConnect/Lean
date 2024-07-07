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
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Provide the base symbol generator implementation
    /// </summary>
    public abstract class BaseSymbolGenerator
    {
        /// <summary>
        /// <see cref="IRandomValueGenerator"/> instance producing random values for use in random data generation
        /// </summary>
        protected IRandomValueGenerator Random { get; }

        /// <summary>
        /// Settings of current random data generation run
        /// </summary>
        protected RandomDataGeneratorSettings Settings { get; }

        /// <summary>
        /// Exchange hours and raw data times zones in various markets
        /// </summary>
        protected MarketHoursDatabase MarketHoursDatabase { get; }

        /// <summary>
        /// Access to specific properties for various symbols
        /// </summary>
        protected SymbolPropertiesDatabase SymbolPropertiesDatabase { get; }

        // used to prevent generating duplicates, but also caps
        // the memory allocated to checking for duplicates
        private readonly FixedSizeHashQueue<Symbol> _symbols;

        /// <summary>
        /// Base constructor implementation for Symbol generator
        /// </summary>
        /// <param name="settings">random data generation run settings</param>
        /// <param name="random">produces random values for use in random data generation</param>
        protected BaseSymbolGenerator(
            RandomDataGeneratorSettings settings,
            IRandomValueGenerator random
        )
        {
            Settings = settings;
            Random = random;
            _symbols = new FixedSizeHashQueue<Symbol>(1000);
            SymbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
            MarketHoursDatabase = MarketHoursDatabase.FromDataFolder();
        }

        /// <summary>
        /// Creates a ad-hoc symbol generator depending on settings
        /// </summary>
        /// <param name="settings">random data generator settings</param>
        /// <param name="random">produces random values for use in random data generation</param>
        /// <returns>New symbol generator</returns>
        public static BaseSymbolGenerator Create(
            RandomDataGeneratorSettings settings,
            IRandomValueGenerator random
        )
        {
            if (settings is null)
            {
                throw new ArgumentNullException(
                    nameof(settings),
                    "Settings cannot be null or empty"
                );
            }

            if (random is null)
            {
                throw new ArgumentNullException(nameof(random), "Randomizer cannot be null");
            }

            switch (settings.SecurityType)
            {
                case SecurityType.Option:
                    return new OptionSymbolGenerator(settings, random, 100m, 75m);

                case SecurityType.Future:
                    return new FutureSymbolGenerator(settings, random);

                default:
                    return new DefaultSymbolGenerator(settings, random);
            }
        }

        /// <summary>
        /// Generates specified number of symbols
        /// </summary>
        /// <returns>Set of random symbols</returns>
        public IEnumerable<Symbol> GenerateRandomSymbols()
        {
            if (!Settings.Tickers.IsNullOrEmpty())
            {
                foreach (var symbol in Settings.Tickers.SelectMany(GenerateAsset))
                {
                    yield return symbol;
                }
            }
            else
            {
                for (var i = 0; i < Settings.SymbolCount; i++)
                {
                    foreach (var symbol in GenerateAsset())
                    {
                        yield return symbol;
                    }
                }
            }
        }

        /// <summary>
        /// Generates a random asset
        /// </summary>
        /// <param name="ticker">Optionally can provide a ticker that should be used</param>
        /// <returns>Random asset</returns>
        protected abstract IEnumerable<Symbol> GenerateAsset(string ticker = null);

        /// <summary>
        /// Generates random symbol, used further down for asset
        /// </summary>
        /// <param name="securityType">security type</param>
        /// <param name="market">market</param>
        /// <param name="ticker">Optionally can provide a ticker to use</param>
        /// <returns>Random symbol</returns>
        public Symbol NextSymbol(SecurityType securityType, string market, string ticker = null)
        {
            if (securityType == SecurityType.Option || securityType == SecurityType.Future)
            {
                throw new ArgumentException(
                    "Please use OptionSymbolGenerator or FutureSymbolGenerator for SecurityType.Option and SecurityType.Future respectively."
                );
            }

            if (ticker == null)
            {
                // we must return a Symbol matching an entry in the Symbol properties database
                // if there is a wildcard entry, we can generate a truly random Symbol
                // if there is no wildcard entry, the symbols we can generate are limited by the entries in the database
                if (
                    SymbolPropertiesDatabase.ContainsKey(
                        market,
                        SecurityDatabaseKey.Wildcard,
                        securityType
                    )
                )
                {
                    // let's make symbols all have 3 chars as it's acceptable for all security types with wildcard entries
                    ticker = NextUpperCaseString(3, 3);
                }
                else
                {
                    ticker = NextTickerFromSymbolPropertiesDatabase(securityType, market);
                }
            }

            // by chance we may generate a ticker that actually exists, and if map files exist that match this
            // ticker then we'll end up resolving the first trading date for use in the SID, otherwise, all
            // generated Symbol will have a date equal to SecurityIdentifier.DefaultDate
            var symbol = Symbol.Create(ticker, securityType, market);
            if (_symbols.Add(symbol))
            {
                return symbol;
            }

            // lo' and behold, we created a duplicate --recurse to find a unique value
            // this is purposefully done as the last statement to enable the compiler to
            // unroll this method into a tail-recursion loop :)
            return NextSymbol(securityType, market);
        }

        /// <summary>
        /// Return a Ticker matching an entry in the Symbol properties database
        /// </summary>
        /// <param name="securityType">security type</param>
        /// <param name="market"></param>
        /// <returns>Random Ticker matching an entry in the Symbol properties database</returns>
        protected string NextTickerFromSymbolPropertiesDatabase(
            SecurityType securityType,
            string market
        )
        {
            // prevent returning a ticker matching any previously generated Symbol
            var existingTickers = _symbols
                .Where(sym => sym.ID.Market == market && sym.ID.SecurityType == securityType)
                .Select(sym => sym.Value);

            // get the available tickers from the Symbol properties database and remove previously generated tickers
            var availableTickers = Enumerable
                .Except(
                    SymbolPropertiesDatabase
                        .GetSymbolPropertiesList(market, securityType)
                        .Select(kvp => kvp.Key.Symbol),
                    existingTickers
                )
                .ToList();

            // there is a limited number of entries in the Symbol properties database so we may run out of tickers
            if (availableTickers.Count == 0)
            {
                throw new NoTickersAvailableException(securityType, market);
            }

            return availableTickers[Random.NextInt(availableTickers.Count)];
        }

        /// <summary>
        /// Generates random expiration date on a friday within specified time range
        /// </summary>
        /// <param name="marketHours">market hours</param>
        /// <param name="minExpiry">minimum expiration date</param>
        /// <param name="maxExpiry">maximum expiration date</param>
        /// <returns>Random date on a friday within specified time range</returns>
        protected DateTime GetRandomExpiration(
            SecurityExchangeHours marketHours,
            DateTime minExpiry,
            DateTime maxExpiry
        )
        {
            // generate a random expiration date on a friday
            var expiry = Random.NextDate(minExpiry, maxExpiry, DayOfWeek.Friday);

            // check to see if we're open on this date and if not, back track until we are
            // we're using the equity market hours as a proxy since we haven't generated the option Symbol yet
            while (!marketHours.IsDateOpen(expiry))
            {
                expiry = expiry.AddDays(-1);
            }

            return expiry;
        }

        /// <summary>
        /// Generates a random <see cref="string"/> within the specified lengths.
        /// </summary>
        /// <param name="minLength">The minimum length, inclusive</param>
        /// <param name="maxLength">The maximum length, inclusive</param>
        /// <returns>A new upper case string within the specified lengths</returns>
        public string NextUpperCaseString(int minLength, int maxLength)
        {
            var str = string.Empty;
            var length = Random.NextInt(minLength, maxLength);
            for (int i = 0; i < length; i++)
            {
                // A=65 - inclusive lower bound
                // Z=90 - inclusive upper bound
                var c = (char)Random.NextInt(65, 91);
                str += c;
            }

            return str;
        }

        /// <summary>
        /// Returns the number of symbols with the specified parameters can be generated.
        /// Returns int.MaxValue if there is no limit for the given parameters.
        /// </summary>
        /// <returns>The number of available symbols for the given parameters, or int.MaxValue if no limit</returns>
        public abstract int GetAvailableSymbolCount();
    }
}
