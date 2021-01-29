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
        private bool _isWarmUpIndicatorWarningSent = false;

        /// <summary>
        /// Gets whether or not WarmUpIndicator is allowed to warm up indicators/>
        /// </summary>
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
        public AccelerationBands ABANDS(Symbol symbol, int period, decimal width = 4, MovingAverageType movingAverageType = MovingAverageType.Simple,
            Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ABANDS({period},{width})", resolution);
            var accelerationBands = new AccelerationBands(name, period, width, movingAverageType);
            RegisterIndicator(symbol, accelerationBands, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, accelerationBands, resolution);
            }

            return accelerationBands;
        }

        /// <summary>
        /// Creates a new AccumulationDistribution indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose AD we want</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The AccumulationDistribution indicator for the requested symbol over the speified period</returns>
        public AccumulationDistribution AD(Symbol symbol, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "AD", resolution);
            var accumulationDistribution = new AccumulationDistribution(name);
            RegisterIndicator(symbol, accumulationDistribution, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, accumulationDistribution, resolution);
            }

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
        /// <returns>The AccumulationDistributionOscillator indicator for the requested symbol over the speified period</returns>
        public AccumulationDistributionOscillator ADOSC(Symbol symbol, int fastPeriod, int slowPeriod, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ADOSC({fastPeriod},{slowPeriod})", resolution);
            var accumulationDistributionOscillator = new AccumulationDistributionOscillator(name, fastPeriod, slowPeriod);
            RegisterIndicator(symbol, accumulationDistributionOscillator, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, accumulationDistributionOscillator, resolution);
            }

            return accumulationDistributionOscillator;
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
        /// <returns>The ARIMA indicator for the requested symbol over the specified period</returns>
        public AutoRegressiveIntegratedMovingAverage ARIMA(Symbol symbol, int arOrder, int diffOrder, int maOrder, int period, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(symbol, $"ARIMA({arOrder},{diffOrder},{maOrder},{period})", resolution);
            var arimaIndicator = new AutoRegressiveIntegratedMovingAverage(name, arOrder, diffOrder, maOrder, period);
            RegisterIndicator(symbol, arimaIndicator, resolution);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, arimaIndicator, resolution);
            }

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
        public AverageDirectionalIndex ADX(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ADX({period})", resolution);
            var averageDirectionalIndex = new AverageDirectionalIndex(name, period);
            RegisterIndicator(symbol, averageDirectionalIndex, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, averageDirectionalIndex, resolution);
            }

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
        public AwesomeOscillator AO(Symbol symbol, int slowPeriod, int fastPeriod, MovingAverageType type, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"AO({fastPeriod},{slowPeriod},{type})", resolution);
            var awesomeOscillator = new AwesomeOscillator(name, fastPeriod, slowPeriod, type);
            RegisterIndicator(symbol, awesomeOscillator, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, awesomeOscillator, resolution);
            }

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
        public AverageDirectionalMovementIndexRating ADXR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ADXR({period})", resolution);
            var averageDirectionalMovementIndexRating = new AverageDirectionalMovementIndexRating(name, period);
            RegisterIndicator(symbol, averageDirectionalMovementIndexRating, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, averageDirectionalMovementIndexRating, resolution);
            }

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
        public ArnaudLegouxMovingAverage ALMA(Symbol symbol, int period, int sigma = 6, decimal offset = 0.85m, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ALMA({period},{sigma},{offset})", resolution);
            var arnaudLegouxMovingAverage = new ArnaudLegouxMovingAverage(name, period, sigma, offset);
            RegisterIndicator(symbol, arnaudLegouxMovingAverage, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, arnaudLegouxMovingAverage, resolution);
            }

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
        public AbsolutePriceOscillator APO(Symbol symbol, int fastPeriod, int slowPeriod, MovingAverageType movingAverageType, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"APO({fastPeriod},{slowPeriod})", resolution);
            var absolutePriceOscillator = new AbsolutePriceOscillator(name, fastPeriod, slowPeriod, movingAverageType);
            RegisterIndicator(symbol, absolutePriceOscillator, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, absolutePriceOscillator, resolution);
            }

            return absolutePriceOscillator;
        }

        /// <summary>
        /// Creates a new AroonOscillator indicator which will compute the AroonUp and AroonDown (as well as the delta)
        /// </summary>
        /// <param name="symbol">The symbol whose Aroon we seek</param>
        /// <param name="period">The look back period for computing number of periods since maximum and minimum</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>An AroonOscillator configured with the specied periods</returns>
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
        public AroonOscillator AROON(Symbol symbol, int upPeriod, int downPeriod, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"AROON({upPeriod},{downPeriod})", resolution);
            var aroonOscillator = new AroonOscillator(name, upPeriod, downPeriod);
            RegisterIndicator(symbol, aroonOscillator, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, aroonOscillator, resolution);
            }

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
        public AverageTrueRange ATR(Symbol symbol, int period, MovingAverageType type = MovingAverageType.Simple, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ATR({period})", resolution);
            var averageTrueRange = new AverageTrueRange(name, period, type);
            RegisterIndicator(symbol, averageTrueRange, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, averageTrueRange, resolution);
            }

            return averageTrueRange;
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
        public BollingerBands BB(Symbol symbol, int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"BB({period},{k})", resolution);
            var bollingerBands = new BollingerBands(name, period, k, movingAverageType);
            RegisterIndicator(symbol, bollingerBands, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, bollingerBands, resolution);
            }

            return bollingerBands;
        }

        /// <summary>
        /// Creates a new Balance Of Power indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose Balance Of Power we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Balance Of Power indicator for the requested symbol.</returns>
        public BalanceOfPower BOP(Symbol symbol, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "BOP", resolution);
            var balanceOfPower = new BalanceOfPower(name);
            RegisterIndicator(symbol, balanceOfPower, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, balanceOfPower, resolution);
            }

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
        public CoppockCurve CC(Symbol symbol, int shortRocPeriod = 11, int longRocPeriod = 14, int lwmaPeriod = 10, Resolution? resolution = null,
                               Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"CC({shortRocPeriod},{longRocPeriod},{lwmaPeriod})", resolution);
            var coppockCurve = new CoppockCurve(name, shortRocPeriod, longRocPeriod, lwmaPeriod);
            RegisterIndicator(symbol, coppockCurve, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, coppockCurve, resolution);
            }

            return coppockCurve;
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
        public CommodityChannelIndex CCI(Symbol symbol, int period, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"CCI({period})", resolution);
            var commodityChannelIndex = new CommodityChannelIndex(name, period, movingAverageType);
            RegisterIndicator(symbol, commodityChannelIndex, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, commodityChannelIndex, resolution);
            }

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
        public ChaikinMoneyFlow CMF(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"CMF({period})", resolution);
            var chaikinMoneyFlow = new ChaikinMoneyFlow(name, period);
            RegisterIndicator(symbol, chaikinMoneyFlow, resolution, selector);
            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, chaikinMoneyFlow, resolution);
            }

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
        public ChandeMomentumOscillator CMO(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"CMO({period})", resolution);
            var chandeMomentumOscillator = new ChandeMomentumOscillator(name, period);
            RegisterIndicator(symbol, chandeMomentumOscillator, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, chandeMomentumOscillator, resolution);
            }

            return chandeMomentumOscillator;
        }
        
        ///<summary>
        /// Creates a new DeMarker Indicator (DEM), an oscillator-type indicator measuring changes in terms of an asset's
        /// High and Low tradebar values. 
        ///</summary>
        /// <param name="symbol">The symbol whose DEM we seek.</param>
        /// <param name="period">The period of the moving average implemented</param>
        /// <param name="movingAverageType">Specifies the type of moving average to be used</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The DeMarker indicator for the requested symbol.</returns>
        public DeMarkerIndicator DEM(Symbol symbol, int period, MovingAverageType type, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"DEM({period},{type})", resolution);
            var deMarkerIndicator = new DeMarkerIndicator(name, period, type);
            RegisterIndicator(symbol, deMarkerIndicator, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, deMarkerIndicator, resolution);
            }

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
        public DonchianChannel DCH(Symbol symbol, int upperPeriod, int lowerPeriod, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"DCH({upperPeriod},{lowerPeriod})", resolution);
            var donchianChannel = new DonchianChannel(name, upperPeriod, lowerPeriod);
            RegisterIndicator(symbol, donchianChannel, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, donchianChannel, resolution);
            }

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
        public DonchianChannel DCH(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            return DCH(symbol, period, period, resolution, selector);
        }

        /// <summary>
        /// Creates a new DoubleExponentialMovingAverage indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose DEMA we want</param>
        /// <param name="period">The period over which to compute the DEMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The DoubleExponentialMovingAverage indicator for the requested symbol over the specified period</returns>
        public DoubleExponentialMovingAverage DEMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"DEMA({period})", resolution);
            var doubleExponentialMovingAverage = new DoubleExponentialMovingAverage(name, period);
            RegisterIndicator(symbol, doubleExponentialMovingAverage, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, doubleExponentialMovingAverage, resolution);
            }

            return doubleExponentialMovingAverage;
        }

        /// <summary>
        /// Creates a new <see cref="DetrendedPriceOscillator"/> indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose DPO we want</param>
        /// <param name="period">The period over which to compute the DPO</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>A new registered DetrendedPriceOscillator indicator for the requested symbol over the specified period</returns>
        public DetrendedPriceOscillator DPO(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"DPO({period})", resolution);
            var detrendedPriceOscillator = new DetrendedPriceOscillator(name, period);
            RegisterIndicator(symbol, detrendedPriceOscillator, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, detrendedPriceOscillator, resolution);
            }

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
        public ExponentialMovingAverage EMA(Symbol symbol, int period, decimal smoothingFactor, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"EMA({period})", resolution);
            var exponentialMovingAverage = new ExponentialMovingAverage(name, period, smoothingFactor);
            RegisterIndicator(symbol, exponentialMovingAverage, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, exponentialMovingAverage, resolution);
            }

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
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The EaseOfMovementValue indicator for the given parameters</returns>
        public EaseOfMovementValue EMV(Symbol symbol, int period = 1, int scale = 10000, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"EMV({period}, {scale})", resolution);
            var easeOfMovementValue = new EaseOfMovementValue(name, period, scale);
            RegisterIndicator(symbol, easeOfMovementValue, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, easeOfMovementValue, resolution);
            }

            return easeOfMovementValue;
        }

        /// <summary>
        /// Creates a new FilteredIdentity indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The symbol whose values we want as an indicator</param>
        /// <param name="selector">Selects a value from the BaseData, if null defaults to the .Value property (x => x.Value)</param>
        /// <param name="filter">Filters the IBaseData send into the indicator, if null defaults to true (x => true) which means no filter</param>
        /// <param name="fieldName">The name of the field being selected</param>
        /// <returns>A new FilteredIdentity indicator for the specified symbol and selector</returns>
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
        /// <param name="selector">Selects a value from the BaseData, if null defaults to the .Value property (x => x.Value)</param>
        /// <param name="filter">Filters the IBaseData send into the indicator, if null defaults to true (x => true) which means no filter</param>
        /// <param name="fieldName">The name of the field being selected</param>
        /// <returns>A new FilteredIdentity indicator for the specified symbol and selector</returns>
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
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The FisherTransform for the given parameters</returns>
        public FisherTransform FISH(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"FISH({period})", resolution);
            var fisherTransform = new FisherTransform(name, period);
            RegisterIndicator(symbol, fisherTransform, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, fisherTransform, resolution);
            }

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
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The FRAMA for the given parameters</returns>
        public FractalAdaptiveMovingAverage FRAMA(Symbol symbol, int period, int longPeriod = 198, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"FRAMA({period},{longPeriod})", resolution);
            var fractalAdaptiveMovingAverage = new FractalAdaptiveMovingAverage(name, period, longPeriod);
            RegisterIndicator(symbol, fractalAdaptiveMovingAverage, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, fractalAdaptiveMovingAverage, resolution);
            }

            return fractalAdaptiveMovingAverage;
        }

        /// <summary>
        /// Creates a new Heikin-Ashi indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose Heikin-Ashi we want</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The Heikin-Ashi indicator for the requested symbol over the specified period</returns>
        public HeikinAshi HeikinAshi(Symbol symbol, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "HA", resolution);
            var heikinAshi = new HeikinAshi(name);
            RegisterIndicator(symbol, heikinAshi, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, heikinAshi, resolution);
            }

            return heikinAshi;
        }

        /// <summary>
        /// Creates a new HullMovingAverage indicator. The Hull moving average is a series of nested weighted moving averages, is fast and smooth.
        /// </summary>
        /// <param name="symbol">The symbol whose Hull moving average we want</param>
        /// <param name="period">The period over which to compute the Hull moving average</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns></returns>
        public HullMovingAverage HMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"HMA({period})", resolution);
            var hullMovingAverage = new HullMovingAverage(name, period);
            RegisterIndicator(symbol, hullMovingAverage, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, hullMovingAverage, resolution);
            }

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
        /// <returns>A new IchimokuKinkoHyo indicator with the specified periods and delays</returns>
        public IchimokuKinkoHyo ICHIMOKU(Symbol symbol, int tenkanPeriod, int kijunPeriod, int senkouAPeriod, int senkouBPeriod, int senkouADelayPeriod, int senkouBDelayPeriod, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(symbol, $"ICHIMOKU({tenkanPeriod},{kijunPeriod},{senkouAPeriod},{senkouBPeriod},{senkouADelayPeriod},{senkouBDelayPeriod})", resolution);
            var ichimokuKinkoHyo = new IchimokuKinkoHyo(name, tenkanPeriod, kijunPeriod, senkouAPeriod, senkouBPeriod, senkouADelayPeriod, senkouBDelayPeriod);
            RegisterIndicator(symbol, ichimokuKinkoHyo, resolution);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, ichimokuKinkoHyo, resolution);
            }

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
        public Identity Identity(Symbol symbol, TimeSpan resolution, Func<IBaseData, decimal> selector = null, string fieldName = null)
        {
            var name = Invariant($"{symbol}({fieldName ?? "close"},{resolution})");
            var identity = new Identity(name);
            RegisterIndicator(symbol, identity, ResolveConsolidator(symbol, resolution), selector);
            return identity;
        }

        /// <summary>
        /// Creates a new KaufmanAdaptiveMovingAverage indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose KAMA we want</param>
        /// <param name="period">The period of the Efficiency Ratio (ER) of KAMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The KaufmanAdaptiveMovingAverage indicator for the requested symbol over the specified period</returns>
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
        public KaufmanAdaptiveMovingAverage KAMA(Symbol symbol, int period, int fastEmaPeriod, int slowEmaPeriod, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"KAMA({period},{fastEmaPeriod},{slowEmaPeriod})", resolution);
            var kaufmanAdaptiveMovingAverage = new KaufmanAdaptiveMovingAverage(name, period, fastEmaPeriod, slowEmaPeriod);
            RegisterIndicator(symbol, kaufmanAdaptiveMovingAverage, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, kaufmanAdaptiveMovingAverage, resolution);
            }

            return kaufmanAdaptiveMovingAverage;
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
        public KeltnerChannels KCH(Symbol symbol, int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"KCH({period},{k})", resolution);
            var keltnerChannels = new KeltnerChannels(name, period, k, movingAverageType);
            RegisterIndicator(symbol, keltnerChannels, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, keltnerChannels, resolution);
            }

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
        public LogReturn LOGR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"LOGR({period})", resolution);
            var logReturn = new LogReturn(name, period);
            RegisterIndicator(symbol, logReturn, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, logReturn, resolution);
            }

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
        public LeastSquaresMovingAverage LSMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"LSMA({period})", resolution);
            var leastSquaresMovingAverage = new LeastSquaresMovingAverage(name, period);
            RegisterIndicator(symbol, leastSquaresMovingAverage, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, leastSquaresMovingAverage, resolution);
            }

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
        public LinearWeightedMovingAverage LWMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"LWMA({period})", resolution);
            var linearWeightedMovingAverage = new LinearWeightedMovingAverage(name, period);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, linearWeightedMovingAverage, resolution);
            }

            RegisterIndicator(symbol, linearWeightedMovingAverage, resolution, selector);
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
        public MovingAverageConvergenceDivergence MACD(Symbol symbol, int fastPeriod, int slowPeriod, int signalPeriod, MovingAverageType type = MovingAverageType.Exponential, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MACD({fastPeriod},{slowPeriod},{signalPeriod})", resolution);
            var movingAverageConvergenceDivergence = new MovingAverageConvergenceDivergence(name, fastPeriod, slowPeriod, signalPeriod, type);
            RegisterIndicator(symbol, movingAverageConvergenceDivergence, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, movingAverageConvergenceDivergence, resolution);
            }

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
        public MeanAbsoluteDeviation MAD(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MAD({period})", resolution);
            var meanAbsoluteDeviation = new MeanAbsoluteDeviation(name, period);
            RegisterIndicator(symbol, meanAbsoluteDeviation, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, meanAbsoluteDeviation, resolution);
            }

            return meanAbsoluteDeviation;
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
                WarmUpIndicator(symbol, maximum, resolution);
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
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The MoneyFlowIndex indicator for the requested symbol over the specified period</returns>
        public MoneyFlowIndex MFI(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MFI({period})", resolution);
            var moneyFlowIndex = new MoneyFlowIndex(name, period);
            RegisterIndicator(symbol, moneyFlowIndex, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, moneyFlowIndex, resolution);
            }

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
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The Mass Index indicator for the requested symbol over the specified period</returns>
        public MassIndex MASS(Symbol symbol, int emaPeriod = 9, int sumPeriod = 25, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MASS({emaPeriod},{sumPeriod})", resolution);
            var massIndex = new MassIndex(name, emaPeriod, sumPeriod);
            RegisterIndicator(symbol, massIndex, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, massIndex, resolution);
            }

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
        public MidPoint MIDPOINT(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MIDPOINT({period})", resolution);
            var midPoint = new MidPoint(name, period);
            RegisterIndicator(symbol, midPoint, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, midPoint, resolution);
            }

            return midPoint;
        }

        /// <summary>
        /// Creates a new MidPrice indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose MIDPRICE we want</param>
        /// <param name="period">The period over which to compute the MIDPRICE</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The MidPrice indicator for the requested symbol over the specified period</returns>
        public MidPrice MIDPRICE(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MIDPRICE({period})", resolution);
            var midPrice = new MidPrice(name, period);
            RegisterIndicator(symbol, midPrice, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, midPrice, resolution);
            }

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
                WarmUpIndicator(symbol, minimum, resolution);
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
        public Momentum MOM(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MOM({period})", resolution);
            var momentum = new Momentum(name, period);
            RegisterIndicator(symbol, momentum, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, momentum, resolution);
            }

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
        public MomersionIndicator MOMERSION(Symbol symbol, int? minPeriod, int fullPeriod, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MOMERSION({minPeriod},{fullPeriod})", resolution);
            var momersion = new MomersionIndicator(name, minPeriod, fullPeriod);
            RegisterIndicator(symbol, momersion, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, momersion, resolution);
            }

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
        public MomentumPercent MOMP(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"MOMP({period})", resolution);
            var momentumPercent = new MomentumPercent(name, period);
            RegisterIndicator(symbol, momentumPercent, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, momentumPercent, resolution);
            }

            return momentumPercent;
        }

        /// <summary>
        /// Creates a new NormalizedAverageTrueRange indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose NATR we want</param>
        /// <param name="period">The period over which to compute the NATR</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The NormalizedAverageTrueRange indicator for the requested symbol over the specified period</returns>
        public NormalizedAverageTrueRange NATR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"NATR({period})", resolution);
            var normalizedAverageTrueRange = new NormalizedAverageTrueRange(name, period);
            RegisterIndicator(symbol, normalizedAverageTrueRange, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, normalizedAverageTrueRange, resolution);
            }

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
        public OnBalanceVolume OBV(Symbol symbol, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "OBV", resolution);
            var onBalanceVolume = new OnBalanceVolume(name);
            RegisterIndicator(symbol, onBalanceVolume, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, onBalanceVolume, resolution);
            }

            return onBalanceVolume;
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
        public PercentagePriceOscillator PPO(Symbol symbol, int fastPeriod, int slowPeriod, MovingAverageType movingAverageType, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"PPO({fastPeriod},{slowPeriod})", resolution);
            var percentagePriceOscillator = new PercentagePriceOscillator(name, fastPeriod, slowPeriod, movingAverageType);
            RegisterIndicator(symbol, percentagePriceOscillator, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, percentagePriceOscillator, resolution);
            }

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
        public ParabolicStopAndReverse PSAR(Symbol symbol, decimal afStart = 0.02m, decimal afIncrement = 0.02m, decimal afMax = 0.2m, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"PSAR({afStart},{afIncrement},{afMax})", resolution);
            var parabolicStopAndReverse = new ParabolicStopAndReverse(name, afStart, afIncrement, afMax);
            RegisterIndicator(symbol, parabolicStopAndReverse, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, parabolicStopAndReverse, resolution);
            }

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
        public RegressionChannel RC(Symbol symbol, int period, decimal k, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"RC({period},{k})", resolution);
            var regressionChannel = new RegressionChannel(name, period, k);
            RegisterIndicator(symbol, regressionChannel, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, regressionChannel, resolution);
            }

            return regressionChannel;
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
        public RateOfChange ROC(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ROC({period})", resolution);
            var rateOfChange = new RateOfChange(name, period);
            RegisterIndicator(symbol, rateOfChange, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, rateOfChange, resolution);
            }

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
        public RateOfChangePercent ROCP(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ROCP({period})", resolution);
            var rateOfChangePercent = new RateOfChangePercent(name, period);
            RegisterIndicator(symbol, rateOfChangePercent, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, rateOfChangePercent, resolution);
            }

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
        public RateOfChangeRatio ROCR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ROCR({period})", resolution);
            var rateOfChangeRatio = new RateOfChangeRatio(name, period);
            RegisterIndicator(symbol, rateOfChangeRatio, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, rateOfChangeRatio, resolution);
            }

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
        public RelativeStrengthIndex RSI(Symbol symbol, int period, MovingAverageType movingAverageType = MovingAverageType.Wilders, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"RSI({period},{movingAverageType})", resolution);
            var relativeStrengthIndex = new RelativeStrengthIndex(name, period, movingAverageType);
            RegisterIndicator(symbol, relativeStrengthIndex, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, relativeStrengthIndex, resolution);
            }

            return relativeStrengthIndex;
        }
        /// <summary>
        /// Creates a new RelativeVigorIndex indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose RVI we want</param>
        /// <param name="period">The period over which to compute the RVI</param>
        /// <param name="movingAverageType">The type of moving average to use</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The RelativeVigorIndex indicator for the requested symbol over the specified period</returns>
        public RelativeVigorIndex RVI(Symbol symbol, int period, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"RVI({period},{movingAverageType})", resolution);
            var relativeVigorIndex = new RelativeVigorIndex(name, period, movingAverageType);
            RegisterIndicator(symbol, relativeVigorIndex, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, relativeVigorIndex, resolution);
            }

            return relativeVigorIndex;
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
        public SimpleMovingAverage SMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"SMA({period})", resolution);
            var simpleMovingAverage = new SimpleMovingAverage(name, period);
            RegisterIndicator(symbol, simpleMovingAverage, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, simpleMovingAverage, resolution);
            }

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
        public SchaffTrendCycle STC(Symbol symbol, int cyclePeriod, int fastPeriod, int slowPeriod, MovingAverageType movingAverageType = MovingAverageType.Exponential, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"STC({cyclePeriod},{fastPeriod},{slowPeriod})", resolution);
            var schaffTrendCycle = new SchaffTrendCycle(name, cyclePeriod, fastPeriod, slowPeriod, movingAverageType);
            RegisterIndicator(symbol, schaffTrendCycle, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, schaffTrendCycle, resolution);
            }

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
        public StandardDeviation STD(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"STD({period})", resolution);
            var standardDeviation = new StandardDeviation(name, period);
            RegisterIndicator(symbol, standardDeviation, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, standardDeviation, resolution);
            }

            return standardDeviation;
        }
        /// <summary>
        /// Creates a new Stochastic indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose stochastic we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="period">The period of the stochastic. Normally 14</param>
        /// <param name="kPeriod">The sum period of the stochastic. Normally 14</param>
        /// <param name="dPeriod">The sum period of the stochastic. Normally 3</param>
        /// <returns>Stochastic indicator for the requested symbol.</returns>
        public Stochastic STO(Symbol symbol, int period, int kPeriod, int dPeriod, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(symbol, $"STO({period},{kPeriod},{dPeriod})", resolution);
            var stochastic = new Stochastic(name, period, kPeriod, dPeriod);
            RegisterIndicator(symbol, stochastic, resolution);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, stochastic, resolution);
            }

            return stochastic;
        }

        /// <summary>
        /// Overload short hand to create a new Stochastic indicator; defaulting to the 3 period for dStoch
        /// </summary>
        /// <param name="symbol">The symbol whose stochastic we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="period">The period of the stochastic. Normally 14</param>
        /// <returns>Stochastic indicator for the requested symbol.</returns>
        public Stochastic STO(Symbol symbol, int period, Resolution? resolution = null)
        {
            return STO(symbol, period, period, 3, resolution);
        }

        /// <summary>
        /// Creates a new Sum indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose Sum we want</param>
        /// <param name="period">The period over which to compute the Sum</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The Sum indicator for the requested symbol over the specified period</returns>
        public Sum SUM(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"SUM({period})", resolution);
            var sum = new Sum(name, period);
            RegisterIndicator(symbol, sum, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, sum, resolution);
            }

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
        public SwissArmyKnife SWISS(Symbol symbol, int period, double delta, SwissArmyKnifeTool tool, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"SWISS({period},{delta},{tool})", resolution);
            var swissArmyKnife = new SwissArmyKnife(name, period, delta, tool);
            RegisterIndicator(symbol, swissArmyKnife, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, swissArmyKnife, resolution);
            }

            return swissArmyKnife;
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
        public T3MovingAverage T3(Symbol symbol, int period, decimal volumeFactor = 0.7m, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"T3({period},{volumeFactor})", resolution);
            var t3MovingAverage = new T3MovingAverage(name, period, volumeFactor);
            RegisterIndicator(symbol, t3MovingAverage, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, t3MovingAverage, resolution);
            }

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
        public TripleExponentialMovingAverage TEMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"TEMA({period})", resolution);
            var tripleExponentialMovingAverage = new TripleExponentialMovingAverage(name, period);
            RegisterIndicator(symbol, tripleExponentialMovingAverage, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, tripleExponentialMovingAverage, resolution);
            }

            return tripleExponentialMovingAverage;
        }

        /// <summary>
        /// Creates a new TrueRange indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose TR we want</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The TrueRange indicator for the requested symbol.</returns>
        public TrueRange TR(Symbol symbol, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "TR", resolution);
            var trueRange = new TrueRange(name);
            RegisterIndicator(symbol, trueRange, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, trueRange, resolution);
            }

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
        public TriangularMovingAverage TRIMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"TRIMA({period})", resolution);
            var triangularMovingAverage = new TriangularMovingAverage(name, period);
            RegisterIndicator(symbol, triangularMovingAverage, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, triangularMovingAverage, resolution);
            }

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
        public Trix TRIX(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"TRIX({period})", resolution);
            var trix = new Trix(name, period);
            RegisterIndicator(symbol, trix, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, trix, resolution);
            }

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
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The UltimateOscillator indicator for the requested symbol over the specified period</returns>
        public UltimateOscillator ULTOSC(Symbol symbol, int period1, int period2, int period3, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"ULTOSC({period1},{period2},{period3})", resolution);
            var ultimateOscillator = new UltimateOscillator(name, period1, period2, period3);
            RegisterIndicator(symbol, ultimateOscillator, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, ultimateOscillator, resolution);
            }

            return ultimateOscillator;
        }

        /// <summary>
        /// Creates a new Variance indicator. This will return the population variance of samples over the specified period.
        /// </summary>
        /// <param name="symbol">The symbol whose VAR we want</param>
        /// <param name="period">The period over which to compute the VAR</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The Variance indicator for the requested symbol over the specified period</returns>
        public Variance VAR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"VAR({period})", resolution);
            var variance = new Variance(name, period);
            RegisterIndicator(symbol, variance, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, variance, resolution);
            }

            return variance;
        }

        /// <summary>
        /// Creates an VolumeWeightedAveragePrice (VWAP) indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose VWAP we want</param>
        /// <param name="period">The period of the VWAP</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The VolumeWeightedAveragePrice for the given parameters</returns>
        public VolumeWeightedAveragePriceIndicator VWAP(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"VWAP({period})", resolution);
            var volumeWeightedAveragePriceIndicator = new VolumeWeightedAveragePriceIndicator(name, period);
            RegisterIndicator(symbol, volumeWeightedAveragePriceIndicator, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, volumeWeightedAveragePriceIndicator, resolution);
            }

            return volumeWeightedAveragePriceIndicator;
        }

        /// <summary>
        /// Creates the canonical VWAP indicator that resets each day. The indicator will be automatically
        /// updated on the security's configured resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose VWAP we want</param>
        /// <returns>The IntradayVWAP for the specified symbol</returns>
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
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The Williams %R indicator for the requested symbol over the specified period</returns>
        public WilliamsPercentR WILR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"WILR({period})", resolution);
            var williamsPercentR = new WilliamsPercentR(name, period);
            RegisterIndicator(symbol, williamsPercentR, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, williamsPercentR, resolution);
            }

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
        public WilderMovingAverage WWMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, $"WWMA({period})", resolution);
            var wilderMovingAverage = new WilderMovingAverage(name, period);
            RegisterIndicator(symbol, wilderMovingAverage, resolution, selector);

            if (EnableAutomaticIndicatorWarmUp)
            {
                WarmUpIndicator(symbol, wilderMovingAverage, resolution);
            }

            return wilderMovingAverage;
        }

        /// <summary>
        /// Creates a new Arms Index indicator
        /// </summary>
        /// <param name="symbols">The symbols whose Arms Index we want</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>The Arms Index indicator for the requested symbol over the specified period</returns>
        public ArmsIndex TRIN(IEnumerable<Symbol> symbols, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(QuantConnect.Symbol.None, "TRIN", resolution ?? GetSubscription(symbols.First()).Resolution);
            var trin = new ArmsIndex(name);
            foreach (var symbol in symbols)
            {
                trin.AddStock(symbol);
                RegisterIndicator(symbol, trin, resolution);

                if (EnableAutomaticIndicatorWarmUp)
                {
                    WarmUpIndicator(symbol, trin, resolution);
                }
            }

            return trin;
        }

        /// <summary>
        /// Creates a new Advance/Decline Ratio indicator
        /// </summary>
        /// <param name="symbols">The symbols whose A/D Ratio we want</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>The Advance/Decline Ratio indicator for the requested symbol over the specified period</returns>
        public AdvanceDeclineRatio ADR(IEnumerable<Symbol> symbols, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(QuantConnect.Symbol.None, "A/D Ratio", resolution ?? GetSubscription(symbols.First()).Resolution);
            var adr = new AdvanceDeclineRatio(name);
            foreach (var symbol in symbols)
            {
                adr.AddStock(symbol);
                RegisterIndicator(symbol, adr, resolution);

                if (EnableAutomaticIndicatorWarmUp)
                {
                    WarmUpIndicator(symbol, adr, resolution);
                }
            }

            return adr;
        }

        /// <summary>
        /// Creates a new Advance/Decline Volume Ratio indicator
        /// </summary>
        /// <param name="symbols">The symbol whose A/D Volume Rate we want</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>The Advance/Decline Volume Ratio indicator for the requested symbol over the specified period</returns>
        public AdvanceDeclineVolumeRatio ADVR(IEnumerable<Symbol> symbols, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(QuantConnect.Symbol.None, "A/D Volume Rate", resolution ?? GetSubscription(symbols.First()).Resolution);
            var advr = new AdvanceDeclineVolumeRatio(name);
            foreach (var symbol in symbols)
            {
                advr.AddStock(symbol);
                RegisterIndicator(symbol, advr, resolution);

                if (EnableAutomaticIndicatorWarmUp)
                {
                    WarmUpIndicator(symbol, advr, resolution);
                }
            }

            return advr;
        }

        /// <summary>
        /// Creates a new name for an indicator created with the convenience functions (SMA, EMA, ect...)
        /// </summary>
        /// <param name="symbol">The symbol this indicator is registered to</param>
        /// <param name="type">The indicator type, for example, 'SMA(5)'</param>
        /// <param name="resolution">The resolution requested</param>
        /// <returns>A unique for the given parameters</returns>
        public string CreateIndicatorName(Symbol symbol, FormattableString type, Resolution? resolution)
        {
            return CreateIndicatorName(symbol, Invariant(type), resolution);
        }
        public string CreateIndicatorName(Symbol symbol, string type, Resolution? resolution)
        {
            if (!resolution.HasValue)
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

            if (symbol != QuantConnect.Symbol.None && symbol != QuantConnect.Symbol.Empty)
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
                    .OrderBy(x => x.TickType)
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
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IndicatorDataPoint> indicator, IDataConsolidator consolidator, Func<IBaseData, decimal> selector = null)
        {
            // default our selector to the Value property on BaseData
            selector = selector ?? (x => x.Value);

            // register the consolidator for automatic updates via SubscriptionManager
            SubscriptionManager.AddConsolidator(symbol, consolidator);

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
        public void RegisterIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, IDataConsolidator consolidator, Func<IBaseData, T> selector = null)
            where T : IBaseData
        {
            // assign default using cast
            var selectorToUse = selector ?? (x => (T)x);

            // register the consolidator for automatic updates via SubscriptionManager
            SubscriptionManager.AddConsolidator(symbol, consolidator);

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
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbol">The symbol whose indicator we want</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The given indicator</returns>
        public IndicatorBase<IndicatorDataPoint> WarmUpIndicator(Symbol symbol, IndicatorBase<IndicatorDataPoint> indicator, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            resolution = GetResolution(symbol, resolution);
            var period = resolution.Value.ToTimeSpan();
            return WarmUpIndicator(symbol, indicator, period, selector);
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbol">The symbol whose indicator we want</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="period">The necessary period to warm up the indicator</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The given indicator</returns>
        public IndicatorBase<IndicatorDataPoint> WarmUpIndicator(Symbol symbol, IndicatorBase<IndicatorDataPoint> indicator, TimeSpan period, Func<IBaseData, decimal> selector = null)
        {
            var history = GetIndicatorWarmUpHistory(symbol, indicator, period);
            if (history == Enumerable.Empty<Slice>()) return indicator;

            // assign default using cast
            selector = selector ?? (x => x.Value);

            Action<IBaseData> onDataConsolidated = bar =>
            {
                var input = new IndicatorDataPoint(bar.Symbol, bar.EndTime, selector(bar));
                indicator.Update(input);
            };

            WarmUpIndicatorImpl(symbol, period, onDataConsolidated, history);
            return indicator;
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbol">The symbol whose indicator we want</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The given indicator</returns>
        public IndicatorBase<T> WarmUpIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, Resolution? resolution = null, Func<IBaseData, T> selector = null)
            where T : class, IBaseData
        {
            resolution = GetResolution(symbol, resolution);
            var period = resolution.Value.ToTimeSpan();
            return WarmUpIndicator(symbol, indicator, period, selector);
        }

        /// <summary>
        /// Warms up a given indicator with historical data
        /// </summary>
        /// <param name="symbol">The symbol whose indicator we want</param>
        /// <param name="indicator">The indicator we want to warm up</param>
        /// <param name="period">The necessary period to warm up the indicator</param>
        /// <param name="selector">Selects a value from the BaseData send into the indicator, if null defaults to a cast (x => (T)x)</param>
        /// <returns>The given indicator</returns>
        public IndicatorBase<T> WarmUpIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, TimeSpan period, Func<IBaseData, T> selector = null)
            where T : class, IBaseData
        {
            var history = GetIndicatorWarmUpHistory(symbol, indicator, period);
            if (history == Enumerable.Empty<Slice>()) return indicator;

            // assign default using cast
            selector = selector ?? (x => (T)x);

            // we expect T type as input
            Action<T> onDataConsolidated = bar =>
            {
                indicator.Update(selector(bar));
            };

            WarmUpIndicatorImpl(symbol, period, onDataConsolidated, history);
            return indicator;
        }

        private IEnumerable<Slice> GetIndicatorWarmUpHistory(Symbol symbol, IIndicator indicator, TimeSpan timeSpan)
        {
            var periods = (indicator as IIndicatorWarmUpPeriodProvider)?.WarmUpPeriod;

            if (periods.HasValue)
            {
                var resolution = timeSpan.ToHigherResolutionEquivalent(false);
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
                Debug($"Warning: the 'WarmUpIndicator' feature only works with indicators which inherit from '{nameof(IIndicatorWarmUpPeriodProvider)}' and define a warm up period." +
                      $" The provided indicator of type '{indicator.GetType().Name}' will not be warmed up.");
                _isEmitWarmupInsightWarningSent = true;
            }

            return Enumerable.Empty<Slice>();
        }

        private void WarmUpIndicatorImpl<T>(Symbol symbol, TimeSpan period, Action<T> handler, IEnumerable<Slice> history)
            where T : class, IBaseData
        {
            IDataConsolidator consolidator;
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
                        symbol.IsCanonical()).OrderBy(tuple => tuple.Item2).First();

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
                var data = slice.Get(consolidatorInputType);
                if (data.ContainsKey(symbol))
                {
                    lastBar = (IBaseData)data[symbol];
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
        public IDataConsolidator ResolveConsolidator(Symbol symbol, TimeSpan? timeSpan, Type dataType = null)
        {
            var tickType = dataType != null ? LeanData.GetCommonTickTypeForCommonDataTypes(dataType, symbol.SecurityType) : (TickType?)null;
            var subscription = GetSubscription(symbol, tickType);

            // if not specified, default to the subscription resolution
            if (!timeSpan.HasValue)
            {
                timeSpan = subscription.Resolution.ToTimeSpan();
            }

            // verify this consolidator will give reasonable results, if someone asks for second consolidation but we have minute
            // data we won't be able to do anything good, we'll call it second, but it would really just be minute!
            if (timeSpan.Value < subscription.Resolution.ToTimeSpan())
            {
                throw new ArgumentException($"Unable to create {symbol} consolidator because {symbol} is registered for " +
                    Invariant($"{subscription.Resolution.ToStringInvariant()} data. Consolidators require higher resolution data to produce lower resolution data.")
                );
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
        public static IDataConsolidator CreateConsolidator(TimeSpan period, Type consolidatorInputType, TickType? tickType = null)
        {
            // if our type can be used as a trade bar, then let's just make one of those
            // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to TradeBar
            if (typeof(TradeBar).IsAssignableFrom(consolidatorInputType))
            {
                return new TradeBarConsolidator(period);
            }

            // if our type can be used as a quote bar, then let's just make one of those
            // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to QuoteBar
            if (typeof(QuoteBar).IsAssignableFrom(consolidatorInputType))
            {
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
                return new DynamicDataConsolidator(period);
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
        public IDataConsolidator Consolidate<T>(Symbol symbol, TimeSpan period, TickType? tickType, Action<T> handler)
            where T : class, IBaseData
        {
            // resolve consolidator input subscription
            var subscription = GetSubscription(symbol, tickType);

            // create requested consolidator
            var consolidator = CreateConsolidator(period, subscription.Type, subscription.TickType);

            AddConsolidator(symbol, consolidator, handler);
            return consolidator;
        }

        /// <summary>
        /// Registers the <paramref name="handler"/> to receive consolidated data for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol who's data is to be consolidated</param>
        /// <param name="calendar">The consolidation calendar</param>
        /// <param name="handler">Data handler receives new consolidated data when generated</param>
        /// <returns>A new consolidator matching the requested parameters with the handler already registered</returns>
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
        private IDataConsolidator Consolidate<T>(Symbol symbol, Func<DateTime, CalendarInfo> calendar, TickType? tickType, Action<T> handler)
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
        private void AddConsolidator<T>(Symbol symbol, IDataConsolidator consolidator, Action<T> handler)
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
            SubscriptionManager.AddConsolidator(symbol, consolidator);
        }

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
    }
}
