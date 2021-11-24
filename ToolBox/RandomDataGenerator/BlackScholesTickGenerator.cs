using QuantConnect.Securities;
using System;


namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Pricing model used to determine the fair price or theoretical value for a call or a put option based
    /// </summary>
    public class BlackScholesTickGenerator : TickGenerator
    {
        private readonly Securities.Option.Option _option;

        public BlackScholesTickGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random, Security security)
            : base(settings, random, security.Symbol)
        {
            _option = security as Securities.Option.Option;
        }

        public override decimal NextValue(decimal referencePrice)
        {
            if (Symbol.SecurityType != SecurityType.Option)
            {
                throw new ArgumentException("Please use TickGenerator for non options.");
            }

            return _option.PriceModel.Evaluate(_option, null, null)
                .TheoreticalPrice;
        }
    }
}
