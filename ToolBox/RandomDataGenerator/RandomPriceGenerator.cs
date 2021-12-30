using System;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class RandomPriceGenerator : IPriceGenerator
    {
        private Symbol _symbol;
        private IRandomValueGenerator _random;

        public RandomPriceGenerator(Symbol symbol, IRandomValueGenerator random)
        {
            _symbol = symbol;
            _random = random;
        }

        public decimal NextReferencePrice(DateTime dateTime, decimal referencePrice, decimal maximumPercentDeviation)
            => _random.NextPrice(_symbol.SecurityType, _symbol.ID.Market, referencePrice, maximumPercentDeviation);


        public decimal NextValue(decimal referencePrice, DateTime referenceDate)
            => referencePrice;
    }
}
