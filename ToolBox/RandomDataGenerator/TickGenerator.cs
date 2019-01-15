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
        private readonly RandomDataGeneratorSettings _settings;
        private readonly IRandomValueGenerator _random;

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
            // create deviations like 0.025 for tick and
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
                    var nextTrade = _random.NextTick(symbol, next, TickType.Trade, previousValues[TickType.Trade], deviation);
                    previousValues[TickType.Trade] = nextTrade.Value;
                    yield return nextTrade;

                    // specify 0 to bind the mean to the previous trade for consistency
                    var nextQuote = _random.NextTick(symbol, next, TickType.Quote, nextTrade.Value, 0m);
                    previousValues[TickType.Quote] = nextQuote.Value;
                    yield return nextQuote;
                }
                else if(_settings.TickTypes.Contains(TickType.Trade))
                {
                    var nextTrade = _random.NextTick(symbol, next, TickType.Trade, previousValues[TickType.Trade], deviation);
                    previousValues[TickType.Trade] = nextTrade.Value;
                    yield return nextTrade;
                }
                else if (_settings.TickTypes.Contains(TickType.Quote))
                {
                    // specify 0 to bind the mean to the previous trade for consistency
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
