using QuantConnect.Securities;
using System;
using QuantConnect.Data.Market;


namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Pricing model used to determine the fair price or theoretical value for a call or a put option based
    /// </summary>
    public class BlackScholesTickGenerator : TickGenerator
    {
        private readonly Securities.Option.Option _option;

        public BlackScholesTickGenerator(RandomDataGeneratorSettings settings, TickType[] tickTypes, IRandomValueGenerator random, Security security)
            : base(settings, tickTypes, security.Symbol)
        {
            _option = security as Securities.Option.Option;
        }

        public override decimal NextReferencePrice(
            DateTime dateTime,
            decimal referencePrice,
            decimal maximumPercentDeviation
            )
            => _option.Underlying.Price;

        public override decimal NextValue(decimal referencePrice, DateTime referenceDate)
        {
            if (Symbol.SecurityType != SecurityType.Option)
            {
                throw new ArgumentException("Please use TickGenerator for non options.");
            }

            return _option.PriceModel
                .Evaluate(
                    _option,
                    null,
                    OptionContract.Create(
                        Symbol,
                        Symbol.Underlying,
                        referenceDate.Add(Settings.Resolution.ToTimeSpan()),
                        _option,
                        referencePrice
                        ))
                .TheoreticalPrice;
        }
    }
}
