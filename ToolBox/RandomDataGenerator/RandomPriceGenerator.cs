using System;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class RandomPriceGenerator : IPriceGenerator
    {
        private readonly Symbol _symbol;
        private readonly IRandomValueGenerator _random;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="random"></param>
        public RandomPriceGenerator(Symbol symbol, IRandomValueGenerator random)
        {
            _symbol = symbol;
            _random = random;
        }

        public decimal NextReferencePrice(decimal referencePrice, decimal maximumPercentDeviation)
            => _random.NextPrice(_symbol.SecurityType, _symbol.ID.Market, referencePrice, maximumPercentDeviation);
        
        public decimal NextValue(decimal referencePrice, DateTime referenceDate)
            => referencePrice;
    }
}
