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
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Algorithm
{
    public partial class QCAlgorithm
    {
        /// <summary>
        /// Creates a new Identity indicator for the symbol The indicator will be automatically
        /// updated on the symbol's subscription resolution
        /// </summary>
        /// <param name="symbol">The symbol whose values we want as an indicator</param>
        /// <param name="selector">Selects a value from the BaseData, if null defaults to the .Value property (x => x.Value)</param>
        /// <param name="fieldName">The name of the field being selected</param>
        /// <returns>A new Identity indicator for the specified symbol and selector</returns>
        public Identity Identity(Symbol symbol, Func<BaseData, decimal> selector = null, string fieldName = null)
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
        public Identity Identity(Symbol symbol, Resolution resolution, Func<BaseData, decimal> selector = null, string fieldName = null)
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
        public Identity Identity(Symbol symbol, TimeSpan resolution, Func<BaseData, decimal> selector = null, string fieldName = null)
        {
            string name = string.Format("{0}({1}_{2})", symbol, fieldName ?? "close", resolution);
            var identity = new Identity(name);
            RegisterIndicator(symbol, identity, ResolveConsolidator(symbol, resolution), selector);
            return identity;
        }

        /// <summary>
        /// Creates a new IchimokuKinkoHyo indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose ATR we want</param>
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
        /// Creates a new AverageTrueRange indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose ATR we want</param>
        /// <param name="period">The smoothing period used to smooth the computed TrueRange values</param>
        /// <param name="type">The type of smoothing to use</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>A new AverageTrueRange indicator with the specified smoothing type and period</returns>
        public AverageTrueRange ATR(Symbol symbol, int period, MovingAverageType type = MovingAverageType.Simple, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            string name = CreateIndicatorName(symbol, "ATR" + period, resolution);
            var atr = new AverageTrueRange(name, period, type);
            RegisterIndicator(symbol, atr, resolution, selector);
            return atr;
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
        public ExponentialMovingAverage EMA(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
        {
            string name = CreateIndicatorName(symbol, "EMA" + period, resolution);
            var ema = new ExponentialMovingAverage(name, period);
            RegisterIndicator(symbol, ema, resolution, selector);
            return ema;
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
        public SimpleMovingAverage SMA(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
        {
            string name = CreateIndicatorName(symbol, "SMA" + period, resolution);
            var sma = new SimpleMovingAverage(name, period);
            RegisterIndicator(symbol, sma, resolution, selector);
            return sma;
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
        public MovingAverageConvergenceDivergence MACD(Symbol symbol, int fastPeriod, int slowPeriod, int signalPeriod, MovingAverageType type = MovingAverageType.Simple, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, string.Format("MACD({0},{1})", fastPeriod, slowPeriod), resolution);
            var macd = new MovingAverageConvergenceDivergence(name, fastPeriod, slowPeriod, signalPeriod, type);
            RegisterIndicator(symbol, macd, resolution, selector);
            return macd;
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
        public Maximum MAX(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
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
        /// Creates a new Minimum indicator to compute the minimum value
        /// </summary>
        /// <param name="symbol">The symbol whose min we want</param>
        /// <param name="period">The look back period over which to compute the min value</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null and the symbol is of type TradeBar defaults to the Low property, 
        /// otherwise it defaults to Value property of BaseData (x => x.Value)</param>
        /// <returns>A Minimum indicator that compute the in value and the periods since the min value</returns>
        public Minimum MIN(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, "MIN" + period, resolution);
            var min = new Minimum(name, period);

            // assign a default value for the selector function
            if (selector == null)
            {
                var subscription = GetSubscription(symbol);
                if (typeof (TradeBar).IsAssignableFrom(subscription.Type))
                {
                    // if we have trade bar data we'll use the Low property, if not x => x.Value will be set in RegisterIndicator
                    selector = x => ((TradeBar) x).Low;
                }
            }

            RegisterIndicator(symbol, min, ResolveConsolidator(symbol, resolution), selector);
            return min;
        }

        /// <summary>
        /// Creates a new AroonOscillator indicator which will compute the AroonUp and AroonDown (as well as the delta)
        /// </summary>
        /// <param name="symbol">The symbol whose Aroon we seek</param>
        /// <param name="period">The look back period for computing number of periods since maximum and minimum</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>An AroonOscillator configured with the specied periods</returns>
        public AroonOscillator AROON(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
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
        /// <returns>An AroonOscillator configured with the specied periods</returns>
        public AroonOscillator AROON(Symbol symbol, int upPeriod, int downPeriod, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, string.Format("AROON({0},{1})", upPeriod, downPeriod), resolution);
            var aroon = new AroonOscillator(name, upPeriod, downPeriod);
            RegisterIndicator(symbol, aroon, resolution, selector);
            return aroon;
        }

        /// <summary>
        /// Creates a new Momentum indicator. This will compute the absolute n-period change in the security.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose momentumwe want</param>
        /// <param name="period">The period over which to compute the momentum</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The momentum indicator for the requested symbol over the specified period</returns>
        public Momentum MOM(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
        {
            string name = CreateIndicatorName(symbol, "MOM" + period, resolution);
            var momentum = new Momentum(name, period);
            RegisterIndicator(symbol, momentum, resolution, selector);
            return momentum;
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
        public MomentumPercent MOMP(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
        {
            string name = CreateIndicatorName(symbol, "MOMP" + period, resolution);
            var momentum = new MomentumPercent(name, period);
            RegisterIndicator(symbol, momentum, resolution, selector);
            return momentum;
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
        public RelativeStrengthIndex RSI(Symbol symbol, int period, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, "RSI" + period, resolution);
            var rsi = new RelativeStrengthIndex(name, period, movingAverageType);
            RegisterIndicator(symbol, rsi, resolution, selector);
            return rsi;
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
        public CommodityChannelIndex CCI(Symbol symbol, int period, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "CCI" + period, resolution);
            var cci = new CommodityChannelIndex(name, period, movingAverageType);
            RegisterIndicator(symbol, cci, resolution, selector);
            return cci;
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
        public MoneyFlowIndex MFI(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "MFI" + period, resolution);
            var mfi = new MoneyFlowIndex(name, period);
            RegisterIndicator(symbol, mfi, resolution, selector);
            return mfi;
        }

        /// <summary>
        /// Creates a new StandardDeviation indicator. This will return the population standard deviation of samples over the specified period.
        /// </summary>
        /// <param name="symbol">The symbol whose STD we want</param>
        /// <param name="period">The period over which to compute the STD</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The StandardDeviation indicator for the requested symbol over the speified period</returns>
        public StandardDeviation STD(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, "STD" + period, resolution);
            var std = new StandardDeviation(name, period);
            RegisterIndicator(symbol, std, resolution, selector);
            return std;
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
        public BollingerBands BB(Symbol symbol, int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
        {
            var name = CreateIndicatorName(symbol, string.Format("BB({0},{1})", period, k), resolution);
            var bb = new BollingerBands(name, period, k, movingAverageType);
            RegisterIndicator(symbol, bb, resolution, selector);
            return bb;
        }

        /// <summary>
        /// Creates a new RateOfChange indicator. This will compute the n-period rate of change in the security.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose rateofchange we want</param>
        /// <param name="period">The period over which to compute the rateofchange</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The rateofchange indicator for the requested symbol over the specified period</returns>
        public RateOfChange ROC(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
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
        /// <param name="symbol">The symbol whose rateofchange we want</param>
        /// <param name="period">The period over which to compute the rateofchangepercent</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns>The rateofchangepercent indicator for the requested symbol over the specified period</returns>
        public RateOfChangePercent ROCP(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
        {
            string name = CreateIndicatorName(symbol, "ROCP" + period, resolution);
            var rateofchangepercent = new RateOfChangePercent(name, period);
            RegisterIndicator(symbol, rateofchangepercent, resolution, selector);
            return rateofchangepercent;
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
        public WilliamsPercentR WILR(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            string name = CreateIndicatorName(symbol, "WILR" + period, resolution);
            var williamspercentr = new WilliamsPercentR(name, period);
            RegisterIndicator(symbol, williamspercentr, resolution, selector);
            return williamspercentr;
        }

        /// <summary>
        /// Creates a new LinearWeightedMovingAverage indicator.  This indicator will linearly distribute
        /// the weights across the periods.  
        /// </summary>
        /// <param name="symbol">The symbol whose Williams %R we want</param>
        /// <param name="period">The period over which to compute the Williams %R</param>
        /// <param name="resolution">The resolution</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        /// <returns></returns>
        public LinearWeightedMovingAverage LWMA(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
        {
            string name = CreateIndicatorName(symbol, "LWMA" + period, resolution);
            var lwma = new LinearWeightedMovingAverage(name, period);
            RegisterIndicator(symbol, lwma, resolution, selector);
            return lwma;
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
        public OnBalanceVolume OBV(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "OBV", resolution);
            var onBalanceVolume = new OnBalanceVolume(name);
            RegisterIndicator(symbol, onBalanceVolume, resolution, selector);
            return onBalanceVolume;
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
        public AverageDirectionalIndex ADX(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "ADX", resolution);
            var averageDirectionalIndex = new AverageDirectionalIndex(name, period);
            RegisterIndicator(symbol, averageDirectionalIndex, resolution, selector);
            return averageDirectionalIndex;
        }

        /// <summary>
        /// Creates a new Keltner Channels indicator. 
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose Keltner Channel we seek</param>
        /// <param name="period">The period over which to compute the Keltner Channels</param>
        /// <param name="k">The number of multiples of the ATR from the middle band of the Keltner Channels</param>
        /// <param name="resolution">The resolution.</param> 
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The Keltner Channel indicator for the requested symbol.</returns>
        public KeltnerChannels KCH(Symbol symbol, int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, "KCH", resolution);
            var keltnerChannels = new KeltnerChannels(name, period, k, movingAverageType);
            RegisterIndicator(symbol, keltnerChannels, resolution, selector);
            return keltnerChannels;
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
        public DonchianChannel DCH(Symbol symbol, int upperPeriod, int lowerPeriod, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
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
        public DonchianChannel DCH(Symbol symbol, int period, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            return DCH(symbol, period, period, resolution, selector);
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
        /// Creates a new LogReturn indicator.
        /// </summary>
        /// <param name="symbol">The symbol whose log return we seek</param>
        /// <param name="period">The period of the log return.</param>
        /// <param name="resolution">The resolution.</param>
        /// <returns>log return indicator for the requested symbol.</returns>
        public LogReturn LOGR(string symbol, int period, Resolution? resolution = null)
        {
            string name = CreateIndicatorName(symbol, "LOGR", resolution);
            var logr = new LogReturn(name, period);
            RegisterIndicator(symbol, logr, resolution);
            return logr;
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
        /// <returns>An AroonOscillator configured with the specied periods</returns>
        public ParabolicStopAndReverse PSAR(Symbol symbol, decimal afStart = 0.02m, decimal afIncrement = 0.02m, decimal afMax = 0.2m, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = CreateIndicatorName(symbol, string.Format("PSAR({0},{1},{2})", afStart, afIncrement, afMax), resolution);
            var psar = new ParabolicStopAndReverse(name, afStart, afIncrement, afMax);
            RegisterIndicator(symbol, psar, resolution, selector);
            return psar;
        }

        /// <summary>
        /// Creates and registers a new consolidator to receive automatic updates at the specified resolution as well as configures
        /// the indicator to receive updates from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IndicatorDataPoint> indicator, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
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
        public void RegisterIndicator(Symbol symbol, IndicatorBase<IndicatorDataPoint> indicator, IDataConsolidator consolidator, Func<BaseData, decimal> selector = null)
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
            where T : BaseData
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
        public void RegisterIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, Resolution? resolution, Func<BaseData, T> selector)
            where T : BaseData
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
        public void RegisterIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, TimeSpan? resolution, Func<BaseData, T> selector = null)
            where T : BaseData
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
        public void RegisterIndicator<T>(Symbol symbol, IndicatorBase<T> indicator, IDataConsolidator consolidator, Func<BaseData, T> selector = null) 
            where T : BaseData
        {
            // assign default using cast
            selector = selector ?? (x => (T) x);

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
        protected IDataConsolidator ResolveConsolidator(Symbol symbol, Resolution? resolution)
        {
            var subscription = GetSubscription(symbol);

            // if the resolution is null or if the requested resolution matches the subscription, return identity
            if (!resolution.HasValue || subscription.Resolution == resolution.Value)
            {
                // since there's a generic type parameter that we don't have access to, we'll just use the activator
                var identityConsolidatorType = typeof(IdentityDataConsolidator<>).MakeGenericType(subscription.Type);
                return (IDataConsolidator)Activator.CreateInstance(identityConsolidatorType);
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
        protected IDataConsolidator ResolveConsolidator(Symbol symbol, TimeSpan? timeSpan)
        {
            var subscription = GetSubscription(symbol);

            // if the time span is null or if the requested time span matches the subscription, return identity
            if (!timeSpan.HasValue || subscription.Resolution.ToTimeSpan() == timeSpan.Value)
            {
                // since there's a generic type parameter that we don't have access to, we'll just use the activator
                var identityConsolidatorType = typeof(IdentityDataConsolidator<>).MakeGenericType(subscription.Type);
                return (IDataConsolidator)Activator.CreateInstance(identityConsolidatorType);
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

            // if our type can be used as a tick then we'll use the tick consolidator
            // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to Tick
            if (typeof(Tick).IsAssignableFrom(subscription.Type))
            {
                return new TickConsolidator(timeSpan.Value);
            }

            // if our type can be used as a DynamicData then we'll use the DynamicDataConsolidator
            if (typeof(DynamicData).IsAssignableFrom(subscription.Type))
            {
                return new DynamicDataConsolidator(timeSpan.Value);
            }

            // no matter what we can always consolidate based on the time-value pair of BaseData
            return new BaseDataConsolidator(timeSpan.Value);
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
                throw new Exception("Please register to receive data for symbol '" + symbol + "' using the AddSecurity() function.");
            }
            return subscription;
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

            return string.Format("{0}({1}{2})", type, symbol.Permtick, res);
        }

    } // End Partial Algorithm Template - Indicators.

} // End QC Namespace
