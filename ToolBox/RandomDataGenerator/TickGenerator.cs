using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Generates random tick data according to the settings provided
    /// </summary>
    public class TickGenerator
    {
        private readonly IRandomValueGenerator _random;
        private readonly RandomDataGeneratorSettings _settings;

        public TickGenerator(RandomDataGeneratorSettings settings)
        {
            _settings = settings;
            _random = new RandomValueGenerator();
        }

        public TickGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random)
        {
            _random = random;
            _settings = settings;
        }

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
                var next = _random.NextTickTime(symbol, current, _settings.Resolution, _settings.DataDensity);
                if (_settings.TickTypes.Contains(TickType.OpenInterest))
                {
                    if (next.Date != current.Date)
                    {
                        // 5% deviation in daily OI
                        var previous = previousValues[TickType.OpenInterest];
                        var openInterest = _random.NextTick(symbol, next.Date, TickType.OpenInterest, previous, 5m);
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
                        var nextTrade = _random.NextTick(symbol, next, TickType.Trade, previousValue, deviation);
                        previousValues[TickType.Trade] = nextTrade.Value;
                        yield return nextTrade;
                    }
                    else
                    {
                        var nextQuote = _random.NextTick(symbol, next, TickType.Quote, previousValue, deviation);
                        previousValues[TickType.Trade] = nextQuote.Value;
                        yield return nextQuote;
                    }

                }
                else if(_settings.TickTypes.Contains(TickType.Trade))
                {
                    var nextTrade = _random.NextTick(symbol, next, TickType.Trade, previousValues[TickType.Trade], deviation);
                    previousValues[TickType.Trade] = nextTrade.Value;
                    yield return nextTrade;
                }
                else if (_settings.TickTypes.Contains(TickType.Quote))
                {
                    var nextQuote = _random.NextTick(symbol, next, TickType.Quote, previousValues[TickType.Quote], deviation);
                    previousValues[TickType.Quote] = nextQuote.Value;
                    yield return nextQuote;
                }

                // advance to the next time step
                current = next;
            }
        }

        private decimal GetMaximumDeviation(Resolution resolution)
        {
            var incr = ((int) resolution) + 0.15m;
            var deviation = incr * incr * 0.1m;
            return deviation;
        }
    }
}
