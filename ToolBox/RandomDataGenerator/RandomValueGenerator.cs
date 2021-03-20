using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using QuantConnect.Util;

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

        // used to prevent generating duplicates, but also caps
        // the memory allocated to checking for duplicates
        private readonly FixedSizeHashQueue<Symbol> _symbols;

        public RandomValueGenerator()
        {
            _random = new Random();
            _symbols = new FixedSizeHashQueue<Symbol>(1000);
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
        }

        public RandomValueGenerator(int seed)
        {
            _random = new Random(seed);
            _symbols = new FixedSizeHashQueue<Symbol>(1000);
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
        }

        public RandomValueGenerator(int seed, MarketHoursDatabase marketHoursDatabase, SymbolPropertiesDatabase symbolPropertiesDatabase)
        {
            _random = new Random(seed);
            _marketHoursDatabase = marketHoursDatabase;
            _symbols = new FixedSizeHashQueue<Symbol>(1000);
            _symbolPropertiesDatabase = symbolPropertiesDatabase;
        }

        public bool NextBool(double percentOddsForTrue)
        {
            return _random.NextDouble() <= percentOddsForTrue/100;
        }

        public virtual string NextUpperCaseString(int minLength, int maxLength)
        {
            var str = string.Empty;
            var length = _random.Next(minLength, maxLength);
            for (int i = 0; i < length; i++)
            {
                // A=65, Z=90
                var c = (char)_random.Next(65, 90);
                str += c;
            }

            return str;
        }

        public virtual decimal NextPrice(SecurityType securityType, string market, decimal referencePrice, decimal maximumPercentDeviation)
        {
            if (referencePrice <= 0)
            {
                throw new ArgumentException("The provided reference price must be a positive number.");
            }

            if (maximumPercentDeviation <= 0)
            {
                throw new ArgumentException("The provided maximum percent deviation must be a postive number");
            }

            // convert from percent space to decimal space
            maximumPercentDeviation /= 100m;

            var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(market, null, securityType, "USD");
            var minimumPriceVariation = symbolProperties.MinimumPriceVariation;

            decimal price;
            var attempts = 0;
            do
            {
                // what follows is a simple model of browning motion that
                // limits the walk to the specified percent deviation

                var deviation = referencePrice * maximumPercentDeviation * (decimal) (_random.NextDouble() - 0.5);
                price = referencePrice + deviation;
                price = RoundPrice(price, minimumPriceVariation);

                attempts++;
            } while (price <= 0 && attempts < 10);

            if (price <= 0)
            {
                // if still invalid, bail
                throw new TooManyFailedAttemptsException(nameof(NextPrice), attempts);
            }

            return price;
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
            var rangeInDays = (int) maxDateTime.Subtract(minDateTime).TotalDays;
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

            var delta = TimeSpan.FromTicks((long) (steps * increment.Ticks));
            var tickTime = previous.Add(delta);
            if (tickTime == previous)
            {
                tickTime = tickTime.Add(increment);
            }

            var barStart = tickTime.Subtract(increment);
            var marketHours = _marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType);
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

        public virtual Tick NextTick(Symbol symbol, DateTime dateTime, TickType tickType, decimal previousValue, decimal maximumPercentDeviation)
        {
            var next = NextPrice(symbol.SecurityType, symbol.ID.Market, previousValue, maximumPercentDeviation);
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
                    tick.Quantity = _random.Next(1, 1500);
                    return tick;

                case TickType.Quote:
                    var bid = NextPrice(symbol.SecurityType, symbol.ID.Market, tick.Value, maximumPercentDeviation);
                    if (bid > tick.Value)
                    {
                        bid = tick.Value - (bid - tick.Value);
                    }
                    var ask = NextPrice(symbol.SecurityType, symbol.ID.Market, tick.Value, maximumPercentDeviation);
                    if (ask < tick.Value)
                    {
                        ask = tick.Value + (tick.Value - ask);
                    }

                    tick.BidPrice = bid;
                    tick.BidSize = _random.Next(1, 1500);
                    tick.AskPrice = ask;
                    tick.AskSize = _random.Next(1, 1500);
                    return tick;

                case TickType.OpenInterest:
                    tick.Value = (long) tick.Value;
                    tick.Quantity = tick.Value;
                    return tick;

                default:
                    throw new ArgumentOutOfRangeException(nameof(tickType), tickType, null);
            }
        }

        public virtual Symbol NextSymbol(SecurityType securityType, string market)
        {
            if (securityType == SecurityType.Option || securityType == SecurityType.Future)
            {
                throw new ArgumentException("Please use NextOption or NextFuture for SecurityType.Option and SecurityType.Future respectively.");
            }

            // let's make symbols all have 3 chars as it's acceptable for all permitted security types in this method
            var ticker = NextUpperCaseString(3, 3);

            // by chance we may generate a ticker that actually exists, and if map files exist that match this
            // ticker then we'll end up resolving the first trading date for use in the SID, otherwise, all
            // generated symbol will have a date equal to SecurityIdentifier.DefaultDate
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

        public virtual Symbol NextOption(string market, DateTime minExpiry, DateTime maxExpiry, decimal underlyingPrice, decimal maximumStrikePriceDeviation)
        {
            // first generate the underlying
            var underlying = NextSymbol(SecurityType.Equity, market);

            var marketHours = _marketHoursDatabase.GetExchangeHours(market, underlying, SecurityType.Equity);
            var expiry = GetRandomExpiration(marketHours, minExpiry, maxExpiry);

            // generate a random strike while respecting the maximum deviation from the underlying's price
            // since these are underlying prices, use Equity as the security type
            var strike = NextPrice(SecurityType.Equity, market, underlyingPrice, maximumStrikePriceDeviation);

            // round the strike price to something reasonable
            var order = 1 + Math.Log10((double) strike);
            strike = strike.RoundToSignificantDigits((int) order);

            var optionRight = (OptionRight) _random.Next(0, 1);

            // when providing a null option w/ an expiry, it will automatically create the OSI ticker string for the Value
            return Symbol.CreateOption(underlying, market, OptionStyle.American, optionRight, strike, expiry);
        }

        public virtual Symbol NextFuture(string market, DateTime minExpiry, DateTime maxExpiry)
        {
            // futures are usually two characters
            var ticker = NextUpperCaseString(2, 2);

            var marketHours = _marketHoursDatabase.GetExchangeHours(market, null, SecurityType.Future);
            var expiry = GetRandomExpiration(marketHours, minExpiry, maxExpiry);

            return Symbol.CreateFuture(ticker, market, expiry);
        }

        private bool IsWithinRange(DateTime value, DateTime min, DateTime max)
        {
            return value >= min && value <= max;
        }

        private static decimal RoundPrice(decimal price, decimal minimumPriceVariation)
        {
            if (minimumPriceVariation == 0) return minimumPriceVariation;
            return Math.Round(price / minimumPriceVariation) * minimumPriceVariation;
        }

        private DateTime GetRandomExpiration(SecurityExchangeHours marketHours, DateTime minExpiry, DateTime maxExpiry)
        {
            // generate a random expiration date on a friday
            var expiry = NextDate(minExpiry, maxExpiry, DayOfWeek.Friday);

            // check to see if we're open on this date and if not, back track until we are
            // we're using the equity market hours as a proxy since we haven't generated the option symbol yet
            while (!marketHours.IsDateOpen(expiry))
            {
                expiry = expiry.AddDays(-1);
            }

            return expiry;
        }
    }
}
