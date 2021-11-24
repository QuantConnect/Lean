using System;
using QLNet;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Statistics;
using Option = QLNet.Option;


namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Pricing model used to determine the fair price or theoretical value for a call or a put option based
    /// </summary>
    public class BlackScholesTickGenerator : TickGenerator
    {
        private readonly IOptionPriceModel _optionPriceModel;
        private readonly ISecurityService _securityService;
        private readonly Securities.Option.Option _option;
        private static IQLUnderlyingVolatilityEstimator _underlyingVolEstimator = new ConstantQLUnderlyingVolatilityEstimator();
        private static IQLRiskFreeRateEstimator _riskFreeRateEstimator = new ConstantQLRiskFreeRateEstimator();
        private static IQLDividendYieldEstimator _dividendYieldEstimator = new ConstantQLDividendYieldEstimator(Convert.ToDouble(PortfolioStatistics.GetRiskFreeRate()));
        
        public BlackScholesTickGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random, Security security)
            : base(settings, random, security.Symbol)
        {
            _option = security as Securities.Option.Option;

            _optionPriceModel = new QLOptionPriceModel(process => new AnalyticEuropeanEngine(process),
                _underlyingVolEstimator,
                _riskFreeRateEstimator,
                _dividendYieldEstimator);
        }

        public override decimal NextValue(decimal referencePrice)
        {
            if (Symbol.SecurityType != SecurityType.Option)
            {
                throw new ArgumentException("Please use TickGenerator for non options.");
            }

            var underlyingSecurity = _securityService.CreateSecurity(
                Symbol.Underlying,
                new SubscriptionDataConfig(typeof(QuoteBar), Symbol.Underlying, Settings.Resolution, TimeZones.Utc, TimeZones.NewYork, false, true, false));
            var security = _securityService.CreateSecurity(
                Symbol,
                new SubscriptionDataConfig(typeof(QuoteBar), Symbol, Settings.Resolution, TimeZones.Utc, TimeZones.NewYork,
                    false, true, false),
                addToSymbolCache: false,
                underlying: underlyingSecurity);

            return (security as Securities.Option.Option).PriceModel.Evaluate(security, null, null)
                .TheoreticalPrice;
        }
    }
}
