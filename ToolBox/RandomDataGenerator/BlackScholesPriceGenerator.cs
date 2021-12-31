using QuantConnect.Securities;
using System;
using QuantConnect.Data.Market;


namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Pricing model used to determine the fair price or theoretical value for a call or a put option based
    /// </summary>
    public class BlackScholesPriceGenerator : IPriceGenerator
    {
        private readonly Securities.Option.Option _option;

        public BlackScholesPriceGenerator(Security security)
        {
            if (security == null)
            {
                throw new ArgumentNullException(nameof(security), "security cannot be null");
            }

            if (!security.Symbol.SecurityType.IsOption())
            {
                throw new ArgumentException("Black-Scholes pricing model cannot be applied to non-option security.");
            }

            _option = security as Securities.Option.Option;
        }

        public decimal NextReferencePrice(
            decimal referencePrice,
            decimal maximumPercentDeviation
            )
            => _option.Underlying.Price;

        public decimal NextValue(decimal referencePrice, DateTime referenceDate)
        {
            return _option.PriceModel
                .Evaluate(
                    _option,
                    null,
                    OptionContract.Create(
                        _option.Symbol,
                        _option.Symbol.Underlying,
                        referenceDate,
                        _option,
                        referencePrice
                        ))
                .TheoreticalPrice;
        }
    }
}
