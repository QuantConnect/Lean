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
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// In this example we look at the canonical 15/30 day moving average cross. This algorithm
    /// will go long when the 15 crosses above the 30 and will liquidate when the 15 crosses
    /// back below the 30.
    /// </summary>
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="indicator classes" />
    /// <meta name="tag" content="moving average cross" />
    /// <meta name="tag" content="strategy example" />
    public class MovingAverageCrossAlgorithm : QCAlgorithm
    {
        private string _symbol = "SPY";
        private DateTime _previous;
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;
        private SimpleMovingAverage[] _ribbon;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // set up our analysis span
            SetStartDate(2009, 01, 01);
            SetEndDate(2015, 01, 01);

            // request SPY data with minute resolution
            AddSecurity(SecurityType.Equity, _symbol, Resolution.Minute);

            // create a 15 day exponential moving average
            _fast = EMA(_symbol, 15, Resolution.Daily);

            // create a 30 day exponential moving average
            _slow = EMA(_symbol, 30, Resolution.Daily);

            var ribbonCount = 8;
            var ribbonInterval = 15;
            _ribbon = Enumerable.Range(0, ribbonCount).Select(x => SMA(_symbol, (x + 1)*ribbonInterval, Resolution.Daily)).ToArray();
        }


        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {
            // a couple things to notice in this method:
            //  1. We never need to 'update' our indicators with the data, the engine takes care of this for us
            //  2. We can use indicators directly in math expressions
            //  3. We can easily plot many indicators at the same time

            // wait for our slow ema to fully initialize
            if (!_slow.IsReady) return;

            // only once per day
            if (_previous.Date == Time.Date) return;

            // define a small tolerance on our checks to avoid bouncing
            const decimal tolerance = 0.00015m;
            var holdings = Portfolio[_symbol].Quantity;

            // we only want to go long if we're currently short or flat
            if (holdings <= 0)
            {
                // if the fast is greater than the slow, we'll go long
                if (_fast > _slow * (1 + tolerance))
                {
                    Log("BUY  >> " + Securities[_symbol].Price);
                    SetHoldings(_symbol, 1.0);
                }
            }

            // we only want to liquidate if we're currently long
            // if the fast is less than the slow we'll liquidate our long
            if (holdings > 0 && _fast < _slow)
            {
                Log("SELL >> " + Securities[_symbol].Price);
                Liquidate(_symbol);
            }

            Plot(_symbol, "Price", data[_symbol].Price);

            // easily plot indicators, the series name will be the name of the indicator
            Plot(_symbol, _fast, _slow);
            Plot("Ribbon", _ribbon);

            _previous = Time;
        }
    }
}