using System;
using QLNet;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Securities.Option;
using QuantConnect.Statistics;


namespace QuantConnect.ToolBox.RandomDataGenerator
{
    /// <summary>
    /// Pricing model used to determine the fair price or theoretical value for a call or a put option based
    /// </summary>
    public class BlackScholesTickGenerator : TickGenerator
    {
        private readonly IOptionPriceModel _optionPriceModel;
        private readonly ISecurityService _securityService;
        private static IQLUnderlyingVolatilityEstimator _underlyingVolEstimator = new ConstantQLUnderlyingVolatilityEstimator();
        private static IQLRiskFreeRateEstimator _riskFreeRateEstimator = new ConstantQLRiskFreeRateEstimator();
        private static IQLDividendYieldEstimator _dividendYieldEstimator = new ConstantQLDividendYieldEstimator(Convert.ToDouble(PortfolioStatistics.GetRiskFreeRate()));

        public BlackScholesTickGenerator(RandomDataGeneratorSettings settings)
            : base(settings)
        {
        }

        public BlackScholesTickGenerator(RandomDataGeneratorSettings settings, IRandomValueGenerator random)
            : base(settings, random)
        {
            _optionPriceModel = new QLOptionPriceModel(process => new AnalyticEuropeanEngine(process),
                _underlyingVolEstimator,
                _riskFreeRateEstimator,
                _dividendYieldEstimator);
        }

        public override decimal NextValue(Symbol symbol, decimal referencePrice)
        {
            if (symbol.SecurityType != SecurityType.Option)
            {
                throw new ArgumentException("Please use TickGenerator for non options.");
            }
            
            var underlyingSecurity = _securityService.CreateSecurity(
                symbol.Underlying,
                new SubscriptionDataConfig(typeof(QuoteBar), symbol.Underlying, Settings.Resolution, TimeZones.Utc, TimeZones.Utc, false, true, false));
            var security = _securityService.CreateSecurity(
                symbol,
                new SubscriptionDataConfig(typeof(QuoteBar), symbol, Settings.Resolution, TimeZones.Utc, TimeZones.Utc,
                    false, true, false),
                addToSymbolCache: false,
                underlying: underlyingSecurity);
            return _optionPriceModel.Evaluate(security, null, null)
                .TheoreticalPrice;
        }
    }
}
