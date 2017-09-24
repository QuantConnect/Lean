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
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration algorthm for the Warm Up feature with basic indicators.
    /// </summary>
    /// <meta name="tag" content="indicators" />
    /// <meta name="tag" content="warm up" />
    /// <meta name="tag" content="history and warm up" />
    /// <meta name="tag" content="using data" />
    public class WarmupAlgorithm : QCAlgorithm
    {
        private bool _first = true;
        private string _symbol = "SPY";
        private const int FastPeriod = 60;
        private const int SlowPeriod = 3600;
        private ExponentialMovingAverage _fast, _slow;

        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            SetCash(100000);             //Set Strategy Cash
            // Find more symbols here: http://quantconnect.com/data
            AddSecurity(SecurityType.Equity, _symbol, Resolution.Second);

            _fast = EMA(_symbol, FastPeriod);
            _slow = EMA(_symbol, SlowPeriod);

            SetWarmup(SlowPeriod);
        }
        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (_first && !IsWarmingUp)
            {
                _first = false;
                Debug("Fast: " + _fast.Samples);
                Debug("Slow: " + _slow.Samples);
            }
            if (_fast > _slow)
            {
                SetHoldings(_symbol, 1);
            }
            else
            {
                SetHoldings(_symbol, -1);
            }
        }
    }
}