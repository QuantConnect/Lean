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
 *
*/

using System;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators.CandlestickPatterns;

namespace QuantConnect.Algorithm
{
    /// <summary>
    /// Provides helpers for using candlestick patterns
    /// </summary>
    public class CandlestickPatterns
    {
        private readonly QCAlgorithm _algorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="CandlestickPatterns"/> class
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        public CandlestickPatterns(QCAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        /// <summary>
        /// Creates a new Two Crows pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public TwoCrows TwoCrows(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "TWOCROWS", resolution);
            var pattern = new TwoCrows(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Three Black Crows pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ThreeBlackCrows ThreeBlackCrows(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "THREEBLACKCROWS", resolution);
            var pattern = new ThreeBlackCrows(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Three Inside Up/Down pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ThreeInside ThreeInside(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "THREEINSIDE", resolution);
            var pattern = new ThreeInside(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Three Line Strike pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ThreeLineStrike ThreeLineStrike(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "THREELINESTRIKE", resolution);
            var pattern = new ThreeLineStrike(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Three Outside Up/Down pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ThreeOutside ThreeOutside(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "THREEOUTSIDE", resolution);
            var pattern = new ThreeOutside(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Three Stars In The South pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ThreeStarsInSouth ThreeStarsInSouth(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "THREESTARSINSOUTH", resolution);
            var pattern = new ThreeStarsInSouth(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Three Advancing White Soldiers pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ThreeWhiteSoldiers ThreeWhiteSoldiers(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "THREEWHITESOLDIERS", resolution);
            var pattern = new ThreeWhiteSoldiers(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Abandoned Baby pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="penetration">Percentage of penetration of a candle within another candle</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public AbandonedBaby AbandonedBaby(Symbol symbol, decimal penetration = 0.3m, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "ABANDONEDBABY", resolution);
            var pattern = new AbandonedBaby(name, penetration);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Advance Block pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public AdvanceBlock AdvanceBlock(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "ADVANCEBLOCK", resolution);
            var pattern = new AdvanceBlock(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Belt-Hold pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public BeltHold BeltHold(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "BELTHOLD", resolution);
            var pattern = new BeltHold(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Breakaway pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public Breakaway Breakaway(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "BREAKAWAY", resolution);
            var pattern = new Breakaway(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Closing Marubozu pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ClosingMarubozu ClosingMarubozu(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "CLOSINGMARUBOZU", resolution);
            var pattern = new ClosingMarubozu(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Concealed Baby Swallow pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public ConcealedBabySwallow ConcealedBabySwallow(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "CONCEALEDBABYSWALLOW", resolution);
            var pattern = new ConcealedBabySwallow(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Counterattack pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public Counterattack Counterattack(Symbol symbol, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "COUNTERATTACK", resolution);
            var pattern = new Counterattack(name);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }

        /// <summary>
        /// Creates a new Dark Cloud Cover pattern indicator.
        /// The indicator will be automatically updated on the given resolution.
        /// </summary>
        /// <param name="symbol">The symbol whose pattern we seek</param>
        /// <param name="penetration">Percentage of penetration of a candle within another candle</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="selector">Selects a value from the BaseData to send into the indicator, if null defaults to casting the input value to a TradeBar</param>
        /// <returns>The pattern indicator for the requested symbol.</returns>
        public DarkCloudCover DarkCloudCover(Symbol symbol, decimal penetration = 0.5m, Resolution? resolution = null, Func<BaseData, TradeBar> selector = null)
        {
            var name = _algorithm.CreateIndicatorName(symbol, "DARKCLOUDCOVER", resolution);
            var pattern = new DarkCloudCover(name, penetration);
            _algorithm.RegisterIndicator(symbol, pattern, resolution, selector);
            return pattern;
        }
    }
}
