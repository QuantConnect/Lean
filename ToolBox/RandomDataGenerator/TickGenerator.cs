using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates random tick data according to the settings provided
    /// </summary>
    public class TickGenerator : ITickGenerator
    {
        private readonly IRandomValueGenerator _random;
        private readonly RandomDataGeneratorSettings _settings;

        protected MarketHoursDatabase MarketHoursDatabase { get; }
        protected SymbolPropertiesDatabase SymbolPropertiesDatabase { get; }

        public TickGenerator(RandomDataGeneratorSettings settings) : this(settings, new RandomValueGenerator())
        { }

        public TickGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random)
        {
            _random = random;
            _settings = settings;
            SymbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
            MarketHoursDatabase = MarketHoursDatabase.FromDataFolder();
        }

        /// <summary>
        /// Generates a random <see cref="Tick"/> that is at most the specified <paramref name="maximumPercentDeviation"/> away from the
        /// <paramref name="previousValue"/> and is of the requested <paramref name="tickType"/>
        /// </summary>
        /// <param name="symbol">The symbol of the generated tick</param>
        /// <param name="dateTime">The time of the generated tick</param>
        /// <param name="tickType">The type of <see cref="Tick"/> to be generated</param>
        /// <param name="previousValue">The previous price, used as a reference for generating
        /// new random prices for the next time step</param>
        /// <param name="maximumPercentDeviation">The maximum percentage to deviate from the
        /// <paramref name="previousValue"/>, for example, 1 would indicate a maximum of 1% deviation from the
        /// <paramref name="previousValue"/>. For a previous price of 100, this would yield a price between 99 and 101 inclusive</param>
        /// <returns>A random <see cref="Tick"/> value that is within the specified <paramref name="maximumPercentDeviation"/>
        /// from the <paramref name="previousValue"/></returns>
        public IEnumerable<Tick> GenerateTicks(Symbol symbol)
        {
            var previousValues = new Dictionary<TickType, decimal>
            {
                {TickType.Trade, 100m},
                {TickType.Quote, 100m},
                {TickType.OpenInterest, 10000m}
            };

            var current = _settings.Start;

            // There is a possibility that even though this succeeds, the DateTime
            // generated may be the same as the starting DateTime, although the probability
            // of this happening diminishes the longer the period we're generating data for is
            if (_random.NextBool(_settings.HasIpoPercentage))
            {
                current = _random.NextDate(_settings.Start, _settings.End, null);
                Console.WriteLine($"\tSymbol: {symbol} has delayed IPO at date {current:yyyy MMMM dd}");
            }

            // creates a max deviation that scales parabolically as resolution decreases (lower frequency)
            var deviation = GetMaximumDeviation(_settings.Resolution);
            while (current <= _settings.End)
            {
                var next = NextTickTime(symbol, current, _settings.Resolution, _settings.DataDensity);
                if (_settings.TickTypes.Contains(TickType.OpenInterest))
                {
                    if (next.Date != current.Date)
                    {
                        // 5% deviation in daily OI
                        var previous = previousValues[TickType.OpenInterest];
                        var openInterest = NextTick(symbol, next.Date, TickType.OpenInterest, previous, 5m);
                        previousValues[TickType.OpenInterest] = openInterest.Value;
                        yield return openInterest;
                    }
                }

                // keeps quotes close to the trades for consistency
                if (_settings.TickTypes.Contains(TickType.Trade) &&
                    _settings.TickTypes.Contains(TickType.Quote))
                {
                    // since we're generating both trades and quotes we'll only reference one previous value
                    // to prevent the trade and quote prices from drifting away from each other
                    var previousValue = previousValues[TickType.Trade];

                    // %odds of getting a trade tick, for example, a quote:trade ratio of 2 means twice as likely
                    // to get a quote, which means you have a 33% chance of getting a trade => 1/3
                    var tradeChancePercent = 100 / (1 + _settings.QuoteTradeRatio);
                    if (_random.NextBool(tradeChancePercent))
                    {
                        var nextTrade = NextTick(symbol, next, TickType.Trade, previousValue, deviation);
                        previousValues[TickType.Trade] = nextTrade.Value;
                        yield return nextTrade;
                    }
                    else
                    {
                        var nextQuote = NextTick(symbol, next, TickType.Quote, previousValue, deviation);
                        previousValues[TickType.Trade] = nextQuote.Value;
                        yield return nextQuote;
                    }

                }
                else if (_settings.TickTypes.Contains(TickType.Trade))
                {
                    var nextTrade = NextTick(symbol, next, TickType.Trade, previousValues[TickType.Trade], deviation);
                    previousValues[TickType.Trade] = nextTrade.Value;
                    yield return nextTrade;
                }
                else if (_settings.TickTypes.Contains(TickType.Quote))
                {
                    var nextQuote = NextTick(symbol, next, TickType.Quote, previousValues[TickType.Quote], deviation);
                    previousValues[TickType.Quote] = nextQuote.Value;
                    yield return nextQuote;
                }

                // advance to the next time step
                current = next;
            }
        }

        /// <summary>
        /// Generates a random <see cref="Tick"/> that is at most the specified <paramref name="maximumPercentDeviation"/> away from the
        /// <paramref name="previousValue"/> and is of the requested <paramref name="tickType"/>
        /// </summary>
        /// <param name="symbol">The symbol of the generated tick</param>
        /// <param name="dateTime">The time of the generated tick</param>
        /// <param name="tickType">The type of <see cref="Tick"/> to be generated</param>
        /// <param name="previousValue">The previous price, used as a reference for generating
        /// new random prices for the next time step</param>
        /// <param name="maximumPercentDeviation">The maximum percentage to deviate from the
        /// <paramref name="previousValue"/>, for example, 1 would indicate a maximum of 1% deviation from the
        /// <paramref name="previousValue"/>. For a previous price of 100, this would yield a price between 99 and 101 inclusive</param>
        /// <returns>A random <see cref="Tick"/> value that is within the specified <paramref name="maximumPercentDeviation"/>
        /// from the <paramref name="previousValue"/></returns>
        public virtual Tick NextTick(Symbol symbol, DateTime dateTime, TickType tickType, decimal previousValue, decimal maximumPercentDeviation)
        {
            var next = _random.NextPrice(symbol.SecurityType, symbol.ID.Market, previousValue, maximumPercentDeviation);
            var tick = new Tick
            {
                Time = dateTime,
                Symbol = symbol,
                TickType = tickType,
                Value = next
            };

            switch (tickType)
            {
                case TickType.Trade:
                    tick.Quantity = _random.NextInt(1, 1500);
                    return tick;

                case TickType.Quote:
                    var bid = _random.NextPrice(symbol.SecurityType, symbol.ID.Market, tick.Value, maximumPercentDeviation);
                    if (bid > tick.Value)
                    {
                        bid = tick.Value - (bid - tick.Value);
                    }
                    var ask = _random.NextPrice(symbol.SecurityType, symbol.ID.Market, tick.Value, maximumPercentDeviation);
                    if (ask < tick.Value)
                    {
                        ask = tick.Value + (tick.Value - ask);
                    }

                    tick.BidPrice = bid;
                    tick.BidSize = _random.NextInt(1, 1500);
                    tick.AskPrice = ask;
                    tick.AskSize = _random.NextInt(1, 1500);
                    return tick;

                case TickType.OpenInterest:
                    tick.Value = (long)tick.Value;
                    tick.Quantity = tick.Value;
                    return tick;

                default:
                    throw new ArgumentOutOfRangeException(nameof(tickType), tickType, null);
            }
        }

        /// <summary>
        /// Generates a random <see cref="DateTime"/> suitable for use as a tick's emit time.
        /// If the density provided is <see cref="DataDensity.Dense"/>, then at least one tick will be generated per <paramref name="resolution"/> step.
        /// If the density provided is <see cref="DataDensity.Sparse"/>, then at least one tick will be generated every 5 <paramref name="resolution"/> steps.
        /// if the density provided is <see cref="DataDensity.VerySparse"/>, then at least one tick will be generated every 50 <paramref name="resolution"/> steps.
        /// Times returned are guaranteed to be within market hours for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol to generate a new tick time for</param>
        /// <param name="previous">The previous tick time</param>
        /// <param name="resolution">The requested resolution of data</param>
        /// <param name="density">The requested data density</param>
        /// <returns>A new <see cref="DateTime"/> that is after <paramref name="previous"/> according to the specified <paramref name="resolution"/>
        /// and <paramref name="density"/> specified</returns>
        public virtual DateTime NextTickTime(Symbol symbol, DateTime previous, Resolution resolution, DataDensity density)
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
                    steps = 0.5 * _random.NextDouble();
                    break;

                case DataDensity.Sparse:
                    steps = 5 * _random.NextDouble();
                    break;

                case DataDensity.VerySparse:
                    steps = 50 * _random.NextDouble();
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
            var marketHours = MarketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
            if (!marketHours.IsDateOpen(tickTime) || !marketHours.IsOpen(barStart, tickTime, false))
            {
                // we ended up outside of market hours, emit a new tick at market open
                var nextMarketOpen = marketHours.GetNextMarketOpen(tickTime, false);
                if (resolution == Resolution.Tick)
                {
                    resolution = Resolution.Second;
                }

                // emit a new tick somewhere in the next trading day at a step higher resolution to guarantee a hit
                return NextTickTime(symbol, nextMarketOpen, resolution - 1, density);
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
