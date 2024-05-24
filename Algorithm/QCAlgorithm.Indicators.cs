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

using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Util;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        /// <summary>
        /// Gets whether or not WarmUpIndicator is allowed to warm up indicators/>
        /// </summary>
        [DocumentationAttribute(Indicators)]
        public bool EnableAutomaticIndicatorWarmUp { get; set; } = false;

        /// <summary>
        /// Creates a new Acceleration Bands indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose Acceleration Bands we want.</param>
        /// <param name="period">The period of the three moving average (middle, upper and lower band).</param>
        /// <param name="width">A coefficient specifying the distance between the middle band and upper or lower bands.</param>
        /// <param name="movingAverageType">Type of the moving average.</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar.</param>
        /// <returns></returns>
        [DocumentationAttribute(Indicators)]
        public AccelerationBands ABANDS(Symbol symbol, int period, decimal width = 4, MovingAverageType movingAverageType = MovingAverageType.Simple,
            Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ABANDS({period},{width})", resolution);
            var accelerationBands = new AccelerationBands(name, period, width, movingAverageType);
            InitializeIndicator(symbol, accelerationBands, resolution, selector);

            return accelerationBands;
        }

        /// <summary>
        /// Creates a new AccumulationDistribution indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose AD we want</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The AccumulationDistribution indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public AccumulationDistribution AD(Symbol symbol, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "AD", resolution);
            var accumulationDistribution = new AccumulationDistribution(name);
            InitializeIndicator(symbol, accumulationDistribution, resolution, selector);

            return accumulationDistribution;
        }

        /// <summary>
        /// Creates a new AccumulationDistributionOscillator indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose ADOSC we want</param>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The AccumulationDistributionOscillator indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public AccumulationDistributionOscillator ADOSC(Symbol symbol, int fastPeriod, int slowPeriod, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ADOSC({fastPeriod},{slowPeriod})", resolution);
            var accumulationDistributionOscillator = new AccumulationDistributionOscillator(name, fastPeriod, slowPeriod);
            InitializeIndicator(symbol, accumulationDistributionOscillator, resolution, selector);

            return accumulationDistributionOscillator;
        }

        /// <summary>
        /// Creates a Alpha indicator for the given target symbol in relation with the reference used.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="target">The target symbol whose Alpha value we want</param>
        /// <param name="reference">The reference symbol to compare with the target symbol</param>
        /// <param name="alphaPeriod">The period of the Alpha indicator</param>
        /// <param name="betaPeriod">The period of the Beta indicator</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Alpha indicator for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public Alpha A(Symbol target, Symbol reference, int alphaPeriod = 1, int betaPeriod = 252, Resolution? resolution = null, decimal? riskFreeRate = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var baseBame = riskFreeRate.HasValue ? $"A({alphaPeriod},{betaPeriod},{riskFreeRate})" : $"A({alphaPeriod},{betaPeriod})";
            var name = CreateIndicatorName(target, baseBame, resolution);

            // If risk free rate is not specified, use the default risk free rate model
            IRiskFreeInterestRateModel riskFreeRateModel = riskFreeRate.HasValue
                ? new ConstantRiskFreeRateInterestRateModel(riskFreeRate.Value)
                : new FuncRiskFreeRateInterestRateModel((datetime) => RiskFreeInterestRateModel.GetInterestRate(datetime));

            var alpha = new Alpha(name, target, reference, alphaPeriod, betaPeriod, riskFreeRateModel);
            InitializeIndicator(target, alpha, resolution, selector);
            InitializeIndicator(reference, alpha, resolution, selector);

            return alpha;
        }

        /// <summary>
        /// Creates a new ARIMA indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose ARIMA indicator we want</param>
        /// <param name="arOrder">AR order (p) -- defines the number of past values to consider in the AR component of the model.</param>
        /// <param name="diffOrder">Difference order (d) -- defines how many times to difference the model before fitting parameters.</param>
        /// <param name="maOrder">MA order (q) -- defines the number of past values to consider in the MA component of the model.</param>
        /// <param name="period">Size of the rolling series to fit onto</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The ARIMA indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public AutoRegressiveIntegratedMovingAverage ARIMA(Symbol symbol, int arOrder, int diffOrder, int maOrder, int period,
            Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ARIMA({arOrder},{diffOrder},{maOrder},{period})", resolution);
            var arimaIndicator = new AutoRegressiveIntegratedMovingAverage(name, arOrder, diffOrder, maOrder, period);
            InitializeIndicator(symbol, arimaIndicator, resolution, selector);

            return arimaIndicator;
        }

        /// <summary>
        /// Creates a new Average Directional Index indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose Average Directional Index we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="period">The period over which to compute the Average Directional Index</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Average Directional Index indicator for the requested symbol.</returns>
        [DocumentationAttribute(Indicators)]
        public AverageDirectionalIndex ADX(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ADX({period})", resolution);
            var averageDirectionalIndex = new AverageDirectionalIndex(name, period);
            InitializeIndicator(symbol, averageDirectionalIndex, resolution, selector);

            return averageDirectionalIndex;
        }

        /// <summary>
        /// Creates a new Awesome Oscillator from the specified periods.
        /// </summary>
        /// <param name="symbol">The symbol whose Awesome Oscillator we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="fastPeriod">The period of the fast moving average associated with the AO</param>
        /// <param name="slowPeriod">The period of the slow moving average associated with the AO</param>
        /// <param name="type">The type of moving average used when computing the fast and slow term. Defaults to simple moving average.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        [DocumentationAttribute(Indicators)]
        public AwesomeOscillator AO(Symbol symbol, int slowPeriod, int fastPeriod, MovingAverageType type, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"AO({fastPeriod},{slowPeriod},{type})", resolution);
            var awesomeOscillator = new AwesomeOscillator(name, fastPeriod, slowPeriod, type);
            InitializeIndicator(symbol, awesomeOscillator, resolution, selector);

            return awesomeOscillator;
        }

        /// <summary>
        /// Creates a new AverageDirectionalMovementIndexRating indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose ADXR we want</param>
        /// <param name="period">The period over which to compute the ADXR</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The AverageDirectionalMovementIndexRating indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public AverageDirectionalMovementIndexRating ADXR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ADXR({period})", resolution);
            var averageDirectionalMovementIndexRating = new AverageDirectionalMovementIndexRating(name, period);
            InitializeIndicator(symbol, averageDirectionalMovementIndexRating, resolution, selector);

            return averageDirectionalMovementIndexRating;
        }

        /// <summary>
        /// Creates a new ArnaudLegouxMovingAverage indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose ALMA we want</param>
        /// <param name="period">int - the number of periods to calculate the ALMA</param>
        /// <param name="sigma"> int - this parameter is responsible for the shape of the curve coefficients.
        /// </param>
        /// <param name="offset">
        /// decimal - This parameter allows regulating the smoothness and high sensitivity of the
        /// Moving Average. The range for this parameter is [0, 1].
        /// </param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The ArnaudLegouxMovingAverage indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public ArnaudLegouxMovingAverage ALMA(Symbol symbol, int period, int sigma = 6, decimal offset = 0.85m, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ALMA({period},{sigma},{offset})", resolution);
            var arnaudLegouxMovingAverage = new ArnaudLegouxMovingAverage(name, period, sigma, offset);
            InitializeIndicator(symbol, arnaudLegouxMovingAverage, resolution, selector);

            return arnaudLegouxMovingAverage;
        }

        /// <summary>
        /// Creates a new AbsolutePriceOscillator indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose APO we want</param>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        /// <param name="movingAverageType">The type of moving average to use</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The AbsolutePriceOscillator indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public AbsolutePriceOscillator APO(Symbol symbol, int fastPeriod, int slowPeriod, MovingAverageType movingAverageType, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"APO({fastPeriod},{slowPeriod})", resolution);
            var absolutePriceOscillator = new AbsolutePriceOscillator(name, fastPeriod, slowPeriod, movingAverageType);
            InitializeIndicator(symbol, absolutePriceOscillator, resolution, selector);

            return absolutePriceOscillator;
        }

        /// <summary>
        /// Creates a new AroonOscillator indicator which will compute the AroonUp and AroonDown (as well as the delta)
        /// </summary>
        /// <param name="symbol">The symbol whose Aroon we seek</param>
        /// <param name="period">The look back period for computing number of periods since maximum and minimum</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>An AroonOscillator configured with the specified periods</returns>
        [DocumentationAttribute(Indicators)]
        public AroonOscillator AROON(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            return AROON(symbol, period, period, resolution, selector);
        }

        /// <summary>
        /// Creates a new AroonOscillator indicator which will compute the AroonUp and AroonDown (as well as the delta)
        /// </summary>
        /// <param name="symbol">The symbol whose Aroon we seek</param>
        /// <param name="upPeriod">The look back period for computing number of periods since maximum</param>
        /// <param name="downPeriod">The look back period for computing number of periods since minimum</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>An AroonOscillator configured with the specified periods</returns>
        [DocumentationAttribute(Indicators)]
        public AroonOscillator AROON(Symbol symbol, int upPeriod, int downPeriod, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"AROON({upPeriod},{downPeriod})", resolution);
            var aroonOscillator = new AroonOscillator(name, upPeriod, downPeriod);
            InitializeIndicator(symbol, aroonOscillator, resolution, selector);

            return aroonOscillator;
        }

        /// <summary>
        /// Creates a new AverageTrueRange indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose ATR we want</param>
        /// <param name="period">The smoothing period used to smooth the computed TrueRange values</param>
        /// <param name="type">The type of smoothing to use</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>A new AverageTrueRange indicator with the specified smoothing type and period</returns>
        [DocumentationAttribute(Indicators)]
        public AverageTrueRange ATR(Symbol symbol, int period, MovingAverageType type = MovingAverageType.Simple, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ATR({period})", resolution);
            var averageTrueRange = new AverageTrueRange(name, period, type);
            InitializeIndicator(symbol, averageTrueRange, resolution, selector);

            return averageTrueRange;
        }

        /// <summary>
        /// Creates an AugenPriceSpike indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose APS we want</param>
        /// <param name="period">The period of the APS</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The AugenPriceSpike indicator for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public AugenPriceSpike APS(Symbol symbol, int period = 3, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"APS({period})", resolution);
            var augenPriceSpike = new AugenPriceSpike(name, period);
            InitializeIndicator(symbol, augenPriceSpike, resolution, selector);

            return augenPriceSpike;
        }

        /// <summary>
        /// Creates a new BollingerBands indicator which will compute the MiddleBand, UpperBand, LowerBand, and StandardDeviation
        /// </summary>
        /// <param name="symbol">The symbol whose BollingerBands we seek</param>
        /// <param name="period">The period of the standard deviation and moving average (middle band)</param>
        /// <param name="k">The number of standard deviations specifying the distance between the middle band and upper or lower bands</param>
        /// <param name="movingAverageType">The type of moving average to be used</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>A BollingerBands configured with the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public BollingerBands BB(Symbol symbol, int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple,
            Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"BB({period},{k})", resolution);
            var bollingerBands = new BollingerBands(name, period, k, movingAverageType);
            InitializeIndicator(symbol, bollingerBands, resolution, selector);

            return bollingerBands;
        }

        /// <summary>
        /// Creates a Beta indicator for the given target symbol in relation with the reference used.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="target">The target symbol whose Beta value we want</param>
        /// <param name="reference">The reference symbol to compare with the target symbol</param>
        /// <param name="period">The period of the Beta indicator</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Beta indicator for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public Beta B(Symbol target, Symbol reference, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(QuantConnect.Symbol.None, $"B({period})", resolution);
            var beta = new Beta(name, target, reference, period);
            InitializeIndicator(target, beta, resolution, selector);
            InitializeIndicator(reference, beta, resolution, selector);

            return beta;
        }

        /// <summary>
        /// Creates a new Balance Of Power indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose Balance Of Power we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Balance Of Power indicator for the requested symbol.</returns>
        [DocumentationAttribute(Indicators)]
        public BalanceOfPower BOP(Symbol symbol, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "BOP", resolution);
            var balanceOfPower = new BalanceOfPower(name);
            InitializeIndicator(symbol, balanceOfPower, resolution, selector);

            return balanceOfPower;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoppockCurve"/> indicator
        /// </summary>
        /// <param name="symbol">The symbol whose Coppock Curve we want</param>
        /// <param name="shortRocPeriod">The period for the short ROC</param>
        /// <param name="longRocPeriod">The period for the long ROC</param>
        /// <param name="lwmaPeriod">The period for the LWMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Coppock Curve indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public CoppockCurve CC(Symbol symbol, int shortRocPeriod = 11, int longRocPeriod = 14, int lwmaPeriod = 10, Resolution? resolution = null,
                               Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"CC({shortRocPeriod},{longRocPeriod},{lwmaPeriod})", resolution);
            var coppockCurve = new CoppockCurve(name, shortRocPeriod, longRocPeriod, lwmaPeriod);
            InitializeIndicator(symbol, coppockCurve, resolution, selector);

            return coppockCurve;
        }

        /// <summary>
        /// Creates a Correlation indicator for the given target symbol in relation with the reference used.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="target">The target symbol of this indicator</param>
        /// <param name="reference">The reference symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        /// <param name="correlationType">Correlation type</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Correlation indicator for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public Correlation C(Symbol target, Symbol reference, int period, CorrelationType correlationType = CorrelationType.Pearson, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(QuantConnect.Symbol.None, $"C({period})", resolution);
            var correlation = new Correlation(name, target, reference, period);
            InitializeIndicator(target, correlation, resolution, selector);
            InitializeIndicator(reference, correlation, resolution, selector);

            return correlation;
        }

        /// <summary>
        /// Creates a new CommodityChannelIndex indicator. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose CCI we want</param>
        /// <param name="period">The period over which to compute the CCI</param>
        /// <param name="movingAverageType">The type of moving average to use in computing the typical price average</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The CommodityChannelIndex indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public CommodityChannelIndex CCI(Symbol symbol, int period, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"CCI({period})", resolution);
            var commodityChannelIndex = new CommodityChannelIndex(name, period, movingAverageType);
            InitializeIndicator(symbol, commodityChannelIndex, resolution, selector);

            return commodityChannelIndex;
        }

        /// <summary>
        /// Creates a new ChaikinMoneyFlow indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose CMF we want</param>
        /// <param name="period">The period over which to compute the CMF</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The ChaikinMoneyFlow indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public ChaikinMoneyFlow CMF(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"CMF({period})", resolution);
            var chaikinMoneyFlow = new ChaikinMoneyFlow(name, period);
            InitializeIndicator(symbol, chaikinMoneyFlow, resolution, selector);

            return chaikinMoneyFlow;

        }
                
        /// <summary>
        /// Creates a new ChandeMomentumOscillator indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose CMO we want</param>
        /// <param name="period">The period over which to compute the CMO</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The ChandeMomentumOscillator indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public ChandeMomentumOscillator CMO(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"CMO({period})", resolution);
            var chandeMomentumOscillator = new ChandeMomentumOscillator(name, period);
            InitializeIndicator(symbol, chandeMomentumOscillator, resolution, selector);

            return chandeMomentumOscillator;
        }

        ///<summary>
        /// Creates a new DeMarker Indicator (DEM), an oscillator-type indicator measuring changes in terms of an asset's
        /// High and Low tradebar values.
        ///</summary>
        /// <param name="symbol">The symbol whose DEM we seek.</param>
        /// <param name="period">The period of the moving average implemented</param>
        /// <param name="type">Specifies the type of moving average to be used</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The DeMarker indicator for the requested symbol.</returns>
        [DocumentationAttribute(Indicators)]
        public DeMarkerIndicator DEM(Symbol symbol, int period, MovingAverageType type, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"DEM({period},{type})", resolution);
            var deMarkerIndicator = new DeMarkerIndicator(name, period, type);
            InitializeIndicator(symbol, deMarkerIndicator, resolution, selector);
            return deMarkerIndicator;
        }

        /// <summary>
        /// Creates a new Donchian Channel indicator which will compute the Upper Band and Lower Band.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose Donchian Channel we seek.</param>
        /// <param name="upperPeriod">The period over which to compute the upper Donchian Channel.</param>
        /// <param name="lowerPeriod">The period over which to compute the lower Donchian Channel.</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Donchian Channel indicator for the requested symbol.</returns>
        [DocumentationAttribute(Indicators)]
        public DonchianChannel DCH(Symbol symbol, int upperPeriod, int lowerPeriod, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"DCH({upperPeriod},{lowerPeriod})", resolution);
            var donchianChannel = new DonchianChannel(name, upperPeriod, lowerPeriod);
            InitializeIndicator(symbol, donchianChannel, resolution, selector);

            return donchianChannel;
        }

        /// <summary>
        /// Overload shorthand to create a new symmetric Donchian Channel indicator which
        /// has the upper and lower channels set to the same period length.
        /// </summary>
        /// <param name="symbol">The symbol whose Donchian Channel we seek.</param>
        /// <param name="period">The period over which to compute the Donchian Channel.</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Donchian Channel indicator for the requested symbol.</returns>
        [DocumentationAttribute(Indicators)]
        public DonchianChannel DCH(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            return DCH(symbol, period, period, resolution, selector);
        }

        /// <summary>
        /// Creates a new Delta indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The option symbol whose values we want as an indicator</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="dividendYield">The dividend yield</param>
        /// <param name="optionModel">The option pricing model used to estimate Delta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <returns>A new Delta indicator for the specified symbol</returns>
        [DocumentationAttribute(Indicators)]
        public Delta D(Symbol symbol, Symbol mirrorOption = null, decimal? riskFreeRate = null, decimal? dividendYield = null, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, 
            OptionPricingModelType? ivModel = null, Resolution? resolution = null)
        {
            var name = InitializeOptionIndicator<Delta>(symbol, out var riskFreeRateModel, out var dividendYieldModel, riskFreeRate, dividendYield, optionModel, resolution);

            var delta = new Delta(name, symbol, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel);
            RegisterIndicator(symbol, delta, ResolveConsolidator(symbol, resolution, typeof(QuoteBar)));
            RegisterIndicator(symbol.Underlying, delta, ResolveConsolidator(symbol.Underlying, resolution));
            if (mirrorOption != null)
            {
                RegisterIndicator(mirrorOption, delta, ResolveConsolidator(mirrorOption, resolution, typeof(QuoteBar)));
            }
            return delta;
        }

        /// <summary>
        /// Creates a new Delta indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The option symbol whose values we want as an indicator</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="dividendYield">The dividend yield</param>
        /// <param name="optionModel">The option pricing model used to estimate Delta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <returns>A new Delta indicator for the specified symbol</returns>
        [DocumentationAttribute(Indicators)]
        public Delta Δ(Symbol symbol, Symbol mirrorOption = null, decimal? riskFreeRate = null, decimal? dividendYield = null, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes,
            OptionPricingModelType? ivModel = null, Resolution? resolution = null)
        {
            return D(symbol, mirrorOption, riskFreeRate, dividendYield, optionModel, ivModel, resolution);
        }

        /// <summary>
        /// Creates a new DoubleExponentialMovingAverage indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose DEMA we want</param>
        /// <param name="period">The period over which to compute the DEMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The DoubleExponentialMovingAverage indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public DoubleExponentialMovingAverage DEMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"DEMA({period})", resolution);
            var doubleExponentialMovingAverage = new DoubleExponentialMovingAverage(name, period);
            InitializeIndicator(symbol, doubleExponentialMovingAverage, resolution, selector);

            return doubleExponentialMovingAverage;
        }
        
        /// <summary>
        /// Creates a new DerivativeOscillator indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose DO we want</param>
        /// <param name="rsiPeriod">The period over which to compute the RSI</param>
        /// <param name="smoothingRsiPeriod">The period over which to compute the smoothing RSI</param>
        /// <param name="doubleSmoothingRsiPeriod">The period over which to compute the double smoothing RSI</param>
        /// <param name="signalLinePeriod">The period over which to compute the signal line</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x =&gt; x.Value)</param>
        /// <returns>The DerivativeOscillator indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public DerivativeOscillator DO(Symbol symbol, int rsiPeriod, int smoothingRsiPeriod, int doubleSmoothingRsiPeriod, int signalLinePeriod, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"DO({rsiPeriod},{smoothingRsiPeriod},{doubleSmoothingRsiPeriod},{signalLinePeriod})", resolution);
            var derivativeOscillator = new DerivativeOscillator(name, rsiPeriod, smoothingRsiPeriod, doubleSmoothingRsiPeriod, signalLinePeriod);
            InitializeIndicator(symbol, derivativeOscillator, resolution, selector);

            return derivativeOscillator;
        }

        /// <summary>
        /// Creates a new <see cref="DetrendedPriceOscillator"/> indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose DPO we want</param>
        /// <param name="period">The period over which to compute the DPO</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>A new registered DetrendedPriceOscillator indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public DetrendedPriceOscillator DPO(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"DPO({period})", resolution);
            var detrendedPriceOscillator = new DetrendedPriceOscillator(name, period);
            InitializeIndicator(symbol, detrendedPriceOscillator, resolution, selector);

            return detrendedPriceOscillator;
        }

        /// <summary>
        /// Creates an ExponentialMovingAverage indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose EMA we want</param>
        /// <param name="period">The period of the EMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The ExponentialMovingAverage for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public ExponentialMovingAverage EMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            return EMA(symbol, period, ExponentialMovingAverage.SmoothingFactorDefault(period), resolution, selector);
        }

        /// <summary>
        /// Creates an ExponentialMovingAverage indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose EMA we want</param>
        /// <param name="period">The period of the EMA</param>
        /// <param name="smoothingFactor">The percentage of data from the previous value to be carried into the next value</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The ExponentialMovingAverage for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public ExponentialMovingAverage EMA(Symbol symbol, int period, decimal smoothingFactor, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"EMA({period})", resolution);
            var exponentialMovingAverage = new ExponentialMovingAverage(name, period, smoothingFactor);
            InitializeIndicator(symbol, exponentialMovingAverage, resolution, selector);

            return exponentialMovingAverage;
        }

        /// <summary>
        /// Creates an EaseOfMovementValue indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose EMV we want</param>
        /// <param name="period">The period of the EMV</param>
        /// <param name="scale">The length of the outputed value</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The EaseOfMovementValue indicator for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public EaseOfMovementValue EMV(Symbol symbol, int period = 1, int scale = 10000, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"EMV({period}, {scale})", resolution);
            var easeOfMovementValue = new EaseOfMovementValue(name, period, scale);
            InitializeIndicator(symbol, easeOfMovementValue, resolution, selector);

            return easeOfMovementValue;
        }

        /// <summary>
        /// Creates a new FilteredIdentity indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The symbol whose values we want as an indicator</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <param name="filter">Filters the IBaseData send into the indicator, if null defaults to true (x => true) which means no filter</param>
        /// <param name="fieldName">The name of the field being selected</param>
        /// <returns>A new FilteredIdentity indicator for the specified symbol and selector</returns>
        [DocumentationAttribute(Indicators)]
        public FilteredIdentity FilteredIdentity(Symbol symbol, Func<IBaseData, IBaseDataBar> selector = null, Func<IBaseData, bool> filter = null, string fieldName = null)
        {
            var resolution = GetSubscription(symbol).Resolution;
            return FilteredIdentity(symbol, resolution, selector, filter, fieldName);
        }

        /// <summary>
        /// Creates a new FilteredIdentity indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The symbol whose values we want as an indicator</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <param name="filter">Filters the IBaseData send into the indicator, if null defaults to true (x => true) which means no filter</param>
        /// <param name="fieldName">The name of the field being selected</param>
        /// <returns>A new FilteredIdentity indicator for the specified symbol and selector</returns>
        [DocumentationAttribute(Indicators)]
        public FilteredIdentity FilteredIdentity(Symbol symbol, Resolution resolution, Func<IBaseData, IBaseDataBar> selector = null, Func<IBaseData, bool> filter = null, string fieldName = null)
        {
            var name = CreateIndicatorName(symbol, fieldName ?? "close", resolution);
            var filteredIdentity = new FilteredIdentity(name, filter);
            RegisterIndicator<IBaseData>(symbol, filteredIdentity, resolution, selector);
            return filteredIdentity;
        }

        /// <summary>
        /// Creates a new FilteredIdentity indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The symbol whose values we want as an indicator</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <param name="selector">Selects a value from the BaseData, if null defaults to the .Value property (x => x.Value)</param>
        /// <param name="filter">Filters the IBaseData send into the indicator, if null defaults to true (x => true) which means no filter</param>
        /// <param name="fieldName">The name of the field being selected</param>
        /// <returns>A new FilteredIdentity indicator for the specified symbol and selector</returns>
        [DocumentationAttribute(Indicators)]
        public FilteredIdentity FilteredIdentity(Symbol symbol, TimeSpan resolution, Func<IBaseData, IBaseDataBar> selector = null, Func<IBaseData, bool> filter = null, string fieldName = null)
        {
            var name = Invariant($"{symbol}({fieldName ?? "close"}_{resolution})");
            var filteredIdentity = new FilteredIdentity(name, filter);
            RegisterIndicator<IBaseData>(symbol, filteredIdentity, ResolveConsolidator(symbol, resolution), selector);
            return filteredIdentity;
        }

        /// <summary>
        /// Creates an FisherTransform indicator for the symbol.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose FisherTransform we want</param>
        /// <param name="period">The period of the FisherTransform</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The FisherTransform for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public FisherTransform FISH(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"FISH({period})", resolution);
            var fisherTransform = new FisherTransform(name, period);
            InitializeIndicator(symbol, fisherTransform, resolution, selector);

            return fisherTransform;
        }

        /// <summary>
        /// Creates an FractalAdaptiveMovingAverage (FRAMA) indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose FRAMA we want</param>
        /// <param name="period">The period of the FRAMA</param>
        /// <param name="longPeriod">The long period of the FRAMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The FRAMA for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public FractalAdaptiveMovingAverage FRAMA(Symbol symbol, int period, int longPeriod = 198, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"FRAMA({period},{longPeriod})", resolution);
            var fractalAdaptiveMovingAverage = new FractalAdaptiveMovingAverage(name, period, longPeriod);
            InitializeIndicator(symbol, fractalAdaptiveMovingAverage, resolution, selector);

            return fractalAdaptiveMovingAverage;
        }

        /// <summary>
        /// Creates a new Gamma indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The option symbol whose values we want as an indicator</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="dividendYield">The dividend yield</param>
        /// <param name="optionModel">The option pricing model used to estimate Gamma</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <returns>A new Gamma indicator for the specified symbol</returns>
        [DocumentationAttribute(Indicators)]
        public Gamma G(Symbol symbol, Symbol mirrorOption = null, decimal? riskFreeRate = null, decimal? dividendYield = null, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes,
            OptionPricingModelType? ivModel = null, Resolution? resolution = null)
        {
            var name = InitializeOptionIndicator<Gamma>(symbol, out var riskFreeRateModel, out var dividendYieldModel, riskFreeRate, dividendYield, optionModel, resolution);

            var gamma = new Gamma(name, symbol, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel);
            RegisterIndicator(symbol, gamma, ResolveConsolidator(symbol, resolution, typeof(QuoteBar)));
            RegisterIndicator(symbol.Underlying, gamma, ResolveConsolidator(symbol.Underlying, resolution));
            if (mirrorOption != null)
            {
                RegisterIndicator(mirrorOption, gamma, ResolveConsolidator(mirrorOption, resolution, typeof(QuoteBar)));
            }
            return gamma;
        }

        /// <summary>
        /// Creates a new Gamma indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The option symbol whose values we want as an indicator</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="dividendYield">The dividend yield</param>
        /// <param name="optionModel">The option pricing model used to estimate Gamma</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <returns>A new Gamma indicator for the specified symbol</returns>
        [DocumentationAttribute(Indicators)]
        public Gamma Γ(Symbol symbol, Symbol mirrorOption = null, decimal? riskFreeRate = null, decimal? dividendYield = null, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes,
            OptionPricingModelType? ivModel = null, Resolution? resolution = null)
        {
            return G(symbol, mirrorOption, riskFreeRate, dividendYield, optionModel, ivModel, resolution);
        }

        /// <summary>
        /// Creates a new Heikin-Ashi indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose Heikin-Ashi we want</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Heikin-Ashi indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public HeikinAshi HeikinAshi(Symbol symbol, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "HA", resolution);
            var heikinAshi = new HeikinAshi(name);
            InitializeIndicator(symbol, heikinAshi, resolution, selector);

            return heikinAshi;
        }

        /// <summary>
        /// Creates a new Hilbert Transform indicator
        /// </summary>
        /// <param name="symbol">The symbol whose Hilbert transform we want</param>
        /// <param name="length">The length of the FIR filter used in the calculation of the Hilbert Transform.
        /// This parameter determines the number of filter coefficients in the FIR filter.</param>
        /// <param name="inPhaseMultiplicationFactor">The multiplication factor used in the calculation of the in-phase component
        /// of the Hilbert Transform. This parameter adjusts the sensitivity and responsiveness of
        /// the transform to changes in the input signal.</param>
        /// <param name="quadratureMultiplicationFactor">The multiplication factor used in the calculation of the quadrature component of
        /// the Hilbert Transform. This parameter also adjusts the sensitivity and responsiveness of the
        /// transform to changes in the input signal.</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        [DocumentationAttribute(Indicators)]
        public HilbertTransform HT(Symbol symbol, int length, decimal inPhaseMultiplicationFactor, decimal quadratureMultiplicationFactor, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"HT({length}, {inPhaseMultiplicationFactor}, {quadratureMultiplicationFactor})", resolution);
            var hilbertTransform = new HilbertTransform(length, inPhaseMultiplicationFactor, quadratureMultiplicationFactor);
            InitializeIndicator(symbol, hilbertTransform, resolution, selector);

            return hilbertTransform;
        }

        /// <summary>
        /// Creates a new HullMovingAverage indicator. The Hull moving average is a series of nested weighted moving averages, is fast and smooth.
        /// </summary>
        /// <param name="symbol">The symbol whose Hull moving average we want</param>
        /// <param name="period">The period over which to compute the Hull moving average</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns></returns>
        [DocumentationAttribute(Indicators)]
        public HullMovingAverage HMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"HMA({period})", resolution);
            var hullMovingAverage = new HullMovingAverage(name, period);
            InitializeIndicator(symbol, hullMovingAverage, resolution, selector);

            return hullMovingAverage;
        }

        /// <summary>
        /// Creates a new IchimokuKinkoHyo indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose ICHIMOKU we want</param>
        /// <param name="tenkanPeriod">The period to calculate the Tenkan-sen period</param>
        /// <param name="kijunPeriod">The period to calculate the Kijun-sen period</param>
        /// <param name="senkouAPeriod">The period to calculate the Tenkan-sen period</param>
        /// <param name="senkouBPeriod">The period to calculate the Tenkan-sen period</param>
        /// <param name="senkouADelayPeriod">The period to calculate the Tenkan-sen period</param>
        /// <param name="senkouBDelayPeriod">The period to calculate the Tenkan-sen period</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>A new IchimokuKinkoHyo indicator with the specified periods and delays</returns>
        [DocumentationAttribute(Indicators)]
        public IchimokuKinkoHyo ICHIMOKU(Symbol symbol, int tenkanPeriod, int kijunPeriod, int senkouAPeriod, int senkouBPeriod,
            int senkouADelayPeriod, int senkouBDelayPeriod, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ICHIMOKU({tenkanPeriod},{kijunPeriod},{senkouAPeriod},{senkouBPeriod},{senkouADelayPeriod},{senkouBDelayPeriod})", resolution);
            var ichimokuKinkoHyo = new IchimokuKinkoHyo(name, tenkanPeriod, kijunPeriod, senkouAPeriod, senkouBPeriod, senkouADelayPeriod, senkouBDelayPeriod);
            InitializeIndicator(symbol, ichimokuKinkoHyo, resolution, selector);

            return ichimokuKinkoHyo;
        }

        /// <summary>
        /// Creates a new Identity indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The symbol whose values we want as an indicator</param>
        /// <param name="selector">Selects a value from the BaseData, if null defaults to the .Value property (x => x.Value)</param>
        /// <param name="fieldName">The name of the field being selected</param>
        /// <returns>A new Identity indicator for the specified symbol and selector</returns>
        [DocumentationAttribute(Indicators)]
        public Identity Identity(Symbol symbol, Func<IBaseData, decimal> selector = null, string fieldName = null)
        {
            var resolution = GetSubscription(symbol).Resolution;
            return Identity(symbol, resolution, selector, fieldName);
        }

        /// <summary>
        /// Creates a new Identity indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The symbol whose values we want as an indicator</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <param name="selector">Selects a value from the BaseData, if null defaults to the .Value property (x => x.Value)</param>
        /// <param name="fieldName">The name of the field being selected</param>
        /// <returns>A new Identity indicator for the specified symbol and selector</returns>
        [DocumentationAttribute(Indicators)]
        public Identity Identity(Symbol symbol, Resolution resolution, Func<IBaseData, decimal> selector = null, string fieldName = null)
        {
            var name = CreateIndicatorName(symbol, fieldName ?? "close", resolution);
            var identity = new Identity(name);
            RegisterIndicator(symbol, identity, resolution, selector);
            return identity;
        }

        /// <summary>
        /// Creates a new Identity indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The symbol whose values we want as an indicator</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <param name="selector">Selects a value from the BaseData, if null defaults to the .Value property (x => x.Value)</param>
        /// <param name="fieldName">The name of the field being selected</param>
        /// <returns>A new Identity indicator for the specified symbol and selector</returns>
        [DocumentationAttribute(Indicators)]
        public Identity Identity(Symbol symbol, TimeSpan resolution, Func<IBaseData, decimal> selector = null, string fieldName = null)
        {
            var name = Invariant($"{symbol}({fieldName ?? "close"},{resolution})");
            var identity = new Identity(name);
            RegisterIndicator(symbol, identity, ResolveConsolidator(symbol, resolution), selector);
            return identity;
        }

        /// <summary>
        /// Creates a new ImpliedVolatility indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The option symbol whose values we want as an indicator</param>
        /// <param name="mirrorOption">The mirror option contract used for parity type calculation</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="dividendYield">The dividend yield</param>
        /// <param name="optionModel">The option pricing model used to estimate IV</param>
        /// <param name="period">The lookback period of historical volatility</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <returns>A new ImpliedVolatility indicator for the specified symbol</returns>
        [DocumentationAttribute(Indicators)]
        public ImpliedVolatility IV(Symbol symbol, Symbol mirrorOption = null, decimal? riskFreeRate = null, decimal? dividendYield = null,
            OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, int period = 252, Resolution? resolution = null)
        {
            var name = InitializeOptionIndicator<ImpliedVolatility>(symbol, out var riskFreeRateModel, out var dividendYieldModel, riskFreeRate, dividendYield, optionModel, resolution);

            var iv = new ImpliedVolatility(name, symbol, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel);
            RegisterIndicator(symbol, iv, ResolveConsolidator(symbol, resolution, typeof(QuoteBar)));
            RegisterIndicator(symbol.Underlying, iv, ResolveConsolidator(symbol.Underlying, resolution));
            if (mirrorOption != null)
            {
                RegisterIndicator(mirrorOption, iv, ResolveConsolidator(mirrorOption, resolution, typeof(QuoteBar)));
            }
            return iv;
        }

        /// <summary>
        /// Creates a new KaufmanAdaptiveMovingAverage indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose KAMA we want</param>
        /// <param name="period">The period of the Efficiency Ratio (ER) of KAMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The KaufmanAdaptiveMovingAverage indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public KaufmanAdaptiveMovingAverage KAMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            return KAMA(symbol, period, 2, 30, resolution, selector);
        }

        /// <summary>
        /// Creates a new KaufmanAdaptiveMovingAverage indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose KAMA we want</param>
        /// <param name="period">The period of the Efficiency Ratio (ER)</param>
        /// <param name="fastEmaPeriod">The period of the fast EMA used to calculate the Smoothing Constant (SC)</param>
        /// <param name="slowEmaPeriod">The period of the slow EMA used to calculate the Smoothing Constant (SC)</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The KaufmanAdaptiveMovingAverage indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public KaufmanAdaptiveMovingAverage KAMA(Symbol symbol, int period, int fastEmaPeriod, int slowEmaPeriod, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"KAMA({period},{fastEmaPeriod},{slowEmaPeriod})", resolution);
            var kaufmanAdaptiveMovingAverage = new KaufmanAdaptiveMovingAverage(name, period, fastEmaPeriod, slowEmaPeriod);
            InitializeIndicator(symbol, kaufmanAdaptiveMovingAverage, resolution, selector);

            return kaufmanAdaptiveMovingAverage;
        }

        /// <summary>
        /// Creates an KaufmanEfficiencyRatio indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose EF we want</param>
        /// <param name="period">The period of the EF</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The KaufmanEfficiencyRatio indicator for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public KaufmanEfficiencyRatio KER(Symbol symbol, int period = 2, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"KER({period})", resolution);
            var kaufmanEfficiencyRatio = new KaufmanEfficiencyRatio(name, period);
            InitializeIndicator(symbol, kaufmanEfficiencyRatio, resolution, selector);

            return kaufmanEfficiencyRatio;
        }

        /// <summary>
        /// Creates a new Keltner Channels indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose Keltner Channel we seek</param>
        /// <param name="period">The period over which to compute the Keltner Channels</param>
        /// <param name="k">The number of multiples of the <see cref="AverageTrueRange"/> from the middle band of the Keltner Channels</param>
        /// <param name="movingAverageType">Specifies the type of moving average to be used as the middle line of the Keltner Channel</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Keltner Channel indicator for the requested symbol.</returns>
        [DocumentationAttribute(Indicators)]
        public KeltnerChannels KCH(Symbol symbol, int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"KCH({period},{k})", resolution);
            var keltnerChannels = new KeltnerChannels(name, period, k, movingAverageType);
            InitializeIndicator(symbol, keltnerChannels, resolution, selector);

            return keltnerChannels;
        }

        /// <summary>
        /// Creates a new LogReturn indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose log return we seek</param>
        /// <param name="period">The period of the log return.</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar.</param>
        /// <returns>log return indicator for the requested symbol.</returns>
        [DocumentationAttribute(Indicators)]
        public LogReturn LOGR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"LOGR({period})", resolution);
            var logReturn = new LogReturn(name, period);
            InitializeIndicator(symbol, logReturn, resolution, selector);

            return logReturn;
        }

        /// <summary>
        /// Creates and registers a new Least Squares Moving Average instance.
        /// </summary>
        /// <param name="symbol">The symbol whose LSMA we seek.</param>
        /// <param name="period">The LSMA period. Normally 14.</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar.</param>
        /// <returns>A LeastSquaredMovingAverage configured with the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public LeastSquaresMovingAverage LSMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"LSMA({period})", resolution);
            var leastSquaresMovingAverage = new LeastSquaresMovingAverage(name, period);
            InitializeIndicator(symbol, leastSquaresMovingAverage, resolution, selector);

            return leastSquaresMovingAverage;
        }

        /// <summary>
        /// Creates a new LinearWeightedMovingAverage indicator.  This indicator will linearly distribute
        /// the weights across the periods.
        /// </summary>
        /// <param name="symbol">The symbol whose LWMA we want</param>
        /// <param name="period">The period over which to compute the LWMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns></returns>
        [DocumentationAttribute(Indicators)]
        public LinearWeightedMovingAverage LWMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"LWMA({period})", resolution);
            var linearWeightedMovingAverage = new LinearWeightedMovingAverage(name, period);
            InitializeIndicator(symbol, linearWeightedMovingAverage, resolution, selector);

            return linearWeightedMovingAverage;
        }

        /// <summary>
        /// Creates a MACD indicator for the symbol. The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose MACD we want</param>
        /// <param name="fastPeriod">The period for the fast moving average</param>
        /// <param name="slowPeriod">The period for the slow moving average</param>
        /// <param name="signalPeriod">The period for the signal moving average</param>
        /// <param name="type">The type of moving average to use for the MACD</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The moving average convergence divergence between the fast and slow averages</returns>
        [DocumentationAttribute(Indicators)]
        public MovingAverageConvergenceDivergence MACD(Symbol symbol, int fastPeriod, int slowPeriod, int signalPeriod, MovingAverageType type = MovingAverageType.Exponential, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MACD({fastPeriod},{slowPeriod},{signalPeriod})", resolution);
            var movingAverageConvergenceDivergence = new MovingAverageConvergenceDivergence(name, fastPeriod, slowPeriod, signalPeriod, type);
            InitializeIndicator(symbol, movingAverageConvergenceDivergence, resolution, selector);

            return movingAverageConvergenceDivergence;
        }

        /// <summary>
        /// Creates a new MeanAbsoluteDeviation indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose MeanAbsoluteDeviation we want</param>
        /// <param name="period">The period over which to compute the MeanAbsoluteDeviation</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The MeanAbsoluteDeviation indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public MeanAbsoluteDeviation MAD(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MAD({period})", resolution);
            var meanAbsoluteDeviation = new MeanAbsoluteDeviation(name, period);
            InitializeIndicator(symbol, meanAbsoluteDeviation, resolution, selector);

            return meanAbsoluteDeviation;
        }

        /// <summary>
        /// Creates an Market Profile indicator for the symbol with Volume Profile (VOL) mode. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose VP we want</param>
        /// <param name="period">The period of the VP</param>
        /// <param name="valueAreaVolumePercentage">The percentage of volume contained in the value area</param>
        /// <param name="priceRangeRoundOff">How many digits you want to round and the precision. i.e 0.01 round to two digits exactly.</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Volume Profile indicator for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public VolumeProfile VP(Symbol symbol, int period = 2, decimal valueAreaVolumePercentage = 0.70m, decimal priceRangeRoundOff = 0.05m, Resolution resolution = Resolution.Daily, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"VP({period})", resolution);
            var marketProfile = new VolumeProfile(name, period, valueAreaVolumePercentage, priceRangeRoundOff);
            InitializeIndicator(symbol, marketProfile, resolution, selector);

            return marketProfile;
        }

        /// <summary>
        /// Creates an Market Profile indicator for the symbol with Time Price Opportunity (TPO) mode. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose TP we want</param>
        /// <param name="period">The period of the TP</param>
        /// <param name="valueAreaVolumePercentage">The percentage of volume contained in the value area</param>
        /// <param name="priceRangeRoundOff">How many digits you want to round and the precision. i.e 0.01 round to two digits exactly.</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Time Profile indicator for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public TimeProfile TP(Symbol symbol, int period = 2, decimal valueAreaVolumePercentage = 0.70m, decimal priceRangeRoundOff = 0.05m, Resolution resolution = Resolution.Daily, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"TP({period})", resolution);
            var marketProfile = new TimeProfile(name, period, valueAreaVolumePercentage, priceRangeRoundOff);
            InitializeIndicator(symbol, marketProfile, resolution, selector);

            return marketProfile;
        }

        /// <summary>
        /// Creates a new Time Series Forecast indicator
        /// </summary>
        /// <param name="symbol">The symbol whose TSF we want</param>
        /// <param name="period">The period of the TSF</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to Value property of BaseData (x => x.Value)</param>
        /// <returns>The TimeSeriesForecast indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public TimeSeriesForecast TSF(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"TSF({period})", resolution);
            var timeSeriesForecast = new TimeSeriesForecast(name, period);
            InitializeIndicator(symbol, timeSeriesForecast, resolution, selector);

            return timeSeriesForecast;
        }

        /// <summary>
        /// Creates a new Maximum indicator to compute the maximum value
        /// </summary>
        /// <param name="symbol">The symbol whose max we want</param>
        /// <param name="period">The look back period over which to compute the max value</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null and the symbol is of type TradeBar defaults to the High property,
        /// otherwise it defaults to Value property of BaseData (x => x.Value)</param>
        /// <returns>A Maximum indicator that compute the max value and the periods since the max value</returns>
        [DocumentationAttribute(Indicators)]
        public Maximum MAX(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MAX({period})", resolution);
            var maximum = new Maximum(name, period);

            // assign a default value for the selector function
            if (selector == null)
            {
                var subscription = GetSubscription(symbol);
                if (typeof(TradeBar).IsAssignableFrom(subscription.Type))
                {
                    // if we have trade bar data we'll use the High property, if not x => x.Value will be set in RegisterIndicator
                    selector = x => ((TradeBar)x).High;
                }
            }

            RegisterIndicator(symbol, maximum, ResolveConsolidator(symbol, resolution), selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, maximum, resolution, selector);
            }

            return maximum;
        }

        /// <summary>
        /// Creates a new MoneyFlowIndex indicator. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose MFI we want</param>
        /// <param name="period">The period over which to compute the MFI</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The MoneyFlowIndex indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public MoneyFlowIndex MFI(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MFI({period})", resolution);
            var moneyFlowIndex = new MoneyFlowIndex(name, period);
            InitializeIndicator(symbol, moneyFlowIndex, resolution, selector);

            return moneyFlowIndex;
        }

        /// <summary>
        /// Creates a new Mass Index indicator. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose Mass Index we want.</param>
        /// <param name="emaPeriod">The period used by both EMA.</param>
        /// <param name="sumPeriod">The sum period.</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Mass Index indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public MassIndex MASS(Symbol symbol, int emaPeriod = 9, int sumPeriod = 25, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MASS({emaPeriod},{sumPeriod})", resolution);
            var massIndex = new MassIndex(name, emaPeriod, sumPeriod);
            InitializeIndicator(symbol, massIndex, resolution, selector);

            return massIndex;
        }

        /// <summary>
        /// Creates a new MidPoint indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose MIDPOINT we want</param>
        /// <param name="period">The period over which to compute the MIDPOINT</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The MidPoint indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public MidPoint MIDPOINT(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MIDPOINT({period})", resolution);
            var midPoint = new MidPoint(name, period);
            InitializeIndicator(symbol, midPoint, resolution, selector);

            return midPoint;
        }

        /// <summary>
        /// Creates a new MidPrice indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose MIDPRICE we want</param>
        /// <param name="period">The period over which to compute the MIDPRICE</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The MidPrice indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public MidPrice MIDPRICE(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MIDPRICE({period})", resolution);
            var midPrice = new MidPrice(name, period);
            InitializeIndicator(symbol, midPrice, resolution, selector);

            return midPrice;
        }

        /// <summary>
        /// Creates a new Minimum indicator to compute the minimum value
        /// </summary>
        /// <param name="symbol">The symbol whose min we want</param>
        /// <param name="period">The look back period over which to compute the min value</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null and the symbol is of type TradeBar defaults to the Low property,
        /// otherwise it defaults to Value property of BaseData (x => x.Value)</param>
        /// <returns>A Minimum indicator that compute the in value and the periods since the min value</returns>
        [DocumentationAttribute(Indicators)]
        public Minimum MIN(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MIN({period})", resolution);
            var minimum = new Minimum(name, period);

            // assign a default value for the selector function
            if (selector == null)
            {
                var subscription = GetSubscription(symbol);
                if (typeof(TradeBar).IsAssignableFrom(subscription.Type))
                {
                    // if we have trade bar data we'll use the Low property, if not x => x.Value will be set in RegisterIndicator
                    selector = x => ((TradeBar)x).Low;
                }
            }

            RegisterIndicator(symbol, minimum, ResolveConsolidator(symbol, resolution), selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, minimum, resolution, selector);
            }

            return minimum;
        }

        /// <summary>
        /// Creates a new Momentum indicator. This will compute the absolute n-period change in the security.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose momentum we want</param>
        /// <param name="period">The period over which to compute the momentum</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The momentum indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public Momentum MOM(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MOM({period})", resolution);
            var momentum = new Momentum(name, period);
            InitializeIndicator(symbol, momentum, resolution, selector);

            return momentum;
        }

        /// <summary>
        /// Creates a new Momersion indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose Momersion we want</param>
        /// <param name="minPeriod">The minimum period over which to compute the Momersion. Must be greater than 3. If null, only full period will be used in computations.</param>
        /// <param name="fullPeriod">The full period over which to compute the Momersion</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The Momersion indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public MomersionIndicator MOMERSION(Symbol symbol, int? minPeriod, int fullPeriod, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MOMERSION({minPeriod},{fullPeriod})", resolution);
            var momersion = new MomersionIndicator(name, minPeriod, fullPeriod);
            InitializeIndicator(symbol, momersion, resolution, selector);

            return momersion;
        }

        /// <summary>
        /// Creates a new MomentumPercent indicator. This will compute the n-period percent change in the security.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose momentum we want</param>
        /// <param name="period">The period over which to compute the momentum</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The momentum indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public MomentumPercent MOMP(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MOMP({period})", resolution);
            var momentumPercent = new MomentumPercent(name, period);
            InitializeIndicator(symbol, momentumPercent, resolution, selector);

            return momentumPercent;
        }

        /// <summary>
        /// Creates a new NormalizedAverageTrueRange indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose NATR we want</param>
        /// <param name="period">The period over which to compute the NATR</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The NormalizedAverageTrueRange indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public NormalizedAverageTrueRange NATR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"NATR({period})", resolution);
            var normalizedAverageTrueRange = new NormalizedAverageTrueRange(name, period);
            InitializeIndicator(symbol, normalizedAverageTrueRange, resolution, selector);

            return normalizedAverageTrueRange;
        }

        /// <summary>
        /// Creates a new On Balance Volume indicator. This will compute the cumulative total volume
        /// based on whether the close price being higher or lower than the previous period.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose On Balance Volume we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The On Balance Volume indicator for the requested symbol.</returns>
        [DocumentationAttribute(Indicators)]
        public OnBalanceVolume OBV(Symbol symbol, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "OBV", resolution);
            var onBalanceVolume = new OnBalanceVolume(name);
            InitializeIndicator(symbol, onBalanceVolume, resolution, selector);

            return onBalanceVolume;
        }

        /// <summary>
        /// Creates a new PivotPointsHighLow indicator
        /// </summary>
        /// <param name="symbol">The symbol whose PPHL we seek</param>
        /// <param name="lengthHigh">The number of surrounding bars whose high values should be less than the current bar's for the bar high to be marked as high pivot point</param>
        /// <param name="lengthLow">The number of surrounding bars whose low values should be more than the current bar's for the bar low to be marked as low pivot point</param>
        /// <param name="lastStoredValues">The number of last stored indicator values</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The PivotPointsHighLow indicator for the requested symbol.</returns>
        [DocumentationAttribute(Indicators)]
        public PivotPointsHighLow PPHL(Symbol symbol, int lengthHigh, int lengthLow, int lastStoredValues = 100, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"PPHL({lengthHigh},{lengthLow})", resolution);
            var pivotPointsHighLow = new PivotPointsHighLow(name, lengthHigh, lengthLow, lastStoredValues);
            InitializeIndicator(symbol, pivotPointsHighLow, resolution, selector);

            return pivotPointsHighLow;
        }

        /// <summary>
        /// Creates a new PercentagePriceOscillator indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose PPO we want</param>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        /// <param name="movingAverageType">The type of moving average to use</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The PercentagePriceOscillator indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public PercentagePriceOscillator PPO(Symbol symbol, int fastPeriod, int slowPeriod, MovingAverageType movingAverageType, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"PPO({fastPeriod},{slowPeriod})", resolution);
            var percentagePriceOscillator = new PercentagePriceOscillator(name, fastPeriod, slowPeriod, movingAverageType);
            InitializeIndicator(symbol, percentagePriceOscillator, resolution, selector);

            return percentagePriceOscillator;
        }

        /// <summary>
        /// Creates a new Parabolic SAR indicator
        /// </summary>
        /// <param name="symbol">The symbol whose PSAR we seek</param>
        /// <param name="afStart">Acceleration factor start value. Normally 0.02</param>
        /// <param name="afIncrement">Acceleration factor increment value. Normally 0.02</param>
        /// <param name="afMax">Acceleration factor max value. Normally 0.2</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>A ParabolicStopAndReverse configured with the specified periods</returns>
        [DocumentationAttribute(Indicators)]
        public ParabolicStopAndReverse PSAR(Symbol symbol, decimal afStart = 0.02m, decimal afIncrement = 0.02m, decimal afMax = 0.2m, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"PSAR({afStart},{afIncrement},{afMax})", resolution);
            var parabolicStopAndReverse = new ParabolicStopAndReverse(name, afStart, afIncrement, afMax);
            InitializeIndicator(symbol, parabolicStopAndReverse, resolution, selector);

            return parabolicStopAndReverse;
        }

        /// <summary>
        /// Creates a new RegressionChannel indicator which will compute the LinearRegression, UpperChannel and LowerChannel lines, the intercept and slope
        /// </summary>
        /// <param name="symbol">The symbol whose RegressionChannel we seek</param>
        /// <param name="period">The period of the standard deviation and least square moving average (linear regression line)</param>
        /// <param name="k">The number of standard deviations specifying the distance between the linear regression and upper or lower channel lines</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>A Regression Channel configured with the specified period and number of standard deviation</returns>
        [DocumentationAttribute(Indicators)]
        public RegressionChannel RC(Symbol symbol, int period, decimal k, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"RC({period},{k})", resolution);
            var regressionChannel = new RegressionChannel(name, period, k);
            InitializeIndicator(symbol, regressionChannel, resolution, selector);

            return regressionChannel;
        }

        /// <summary>
        /// Creates a new Relative Moving Average indicator for the symbol. The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose relative moving average we seek</param>
        /// <param name="period">The period of the relative moving average</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>A relative moving average configured with the specified period and number of standard deviation</returns>
        [DocumentationAttribute(Indicators)]
        public RelativeMovingAverage RMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"RMA({period})", resolution);
            var relativeMovingAverage = new RelativeMovingAverage(name, period);
            InitializeIndicator(symbol, relativeMovingAverage, resolution, selector);

            return relativeMovingAverage;
        }


        /// <summary>
        /// Creates a new RateOfChange indicator. This will compute the n-period rate of change in the security.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose RateOfChange we want</param>
        /// <param name="period">The period over which to compute the RateOfChange</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The RateOfChange indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public RateOfChange ROC(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ROC({period})", resolution);
            var rateOfChange = new RateOfChange(name, period);
            InitializeIndicator(symbol, rateOfChange, resolution, selector);

            return rateOfChange;
        }

        /// <summary>
        /// Creates a new RateOfChangePercent indicator. This will compute the n-period percentage rate of change in the security.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose RateOfChangePercent we want</param>
        /// <param name="period">The period over which to compute the RateOfChangePercent</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The RateOfChangePercent indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public RateOfChangePercent ROCP(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ROCP({period})", resolution);
            var rateOfChangePercent = new RateOfChangePercent(name, period);
            InitializeIndicator(symbol, rateOfChangePercent, resolution, selector);

            return rateOfChangePercent;
        }

        /// <summary>
        /// Creates a new RateOfChangeRatio indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose ROCR we want</param>
        /// <param name="period">The period over which to compute the ROCR</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The RateOfChangeRatio indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public RateOfChangeRatio ROCR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ROCR({period})", resolution);
            var rateOfChangeRatio = new RateOfChangeRatio(name, period);
            InitializeIndicator(symbol, rateOfChangeRatio, resolution, selector);

            return rateOfChangeRatio;
        }

        /// <summary>
        /// Creates a new RelativeStrengthIndex indicator. This will produce an oscillator that ranges from 0 to 100 based
        /// on the ratio of average gains to average losses over the specified period.
        /// </summary>
        /// <param name="symbol">The symbol whose RSI we want</param>
        /// <param name="period">The period over which to compute the RSI</param>
        /// <param name="movingAverageType">The type of moving average to use in computing the average gain/loss values</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The RelativeStrengthIndex indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public RelativeStrengthIndex RSI(Symbol symbol, int period, MovingAverageType movingAverageType = MovingAverageType.Wilders, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"RSI({period},{movingAverageType})", resolution);
            var relativeStrengthIndex = new RelativeStrengthIndex(name, period, movingAverageType);
            InitializeIndicator(symbol, relativeStrengthIndex, resolution, selector);

            return relativeStrengthIndex;
        }
        
        /// <summary>
        /// Creates a new RelativeVigorIndex indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose RVI we want</param>
        /// <param name="period">The period over which to compute the RVI</param>
        /// <param name="movingAverageType">The type of moving average to use</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The RelativeVigorIndex indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public RelativeVigorIndex RVI(Symbol symbol, int period, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"RVI({period},{movingAverageType})", resolution);
            var relativeVigorIndex = new RelativeVigorIndex(name, period, movingAverageType);
            InitializeIndicator(symbol, relativeVigorIndex, resolution, selector);

            return relativeVigorIndex;
        }

        /// <summary>
        /// Creates an RelativeDailyVolume indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose RDV we want</param>
        /// <param name="period">The period of the RDV</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Relative Volume indicator for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public RelativeDailyVolume RDV(Symbol symbol, int period = 2, Resolution resolution = Resolution.Daily, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"RDV({period})", resolution);
            var relativeDailyVolume = new RelativeDailyVolume(name, period);
            RegisterIndicator(symbol, relativeDailyVolume, resolution, selector);

            return relativeDailyVolume;
        }

        /// <summary>
        /// Creates a new Rho indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The option symbol whose values we want as an indicator</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="dividendYield">The dividend yield</param>
        /// <param name="optionModel">The option pricing model used to estimate Rho</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <returns>A new Rho indicator for the specified symbol</returns>
        [DocumentationAttribute(Indicators)]
        public Rho R(Symbol symbol, Symbol mirrorOption = null, decimal? riskFreeRate = null, decimal? dividendYield = null, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes,
            OptionPricingModelType? ivModel = null, Resolution? resolution = null)
        {
            var name = InitializeOptionIndicator<Rho>(symbol, out var riskFreeRateModel, out var dividendYieldModel, riskFreeRate, dividendYield, optionModel, resolution);

            var rho = new Rho(name, symbol, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel);
            RegisterIndicator(symbol, rho, ResolveConsolidator(symbol, resolution, typeof(QuoteBar)));
            RegisterIndicator(symbol.Underlying, rho, ResolveConsolidator(symbol.Underlying, resolution));
            if (mirrorOption != null)
            {
                RegisterIndicator(mirrorOption, rho, ResolveConsolidator(mirrorOption, resolution, typeof(QuoteBar)));
            }
            return rho;
        }

        /// <summary>
        /// Creates a new Rho indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The option symbol whose values we want as an indicator</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="dividendYield">The dividend yield</param>
        /// <param name="optionModel">The option pricing model used to estimate Rho</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <returns>A new Rho indicator for the specified symbol</returns>
        [DocumentationAttribute(Indicators)]
        public Rho ρ(Symbol symbol, Symbol mirrorOption = null, decimal? riskFreeRate = null, decimal? dividendYield = null, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes,
            OptionPricingModelType? ivModel = null, Resolution? resolution = null)
        {
            return R(symbol, mirrorOption, riskFreeRate, dividendYield, optionModel, ivModel, resolution);
        }

        /// <summary>
        /// Creates a new SuperTrend indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose SuperTrend indicator we want.</param>
        /// <param name="period">The smoothing period for average true range.</param>
        /// <param name="multiplier">Multiplier to calculate basic upper and lower bands width.</param>
        /// <param name="movingAverageType">Smoother type for average true range, defaults to Wilders.</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        [DocumentationAttribute(Indicators)]
        public SuperTrend STR(Symbol symbol, int period, decimal multiplier, MovingAverageType movingAverageType = MovingAverageType.Wilders,
            Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"STR({period},{multiplier})", resolution);
            var strend = new SuperTrend(name, period, multiplier, movingAverageType);
            InitializeIndicator(symbol, strend, resolution, selector);

            return strend;
        }

        /// <summary>
        /// Creates a new SharpeRatio indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose RSR we want</param>
        /// <param name="sharpePeriod">Period of historical observation for sharpe ratio calculation</param>
        /// <param name="riskFreeRate">
        /// Risk-free rate for sharpe ratio calculation. If not specified, it will use the algorithms' <see cref="RiskFreeInterestRateModel"/>
        /// </param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The SharpeRatio indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public SharpeRatio SR(Symbol symbol, int sharpePeriod, decimal? riskFreeRate = null, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var baseBame = riskFreeRate.HasValue ? $"SR({sharpePeriod},{riskFreeRate})" : $"SR({sharpePeriod})";
            var name = CreateIndicatorName(symbol, baseBame, resolution);
            IRiskFreeInterestRateModel riskFreeRateModel = riskFreeRate.HasValue
                ? new ConstantRiskFreeRateInterestRateModel(riskFreeRate.Value)
                // Make it a function so it's lazily evaluated: SetRiskFreeInterestRateModel can be called after this method
                : new FuncRiskFreeRateInterestRateModel((datetime) => RiskFreeInterestRateModel.GetInterestRate(datetime));
            var sharpeRatio = new SharpeRatio(name, sharpePeriod, riskFreeRateModel);
            InitializeIndicator(symbol, sharpeRatio, resolution, selector);

            return sharpeRatio;
        }

        /// <summary>
        /// Creates a new Sortino indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose Sortino we want</param>
        /// <param name="sortinoPeriod">Period of historical observation for Sortino ratio calculation</param>
        /// <param name="minimumAcceptableReturn">Minimum acceptable return (eg risk-free rate) for the Sortino ratio calculation</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The SortinoRatio indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public SortinoRatio SORTINO(Symbol symbol, int sortinoPeriod, double minimumAcceptableReturn = 0.0, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"SORTINO({sortinoPeriod},{minimumAcceptableReturn})", resolution);
            var sortinoRatio = new SortinoRatio(name, sortinoPeriod, minimumAcceptableReturn);
            InitializeIndicator(symbol, sortinoRatio, resolution, selector);

            return sortinoRatio;
        }


        /// <summary>
        /// Creates an SimpleMovingAverage indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose SMA we want</param>
        /// <param name="period">The period of the SMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The SimpleMovingAverage for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public SimpleMovingAverage SMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"SMA({period})", resolution);
            var simpleMovingAverage = new SimpleMovingAverage(name, period);
            InitializeIndicator(symbol, simpleMovingAverage, resolution, selector);

            return simpleMovingAverage;
        }

        /// <summary>
        /// Creates a new Schaff Trend Cycle indicator
        /// </summary>
        /// <param name="symbol">The symbol for the indicator to track</param>
        /// <param name="fastPeriod">The fast moving average period</param>
        /// <param name="slowPeriod">The slow moving average period</param>
        /// <param name="cyclePeriod">The signal period</param>
        /// <param name="movingAverageType">The type of moving average to use</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The SchaffTrendCycle indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public SchaffTrendCycle STC(Symbol symbol, int cyclePeriod, int fastPeriod, int slowPeriod, MovingAverageType movingAverageType = MovingAverageType.Exponential, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"STC({cyclePeriod},{fastPeriod},{slowPeriod})", resolution);
            var schaffTrendCycle = new SchaffTrendCycle(name, cyclePeriod, fastPeriod, slowPeriod, movingAverageType);
            InitializeIndicator(symbol, schaffTrendCycle, resolution, selector);

            return schaffTrendCycle;
        }

        /// <summary>
        /// Creates a new StandardDeviation indicator. This will return the population standard deviation of samples over the specified period.
        /// </summary>
        /// <param name="symbol">The symbol whose STD we want</param>
        /// <param name="period">The period over which to compute the STD</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The StandardDeviation indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public StandardDeviation STD(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"STD({period})", resolution);
            var standardDeviation = new StandardDeviation(name, period);
            InitializeIndicator(symbol, standardDeviation, resolution, selector);

            return standardDeviation;
        }

        /// <summary>
        /// Creates a new TargetDownsideDeviation indicator. The target downside deviation is defined as the root-mean-square, or RMS, of the deviations of the
        /// realized return’s underperformance from the target return where all returns above the target return are treated as underperformance of 0.
        /// </summary>
        /// <param name="symbol">The symbol whose TDD we want</param>
        /// <param name="period">The period over which to compute the TDD</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="minimumAcceptableReturn">Minimum acceptable return (MAR) for the target downside deviation calculation</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The TargetDownsideDeviation indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public TargetDownsideDeviation TDD(Symbol symbol, int period, double minimumAcceptableReturn = 0, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"TDD({period},{minimumAcceptableReturn})", resolution);
            var targetDownsideDeviation = new TargetDownsideDeviation(name, period, minimumAcceptableReturn);
            InitializeIndicator(symbol, targetDownsideDeviation, resolution, selector);

            return targetDownsideDeviation;
        }

        /// <summary>
        /// Creates a new Stochastic indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose stochastic we seek</param>
        /// <param name="period">The period of the stochastic. Normally 14</param>
        /// <param name="kPeriod">The sum period of the stochastic. Normally 14</param>
        /// <param name="dPeriod">The sum period of the stochastic. Normally 3</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>Stochastic indicator for the requested symbol.</returns>
        [DocumentationAttribute(Indicators)]
        public Stochastic STO(Symbol symbol, int period, int kPeriod, int dPeriod, Resolution? resolution = null,
            Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"STO({period},{kPeriod},{dPeriod})", resolution);
            var stochastic = new Stochastic(name, period, kPeriod, dPeriod);
            InitializeIndicator(symbol, stochastic, resolution, selector);

            return stochastic;
        }

        /// <summary>
        /// Overload short hand to create a new Stochastic indicator; defaulting to the 3 period for dStoch
        /// </summary>
        /// <param name="symbol">The symbol whose stochastic we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="period">The period of the stochastic. Normally 14</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>Stochastic indicator for the requested symbol.</returns>
        [DocumentationAttribute(Indicators)]
        public Stochastic STO(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            return STO(symbol, period, period, 3, resolution, selector);
        }

        /// <summary>
        /// Creates a new Sum indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose Sum we want</param>
        /// <param name="period">The period over which to compute the Sum</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The Sum indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public Sum SUM(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"SUM({period})", resolution);
            var sum = new Sum(name, period);
            InitializeIndicator(symbol, sum, resolution, selector);

            return sum;
        }

        /// <summary>
        /// Creates Swiss Army Knife transformation for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol to use for calculations</param>
        /// <param name="period">The period of the calculation</param>
        /// <param name="delta">The delta scale of the BandStop or BandPass</param>
        /// <param name="tool">The tool os the Swiss Army Knife</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">elects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The calculation using the given tool</returns>
        [DocumentationAttribute(Indicators)]
        public SwissArmyKnife SWISS(Symbol symbol, int period, double delta, SwissArmyKnifeTool tool, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"SWISS({period},{delta},{tool})", resolution);
            var swissArmyKnife = new SwissArmyKnife(name, period, delta, tool);
            InitializeIndicator(symbol, swissArmyKnife, resolution, selector);

            return swissArmyKnife;
        }

        /// <summary>
        /// Creates a new Theta indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The option symbol whose values we want as an indicator</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="dividendYield">The dividend yield</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <returns>A new Theta indicator for the specified symbol</returns>
        [DocumentationAttribute(Indicators)]
        public Theta T(Symbol symbol, Symbol mirrorOption = null, decimal? riskFreeRate = null, decimal? dividendYield = null, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes,
            OptionPricingModelType? ivModel = null, Resolution? resolution = null)
        {
            var name = InitializeOptionIndicator<Theta>(symbol, out var riskFreeRateModel, out var dividendYieldModel, riskFreeRate, dividendYield, optionModel, resolution);

            var theta = new Theta(name, symbol, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel);
            RegisterIndicator(symbol, theta, ResolveConsolidator(symbol, resolution, typeof(QuoteBar)));
            RegisterIndicator(symbol.Underlying, theta, ResolveConsolidator(symbol.Underlying, resolution));
            if (mirrorOption != null)
            {
                RegisterIndicator(mirrorOption, theta, ResolveConsolidator(mirrorOption, resolution, typeof(QuoteBar)));
            }
            return theta;
        }

        /// <summary>
        /// Creates a new Theta indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The option symbol whose values we want as an indicator</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="dividendYield">The dividend yield</param>
        /// <param name="optionModel">The option pricing model used to estimate Theta</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <returns>A new Theta indicator for the specified symbol</returns>
        [DocumentationAttribute(Indicators)]
        public Theta Θ(Symbol symbol, Symbol mirrorOption = null, decimal? riskFreeRate = null, decimal? dividendYield = null, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes,
            OptionPricingModelType? ivModel = null, Resolution? resolution = null)
        {
            return T(symbol, mirrorOption, riskFreeRate, dividendYield, optionModel, ivModel, resolution);
        }

        /// <summary>
        /// Creates a new T3MovingAverage indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose T3 we want</param>
        /// <param name="period">The period over which to compute the T3</param>
        /// <param name="volumeFactor">The volume factor to be used for the T3 (value must be in the [0,1] range, defaults to 0.7)</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The T3MovingAverage indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public T3MovingAverage T3(Symbol symbol, int period, decimal volumeFactor = 0.7m, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"T3({period},{volumeFactor})", resolution);
            var t3MovingAverage = new T3MovingAverage(name, period, volumeFactor);
            InitializeIndicator(symbol, t3MovingAverage, resolution, selector);

            return t3MovingAverage;
        }

        /// <summary>
        /// Creates a new TripleExponentialMovingAverage indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose TEMA we want</param>
        /// <param name="period">The period over which to compute the TEMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The TripleExponentialMovingAverage indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public TripleExponentialMovingAverage TEMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"TEMA({period})", resolution);
            var tripleExponentialMovingAverage = new TripleExponentialMovingAverage(name, period);
            InitializeIndicator(symbol, tripleExponentialMovingAverage, resolution, selector);

            return tripleExponentialMovingAverage;
        }

        /// <summary>
        /// Creates a TrueStrengthIndex indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose TSI we want</param>
        /// <param name="shortTermPeriod">Period used for the first price change smoothing</param>
        /// <param name="longTermPeriod">Period used for the second (double) price change smoothing</param>
        /// <param name="signalPeriod">The signal period</param>
        /// <param name="signalType">The type of moving average to use for the signal</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The TrueStrengthIndex indicator for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public TrueStrengthIndex TSI(Symbol symbol, int longTermPeriod = 25, int shortTermPeriod = 13, int signalPeriod = 7,
            MovingAverageType signalType = MovingAverageType.Exponential, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"TSI({longTermPeriod},{shortTermPeriod},{signalPeriod})", resolution);
            var trueStrengthIndex = new TrueStrengthIndex(name, longTermPeriod, shortTermPeriod, signalPeriod, signalType);
            InitializeIndicator(symbol, trueStrengthIndex, resolution, selector);

            return trueStrengthIndex;
        }

        /// <summary>
        /// Creates a new TrueRange indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose TR we want</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The TrueRange indicator for the requested symbol.</returns>
        [DocumentationAttribute(Indicators)]
        public TrueRange TR(Symbol symbol, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "TR", resolution);
            var trueRange = new TrueRange(name);
            InitializeIndicator(symbol, trueRange, resolution, selector);

            return trueRange;
        }

        /// <summary>
        /// Creates a new TriangularMovingAverage indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose TRIMA we want</param>
        /// <param name="period">The period over which to compute the TRIMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The TriangularMovingAverage indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public TriangularMovingAverage TRIMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"TRIMA({period})", resolution);
            var triangularMovingAverage = new TriangularMovingAverage(name, period);
            InitializeIndicator(symbol, triangularMovingAverage, resolution, selector);

            return triangularMovingAverage;
        }

        /// <summary>
        /// Creates a new Trix indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose TRIX we want</param>
        /// <param name="period">The period over which to compute the TRIX</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The Trix indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public Trix TRIX(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"TRIX({period})", resolution);
            var trix = new Trix(name, period);
            InitializeIndicator(symbol, trix, resolution, selector);

            return trix;
        }

        /// <summary>
        /// Creates a new UltimateOscillator indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose ULTOSC we want</param>
        /// <param name="period1">The first period over which to compute the ULTOSC</param>
        /// <param name="period2">The second period over which to compute the ULTOSC</param>
        /// <param name="period3">The third period over which to compute the ULTOSC</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The UltimateOscillator indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public UltimateOscillator ULTOSC(Symbol symbol, int period1, int period2, int period3, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ULTOSC({period1},{period2},{period3})", resolution);
            var ultimateOscillator = new UltimateOscillator(name, period1, period2, period3);
            InitializeIndicator(symbol, ultimateOscillator, resolution, selector);

            return ultimateOscillator;
        }

        /// <summary>
        /// Creates a new Vega indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The option symbol whose values we want as an indicator</param>
        /// <param name="mirrorOption">The mirror option for parity calculation</param>
        /// <param name="riskFreeRate">The risk free rate</param>
        /// <param name="dividendYield">The dividend yield</param>
        /// <param name="optionModel">The option pricing model used to estimate Vega</param>
        /// <param name="ivModel">The option pricing model used to estimate IV</param>
        /// <param name="resolution">The desired resolution of the data</param>
        /// <returns>A new Vega indicator for the specified symbol</returns>
        [DocumentationAttribute(Indicators)]
        public Vega V(Symbol symbol, Symbol mirrorOption = null, decimal? riskFreeRate = null, decimal? dividendYield = null, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes,
            OptionPricingModelType? ivModel = null, Resolution? resolution = null)
        {
            var name = InitializeOptionIndicator<Vega>(symbol, out var riskFreeRateModel, out var dividendYieldModel, riskFreeRate, dividendYield, optionModel, resolution);

            var vega = new Vega(name, symbol, riskFreeRateModel, dividendYieldModel, mirrorOption, optionModel, ivModel);
            RegisterIndicator(symbol, vega, ResolveConsolidator(symbol, resolution, typeof(QuoteBar)));
            RegisterIndicator(symbol.Underlying, vega, ResolveConsolidator(symbol.Underlying, resolution));
            if (mirrorOption != null)
            {
                RegisterIndicator(mirrorOption, vega, ResolveConsolidator(mirrorOption, resolution, typeof(QuoteBar)));
            }
            return vega;
        }

        /// <summary>
        /// Creates a new Chande's Variable Index Dynamic Average indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose VIDYA we want</param>
        /// <param name="period">The period over which to compute the VIDYA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The VariableIndexDynamicAverage indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public VariableIndexDynamicAverage VIDYA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"VIDYA({period})", resolution);
            var variableIndexDynamicAverage = new VariableIndexDynamicAverage(name, period);
            InitializeIndicator(symbol, variableIndexDynamicAverage, resolution, selector);

            return variableIndexDynamicAverage;
        }

        /// <summary>
        /// Creates a new Variance indicator. This will return the population variance of samples over the specified period.
        /// </summary>
        /// <param name="symbol">The symbol whose VAR we want</param>
        /// <param name="period">The period over which to compute the VAR</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The Variance indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        [Obsolete("'VAR' is obsolete please use 'V' instead")]
        public Variance VAR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            return V(symbol, period, resolution, selector);
        }

        /// <summary>
        /// Creates a new Variance indicator. This will return the population variance of samples over the specified period.
        /// </summary>
        /// <param name="symbol">The symbol whose variance we want</param>
        /// <param name="period">The period over which to compute the variance</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The Variance indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public Variance V(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"V({period})", resolution);
            var variance = new Variance(name, period);
            InitializeIndicator(symbol, variance, resolution, selector);

            return variance;
        }

        /// <summary>
        /// Creates a new ValueAtRisk indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose VAR we want</param>
        /// <param name="period">The period over which to compute the VAR</param>
        /// <param name="confidenceLevel">The confidence level for Value at risk calculation</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The ValueAtRisk indicator for the requested Symbol, lookback period, and confidence level</returns>
        public ValueAtRisk VAR(Symbol symbol, int period, double confidenceLevel, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"VAR({period},{confidenceLevel})", resolution);
            var valueAtRisk = new ValueAtRisk(name, period, confidenceLevel);
            InitializeIndicator(symbol, valueAtRisk, resolution, selector);

            return valueAtRisk;
        }

        /// <summary>
        /// Creates an VolumeWeightedAveragePrice (VWAP) indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose VWAP we want</param>
        /// <param name="period">The period of the VWAP</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The VolumeWeightedAveragePrice for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public VolumeWeightedAveragePriceIndicator VWAP(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"VWAP({period})", resolution);
            var volumeWeightedAveragePriceIndicator = new VolumeWeightedAveragePriceIndicator(name, period);
            InitializeIndicator(symbol, volumeWeightedAveragePriceIndicator, resolution, selector);

            return volumeWeightedAveragePriceIndicator;
        }

        /// <summary>
        /// Creates the canonical VWAP indicator that resets each day. The indicator will be automatically
        /// updated on the security's configured resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose VWAP we want</param>
        /// <returns>The IntradayVWAP for the specified symbol</returns>
        [DocumentationAttribute(Indicators)]
        public IntradayVwap VWAP(Symbol symbol)
        {
            var name = CreateIndicatorName(symbol, "VWAP", null);
            var intradayVwap = new IntradayVwap(name);
            RegisterIndicator(symbol, intradayVwap);
            return intradayVwap;
        }

        /// <summary>
        /// Creates a new Williams %R indicator. This will compute the percentage change of
        /// the current closing price in relation to the high and low of the past N periods.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose Williams %R we want</param>
        /// <param name="period">The period over which to compute the Williams %R</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Williams %R indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public WilliamsPercentR WILR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"WILR({period})", resolution);
            var williamsPercentR = new WilliamsPercentR(name, period);
            InitializeIndicator(symbol, williamsPercentR, resolution, selector);

            return williamsPercentR;
        }

        /// <summary>
        /// Creates a WilderMovingAverage indicator for the symbol.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose WMA we want</param>
        /// <param name="period">The period of the WMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The WilderMovingAverage for the given parameters</returns>
        /// <remarks>WWMA for Welles Wilder Moving Average</remarks>
        [DocumentationAttribute(Indicators)]
        public WilderMovingAverage WWMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"WWMA({period})", resolution);
            var wilderMovingAverage = new WilderMovingAverage(name, period);
            InitializeIndicator(symbol, wilderMovingAverage, resolution, selector);

            return wilderMovingAverage;
        }

        /// <summary>
        /// Creates a Wilder Swing Index (SI) indicator for the symbol.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose SI we want</param>
        /// <param name="limitMove">The maximum daily change in price for the SI</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The WilderSwingIndex for the given parameters</returns>
        /// <remarks>SI for Wilder Swing Index</remarks>
        [DocumentationAttribute(Indicators)]
        public WilderSwingIndex SI(Symbol symbol, decimal limitMove, Resolution? resolution = Resolution.Daily,
            Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "SI", resolution);
            var si = new WilderSwingIndex(name, limitMove);
            InitializeIndicator(symbol, si, resolution, selector);

            return si;
        }

        /// <summary>
        /// Creates a Wilder Accumulative Swing Index (ASI) indicator for the symbol.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose ASI we want</param>
        /// <param name="limitMove">The maximum daily change in price for the ASI</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The WilderAccumulativeSwingIndex for the given parameters</returns>
        /// <remarks>ASI for Wilder Accumulative Swing Index</remarks>
        [DocumentationAttribute(Indicators)]
        public WilderAccumulativeSwingIndex ASI(Symbol symbol, decimal limitMove, Resolution? resolution = Resolution.Daily,
            Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "ASI", resolution);
            var asi = new WilderAccumulativeSwingIndex(name, limitMove);
            InitializeIndicator(symbol, asi, resolution, selector);

            return asi;
        }

        /// <summary>
        /// Creates a new Arms Index indicator
        /// </summary>
        /// <param name="symbols">The symbols whose Arms Index we want</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Arms Index indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public ArmsIndex TRIN(IEnumerable<Symbol> symbols, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            return TRIN(symbols.ToArray(), resolution, selector);
        }

        /// <summary>
        /// Creates a new Arms Index indicator
        /// </summary>
        /// <param name="symbols">The symbols whose Arms Index we want</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Arms Index indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public ArmsIndex TRIN(Symbol[] symbols, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(QuantConnect.Symbol.None, "TRIN", resolution ?? GetSubscription(symbols.First()).Resolution);
            var trin = new ArmsIndex(name);
            foreach (var symbol in symbols)
            {
                trin.Add(symbol);
                InitializeIndicator(symbol, trin, resolution, selector);
            }

            return trin;
        }

        /// <summary>
        /// Creates a new Advance/Decline Ratio indicator
        /// </summary>
        /// <param name="symbols">The symbols whose A/D Ratio we want</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Advance/Decline Ratio indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public AdvanceDeclineRatio ADR(IEnumerable<Symbol> symbols, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(QuantConnect.Symbol.None, "A/D Ratio", resolution ?? GetSubscription(symbols.First()).Resolution);
            var adr = new AdvanceDeclineRatio(name);
            foreach (var symbol in symbols)
            {
                adr.Add(symbol);
                InitializeIndicator(symbol, adr, resolution, selector);
            }

            return adr;
        }

        /// <summary>
        /// Creates a new Advance/Decline Volume Ratio indicator
        /// </summary>
        /// <param name="symbols">The symbol whose A/D Volume Rate we want</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Advance/Decline Volume Ratio indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public AdvanceDeclineVolumeRatio ADVR(IEnumerable<Symbol> symbols, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(QuantConnect.Symbol.None, "A/D Volume Rate", resolution ?? GetSubscription(symbols.First()).Resolution);
            var advr = new AdvanceDeclineVolumeRatio(name);
            foreach (var symbol in symbols)
            {
                advr.Add(symbol);
                InitializeIndicator(symbol, advr, resolution, selector);
            }

            return advr;
        }

        /// <summary>
        /// Creates a new Advance/Decline Difference indicator
        /// </summary>
        /// <param name="symbols">The symbols whose A/D Difference we want</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Advance/Decline Difference indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public AdvanceDeclineDifference ADDIFF(IEnumerable<Symbol> symbols, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(QuantConnect.Symbol.None, "A/D Difference", resolution ?? GetSubscription(symbols.First()).Resolution);
            var adDiff = new AdvanceDeclineDifference(name);
            foreach (var symbol in symbols)
            {
                adDiff.Add(symbol);
                InitializeIndicator(symbol, adDiff, resolution, selector);
            }

            return adDiff;
        }

        /// <summary>
        /// Creates a new McClellan Oscillator indicator
        /// </summary>
        /// <param name="symbols">The symbols whose McClellan Oscillator we want</param>
        /// <param name="fastPeriod">Fast period EMA of advance decline difference</param>
        /// <param name="slowPeriod">Slow period EMA of advance decline difference</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The McClellan Oscillator indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public McClellanOscillator MOSC(IEnumerable<Symbol> symbols, int fastPeriod = 19, int slowPeriod = 39, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            return MOSC(symbols.ToArray(), fastPeriod, slowPeriod, resolution, selector);
        }

        /// <summary>
        /// Creates a new McClellan Oscillator indicator
        /// </summary>
        /// <param name="symbols">The symbols whose McClellan Oscillator we want</param>
        /// <param name="fastPeriod">Fast period EMA of advance decline difference</param>
        /// <param name="slowPeriod">Slow period EMA of advance decline difference</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The McClellan Oscillator indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public McClellanOscillator MOSC(Symbol[] symbols, int fastPeriod = 19, int slowPeriod = 39, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(QuantConnect.Symbol.None, $"MO({fastPeriod},{slowPeriod})", resolution ?? GetSubscription(symbols.First()).Resolution);
            var mosc = new McClellanOscillator(name, fastPeriod, slowPeriod);
            foreach (var symbol in symbols)
            {
                mosc.Add(symbol);
                InitializeIndicator(symbol, mosc, resolution, selector);
            }

            return mosc;
        }

        /// <summary>
        /// Creates a new McClellan Summation Index indicator
        /// </summary>
        /// <param name="symbols">The symbols whose McClellan Summation Index we want</param>
        /// <param name="fastPeriod">Fast period EMA of advance decline difference</param>
        /// <param name="slowPeriod">Slow period EMA of advance decline difference</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The McClellan Summation Index indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public McClellanSummationIndex MSI(IEnumerable<Symbol> symbols, int fastPeriod = 19, int slowPeriod = 39, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            return MSI(symbols.ToArray(), fastPeriod, slowPeriod, resolution, selector);
        }

        /// <summary>
        /// Creates a new McClellan Summation Index indicator
        /// </summary>
        /// <param name="symbols">The symbols whose McClellan Summation Index we want</param>
        /// <param name="fastPeriod">Fast period EMA of advance decline difference</param>
        /// <param name="slowPeriod">Slow period EMA of advance decline difference</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The McClellan Summation Index indicator for the requested symbol over the specified period</returns>
        [DocumentationAttribute(Indicators)]
        public McClellanSummationIndex MSI(Symbol[] symbols, int fastPeriod = 19, int slowPeriod = 39, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(QuantConnect.Symbol.None, $"MSI({fastPeriod},{slowPeriod})", resolution ?? GetSubscription(symbols.First()).Resolution);
            var msi = new McClellanSummationIndex(name, fastPeriod, slowPeriod);
            foreach (var symbol in symbols)
            {
                msi.Add(symbol);
                InitializeIndicator(symbol, msi, resolution, selector);
            }

            return msi;
        }

        /// <summary>
        /// Creates a new name for an indicator created with the convenience functions (SMA, EMA, ect...)
        /// </summary>
        /// <param name="symbol">The symbol this indicator is registered to</param>
        /// <param name="type">The indicator type, for example, 'SMA(5)'</param>
        /// <param name="resolution">The resolution requested</param>
        /// <returns>A unique for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public string CreateIndicatorName(Symbol symbol, FormattableString type, Resolution? resolution)
        {
            return CreateIndicatorName(symbol, Invariant(type), resolution);
        }

        /// <summary>
        /// Creates a new name for an indicator created with the convenience functions (SMA, EMA, ect...)
        /// </summary>
        /// <param name="symbol">The symbol this indicator is registered to</param>
        /// <param name="type">The indicator type, for example, 'SMA(5)'</param>
        /// <param name="resolution">The resolution requested</param>
        /// <returns>A unique for the given parameters</returns>
        [DocumentationAttribute(Indicators)]
        public string CreateIndicatorName(Symbol symbol, string type, Resolution? resolution)
        {
            var symbolIsNotEmpty = symbol != QuantConnect.Symbol.None && symbol != QuantConnect.Symbol.Empty;

            if (!resolution.HasValue && symbolIsNotEmpty)
            {
                resolution = GetSubscription(symbol).Resolution;
            }

            var res = string.Empty;
            switch (resolution)
            {
                case Resolution.Tick:
                    res = "tick";
                    break;

                case Resolution.Second:
                    res = "sec";
                    break;

                case Resolution.Minute:
                    res = "min";
                    break;

                case Resolution.Hour:
                    res = "hr";
                    break;

                case Resolution.Daily:
                    res = "day";
                    break;

                case null:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "resolution parameter is out of range.");
            }

            var parts = new List<string>();

            if (symbolIsNotEmpty)
            {
                parts.Add(symbol.ToString());
            }
            parts.Add(res);

            return Invariant($"{type}({string.Join("_", parts)})").Replace(")(", ",");
        }

        /// <summary>
        /// Gets the SubscriptionDataConfig for the specified symbol and tick type
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if no configuration is found for the requested symbol</exception>
        /// <param name="symbol">The symbol to retrieve configuration for</param>
        /// <param name="tickType">The tick type of the subscription to get. If null, will use the first ordered by TickType</param>
        /// <returns>The SubscriptionDataConfig for the specified symbol</returns>
        private SubscriptionDataConfig GetSubscription(Symbol symbol, TickType? tickType = null)
        {
            SubscriptionDataConfig subscription;
            try
            {
                // deterministic ordering is required here
                var subscriptions = SubscriptionManager.SubscriptionDataConfigService
                    .GetSubscriptionDataConfigs(symbol)
                    // make sure common lean data types are at the bottom
                    .OrderByDescending(x => LeanData.IsCommonLeanDataType(x.Type))
                    .ThenBy(x => x.TickType)
                    .ToList();

                // find our subscription
                subscription = subscriptions.FirstOrDefault(x => tickType == null || tickType == x.TickType);
                if (subscription == null)
                {
                    // if we can't locate the exact subscription by tick type just grab the first one we find
                    subscription = subscriptions.First();
                }
            }
            catch (InvalidOperationException)
            {
                // this will happen if we did not find the subscription, let's give the user a decent error message
                throw new Exception($"Please register to receive data for symbol \'{symbol}\' using the AddSecurity() function.");
            }
            return subscription;
        }

        /// <summary>
        /// Creates and registers a new consolidator to receive automatic updates at the specified resolution as well as configures
        /// the indicator to receive updates from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        [DocumentationAttribute(ConsolidatingData)]
        [DocumentationAttribute(Indicators)]
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IndicatorDataPoint> indicator, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            RegisterIndicator(symbol, indicator, ResolveConsolidator(symbol, resolution), selector ?? (x => x.Value));
        }

        /// <summary>
        /// Creates and registers a new consolidator to receive automatic updates at the specified resolution as well as configures
        /// the indicator to receive updates from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        [DocumentationAttribute(ConsolidatingData)]
        [DocumentationAttribute(Indicators)]
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IndicatorDataPoint> indicator, TimeSpan? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            RegisterIndicator(symbol, indicator, ResolveConsolidator(symbol, resolution), selector ?? (x => x.Value));
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="consolidator">The consolidator to receive raw subscription data</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        [DocumentationAttribute(ConsolidatingData)]
        [DocumentationAttribute(Indicators)]
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IndicatorDataPoint> indicator, IDataConsolidator consolidator, Func<IBaseData, decimal> selector = null)
        {
            // default our selector to the Value property on BaseData
            selector = selector ?? (x => x.Value);

            RegisterConsolidator(indicator, symbol, consolidator);

            // attach to the DataConsolidated event so it updates our indicator
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                var value = selector(consolidated);
                indicator.Update(new IndicatorDataPoint(consolidated.Symbol, consolidated.EndTime, value));
            };
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        [DocumentationAttribute(ConsolidatingData)]
        [DocumentationAttribute(Indicators)]
        public void RegisterIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, Resolution? resolution = null)
            where T : IBaseData
        {
            RegisterIndicator(symbol, indicator, ResolveConsolidator(symbol, resolution, typeof(T)));
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        [DocumentationAttribute(ConsolidatingData)]
        [DocumentationAttribute(Indicators)]
        public void RegisterIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, Resolution? resolution, Func<IBaseData, T> selector)
            where T : IBaseData
        {
            RegisterIndicator(symbol, indicator, ResolveConsolidator(symbol, resolution, typeof(T)), selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        [DocumentationAttribute(ConsolidatingData)]
        [DocumentationAttribute(Indicators)]
        public void RegisterIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, TimeSpan? resolution, Func<IBaseData, T> selector = null)
            where T : IBaseData
        {
            RegisterIndicator(symbol, indicator, ResolveConsolidator(symbol, resolution, typeof(T)), selector);
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="consolidator">The consolidator to receive raw subscription data</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        [DocumentationAttribute(ConsolidatingData)]
        [DocumentationAttribute(Indicators)]
        public void RegisterIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, IDataConsolidator consolidator, Func<IBaseData, T> selector = null)
            where T : IBaseData
        {
            // assign default using cast
            var selectorToUse = selector ?? (x => (T)x);

            RegisterConsolidator(indicator, symbol, consolidator);

            // check the output type of the consolidator and verify we can assign it to T
            var type = typeof(T);
            if (!type.IsAssignableFrom(consolidator.OutputType))
            {
                if (type == typeof(IndicatorDataPoint) && selector == null)
                {
                    // if no selector was provided and the indicator input is of 'IndicatorDataPoint', common case, a selector with a direct cast will fail
                    // so we use a smarter selector as in other API methods
                    selectorToUse = consolidated => (T)(object)new IndicatorDataPoint(consolidated.Symbol, consolidated.EndTime, consolidated.Value);
                }
                else
                {
                    throw new ArgumentException($"Type mismatch found between consolidator and indicator for symbol: {symbol}." +
                                                $"Consolidator outputs type {consolidator.OutputType.Name} but indicator expects input type {type.Name}"
                    );
                }
            }

            // attach to the DataConsolidated event so it updates our indicator
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                var value = selectorToUse(consolidated);
                indicator.Update(value);
            };
        }

        /// <summary>
        /// Will unregister an indicator and it's associated consolidator instance so they stop receiving data updates
        /// </summary>
        /// <param name="indicator">The indicator instance to unregister</param>
        [DocumentationAttribute(ConsolidatingData)]
        [DocumentationAttribute(Indicators)]
        public void UnregisterIndicator(IndicatorBase indicator)
        {
            DeregisterIndicator(indicator);
        }

        /// <summary>
        /// Will deregister an indicator and it's associated consolidator instance so they stop receiving data updates
        /// </summary>
        /// <param name="indicator">The indicator instance to deregister</param>
        [DocumentationAttribute(ConsolidatingData)]
        [DocumentationAttribute(Indicators)]
        public void DeregisterIndicator(IndicatorBase indicator)
        {
            foreach (var consolidator in indicator.Consolidators)
            {
                SubscriptionManager.RemoveConsolidator(null, consolidator);
            }

            indicator.Consolidators.Clear();
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbol">The symbol whose indicator we want</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        [DocumentationAttribute(HistoricalData)]
        [DocumentationAttribute(Indicators)]
        public void WarmUpIndicator(Symbol symbol, IndicatorBase<IndicatorDataPoint> indicator, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            resolution = GetResolution(symbol, resolution, null);
            var period = resolution.Value.ToTimeSpan();
            WarmUpIndicator(symbol, indicator, period, selector);
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbol">The symbol whose indicator we want</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="period">The necessary period to warm up the indicator</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        [DocumentationAttribute(HistoricalData)]
        [DocumentationAttribute(Indicators)]
        public void WarmUpIndicator(Symbol symbol, IndicatorBase<IndicatorDataPoint> indicator, TimeSpan period, Func<IBaseData, decimal> selector = null)
        {
            var history = GetIndicatorWarmUpHistory(symbol, indicator, period, out var identityConsolidator);
            if (history == Enumerable.Empty<Slice>()) return;

            // assign default using cast
            selector ??= (x => x.Value);

            Action<IBaseData> onDataConsolidated = bar =>
            {
                var input = new IndicatorDataPoint(bar.Symbol, bar.EndTime, selector(bar));
                indicator.Update(input);
            };

            WarmUpIndicatorImpl(symbol, period, onDataConsolidated, history, identityConsolidator);
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbol">The symbol whose indicator we want</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        [DocumentationAttribute(HistoricalData)]
        [DocumentationAttribute(Indicators)]
        public void WarmUpIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, Resolution? resolution = null, Func<IBaseData, T> selector = null)
            where T : class, IBaseData
        {
            resolution = GetResolution(symbol, resolution, typeof(T));
            var period = resolution.Value.ToTimeSpan();
            WarmUpIndicator(symbol, indicator, period, selector);
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbol">The symbol whose indicator we want</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="period">The necessary period to warm up the indicator</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        [DocumentationAttribute(HistoricalData)]
        [DocumentationAttribute(Indicators)]
        public void WarmUpIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, TimeSpan period, Func<IBaseData, T> selector = null)
            where T : class, IBaseData
        {
            var history = GetIndicatorWarmUpHistory(symbol, indicator, period, out var identityConsolidator);
            if (history == Enumerable.Empty<Slice>()) return;

            // assign default using cast
            selector ??= (x => (T)x);

            // we expect T type as input
            Action<T> onDataConsolidated = bar =>
            {
                indicator.Update(selector(bar));
            };

            WarmUpIndicatorImpl(symbol, period, onDataConsolidated, history, identityConsolidator);
        }

        private IEnumerable<Slice> GetIndicatorWarmUpHistory(Symbol symbol, IIndicator indicator, TimeSpan timeSpan, out bool identityConsolidator)
        {
            identityConsolidator = false;
            var periods = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (periods.HasValue && periods != 0)
            {
                var resolution = timeSpan.ToHigherResolutionEquivalent(false);
                // if they are the same, means we can use an identity consolidator
                identityConsolidator = resolution.ToTimeSpan() == timeSpan;
                var resolutionTicks = resolution.ToTimeSpan().Ticks;
                if (resolutionTicks != 0)
                {
                    periods *= (int)(timeSpan.Ticks / resolutionTicks);
                }

                try
                {
                    return History(new[] { symbol }, periods.Value, resolution);
                }
                catch (ArgumentException e)
                {
                    Debug($"{indicator.Name} could not be warmed up. Reason: {e.Message}");
                }
            }
            else if (!_isEmitWarmupInsightWarningSent)
            {
                Debug($"Warning: the 'WarmUpIndicator' feature only works with indicators which inherit from '{nameof(IIndicatorWarmUpPeriodProvider)}'" +
                      $" and define a warm up period, setting property 'WarmUpPeriod' with a value > 0." +
                      $" The provided indicator of type '{indicator.GetType().Name}' will not be warmed up.");
                _isEmitWarmupInsightWarningSent = true;
            }

            return Enumerable.Empty<Slice>();
        }

        private void WarmUpIndicatorImpl<T>(Symbol symbol, TimeSpan period, Action<T> handler, IEnumerable<Slice> history, bool identityConsolidator)
            where T : class, IBaseData
        {
            IDataConsolidator consolidator;
            if (identityConsolidator)
            {
                period = TimeSpan.Zero;
            }
            if (SubscriptionManager.SubscriptionDataConfigService.GetSubscriptionDataConfigs(symbol).Count > 0)
            {
                consolidator = Consolidate(symbol, period, handler);
            }
            else
            {
                var providedType = typeof(T);
                if (providedType.IsAbstract)
                {
                    var dataType = SubscriptionManager.LookupSubscriptionConfigDataTypes(
                        symbol.SecurityType,
                        Resolution.Daily,
                        // order by tick type so that behavior is consistent with 'GetSubscription()'
                        symbol.IsCanonical())
                        // make sure common lean data types are at the bottom
                        .OrderByDescending(tuple => LeanData.IsCommonLeanDataType(tuple.Item1))
                        .ThenBy(tuple => tuple.Item2).First();

                    consolidator = CreateConsolidator(period, dataType.Item1, dataType.Item2);
                }
                else
                {
                    // if the 'providedType' is not abstract we use it instead to determine which consolidator to use
                    var tickType = LeanData.GetCommonTickTypeForCommonDataTypes(providedType, symbol.SecurityType);
                    consolidator = CreateConsolidator(period, providedType, tickType);
                }
                consolidator.DataConsolidated += (s, bar) => handler((T)bar);
            }

            var consolidatorInputType = consolidator.InputType;
            IBaseData lastBar = null;
            foreach (var slice in history)
            {
                if (slice.TryGet(consolidatorInputType, symbol, out var data))
                {
                    lastBar = data;
                    consolidator.Update(lastBar);
                }
            }

            // Scan for time after we've pumped all the data through for this consolidator
            if (lastBar != null)
            {
                consolidator.Scan(lastBar.EndTime);
            }

            SubscriptionManager.RemoveConsolidator(symbol, consolidator);
        }

        /// <summary>
        /// Gets the default consolidator for the specified symbol and resolution
        /// </summary>
        /// <param name="symbol">The symbol whose data is to be consolidated</param>
        /// <param name="resolution">The resolution for the consolidator, if null, uses the resolution from subscription</param>
        /// <param name="dataType">The data type for this consolidator, if null, uses TradeBar over QuoteBar if present</param>
        /// <returns>The new default consolidator</returns>
        [DocumentationAttribute(ConsolidatingData)]
        [DocumentationAttribute(Indicators)]
        public IDataConsolidator ResolveConsolidator(Symbol symbol, Resolution? resolution, Type dataType = null)
        {
            TimeSpan? timeSpan = null;
            if (resolution.HasValue)
            {
                timeSpan = resolution.Value.ToTimeSpan();
            }
            return ResolveConsolidator(symbol, timeSpan, dataType);
        }

        /// <summary>
        /// Gets the default consolidator for the specified symbol and resolution
        /// </summary>
        /// <param name="symbol">The symbol whose data is to be consolidated</param>
        /// <param name="timeSpan">The requested time span for the consolidator, if null, uses the resolution from subscription</param>
        /// <param name="dataType">The data type for this consolidator, if null, uses TradeBar over QuoteBar if present</param>
        /// <returns>The new default consolidator</returns>
        [DocumentationAttribute(ConsolidatingData)]
        [DocumentationAttribute(Indicators)]
        public IDataConsolidator ResolveConsolidator(Symbol symbol, TimeSpan? timeSpan, Type dataType = null)
        {
            var tickType = dataType != null ? LeanData.GetCommonTickTypeForCommonDataTypes(dataType, symbol.SecurityType) : (TickType?)null;
            var subscription = GetSubscription(symbol, tickType);
            var subscriptionTimeSpan = subscription.Resolution.ToTimeSpan();

            // if not specified, default to the subscription resolution
            if (!timeSpan.HasValue)
            {
                timeSpan = subscriptionTimeSpan;
            }

            // verify this consolidator will give reasonable results, if someone asks for second consolidation but we have minute
            // data we won't be able to do anything good, we'll call it second, but it would really just be minute!
            if (timeSpan.Value < subscriptionTimeSpan)
            {
                throw new ArgumentException($"Unable to create {symbol} consolidator because {symbol} is registered for " +
                    Invariant($"{subscription.Resolution.ToStringInvariant()} data. Consolidators require higher resolution data to produce lower resolution data.")
                );
            }
            else if (timeSpan.Value == subscriptionTimeSpan)
            {
                // input and expected output share the same time span, means we just want an identity consolidator
                timeSpan = TimeSpan.Zero;
            }

            return CreateConsolidator(timeSpan.Value, subscription.Type, subscription.TickType);
        }

        /// <summary>
        /// Creates a new consolidator for the specified period, generating the requested output type.
        /// </summary>
        /// <param name="period">The consolidation period</param>
        /// <param name="consolidatorInputType">The desired input type of the consolidator, such as TradeBar or QuoteBar</param>
        /// <param name="tickType">Trade or Quote. Optional, defaults to trade</param>
        /// <returns>A new consolidator matching the requested parameters</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public static IDataConsolidator CreateConsolidator(TimeSpan period, Type consolidatorInputType, TickType? tickType = null)
        {
            // if our type can be used as a trade bar, then let's just make one of those
            // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to TradeBar
            if (typeof(TradeBar).IsAssignableFrom(consolidatorInputType))
            {
                // Use IdentityDataConsolidator when data are not meant to consolidated into bars
                if (period.Ticks == 0)
                {
                    return new IdentityDataConsolidator<TradeBar>();
                }
                return new TradeBarConsolidator(period);
            }

            // if our type can be used as a quote bar, then let's just make one of those
            // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to QuoteBar
            if (typeof(QuoteBar).IsAssignableFrom(consolidatorInputType))
            {
                // Use IdentityDataConsolidator when data are not meant to consolidated into bars
                if (period.Ticks == 0)
                {
                    return new IdentityDataConsolidator<QuoteBar>();
                }
                return new QuoteBarConsolidator(period);
            }

            // if our type can be used as a tick then we'll use a consolidator that keeps the TickType
            // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to Tick
            if (typeof(Tick).IsAssignableFrom(consolidatorInputType))
            {
                // Use IdentityDataConsolidator when ticks are not meant to consolidated into bars
                if (period.Ticks == 0)
                {
                    return new IdentityDataConsolidator<Tick>();
                }

                switch (tickType)
                {
                    case TickType.OpenInterest:
                        return new OpenInterestConsolidator(period);

                    case TickType.Quote:
                        return new TickQuoteBarConsolidator(period);

                    default:
                        return new TickConsolidator(period);
                }
            }

            // if our type can be used as a DynamicData then we'll use the DynamicDataConsolidator
            if (typeof(DynamicData).IsAssignableFrom(consolidatorInputType))
            {
                // Use IdentityDataConsolidator when data are not meant to consolidated into bars
                if (period.Ticks == 0)
                {
                    return new DynamicDataConsolidator(1);
                }
                return new DynamicDataConsolidator(period);
            }

            // Use IdentityDataConsolidator when data are not meant to consolidated into bars
            if (period.Ticks == 0)
            {
                return new IdentityDataConsolidator<BaseData>();
            }
            // no matter what we can always consolidate based on the time-value pair of BaseData
            return new BaseDataConsolidator(period);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate(Symbol symbol, Resolution period, Action<TradeBar> handler)
        {
            return Consolidate(symbol, period.ToTimeSpan(), TickType.Trade, handler);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate(Symbol symbol, TimeSpan period, Action<TradeBar> handler)
        {
            return Consolidate(symbol, period, TickType.Trade, handler);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate(Symbol symbol, Resolution period, Action<QuoteBar> handler)
        {
            return Consolidate(symbol, period.ToTimeSpan(), TickType.Quote, handler);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate(Symbol symbol, TimeSpan period, Action<QuoteBar> handler)
        {
            return Consolidate(symbol, period, TickType.Quote, handler);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol and tick type.
        /// The handler and tick type must match.
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate<T>(Symbol symbol, TimeSpan period, Action<T> handler)
            where T : class, IBaseData
        {
            // only infer TickType from T if it's not abstract (for example IBaseData, BaseData), else if will end up being TradeBar let's not take that
            // decision here (default type), it will be taken later by 'GetSubscription' so we keep it centralized
            // This could happen when a user passes in a generic 'Action<BaseData>' handler
            var tickType = typeof(T).IsAbstract ? (TickType?)null : LeanData.GetCommonTickTypeForCommonDataTypes(typeof(T), symbol.SecurityType);
            return Consolidate(symbol, period, tickType, handler);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol and tick type.
        /// The handler and tick type must match.
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="tickType">The tick type of subscription used as data source for consolidator. Specify null to use first subscription found.</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate<T>(Symbol symbol, Resolution period, TickType? tickType, Action<T> handler)
            where T : class, IBaseData
        {
            return Consolidate(symbol, period.ToTimeSpan(), tickType, handler);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol and tick type.
        /// The handler and tick type must match.
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="period">The consolidation period</param>
        /// <param name="tickType">The tick type of subscription used as data source for consolidator. Specify null to use first subscription found.</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate<T>(Symbol symbol, TimeSpan period, TickType? tickType, Action<T> handler)
            where T : class, IBaseData
        {
            // resolve consolidator input subscription
            var subscription = GetSubscription(symbol, tickType);

            // create requested consolidator
            var consolidator = CreateConsolidator(period, subscription.Type, subscription.TickType);

            AddConsolidator(symbol, consolidator, handler, tickType);
            return consolidator;
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="calendar">The consolidation calendar</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate(Symbol symbol, Func<DateTime, CalendarInfo> calendar, Action<QuoteBar> handler)
        {
            return Consolidate(symbol, calendar, TickType.Quote, handler);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="calendar">The consolidation calendar</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate(Symbol symbol, Func<DateTime, CalendarInfo> calendar, Action<TradeBar> handler)
        {
            return Consolidate(symbol, calendar, TickType.Trade, handler);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol and tick type.
        /// The handler and tick type must match.
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="calendar">The consolidation calendar</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate<T>(Symbol symbol, Func<DateTime, CalendarInfo> calendar, Action<T> handler)
            where T : class, IBaseData
        {
            // only infer TickType from T if it's not abstract (for example IBaseData, BaseData), else if will end up being TradeBar let's not take that
            // decision here (default type), it will be taken later by 'GetSubscription' so we keep it centralized
            // This could happen when a user passes in a generic 'Action<BaseData>' handler
            var tickType = typeof(T).IsAbstract ? (TickType?)null : LeanData.GetCommonTickTypeForCommonDataTypes(typeof(T), symbol.SecurityType);
            return Consolidate(symbol, calendar, tickType, handler);
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol and tick type.
        /// The handler and tick type must match.
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="calendar">The consolidation calendar</param>
        /// <param name="tickType">The tick type of subscription used as data source for consolidator. Specify null to use first subscription found.</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
        [DocumentationAttribute(ConsolidatingData)]
        public IDataConsolidator Consolidate<T>(Symbol symbol, Func<DateTime, CalendarInfo> calendar, TickType? tickType, Action<T> handler)
            where T : class, IBaseData
        {
            // resolve consolidator input subscription
            var subscription = GetSubscription(symbol, tickType);

            // create requested consolidator
            var consolidator = CreateConsolidator(calendar, subscription.Type, subscription.TickType);

            AddConsolidator(symbol, consolidator, handler);
            return consolidator;
        }

        /// <summary>
        /// Adds the provided consolidator and asserts the handler T type is assignable from the consolidator output,
        /// if not will throw <see cref="ArgumentException"/>
        /// </summary>
        [DocumentationAttribute(ConsolidatingData)]
        private void AddConsolidator<T>(Symbol symbol, IDataConsolidator consolidator, Action<T> handler, TickType? tickType = null)
        {
            if (!typeof(T).IsAssignableFrom(consolidator.OutputType))
            {
                // special case downgrading of QuoteBar -> TradeBar
                if (typeof(T) == typeof(TradeBar) && consolidator.OutputType == typeof(QuoteBar))
                {
                    // collapse quote bar into trade bar (ignore the funky casting, required due to generics)
                    consolidator.DataConsolidated += (sender, consolidated) => handler((T)(object)((QuoteBar)consolidated).Collapse());
                }

                throw new ArgumentException(
                    $"Unable to consolidate with the specified handler because the consolidator's output type " +
                    $"is {consolidator.OutputType.Name} but the handler's input type is {typeof(T).Name}.");
            }

            // register user-defined handler to receive consolidated data events
            consolidator.DataConsolidated += (sender, consolidated) => handler((T)consolidated);

            // register the consolidator for automatic updates via SubscriptionManager
            SubscriptionManager.AddConsolidator(symbol, consolidator, tickType);
        }

        [DocumentationAttribute(ConsolidatingData)]
        private IDataConsolidator CreateConsolidator(Func<DateTime, CalendarInfo> calendar, Type consolidatorInputType, TickType tickType)
        {
            // if our type can be used as a trade bar, then let's just make one of those
            // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to TradeBar
            if (typeof(TradeBar).IsAssignableFrom(consolidatorInputType))
            {
                return new TradeBarConsolidator(calendar);
            }

            // if our type can be used as a quote bar, then let's just make one of those
            // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to QuoteBar
            if (typeof(QuoteBar).IsAssignableFrom(consolidatorInputType))
            {
                return new QuoteBarConsolidator(calendar);
            }

            // if our type can be used as a tick then we'll use a consolidator that keeps the TickType
            // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to Tick
            if (typeof(Tick).IsAssignableFrom(consolidatorInputType))
            {
                if (tickType == TickType.Quote)
                {
                    return new TickQuoteBarConsolidator(calendar);
                }
                return new TickConsolidator(calendar);
            }

            // if our type can be used as a DynamicData then we'll use the DynamicDataConsolidator
            if (typeof(DynamicData).IsAssignableFrom(consolidatorInputType))
            {
                return new DynamicDataConsolidator(calendar);
            }

            // no matter what we can always consolidate based on the time-value pair of BaseData
            return new BaseDataConsolidator(calendar);
        }

        /// <summary>
        /// Registers and warms up (if EnableAutomaticIndicatorWarmUp is set) the indicator
        /// </summary>
        private void InitializeIndicator(Symbol symbol, IndicatorBase<IndicatorDataPoint> indicator, Resolution? resolution = null,
            Func<IBaseData, decimal> selector = null)
        {
            RegisterIndicator(symbol, indicator, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, indicator, resolution, selector);
            }
        }

        private void InitializeIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, Resolution? resolution = null,
            Func<IBaseData, T> selector = null)
            where T : class, IBaseData
        {
            RegisterIndicator(symbol, indicator, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, indicator, resolution, selector);
            }
        }

        private string InitializeOptionIndicator<T>(Symbol symbol, out IRiskFreeInterestRateModel riskFreeRateModel, out IDividendYieldModel dividendYieldModel, 
            decimal? riskFreeRate = null, decimal? dividendYield = null, OptionPricingModelType optionModel = OptionPricingModelType.BlackScholes, Resolution? resolution = null)
            where T : OptionIndicatorBase
        {
            var name = CreateIndicatorName(symbol, $"{typeof(T).Name}({riskFreeRate},{dividendYield},{optionModel})", resolution);

            riskFreeRateModel = riskFreeRate.HasValue
                ? new ConstantRiskFreeRateInterestRateModel(riskFreeRate.Value)
                // Make it a function so it's lazily evaluated: SetRiskFreeInterestRateModel can be called after this method
                : new FuncRiskFreeRateInterestRateModel((datetime) => RiskFreeInterestRateModel.GetInterestRate(datetime));

            if (dividendYield.HasValue)
            {
                dividendYieldModel = new ConstantDividendYieldModel(dividendYield.Value);
            }
            else if (symbol.ID.SecurityType == SecurityType.FutureOption || symbol.ID.SecurityType == SecurityType.IndexOption)
            {
                dividendYieldModel = new DividendYieldProvider();
            }
            else
            {
                dividendYieldModel = new DividendYieldProvider(symbol.Underlying);
            }

            return name;
        }

        private void RegisterConsolidator(IndicatorBase indicatorBase, Symbol symbol, IDataConsolidator consolidator)
        {
            // keep a reference of the consolidator so we can unregister it later using only a reference to the indicator
            indicatorBase.Consolidators.Add(consolidator);

            // register the consolidator for automatic updates via SubscriptionManager
            SubscriptionManager.AddConsolidator(symbol, consolidator);
        }
    }
}
