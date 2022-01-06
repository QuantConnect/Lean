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

using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using QLNet;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates random tick data according to the settings provided
    /// </summary>
    public class TickGenerator : ITickGenerator
    {
        private IPriceGenerator _priceGenerator;

        protected IRandomValueGenerator Random;
        protected RandomDataGeneratorSettings Settings;
        protected TickType[] TickTypes;

        protected MarketHoursDatabase MarketHoursDatabase { get; }
        protected SymbolPropertiesDatabase SymbolPropertiesDatabase { get; }
        public Security Security { get; }
        public Symbol Symbol => Security.Symbol;

        public TickGenerator(RandomDataGeneratorSettings settings, TickType[] tickTypes, Security security, IRandomValueGenerator random)
        {
            Random = random;
            Settings = settings;
            TickTypes = tickTypes;
            Security = security;
            SymbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
            MarketHoursDatabase = MarketHoursDatabase.FromDataFolder();

            if (Symbol.SecurityType.IsOption())
            {
                _priceGenerator = new BlackScholesPriceGenerator(security);
            }
            else
            {
                _priceGenerator = new RandomPriceGenerator(security, random);
            }
        }

        public IEnumerable<Tick> GenerateTicks()
        {
            var current = Settings.Start;

            // There is a possibility that even though this succeeds, the DateTime
            // generated may be the same as the starting DateTime, although the probability
            // of this happening diminishes the longer the period we're generating data for is
            if (Random.NextBool(Settings.HasIpoPercentage))
            {
                current = Random.NextDate(Settings.Start, Settings.End, null);
                Console.WriteLine($"\tSymbol: {Symbol} has delayed IPO at date {current:yyyy MMMM dd}");
            }

            // creates a max deviation that scales parabolically as resolution decreases (lower frequency)
            var deviation = GetMaximumDeviation(Settings.Resolution);
            while (current <= Settings.End)
            {

                var next = NextTickTime(current, Settings.Resolution, Settings.DataDensity);
                if (TickTypes.Contains(TickType.OpenInterest))
                {
                    if (next.Date != current.Date)
                    {
                        // 5% deviation in daily OI
                        var openInterest = NextTick(next.Date, TickType.OpenInterest, 5m);
                        yield return openInterest;
                    }
                }

                Tick nextTick = null;
                // keeps quotes close to the trades for consistency
                if (TickTypes.Contains(TickType.Trade) &&
                    TickTypes.Contains(TickType.Quote))
                {
                    // %odds of getting a trade tick, for example, a quote:trade ratio of 2 means twice as likely
                    // to get a quote, which means you have a 33% chance of getting a trade => 1/3
                    var tradeChancePercent = 100 / (1 + Settings.QuoteTradeRatio);
                    nextTick = NextTick(
                        next,
                        Random.NextBool(tradeChancePercent)
                            ? TickType.Trade
                            : TickType.Quote,
                        deviation);
                }
                else if (TickTypes.Contains(TickType.Trade))
                {
                    nextTick = NextTick(next, TickType.Trade, deviation);

                }
                else if (TickTypes.Contains(TickType.Quote))
                {
                    nextTick = NextTick(next, TickType.Quote, deviation);
                }

                if (nextTick != null && _priceGenerator.WarmedUp)
                {
                    yield return nextTick;
                }

                // advance to the next time step
                current = next;
            }
        }

        /// <summary>
        /// Generates a random <see cref="Tick"/> that is at most the specified <paramref name="maximumPercentDeviation"/> away from the
        /// previous price and is of the requested <paramref name="tickType"/>
        /// </summary>
        /// <param name="dateTime">The time of the generated tick</param>
        /// <param name="tickType">The type of <see cref="Tick"/> to be generated</param>
        /// <param name="maximumPercentDeviation">The maximum percentage to deviate from the
        /// previous price for example, 1 would indicate a maximum of 1% deviation from the
        /// previous price. For a previous price of 100, this would yield a price between 99 and 101 inclusive</param>
        /// <returns>A random <see cref="Tick"/> value that is within the specified <paramref name="maximumPercentDeviation"/>
        /// from the previous price</returns>
        public virtual Tick NextTick(DateTime dateTime, TickType tickType, decimal maximumPercentDeviation)
        {
            var next = _priceGenerator.NextValue(maximumPercentDeviation, dateTime);
            var tick = new Tick
            {
                Time = dateTime,
                Symbol = Symbol,
                TickType = tickType,
                Value = next
            };

            switch (tickType)
            {
                case TickType.OpenInterest:
                    return NextOpenInterest(dateTime, Security.OpenInterest, maximumPercentDeviation);

                case TickType.Trade:
                    tick.Quantity = Random.NextInt(1, 1500);
                    return tick;

                case TickType.Quote:
                    var bid = Random.NextPrice(Symbol.SecurityType, Symbol.ID.Market, tick.Value, maximumPercentDeviation);
                    if (bid > tick.Value)
                    {
                        bid = tick.Value - (bid - tick.Value);
                    }
                    var ask = Random.NextPrice(Symbol.SecurityType, Symbol.ID.Market, tick.Value, maximumPercentDeviation);
                    if (ask < tick.Value)
                    {
                        ask = tick.Value + (tick.Value - ask);
                    }

                    tick.BidPrice = bid;
                    tick.BidSize = Random.NextInt(1, 1500);
                    tick.AskPrice = ask;
                    tick.AskSize = Random.NextInt(1, 1500);
                    return tick;

                default:
                    throw new ArgumentOutOfRangeException(nameof(tickType), tickType, null);
            }
        }

        /// <summary>
        /// Generates a random <see cref="Tick"/> that is at most the specified <paramref name="maximumPercentDeviation"/> away from the
        /// <paramref name="previousValue"/> and is of the Open Interest
        /// </summary>
        /// <param name="dateTime">The time of the generated tick</param>
        /// <param name="previousValue">The previous price, used as a reference for generating
        /// new random prices for the next time step</param>
        /// <param name="maximumPercentDeviation">The maximum percentage to deviate from the
        /// <paramref name="previousValue"/>, for example, 1 would indicate a maximum of 1% deviation from the
        /// <paramref name="previousValue"/>. For a previous price of 100, this would yield a price between 99 and 101 inclusive</param>
        /// <returns>A random <see cref="Tick"/> value that is within the specified <paramref name="maximumPercentDeviation"/>
        /// from the <paramref name="previousValue"/></returns>
        public Tick NextOpenInterest(DateTime dateTime, decimal previousValue, decimal maximumPercentDeviation)
        {
            var next = (long)Random.NextPrice(Symbol.SecurityType, Symbol.ID.Market, previousValue, maximumPercentDeviation);
            return new Tick
            {
                Time = dateTime,
                Symbol = Symbol,
                TickType = TickType.OpenInterest,
                Value = next,
                Quantity = next
            };
        }

        /// <summary>
        /// Generates a random <see cref="DateTime"/> suitable for use as a tick's emit time.
        /// If the density provided is <see cref="DataDensity.Dense"/>, then at least one tick will be generated per <paramref name="resolution"/> step.
        /// If the density provided is <see cref="DataDensity.Sparse"/>, then at least one tick will be generated every 5 <paramref name="resolution"/> steps.
        /// if the density provided is <see cref="DataDensity.VerySparse"/>, then at least one tick will be generated every 50 <paramref name="resolution"/> steps.
        /// Times returned are guaranteed to be within market hours for the specified Symbol
        /// </summary>
        /// <param name="previous">The previous tick time</param>
        /// <param name="resolution">The requested resolution of data</param>
        /// <param name="density">The requested data density</param>
        /// <returns>A new <see cref="DateTime"/> that is after <paramref name="previous"/> according to the specified <paramref name="resolution"/>
        /// and <paramref name="density"/> specified</returns>
        public virtual DateTime NextTickTime(DateTime previous, Resolution resolution, DataDensity density)
        {
            var increment = resolution.ToTimeSpan();
            if (increment == TimeSpan.Zero)
            {
                increment = TimeSpan.FromMilliseconds(500);
            }

            double steps;
            switch (density)
            {
                case DataDensity.Dense:
                    steps = 0.5 * Random.NextDouble();
                    break;

                case DataDensity.Sparse:
                    steps = 5 * Random.NextDouble();
                    break;

                case DataDensity.VerySparse:
                    steps = 50 * Random.NextDouble();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(density), density, null);
            }

            var delta = TimeSpan.FromTicks((long)(steps * increment.Ticks));
            var tickTime = previous.Add(delta);
            if (tickTime == previous)
            {
                tickTime = tickTime.Add(increment);
            }

            var barStart = tickTime.Subtract(increment);
            var marketHours = MarketHoursDatabase.GetExchangeHours(Symbol.ID.Market, Symbol, Symbol.SecurityType);
            if (!marketHours.IsDateOpen(tickTime) || !marketHours.IsOpen(barStart, tickTime, false))
            {
                // we ended up outside of market hours, emit a new tick at market open
                var nextMarketOpen = marketHours.GetNextMarketOpen(tickTime, false);
                if (resolution == Resolution.Tick)
                {
                    resolution = Resolution.Second;
                }

                // emit a new tick somewhere in the next trading day at a step higher resolution to guarantee a hit
                return NextTickTime(nextMarketOpen, resolution - 1, density);
            }

            return tickTime;
        }

        private static decimal GetMaximumDeviation(Resolution resolution)
        {
            var incr = ((int)resolution) + 0.15m;
            var deviation = incr * incr * 0.1m;
            return deviation;
        }
    }
}
