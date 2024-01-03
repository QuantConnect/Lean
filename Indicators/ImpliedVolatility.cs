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
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Implied Volatility indicator that calculate the IV of an option using Black-Scholes Model
    /// </summary>
    public class ImpliedVolatility : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly Symbol _optionSymbol;
        private readonly Symbol _underlyingSymbol;
        private readonly int _period;
        private RateOfChange _roc;
        private decimal _impliedVolatility;

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
        /// Gets the historical volatility
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
        /// <param name="strike">The strike price of the option</param>
        /// <param name="expiry">The expiry time of the option</param>
        /// <param name="right">The option right (call/put) of the option</param>
        /// <param name="style">The option style (European/American) of the option</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(Symbol option, decimal riskFreeRate = 0.05m, int period = 252)
            : this($"IV({option.Value},{riskFreeRate},{period})", option, riskFreeRate, period)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImpliedVolatility class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="option">The option to be tracked</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="period">The lookback period of historical volatility</param>
        public ImpliedVolatility(string name, Symbol option, decimal riskFreeRate = 0.05m, int period = 252)
            : base(name)
        {
            var sid = option.ID;
            if (sid.SecurityType != SecurityType.Option)
            {
                throw new ArgumentException("ImpliedVolatility only support SecurityType.Option.");
            }

            _optionSymbol = option;
            _underlyingSymbol = option.Underlying;
            _period = period;
            _roc = new RateOfChange(1);

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

            WarmUpPeriod = period;
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => HistoricalVolatility.IsReady && Price.IsReady && UnderlyingPrice.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Computes the next value of the following sub-indicators from the given state:
        /// StandardDeviation, MiddleBand, UpperBand, LowerBand, BandWidth, %B
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>The input is returned unmodified.</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            var inputSymbol = input.Symbol;
            if (inputSymbol == _optionSymbol)
            {
                Price.Update(input);
            }
            else if (inputSymbol == _underlyingSymbol)
            {
                _roc.Update(input);
                UnderlyingPrice.Update(input);
            }
            else
            {
                throw new ArgumentException("The given symbol was not target or reference symbol");
            }

            var time = Price.Current.Time;
            if (time == UnderlyingPrice.Current.Time)
            {
                _impliedVolatility = CalculateIV(time);
            }
            return input.Value;
        }

        // Calculate the theoretical price
        private decimal TheoreticalPrice(decimal volatility, decimal spotPrice, decimal strikePrice, decimal timeToExpiration, decimal riskFreeRate, OptionRight optionType)
        {
            switch (Style)
            {
                case OptionStyle.European:
                    return OptionGreekIndicatorsHelper.BlackTheoreticalPrice(volatility, spotPrice, strikePrice, timeToExpiration, riskFreeRate, optionType);
                case OptionStyle.American:
                    return OptionGreekIndicatorsHelper.LeisenReimerTheoreticalPrice(volatility, spotPrice, strikePrice, timeToExpiration, riskFreeRate, optionType);
                default:
                    throw new Exception("Unsupported Option Style");
            }
        }

        private decimal CalculateIV(DateTime time)
        {
            var price = Price.Current.Value;
            var spotPrice = UnderlyingPrice.Current.Value;
            var timeToExpiration = Convert.ToDecimal((Expiry - time).TotalDays) / 365m;

            Func<decimal, decimal> f = (vol) => TheoreticalPrice(vol, spotPrice, Strike, timeToExpiration, RiskFreeRate, Right);
            return OptionGreekIndicatorsHelper.BrentApproximation(f, price, 0m, 10m);
        }

        /// <summary>
        /// Resets this indicator and all sub-indicators (StandardDeviation, LowerBand, MiddleBand, UpperBand, BandWidth, %B)
        /// </summary>
        public override void Reset()
        {
            HistoricalVolatility.Reset();
            Price.Reset();
            UnderlyingPrice.Reset();
            base.Reset();
        }
    }
}
