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
/**********************************************************
* USING NAMESPACES
**********************************************************/
using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;


namespace QuantConnect.Algorithm
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    public partial class QCAlgorithm
    {
        /******************************************************** 
        * CLASS PRIVATE VARIABLES
        *********************************************************/


        /******************************************************** 
        * CLASS PUBLIC PROPERTIES
        *********************************************************/


        /******************************************************** 
        * CLASS METHODS
        *********************************************************/

        /// <summary>
        /// Creates a new AverageTrueRange indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose ATR we want</param>
        /// <param name="period">The smoothing period used to smooth the computed TrueRange values</param>
        /// <param name="type">The type of smoothing to use</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>A new AverageTrueRange indicator with the specified smoothing type and period</returns>
        public AverageTrueRange ATR(string symbol, int period, MovingAverageType type = MovingAverageType.Simple, Resolution? resolution = null)
        {
            string name = CreateIndicatorName(symbol, "ATR" + period, resolution);
            var atr = new AverageTrueRange(name, period, type);
            RegisterIndicator(symbol, atr, resolution);
            return atr;
        }

        /// <summary>
        /// Creates an ExponentialMovingAverage indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose EMA we want</param>
        /// <param name="period">The period of the EMA</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>The ExponentialMovingAverage for the given parameters</returns>
        public ExponentialMovingAverage EMA(string symbol, int period, Resolution? resolution = null)
        {
            string name = CreateIndicatorName(symbol, "EMA" + period, resolution);
            var ema = new ExponentialMovingAverage(name, period);
            RegisterIndicator(symbol, ema, resolution, x => x.Value);
            return ema;
        }

        /// <summary>
        /// Creates an SimpleMovingAverage indicator for the symbol. The indicator will be automatically
        /// updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose SMA we want</param>
        /// <param name="period">The period of the SMA</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>The SimpleMovingAverage for the given parameters</returns>
        public SimpleMovingAverage SMA(string symbol, int period, Resolution? resolution = null)
        {
            string name = CreateIndicatorName(symbol, "SMA" + period, resolution);
            var sma = new SimpleMovingAverage(name, period);
            RegisterIndicator(symbol, sma, resolution, x => x.Value);
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
        /// <returns>The moving average convergence divergence between the fast and slow averages</returns>
        public MovingAverageConvergenceDivergence MACD(string symbol, int fastPeriod, int slowPeriod, int signalPeriod, MovingAverageType type = MovingAverageType.Simple, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(symbol, string.Format("MACD({0},{1})", fastPeriod, slowPeriod), resolution);
            var macd = new MovingAverageConvergenceDivergence(name, fastPeriod, slowPeriod, signalPeriod, type);
            RegisterIndicator(symbol, macd, resolution, x => x.Value);
            return macd;
        }

        /// <summary>
        /// Creates a new Maximum indicator to compute the maximum value
        /// </summary>
        /// <param name="symbol">The symbol whose max we want</param>
        /// <param name="period">The look back period over which to compute the max value</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>A Maximum indicator that compute the max value and the periods since the max value</returns>
        public Maximum MAX(string symbol, int period, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(symbol, "MAX" + period, resolution);
            var max = new Maximum(name, period);

            // we want to hook this guy up to receive high data so we get a true max value for the range
            RegisterIndicator(symbol, max, ResolveConsolidator(symbol, resolution), baseData => ((TradeBar)baseData).High);
            return max;
        }

        /// <summary>
        /// Creates a new Minimum indicator to compute the minimum value
        /// </summary>
        /// <param name="symbol">The symbol whose min we want</param>
        /// <param name="period">The look back period over which to compute the min value</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>A Minimum indicator that compute the in value and the periods since the min value</returns>
        public Minimum MIN(string symbol, int period, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(symbol, "MIN" + period, resolution);
            var min = new Minimum(name, period);

            // we want to hook this guy up to receive high data so we get a true max value for the range
            RegisterIndicator(symbol, min, ResolveConsolidator(symbol, resolution), baseData => ((TradeBar)baseData).Low);
            return min;
        }

        /// <summary>
        /// Creates a new AroonOscillator indicator which will compute the AroonUp and AroonDown (as well as the delta)
        /// </summary>
        /// <param name="symbol">The symbol whose Aroon we seek</param>
        /// <param name="period">The look back period for computing number of periods since maximum and minimum</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>An AroonOscillator configured with the specied periods</returns>
        public AroonOscillator AROON(string symbol, int period, Resolution? resolution = null)
        {
            return AROON(symbol, period, period, resolution);
        }
        
        /// <summary>
        /// Creates a new AroonOscillator indicator which will compute the AroonUp and AroonDown (as well as the delta)
        /// </summary>
        /// <param name="symbol">The symbol whose Aroon we seek</param>
        /// <param name="upPeriod">The look back period for computing number of periods since maximum</param>
        /// <param name="downPeriod">The look back period for computing number of periods since minimum</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>An AroonOscillator configured with the specied periods</returns>
        public AroonOscillator AROON(string symbol, int upPeriod, int downPeriod, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(symbol, string.Format("AROON({0},{1})", upPeriod, downPeriod), resolution);
            var aroon = new AroonOscillator(name, upPeriod, downPeriod);
            RegisterIndicator(symbol, aroon, resolution);
            return aroon;
        }

        /// <summary>
        /// Creates a new Momentum indicator. This will compute the absolute n-period change in the security.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose momentumwe want</param>
        /// <param name="period">The period over which to compute the momentum</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>The momentum indicator for the requested symbol over the specified period</returns>
        public Momentum MOM(string symbol, int period, Resolution? resolution = null)
        {
            string name = CreateIndicatorName(symbol, "MOM" + period, resolution);
            var momentum = new Momentum(name, period);
            RegisterIndicator(symbol, momentum, resolution, x => x.Value);
            return momentum;
        }

        /// <summary>
        /// Creates a new MomentumPercent indicator. This will compute the n-period percent change in the security.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose momentum we want</param>
        /// <param name="period">The period over which to compute the momentum</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>The momentum indicator for the requested symbol over the specified period</returns>
        public MomentumPercent MOMP(string symbol, int period, Resolution? resolution = null)
        {
            string name = CreateIndicatorName(symbol, "MOMP" + period, resolution);
            var momentum = new MomentumPercent(name, period);
            RegisterIndicator(symbol, momentum, resolution, x => x.Value);
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
        /// <returns>The RelativeStrengthIndex indicator for the requested symbol over the specified period</returns>
        public RelativeStrengthIndex RSI(string symbol, int period, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(symbol, "RSI" + period, resolution);
            var rsi = new RelativeStrengthIndex(name, period, movingAverageType);
            RegisterIndicator(symbol, rsi, resolution, x => x.Value);
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
        /// <returns>The CommodityChannelIndex indicator for the requested symbol over the specified period</returns>
        public CommodityChannelIndex CCI(string symbol, int period, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null) {
            var name = CreateIndicatorName(symbol, "CCI" + period, resolution);
            var cci = new CommodityChannelIndex(name, period, movingAverageType);
            RegisterIndicator(symbol, cci, resolution);
            return cci;
        }

        /// <summary>
        /// Creates a new StandardDeviation indicator. This will return the population standard deviation of samples over the specified period.
        /// </summary>
        /// <param name="symbol">The symbol whose STD we want</param>
        /// <param name="period">The period over which to compute the STD</param>
        /// <param name="resolution">The resolution</param>
        /// <returns>The StandardDeviation indicator for the requested symbol over the speified period</returns>
        public StandardDeviation STD(string symbol, int period, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(symbol, "STD" + period, resolution);
            var std = new StandardDeviation(name, period);
            RegisterIndicator(symbol, std, resolution, x => x.Value);
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
        /// <returns>A BollingerBands configured with the specied period</returns>
        public BollingerBands BB(string symbol, int period, decimal k, MovingAverageType movingAverageType = MovingAverageType.Simple, Resolution? resolution = null)
        {
            var name = CreateIndicatorName(symbol, string.Format("BB({0},{1})", period, k), resolution);
            var bb = new BollingerBands(name, period, k, movingAverageType);
            RegisterIndicator(symbol, bb, resolution, x => x.Value);
            return bb;
        }

        /// <summary>
        /// Creates and registers a new consolidator to receive automatic at the specified resolution as well as configures
        /// the indicator to receive updates from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to the Value property of BaseData (x => x.Value)</param>
        public void RegisterIndicator(string symbol, IndicatorBase<IndicatorDataPoint> indicator, Resolution? resolution = null, Func<BaseData, decimal> selector = null)
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
        public void RegisterIndicator(string symbol, IndicatorBase<IndicatorDataPoint> indicator, IDataConsolidator consolidator, Func<BaseData, decimal> selector = null)
        {
            // default our selector to the Value property on BaseData
            selector = selector ?? (x => x.Value);

            // register the consolidator for automatic updates via SubscriptionManager
            SubscriptionManager.AddConsolidator(symbol, consolidator);

            // attach to the DataConsolidated event so it updates our indicator
            consolidator.DataConsolidated += (sender, consolidated) =>
            {
                var value = selector(consolidated);
                indicator.Update(consolidated.Time, value);
            };
        }

        /// <summary>
        /// Registers the consolidator to receive automatic updates as well as configures the indicator to receive updates
        /// from the consolidator.
        /// </summary>
        /// <param name="symbol">The symbol to register against</param>
        /// <param name="indicator">The indicator to receive data from the consolidator</param>
        /// <param name="resolution">The resolution at which to send data to the indicator, null to use the same resolution as the subscription</param>
        public void RegisterIndicator<T>(string symbol, IndicatorBase<T> indicator, Resolution? resolution = null)
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
        /// <param name="consolidator">The consolidator to receive raw subscription data</param>
        public void RegisterIndicator<T>(string symbol, IndicatorBase<T> indicator, IDataConsolidator consolidator) 
            where T : BaseData
        {
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
                indicator.Update(consolidated as T);
            };
        }

        /// <summary>
        /// Gets the default consolidator for the specified symbol and resolution
        /// </summary>
        /// <param name="symbol">The symbo whose data is to be consolidated</param>
        /// <param name="resolution">The resolution for the consolidator, if null, uses the resolution from subscription</param>
        /// <returns>The new default consolidator</returns>
        protected IDataConsolidator ResolveConsolidator(string symbol, Resolution? resolution)
        {
            symbol = symbol.ToUpper();
            try
            {
                // find our subscription to this symbol
                var subscription = SubscriptionManager.Subscriptions.First(x => x.Symbol == symbol);

                // if the resolution is null or if the requested resolution matches the subscription, return identity
                if (!resolution.HasValue || subscription.Resolution == resolution.Value)
                {
                    // since there's a generic type parameter that we don't have access to, we'll just use the activator
                    var identityConsolidatorType = typeof (IdentityDataConsolidator<>).MakeGenericType(subscription.Type);
                    return (IDataConsolidator) Activator.CreateInstance(identityConsolidatorType);
                }

                // if our type can be used as a trade bar, then let's just make one of those
                // we use IsAssignableFrom instead of IsSubclassOf so that we can account for types that are able to be cast to TradeBar
                if (typeof(TradeBar).IsAssignableFrom(subscription.Type))
                {
                    return TradeBarConsolidator.FromResolution(resolution.Value);
                }

                // TODO : Add default IDataConsolidator for Tick
                // if it is tick data we would need a different consolidator, what should the default consolidator of tick data be?
                // I imagine it would be something that produces a TradeBar from ticks!


                // if it is custom data I don't think we can resolve a default consolidator for the type unless it was assignable to trade bar
            }
            catch (InvalidOperationException)
            {
                // this will happen if wedid not find the subscription, let's give the user a decent error message
                throw new Exception("Please register to receive data for symbol '" + symbol + "' using the AddSecurity() function.");
            }

            throw new NotSupportedException("QCAlgorithm.ResolveConsolidator(): Currently this only supports TradeBar data.");
        }

        /// <summary>
        /// Creates a new name for an indicator created with the convenience functions (SMA, EMA, ect...)
        /// </summary>
        /// <param name="symbol">The symbol this indicator is registered to</param>
        /// <param name="type">The indicator type, for example, 'SMA5'</param>
        /// <param name="resolution">The resolution requested</param>
        /// <returns>A unique for the given parameters</returns>
        protected static string CreateIndicatorName(string symbol, string type, Resolution? resolution)
        {
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

            return string.Format("{0}({1}{2})", type, symbol.ToUpper(), res);
        }

    } // End Partial Algorithm Template - Indicators.

} // End QC Namespace
