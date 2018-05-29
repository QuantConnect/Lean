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
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    /// <summary>
    /// Constructs a displaced moving average ribbon 
    /// </summary>
    public class IndicatorRibbonBenchmark : QCAlgorithm
    {
        private Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private IndicatorBase<IndicatorDataPoint>[] _ribbon;

        public override void Initialize()
        {
            SetStartDate(2010, 01, 01);
            SetEndDate(2018, 01, 01);

            AddSecurity(SecurityType.Equity, "SPY", Resolution.Minute);

            const int count = 50;
            const int offset = 5;
            const int period = 15;

            // define our sma as the base of the ribbon
            var sma = new SimpleMovingAverage(period);

            _ribbon = Enumerable.Range(0, count).Select(x =>
            {
                // define our offset to the zero sma, these various offsets will create our 'displaced' ribbon
                var delay = new Delay(offset * (x + 1));

                // define an indicator that takes the output of the sma and pipes it into our delay indicator
                var delayedSma = delay.Of(sma);

                // register our new 'delayedSma' for automaic updates on a daily resolution
                RegisterIndicator(_spy, delayedSma, Resolution.Daily, data => data.Value);

                return delayedSma;
            }).ToArray();
        }

        public void OnData(TradeBars data)
        {
            // wait for our entire ribbon to be ready
            if (!_ribbon.All(x => x.IsReady)) return;
            foreach (var indicator in _ribbon)
            {
                var value = indicator.Current.Value;
            }
        }
    }
}