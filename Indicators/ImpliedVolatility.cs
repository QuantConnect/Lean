/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using MathNet.Numerics.RootFinding;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Implied Volatility indicator that calculate the IV of an option using Black-Scholes Model
    /// </summary>
    public class ImpliedVolatility : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly Symbol _optionSymbol;
        private readonly Symbol _underlyingSymbol;
        private BaseDataConsolidator _consolidator;
        private RateOfChange _roc;
        private decimal _impliedVolatility;
        private OptionPricingModelType _optionModel;

        /// <summary>
        /// Gets the expiration time of the option
        /// </summary>
        public DateTime Expiry { get; }

        /// <summary>
        /// Gets the option right (call/put) of the option
        /// </summary>
        public OptionRight Right { get; }

        /// <summary>
        /// Risk Free Rate
        /// </summary>
        public decimal RiskFreeRate { get; set; }

        /// <summary>
        /// Gets the strike price of the option
        /// </summary>
        public decimal Strike { get; }

        /// <summary>
        /// Gets the option style (European/American) of the option
        /// </summary>
        public OptionStyle Style { get; }

        /// <summary>
        /// Gets the historical volatility of the underlying
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> HistoricalVolatility { get; }

        /// <summary>
        /// Gets the option price level
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> Price { get; }

        /// <summary>
        /// Gets the underlying's price level
        /// </summary>
        public IndicatorBase<IndicatorDataPoint> UnderlyingPrice { get; }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="option">The option to be tracked</param>am>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(Symbol option, decimal riskFreeRate = 0.05m, int period = 252,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
            : this($"IV({option.Value},{riskFreeRate},{period},{optionModel})", option, riskFreeRate, period, optionModel)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        public ImpliedVolatility(string name, Symbol option, decimal riskFreeRate = 0.05m, int period = 252, 
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
            : base(name)
        {
            var sid = option.ID;
            if (sid.SecurityType != SecurityType.Option)
            {
                throw new ArgumentException("ImpliedVolatility only support SecurityType.Option.");
            }

            _optionSymbol = option;
            _underlyingSymbol = option.Underlying;
            _roc = new(1);
            _optionModel = optionModel;

            Strike = sid.StrikePrice;
            Expiry = sid.Date;
            Right = sid.OptionRight;
            Style = sid.OptionStyle;
            RiskFreeRate = riskFreeRate;
 
            HistoricalVolatility = IndicatorExtensions.Times(
                IndicatorExtensions.Of(
                    new StandardDeviation(period),
                    _roc
                ),
                Convert.ToDecimal(Math.Sqrt(252))
            );
            Price = new Identity(name + "_Close");
            UnderlyingPrice = new Identity(name + "_UnderlyingClose");

            _consolidator = new(TimeSpan.FromDays(1));
            _consolidator.DataConsolidated += (_, bar) => {
                _roc.Update(bar.EndTime, bar.Price);
            };

            WarmUpPeriod = period;
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => HistoricalVolatility.Samples >= 2 && Price.Current.Time == UnderlyingPrice.Current.Time;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Computes the next value
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var inputSymbol = input.Symbol;
            if (inputSymbol == _optionSymbol)
            {
                Price.Update(input.EndTime, input.Close);
            }
            else if (inputSymbol == _underlyingSymbol)
            {
                _consolidator.Update(input);
                UnderlyingPrice.Update(input.EndTime, input.Close);
            }
            else
            {
                throw new ArgumentException("The given symbol was not target or reference symbol");
            }

            var time = Price.Current.Time;
            if (time == UnderlyingPrice.Current.Time && Price.IsReady && UnderlyingPrice.IsReady)
            {
                _impliedVolatility = CalculateIV(time);
            }
            return _impliedVolatility;
        }

        // Calculate the theoretical option price
        private decimal TheoreticalPrice(decimal volatility, decimal spotPrice, decimal strikePrice, decimal timeToExpiration, decimal riskFreeRate, 
            OptionRight optionType, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes)
        {
            switch (optionModel)
            {
                case OptionPricingModelType.BinomialCoxRossRubinstein:
                    return OptionGreekIndicatorsHelper.CRRTheoreticalPrice(volatility, spotPrice, strikePrice, timeToExpiration, riskFreeRate, optionType);
                case OptionPricingModelType.BlackScholes:
                default:
                    return OptionGreekIndicatorsHelper.BlackTheoreticalPrice(volatility, spotPrice, strikePrice, timeToExpiration, riskFreeRate, optionType);
            }
        }

        // Calculate the IV of the option
        private decimal CalculateIV(DateTime time)
        {
            var price = Price.Current.Value;
            var spotPrice = UnderlyingPrice.Current.Value;
            var timeToExpiration = Convert.ToDecimal((Expiry - time).TotalDays) / 365m;

            Func<double, double> f = (vol) => (double)(TheoreticalPrice(Convert.ToDecimal(vol), spotPrice, Strike, timeToExpiration, RiskFreeRate, Right, _optionModel) - price);
            try
            {
                return Convert.ToDecimal(Brent.FindRoot(f, 0.01d, 1.0d, 1e-5d, 20));
            }
            catch
            {
                Log.Error("Fail to converge, returning 0.");
                return 0m;
            }
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators
        /// </summary>
        public override void Reset()
        {
            _consolidator = new(TimeSpan.FromDays(1));
            _consolidator.DataConsolidated += (_, bar) => {
                _roc.Update(bar.EndTime, bar.Price);
            };

            _roc.Reset();
            HistoricalVolatility.Reset();
            Price.Reset();
            UnderlyingPrice.Reset();
            base.Reset();
        }
    }
}
