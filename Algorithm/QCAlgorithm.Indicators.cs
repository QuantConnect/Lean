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
using System.Linq;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
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
            var name = CreateIndicatorName(symbol, string.Format("ABANDS_{0}_{1}", period, width), resolution);
            var abands = new AccelerationBands(name, period, width, movingAverageType);
            RegisterIndicator(symbol, abands, resolution, selector);
            return abands;
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
            var ad = new AccumulationDistribution(name);
            RegisterIndicator(symbol, ad, resolution, selector);
            return ad;
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
            var name = CreateIndicatorName(symbol, string.Format("ADOSC({0},{1})", fastPeriod, slowPeriod), resolution);
            var adOsc = new AccumulationDistributionOscillator(name, fastPeriod, slowPeriod);
            RegisterIndicator(symbol, adOsc, resolution, selector);
            return adOsc;
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
            var name = CreateIndicatorName(symbol, "ADX", resolution);
            var averageDirectionalIndex = new AverageDirectionalIndex(name, period);
            RegisterIndicator(symbol, averageDirectionalIndex, resolution, selector);
            return averageDirectionalIndex;
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
            var name = CreateIndicatorName(symbol, "ADXR" + period, resolution);
            var adxr = new AverageDirectionalMovementIndexRating(name, period);
            RegisterIndicator(symbol, adxr, resolution, selector);
            return adxr;
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
            var name = CreateIndicatorName(symbol, string.Format("ALMA_{0}_{1}_{2}", period, sigma, offset), resolution);
            var alma = new ArnaudLegouxMovingAverage(name, period, sigma, offset);
            RegisterIndicator(symbol, alma, resolution, selector);
            return alma;
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
            var name = CreateIndicatorName(symbol, string.Format("APO({0},{1})", fastPeriod, slowPeriod), resolution);
            var apo = new AbsolutePriceOscillator(name, fastPeriod, slowPeriod, movingAverageType);
            RegisterIndicator(symbol, apo, resolution, selector);
            return apo;
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
            var name = CreateIndicatorName(symbol, string.Format("AROON({0},{1})", upPeriod, downPeriod), resolution);
            var aroon = new AroonOscillator(name, upPeriod, downPeriod);
            RegisterIndicator(symbol, aroon, resolution, selector);
            return aroon;
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
            string name = CreateIndicatorName(symbol, "ATR" + period, resolution);
            var atr = new AverageTrueRange(name, period, type);
            RegisterIndicator(symbol, atr, resolution, selector);
            return atr;
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
        /// <returns>A BollingerBands configured with the specied period</returns>
        public BollingerBands BB(Symbol symbol, int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, string.Format("BB({0},{1})", period, k), resolution);
            var bb = new BollingerBands(name, period, k, movingAverageType);
            RegisterIndicator(symbol, bb, resolution, selector);
            return bb;
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
            var bop = new BalanceOfPower(name);
            RegisterIndicator(symbol, bop, resolution, selector);
            return bop;
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
            var name = CreateIndicatorName(symbol, "CC", resolution);
            var cc = new CoppockCurve(name, shortRocPeriod, longRocPeriod, lwmaPeriod);
            RegisterIndicator(symbol, cc, resolution, selector);
            return cc;
        }

        /// <summary>
        /// Creates a new CommodityChannelIndex indicator. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose CCI we want</param>
        /// <param name="period">The period over which to compute the CCI</param>
        /// <param name="movingAverageType">The type of moving average to use in computing the typical price averge</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The CommodityChannelIndex indicator for the requested symbol over the specified period</returns>
        public CommodityChannelIndex CCI(Symbol symbol, int period, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "CCI" + period, resolution);
            var cci = new CommodityChannelIndex(name, period, movingAverageType);
            RegisterIndicator(symbol, cci, resolution, selector);
            return cci;
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
            var name = CreateIndicatorName(symbol, "CMO" + period, resolution);
            var cmo = new ChandeMomentumOscillator(name, period);
            RegisterIndicator(symbol, cmo, resolution, selector);
            return cmo;
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
            var name = CreateIndicatorName(symbol, "DCH", resolution);
            var donchianChannel = new DonchianChannel(name, upperPeriod, lowerPeriod);
            RegisterIndicator(symbol, donchianChannel, resolution, selector);
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
            var name = CreateIndicatorName(symbol, "DEMA" + period, resolution);
            var dema = new DoubleExponentialMovingAverage(name, period);
            RegisterIndicator(symbol, dema, resolution, selector);
            return dema;
        }

        /// <summary>
        /// Creates a new <see cref="DetrendedPriceOscillator"/> indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose DPO we want</param>
        /// <param name="period">The period over which to compute the DPO</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>A new registered DetrendedPriceOscillator indicator for the requested symbol over the specified period</returns>
        public DetrendedPriceOscillator DPO(Symbol symbol, int period, Resolution? resolution = null,
                                            Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, "DPO" + period, resolution);
            var dpo = new DetrendedPriceOscillator(name, period);
            RegisterIndicator(symbol, dpo, resolution, selector);
            return dpo;
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
            string name = CreateIndicatorName(symbol, "EMA" + period, resolution);
            var ema = new ExponentialMovingAverage(name, period);
            RegisterIndicator(symbol, ema, resolution, selector);
            return ema;
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
            string name = CreateIndicatorName(symbol, fieldName ?? "close", resolution);
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
            string name = string.Format("{0}({1}_{2})", symbol, fieldName ?? "close", resolution);
            var filteredIdentity = new FilteredIdentity(name, filter);
            RegisterIndicator<IBaseData>(symbol, filteredIdentity, ResolveConsolidator(symbol, resolution), selector);
            return filteredIdentity;
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
            var name = CreateIndicatorName(symbol, "FRAMA" + period, resolution);
            var frama = new FractalAdaptiveMovingAverage(name, period, longPeriod);
            RegisterIndicator(symbol, frama, resolution, selector);
            return frama;
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
            var ha = new HeikinAshi(name);
            RegisterIndicator(symbol, ha, resolution, selector);
            return ha;
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
            var name = CreateIndicatorName(symbol, "HMA" + period, resolution);
            var hma = new HullMovingAverage(name, period);
            RegisterIndicator(symbol, hma, resolution, selector);
            return hma;
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
            var name = CreateIndicatorName(symbol, string.Format("ICHIMOKU({0},{1})", tenkanPeriod, kijunPeriod), resolution);
            var ichimoku = new IchimokuKinkoHyo(name, tenkanPeriod, kijunPeriod, senkouAPeriod, senkouBPeriod, senkouADelayPeriod, senkouBDelayPeriod);
            RegisterIndicator(symbol, ichimoku, resolution);
            return ichimoku;
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
            string name = CreateIndicatorName(symbol, fieldName ?? "close", resolution);
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
            string name = string.Format("{0}({1}_{2})", symbol, fieldName ?? "close", resolution);
            var identity = new Identity(name);
            RegisterIndicator(symbol, identity, ResolveConsolidator(symbol, resolution), selector);
            return identity;
        }

        /// <summary>
        /// Creates a new KaufmanAdaptiveMovingAverage indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose KAMA we want</param>
        /// <param name="period">The period over which to compute the KAMA</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The KaufmanAdaptiveMovingAverage indicator for the requested symbol over the specified period</returns>
        public KaufmanAdaptiveMovingAverage KAMA(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, "KAMA" + period, resolution);
            var kama = new KaufmanAdaptiveMovingAverage(name, period);
            RegisterIndicator(symbol, kama, resolution, selector);
            return kama;
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
            var name = CreateIndicatorName(symbol, "KCH", resolution);
            var keltnerChannels = new KeltnerChannels(name, period, k, movingAverageType);
            RegisterIndicator(symbol, keltnerChannels, resolution, selector);
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
            string name = CreateIndicatorName(symbol, "LOGR", resolution);
            var logr = new LogReturn(name, period);
            RegisterIndicator(symbol, logr, resolution, selector);
            return logr;
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
            var name = CreateIndicatorName(symbol, "LSMA" + period, resolution);
            var lsma = new LeastSquaresMovingAverage(name, period);
            RegisterIndicator(symbol, lsma, resolution, selector);
            return lsma;
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
            string name = CreateIndicatorName(symbol, "LWMA" + period, resolution);
            var lwma = new LinearWeightedMovingAverage(name, period);
            RegisterIndicator(symbol, lwma, resolution, selector);
            return lwma;
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
            var name = CreateIndicatorName(symbol, string.Format("MACD({0},{1})", fastPeriod, slowPeriod), resolution);
            var macd = new MovingAverageConvergenceDivergence(name, fastPeriod, slowPeriod, signalPeriod, type);
            RegisterIndicator(symbol, macd, resolution, selector);
            return macd;
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
            var name = CreateIndicatorName(symbol, "MAD" + period, resolution);
            var mad = new MeanAbsoluteDeviation(name, period);
            RegisterIndicator(symbol, mad, resolution, selector);
            return mad;
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
            var name = CreateIndicatorName(symbol, "MAX" + period, resolution);
            var max = new Maximum(name, period);

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

            RegisterIndicator(symbol, max, ResolveConsolidator(symbol, resolution), selector);
            return max;
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
            var name = CreateIndicatorName(symbol, "MFI" + period, resolution);
            var mfi = new MoneyFlowIndex(name, period);
            RegisterIndicator(symbol, mfi, resolution, selector);
            return mfi;
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
            var name = CreateIndicatorName(symbol, "MII" + emaPeriod + sumPeriod, resolution);
            var mi = new MassIndex(name, emaPeriod, sumPeriod);
            RegisterIndicator(symbol, mi, resolution, selector);
            return mi;
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
            var name = CreateIndicatorName(symbol, "MIDPOINT" + period, resolution);
            var midpoint = new MidPoint(name, period);
            RegisterIndicator(symbol, midpoint, resolution, selector);
            return midpoint;
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
            var name = CreateIndicatorName(symbol, "MIDPRICE" + period, resolution);
            var midprice = new MidPrice(name, period);
            RegisterIndicator(symbol, midprice, resolution, selector);
            return midprice;
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
            var name = CreateIndicatorName(symbol, "MIN" + period, resolution);
            var min = new Minimum(name, period);

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

            RegisterIndicator(symbol, min, ResolveConsolidator(symbol, resolution), selector);
            return min;
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
            string name = CreateIndicatorName(symbol, "MOM" + period, resolution);
            var momentum = new Momentum(name, period);
            RegisterIndicator(symbol, momentum, resolution, selector);
            return momentum;
        }

        /// <summary>
        /// Creates a new Momersion indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose Momersion we want</param>
        /// <param name="minPeriod">The minimum period over which to compute the Momersion</param>
        /// <param name="fullPeriod">The full period over which to compute the Momersion</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The Momersion indicator for the requested symbol over the specified period</returns>
        public MomersionIndicator MOMERSION(Symbol symbol, int minPeriod, int fullPeriod, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, string.Format("MOMERSION({0},{1})", minPeriod, fullPeriod), resolution);
            var momersion = new MomersionIndicator(name, minPeriod, fullPeriod);
            RegisterIndicator(symbol, momersion, resolution, selector);
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
            string name = CreateIndicatorName(symbol, "MOMP" + period, resolution);
            var momentum = new MomentumPercent(name, period);
            RegisterIndicator(symbol, momentum, resolution, selector);
            return momentum;
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
            var name = CreateIndicatorName(symbol, "NATR" + period, resolution);
            var natr = new NormalizedAverageTrueRange(name, period);
            RegisterIndicator(symbol, natr, resolution, selector);
            return natr;
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
            var name = CreateIndicatorName(symbol, string.Format("PPO({0},{1})", fastPeriod, slowPeriod), resolution);
            var ppo = new PercentagePriceOscillator(name, fastPeriod, slowPeriod, movingAverageType);
            RegisterIndicator(symbol, ppo, resolution, selector);
            return ppo;
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
            var name = CreateIndicatorName(symbol, string.Format("PSAR({0},{1},{2})", afStart, afIncrement, afMax), resolution);
            var psar = new ParabolicStopAndReverse(name, afStart, afIncrement, afMax);
            RegisterIndicator(symbol, psar, resolution, selector);
            return psar;
        }

        /// <summary>
        /// Creates a new RegressionChannel indicator which will compute the LinearRegression, UpperChannel and LowerChannel lines, the intercept and slope
        /// </summary>
        /// <param name="symbol">The symbol whose RegressionChannel we seek</param>
        /// <param name="period">The period of the standard deviation and least square moving average (linear regression line)</param>
        /// <param name="k">The number of standard deviations specifying the distance between the linear regression and upper or lower channel lines</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>A Regression Channel configured with the specied period and number of standard deviation</returns>
        public RegressionChannel RC(Symbol symbol, int period, decimal k, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, string.Format("RC({0},{1})", period, k), resolution);
            var rc = new RegressionChannel(name, period, k);
            RegisterIndicator(symbol, rc, resolution, selector);
            return rc;
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
            string name = CreateIndicatorName(symbol, "ROC" + period, resolution);
            var rateofchange = new RateOfChange(name, period);
            RegisterIndicator(symbol, rateofchange, resolution, selector);
            return rateofchange;
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
            string name = CreateIndicatorName(symbol, "ROCP" + period, resolution);
            var rateofchangepercent = new RateOfChangePercent(name, period);
            RegisterIndicator(symbol, rateofchangepercent, resolution, selector);
            return rateofchangepercent;
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
            var name = CreateIndicatorName(symbol, "ROCR" + period, resolution);
            var rocr = new RateOfChangeRatio(name, period);
            RegisterIndicator(symbol, rocr, resolution, selector);
            return rocr;
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
        public RelativeStrengthIndex RSI(Symbol symbol, int period, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, "RSI" + period, resolution);
            var rsi = new RelativeStrengthIndex(name, period, movingAverageType);
            RegisterIndicator(symbol, rsi, resolution, selector);
            return rsi;
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
            string name = CreateIndicatorName(symbol, "SMA" + period, resolution);
            var sma = new SimpleMovingAverage(name, period);
            RegisterIndicator(symbol, sma, resolution, selector);
            return sma;
        }
        /// <summary>
        /// Creates a new StandardDeviation indicator. This will return the population standard deviation of samples over the specified period.
        /// </summary>
        /// <param name="symbol">The symbol whose STD we want</param>
        /// <param name="period">The period over which to compute the STD</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The StandardDeviation indicator for the requested symbol over the speified period</returns>
        public StandardDeviation STD(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, "STD" + period, resolution);
            var std = new StandardDeviation(name, period);
            RegisterIndicator(symbol, std, resolution, selector);
            return std;
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
            string name = CreateIndicatorName(symbol, "STO", resolution);
            var stoch = new Stochastic(name, period, kPeriod, dPeriod);
            RegisterIndicator(symbol, stoch, resolution);
            return stoch;
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
            var name = CreateIndicatorName(symbol, "SUM" + period, resolution);
            var sum = new Sum(name, period);
            RegisterIndicator(symbol, sum, resolution, selector);
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
            string name = CreateIndicatorName(symbol, "SWISS" + period, resolution);
            var swiss = new SwissArmyKnife(name, period, delta, tool);
            RegisterIndicator(symbol, swiss, resolution, selector);
            return swiss;
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
            var name = CreateIndicatorName(symbol, string.Format("T3({0},{1})", period, volumeFactor), resolution);
            var t3 = new T3MovingAverage(name, period, volumeFactor);
            RegisterIndicator(symbol, t3, resolution, selector);
            return t3;
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
            var name = CreateIndicatorName(symbol, "TEMA" + period, resolution);
            var tema = new TripleExponentialMovingAverage(name, period);
            RegisterIndicator(symbol, tema, resolution, selector);
            return tema;
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
            var tr = new TrueRange(name);
            RegisterIndicator(symbol, tr, resolution, selector);
            return tr;
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
            var name = CreateIndicatorName(symbol, "TRIMA" + period, resolution);
            var trima = new TriangularMovingAverage(name, period);
            RegisterIndicator(symbol, trima, resolution, selector);
            return trima;
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
            var name = CreateIndicatorName(symbol, "TRIX" + period, resolution);
            var trix = new Trix(name, period);
            RegisterIndicator(symbol, trix, resolution, selector);
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
            var name = CreateIndicatorName(symbol, string.Format("ULTOSC({0},{1},{2})", period1, period2, period3), resolution);
            var ultosc = new UltimateOscillator(name, period1, period2, period3);
            RegisterIndicator(symbol, ultosc, resolution, selector);
            return ultosc;
        }

        /// <summary>
        /// Creates a new Variance indicator. This will return the population variance of samples over the specified period.
        /// </summary>
        /// <param name="symbol">The symbol whose VAR we want</param>
        /// <param name="period">The period over which to compute the VAR</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The Variance indicator for the requested symbol over the speified period</returns>
        public Variance VAR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, "VAR" + period, resolution);
            var variance = new Variance(name, period);
            RegisterIndicator(symbol, variance, resolution, selector);
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
            var name = CreateIndicatorName(symbol, "VWAP" + period, resolution);
            var vwap = new VolumeWeightedAveragePriceIndicator(name, period);
            RegisterIndicator(symbol, vwap, resolution, selector);
            return vwap;
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
            var vwap = new IntradayVwap(name);
            RegisterIndicator(symbol, vwap);
            return vwap;
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
        /// <returns>The rateofchangepercent indicator for the requested symbol over the specified period</returns>
        public WilliamsPercentR WILR(Symbol symbol, int period, Resolution? resolution = null, Func<IBaseData, IBaseDataBar> selector = null)
        {
            string name = CreateIndicatorName(symbol, "WILR" + period, resolution);
            var williamspercentr = new WilliamsPercentR(name, period);
            RegisterIndicator(symbol, williamspercentr, resolution, selector);
            return williamspercentr;
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
            string name = CreateIndicatorName(symbol, "WWMA" + period, resolution);
            var wwma = new WilderMovingAverage(name, period);
            RegisterIndicator(symbol, wwma, resolution, selector);
            return wwma;
        }

        /// <summary>
        /// Creates a new name for an indicator created with the convenience functions (SMA, EMA, ect...)
        /// </summary>
        /// <param name="symbol">The symbol this indicator is registered to</param>
        /// <param name="type">The indicator type, for example, 'SMA5'</param>
        /// <param name="resolution">The resolution requested</param>
        /// <returns>A unique for the given parameters</returns>
        public string CreateIndicatorName(Symbol symbol, string type, Resolution? resolution)
        {
            if (!resolution.HasValue)
            {
                resolution = GetSubscription(symbol).Resolution;
            }
            string res;
            switch (resolution)
            {
                case Resolution.Tick:
                    res = "_tick";
                    break;

                case Resolution.Second:
                    res = "_sec";
                    break;

                case Resolution.Minute:
                    res = "_min";
                    break;

                case Resolution.Hour:
                    res = "_hr";
                    break;

                case Resolution.Daily:
                    res = "_day";
                    break;

                case null:
                    res = string.Empty;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("resolution");
            }

            return string.Format("{0}({1}{2})", type, symbol.ToString(), res);
        }

        /// <summary>
        /// Gets the SubscriptionDataConfig for the specified symbol
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if no configuration is found for the requested symbol</exception>
        /// <param name="symbol">The symbol to retrieve configuration for</param>
        /// <returns>The SubscriptionDataConfig for the specified symbol</returns>
        protected SubscriptionDataConfig GetSubscription(Symbol symbol)
        {
            SubscriptionDataConfig subscription;
            try
            {
                // find our subscription to this symbol
                subscription = SubscriptionManager.Subscriptions.First(x => x.Symbol == symbol);
            }
            catch (InvalidOperationException)
            {
                // this will happen if we did not find the subscription, let's give the user a decent error message
                throw new Exception("Please register to receive data for symbol '" + symbol.ToString() + "' using the AddSecurity() function.");
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
            RegisterIndicator(symbol, indicator, ResolveConsolidator(symbol, resolution));
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
            RegisterIndicator(symbol, indicator, ResolveConsolidator(symbol, resolution), selector);
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
            RegisterIndicator(symbol, indicator, ResolveConsolidator(symbol, resolution), selector);
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
            selector = selector ?? (x => (T)x);

            // register the consolidator for automatic updates via SubscriptionManager
            SubscriptionManager.AddConsolidator(symbol, consolidator);

            // check the output type of the consolidator and verify we can assign it to T
            var type = typeof(T);
            if (!type.IsAssignableFrom(consolidator.OutputType))
            {
                throw new ArgumentException(string.Format("Type mismatch found between consolidator and indicator for symbol: {0}." +
                                                          "Consolidator outputs type {1} but indicator expects input type {2}",
                    symbol, consolidator.OutputType.Name, type.Name)
                );
            }

            // attach to the DataConsolidated event so it updates our indicator
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                var value = selector(consolidated);
                indicator.Update(value);
            };
        }

        /// <summary>
        /// Gets the default consolidator for the specified symbol and resolution
        /// </summary>
        /// <param name="symbol">The symbo whose data is to be consolidated</param>
        /// <param name="resolution">The resolution for the consolidator, if null, uses the resolution from subscription</param>
        /// <returns>The new default consolidator</returns>
        public IDataConsolidator ResolveConsolidator(Symbol symbol, Resolution? resolution)
        {
            var subscription = GetSubscription(symbol);

            // if not specified, default to the subscription's resolution
            if (!resolution.HasValue)
            {
                resolution = subscription.Resolution;
            }

            var timeSpan = resolution.Value.ToTimeSpan();

            // verify this consolidator will give reasonable results, if someone asks for second consolidation but we have minute
            // data we won't be able to do anything good, we'll call it second, but it would really just be minute!
            if (timeSpan < subscription.Resolution.ToTimeSpan())
            {
                throw new ArgumentException(string.Format("Unable to create {0} {1} consolidator because {0} is registered for {2} data. " +
                                                          "Consolidators require higher resolution data to produce lower resolution data.",
                    symbol, resolution.Value, subscription.Resolution)
                );
            }

            return ResolveConsolidator(symbol, timeSpan);
        }

        /// <summary>
        /// Gets the default consolidator for the specified symbol and resolution
        /// </summary>
        /// <param name="symbol">The symbo whose data is to be consolidated</param>
        /// <param name="timeSpan">The requested time span for the consolidator, if null, uses the resolution from subscription</param>
        /// <returns>The new default consolidator</returns>
        public IDataConsolidator ResolveConsolidator(Symbol symbol, TimeSpan? timeSpan)
        {
            var subscription = GetSubscription(symbol);

            // if not specified, default to the subscription resolution
            if (!timeSpan.HasValue)
            {
                timeSpan = subscription.Resolution.ToTimeSpan();
            }

            // verify this consolidator will give reasonable results, if someone asks for second consolidation but we have minute
            // data we won't be able to do anything good, we'll call it second, but it would really just be minute!
            if (timeSpan.Value < subscription.Resolution.ToTimeSpan())
            {
                throw new ArgumentException(string.Format("Unable to create {0} consolidator because {0} is registered for {1} data. " +
                                                          "Consolidators require higher resolution data to produce lower resolution data.",
                    symbol, subscription.Resolution)
                );
            }

            // if our type can be used as a trade bar, then let's just make one of those
            // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to TradeBar
            if (typeof(TradeBar).IsAssignableFrom(subscription.Type))
            {
                return new TradeBarConsolidator(timeSpan.Value);
            }

            // if our type can be used as a quote bar, then let's just make one of those
            // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to QuoteBar
            if (typeof(QuoteBar).IsAssignableFrom(subscription.Type))
            {
                return new QuoteBarConsolidator(timeSpan.Value);
            }

            // if our type can be used as a tick then we'll use a consolidator that keeps the TickType
            // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to Tick
            if (typeof(Tick).IsAssignableFrom(subscription.Type))
            {
                // Use IdentityDataConsolidator when ticks are not meant to consolidated into bars
                if (timeSpan.Value.Ticks == 0)
                {
                    return new IdentityDataConsolidator<Tick>();
                }

                switch (subscription.TickType)
                {
                    case TickType.OpenInterest:
                        return new OpenInterestConsolidator(timeSpan.Value);

                    case TickType.Quote:
                        return new TickQuoteBarConsolidator(timeSpan.Value);

                    default:
                        return new TickConsolidator(timeSpan.Value);
                }
            }

            // if our type can be used as a DynamicData then we'll use the DynamicDataConsolidator
            if (typeof(DynamicData).IsAssignableFrom(subscription.Type))
            {
                return new DynamicDataConsolidator(timeSpan.Value);
            }

            // no matter what we can always consolidate based on the time-value pair of BaseData
            return new BaseDataConsolidator(timeSpan.Value);
        }
    } // End Partial Algorithm Template - Indicators.
} // End QC Namespace