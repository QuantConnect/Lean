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

namespace QuantConnect.Algorithm.Examples
{
    /// <summary>
    /// In this example we look at the canonical 15/30 day moving average cross. This algorithm
    /// will go long when the 15 crosses above the 30 and will liquidate when the 15 crosses
    /// back below the 30.
    /// </summary>
    public class MovingAverageCrossAlgorithm : QCAlgorithm
    {
        private const string Symbol = "SPY";
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
            SetStartDate(2009, 01, 01);
            SetEndDate(2015, 01, 01);

            // request SPY data with minute resolution
            AddSecurity(SecurityType.Equity, Symbol, Resolution.Minute);

            // create a 15 day exponential moving average
            fast = EMA(Symbol, 15, Resolution.Daily);

            // create a 30 day exponential moving average
            slow = EMA(Symbol, 30, Resolution.Daily);

            int ribbonCount = 8;
            int ribbonInterval = 15;
            ribbon = Enumerable.Range(0, ribbonCount).Select(x => SMA(Symbol, (x + 1)*ribbonInterval, Resolution.Daily)).ToArray();
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
            if (!slow.IsReady) return;

            // only once per day
            if (previous.Date == Time.Date) return;

            // define a small tolerance on our checks to avoid bouncing
            const decimal tolerance = 0.00015m;
            var holdings = Portfolio[Symbol].Quantity;

            // we only want to go long if we're currently short or flat
            if (holdings <= 0)
            {
                // if the fast is greater than the slow, we'll go long
                if (fast > slow * (1 + tolerance))
                {
                    Log("BUY  >> " + Securities[Symbol].Price);
                    SetHoldings(Symbol, 1.0);
                }
            }

            // we only want to liquidate if we're currently long
            // if the fast is less than the slow we'll liquidate our long
            if (holdings > 0 && fast < slow)
            {
                Log("SELL >> " + Securities[Symbol].Price);
                Liquidate(Symbol);    
            }

            Plot(Symbol, "Price", data[Symbol].Price);
            
            // easily plot indicators, the series name will be the name of the indicator
            Plot(Symbol, fast, slow);
            Plot("Ribbon", ribbon);

            previous = Time;
        }
    }
}