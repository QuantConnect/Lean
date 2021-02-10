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

using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Report.Capacity.Strategies
{
    public class IntradayMinuteScalping : QCAlgorithm
    {
        private Symbol _spy;
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;


        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetStartDate(2020, 1, 30);
            SetCash(100000);
            SetWarmup(100);

            _spy = AddEquity("SPY", Resolution.Minute).Symbol;
            _fast = EMA(_spy, 20);
            _slow = EMA(_spy, 40);
        }

        public override void OnData(Slice data)
        {
            if (Portfolio[_spy].Quantity <= 0 && _fast > _slow)
            {
                SetHoldings(_spy, 1);
            }
            else if (Portfolio[_spy].Quantity >= 0 && _fast < _slow)
            {
                SetHoldings(_spy, -1);
            }
        }
    }
}
