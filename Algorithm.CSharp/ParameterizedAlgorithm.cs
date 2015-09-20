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
using QuantConnect.Data;

namespace QuantConnect.Algorithm.Examples
{
    /// <summary>
    /// In this example we look at the canonical 15/30 day moving average cross. This algorithm
    /// will go long when the 15 crosses above the 30 and will liquidate when the 15 crosses
    /// back below the 30.
    /// </summary>
    
    
    public class ParameterizedAlgorithm : QCAlgorithm
    {
        [IntParameter(5, 10)]
        public int FastInterval = 15;

        [IntParameter(10, 20, 2)]
        public int SlowInterval = 30;

        [DateTimeParameter("2013-10-07", "2013-10-11")]
        public DateTime Starting = new DateTime(2013, 10, 7);

        [DateTimeParameter("2013-10-07", "2013-10-11")]
        public DateTime Ending = new DateTime(2013, 10, 11);

        public string Symbol = "SPY";

        private DateTime previous;
        private ExponentialMovingAverage fast;
        private ExponentialMovingAverage slow;
        private SimpleMovingAverage[] ribbon;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            // set up our analysis span
            SetStartDate(Starting.Year, Starting.Month, Starting.Day);
            SetEndDate(Ending.Year, Ending.Month, Ending.Day);

            // request SPY data with minute resolution
            AddSecurity(SecurityType.Equity, Symbol, Resolution.Minute);

            Debug("Initializing " + StartDate + "; " + EndDate);

            // create a 15 minute exponential moving average
            fast = EMA(Symbol, FastInterval, Resolution.Minute);

            // create a 30 minute exponential moving average
            slow = EMA(Symbol, SlowInterval, Resolution.Minute);

            int ribbonCount = 8;
            int ribbonInterval = 15;
            ribbon = Enumerable.Range(0, ribbonCount).Select(x => SMA(Symbol, (x + 1) * ribbonInterval, Resolution.Minute)).ToArray();
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public override void OnData(Slice data)
        {
            // a couple things to notice in this method:
            //  1. We never need to 'update' our indicators with the data, the engine takes care of this for us
            //  2. We can use indicators directly in math expressions
            //  3. We can easily plot many indicators at the same time

            TradeBar last = data.Bars.Last().Value;
            //Debug(String.Format("{0} :  O {1}  H {2}  L {3}  C {4}  V {5}  F {6}  S {7}",
                //last.Time, last.Open, last.High, last.Low, last.Close, last.Volume, fast, slow));

            // wait for our slow ema to fully initialize
            if (!slow.IsReady) return;

            // define a small tolerance on our checks to avoid bouncing
            const decimal tolerance = 0.00015m;
            var holdings = Portfolio[Symbol].Quantity;

            // we only want to go long if we're currently short or flat
            if (holdings <= 0)
            {
                // if the fast is greater than the slow, we'll go long
                if (fast > slow * (1 + tolerance))
                {
                    Log(last.Time + "  BUY  >> " + Securities[Symbol].Price);
                    SetHoldings(Symbol, 1.0);
                }
            }

            // we only want to liquidate if we're currently long
            // if the fast is less than the slow we'll liquidate our long
            if (holdings > 0 && fast < slow)
            {
                Log(last.Time + "  SELL >> " + Securities[Symbol].Price);
                Liquidate(Symbol);
            }

            Plot(Symbol, "Price", data[Symbol].Price);

            // easily plot indicators, the series name will be the name of the indicator
            Plot(Symbol, fast, slow);
            Plot("Ribbon", ribbon);

            previous = data.Time;
        }
    }
}