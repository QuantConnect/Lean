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
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration algorithm showing how to easily convert an old algorithm into the framework.
    ///
    /// 1. Make class derive from QCAlgorithmFrameworkBridge instead of QCAlgorithm.
    /// 2. When making orders, also create insights for the correct direction (up/down), can also set insight prediction period/magnitude/direction
    /// 3. Profit :)
    /// </summary>
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="indicator classes" />
    /// <meta name="tag" content="plotting indicators" />
    public class ConvertToFrameworkAlgorithm : QCAlgorithmFrameworkBridge // 1. Derive from QCAlgorithmFrameworkBridge
    {
        private MovingAverageConvergenceDivergence _macd;
        private readonly string _symbol = "SPY";

        public readonly int FastEmaPeriod = 12;
        public readonly int SlowEmaPeriod = 26;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2004, 01, 01);
            SetEndDate(2015, 01, 01);

            AddSecurity(SecurityType.Equity, _symbol, Resolution.Daily);

            // define our daily macd(12,26) with a 9 day signal
            _macd = MACD(_symbol, FastEmaPeriod, SlowEmaPeriod, 9, MovingAverageType.Exponential, Resolution.Daily);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            // wait for our indicator to be ready
            if (!_macd.IsReady) return;

            var holding = Portfolio[_symbol];

            var signalDeltaPercent = (_macd - _macd.Signal) / _macd.Fast;
            var tolerance = 0.0025m;

            // if our macd is greater than our signal, then let's go long
            if (holding.Quantity <= 0 && signalDeltaPercent > tolerance)
            {
                // 2. Call EmitInsights with insights created in correct direction, here we're going long
                //    The EmitInsights method can accept multiple insights separated by commas
                EmitInsights(
                    // Creates an insight for our symbol, predicting that it will move up within the fast ema period number of days
                    Insight.Price(_symbol, TimeSpan.FromDays(FastEmaPeriod), InsightDirection.Up)
                );

                // longterm says buy as well
                SetHoldings(_symbol, 1.0);
            }
            // if our macd is less than our signal, then let's go short
            else if (holding.Quantity >= 0 && signalDeltaPercent < -tolerance)
            {
                // 2. Call EmitInsights with insights created in correct direction, here we're going short
                //    The EmitInsights method can accept multiple insights separated by commas
                EmitInsights(
                    // Creates an insight for our symbol, predicting that it will move down within the fast ema period number of days
                    Insight.Price(_symbol, TimeSpan.FromDays(FastEmaPeriod), InsightDirection.Down)
                );

                // shortterm says sell as well
                SetHoldings(_symbol, -1.0);
            }

            // plot both lines
            Plot("MACD", _macd, _macd.Signal);
            Plot(_symbol, "Open", data[_symbol].Open);
            Plot(_symbol, _macd.Fast, _macd.Slow);
        }
    }
}
