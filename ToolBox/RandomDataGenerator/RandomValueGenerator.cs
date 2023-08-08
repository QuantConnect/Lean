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

using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Linq;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Provides an implementation of <see cref="IRandomValueGenerator"/> that uses
    /// <see cref="Random"/> to generate random values
    /// </summary>
    public class RandomValueGenerator : IRandomValueGenerator
    {
        private readonly Random _random;
        private readonly MarketHoursDatabase _marketHoursDatabase;
        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase;
        private const decimal _maximumPriceAllowed = 1000000m;


        public RandomValueGenerator()
            : this(new Random())
        { }

        public RandomValueGenerator(int seed)
            : this(new Random(seed))
        { }

        public RandomValueGenerator(Random random)
            : this(random, MarketHoursDatabase.FromDataFolder(), SymbolPropertiesDatabase.FromDataFolder())
        { }

        public RandomValueGenerator(
            int seed,
            MarketHoursDatabase marketHoursDatabase,
            SymbolPropertiesDatabase symbolPropertiesDatabase
            )
            : this(new Random(seed), marketHoursDatabase, symbolPropertiesDatabase)
        { }

        public RandomValueGenerator(Random random, MarketHoursDatabase marketHoursDatabase, SymbolPropertiesDatabase symbolPropertiesDatabase)
        {
            _random = random;
            _marketHoursDatabase = marketHoursDatabase;
            _symbolPropertiesDatabase = symbolPropertiesDatabase;
        }

        public bool NextBool(double percentOddsForTrue)
        {
            return _random.NextDouble() <= percentOddsForTrue / 100;
        }

        public virtual DateTime NextDate(DateTime minDateTime, DateTime maxDateTime, DayOfWeek? dayOfWeek)
        {
            if (maxDateTime < minDateTime)
            {
                throw new ArgumentException(
                    "The maximum date time must be less than or equal to the minimum date time specified"
                );
            }

            // compute a random date time value
            var rangeInDays = (int)maxDateTime.Subtract(minDateTime).TotalDays;
            var daysOffsetFromMin = _random.Next(0, rangeInDays);
            var dateTime = minDateTime.AddDays(daysOffsetFromMin);

            var currentDayOfWeek = dateTime.DayOfWeek;
            if (!dayOfWeek.HasValue || currentDayOfWeek == dayOfWeek.Value)
            {
                // either DOW wasn't specified or we got REALLY lucky, although, I suppose it'll happen 1/7 (~14%) of the time
                return dateTime;
            }

            var nextDayOfWeek = Enumerable.Range(0, 7)
                .Select(i => dateTime.AddDays(i))
                .First(dt => dt.DayOfWeek == dayOfWeek.Value);

            var previousDayOfWeek = Enumerable.Range(0, 7)
                .Select(i => dateTime.AddDays(-i))
                .First(dt => dt.DayOfWeek == dayOfWeek.Value);

            // both are valid dates, so chose one randomly
            if (IsWithinRange(nextDayOfWeek, minDateTime, maxDateTime) &&
                IsWithinRange(previousDayOfWeek, minDateTime, maxDateTime)
            )
            {
                return _random.Next(0, 1) == 0
                    ? previousDayOfWeek
                    : nextDayOfWeek;
            }

            if (IsWithinRange(nextDayOfWeek, minDateTime, maxDateTime))
            {
                return nextDayOfWeek;
            }

            if (IsWithinRange(previousDayOfWeek, minDateTime, maxDateTime))
            {
                return previousDayOfWeek;
            }

            throw new ArgumentException("The provided min and max dates do not have the requested day of week between them");
        }

        public double NextDouble() => _random.NextDouble();

        public int NextInt(int v1, int v2) => _random.Next(v1, v2);

        public int NextInt(int v1) => _random.Next(v1);

        /// <summary>
        /// Generates a random <see cref="decimal"/> suitable as a price. This should observe minimum price
        /// variations if available in <see cref="SymbolPropertiesDatabase"/>, and if not, truncating to 2
        /// decimal places.
        /// </summary>
        /// <exception cref="ArgumentException">Throw when the <paramref name="referencePrice"/> or <paramref name="maximumPercentDeviation"/>
        /// is less than or equal to zero.</exception>
        /// <param name="securityType">The security type the price is being generated for</param>
        /// <param name="market">The market of the security the price is being generated for</param>
        /// <param name="referencePrice">The reference price used as the mean of random price generation</param>
        /// <param name="maximumPercentDeviation">The maximum percent deviation. This value is in percent space,
        ///     so a value of 1m is equal to 1%.</param>
        /// <returns>A new decimal suitable for usage as price within the specified deviation from the reference price</returns>
        public virtual decimal NextPrice(SecurityType securityType, string market, decimal referencePrice, decimal maximumPercentDeviation)
        {
            if (referencePrice <= 0)
            {
                if (securityType == SecurityType.Option && referencePrice == 0)
                {
                    return 0;
                }
                throw new ArgumentException("The provided reference price must be a positive number.");
            }

            if (maximumPercentDeviation <= 0)
            {
                throw new ArgumentException("The provided maximum percent deviation must be a positive number");
            }

            // convert from percent space to decimal space
            maximumPercentDeviation /= 100m;

            var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(market, null, securityType, "USD");
            var minimumPriceVariation = symbolProperties.MinimumPriceVariation;

            decimal price;
            var attempts = 0;
            var increaseProbabilityFactor = 0.5;
            do
            {
                // what follows is a simple model of browning motion that
                // limits the walk to the specified percent deviation

                var deviation = referencePrice * maximumPercentDeviation * (decimal)(NextDouble() - increaseProbabilityFactor);
                deviation = Math.Sign(deviation) * Math.Max(Math.Abs(deviation), minimumPriceVariation);
                price = referencePrice + deviation;
                price = RoundPrice(price, minimumPriceVariation);

                if (price < 20 * minimumPriceVariation)
                {
                    // The price should not be to close to the minimum price variation.
                    // Invalidate the price to try again and increase the probability of it to going up
                    price = -1m;
                    increaseProbabilityFactor = Math.Max(increaseProbabilityFactor - 0.05, 0);
                }

                if (price > (_maximumPriceAllowed / 10m))
                {
                    // The price should not be too higher
                    // Decrease the probability of it to going up
                    increaseProbabilityFactor = increaseProbabilityFactor + 0.05;
                }

                if (price > _maximumPriceAllowed)
                {
                    // The price should not be too higher
                    // Invalidate the price to try again
                    price = -1;
                }
            } while (!IsPriceValid(securityType, price) && ++attempts < 10);

            if (!IsPriceValid(securityType, price))
            {
                // if still invalid, use the last price
                price = referencePrice;
            }

            return price;
        }

        private static decimal RoundPrice(decimal price, decimal minimumPriceVariation)
        {
            if (minimumPriceVariation == 0) return minimumPriceVariation;
            return Math.Round(price / minimumPriceVariation) * minimumPriceVariation;
        }

        private bool IsWithinRange(DateTime value, DateTime min, DateTime max)
        {
            return value >= min && value <= max;
        }

        private static bool IsPriceValid(SecurityType securityType, decimal price)
        {
            switch (securityType)
            {
                case SecurityType.Option:
                {
                    return price >= 0;
                }
                default:
                {
                    return price > 0 && price < _maximumPriceAllowed;
                }
            }
        }
    }
}
