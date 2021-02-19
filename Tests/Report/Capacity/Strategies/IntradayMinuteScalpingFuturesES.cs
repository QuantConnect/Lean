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

using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Report.Capacity.Strategies
{
    public class IntradayMinuteScalpingFuturesES : QCAlgorithm
    {
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;
        private Symbol _contract;

        public override void Initialize()
        {
            SetStartDate(2021, 1, 1);
            SetEndDate(2021, 1, 31);
            SetCash(100000);
            SetWarmup(1000);

            var a = AddFuture("ES", Resolution.Minute, Market.CME);
            a.SetFilter(0, 10000);
        }

        public override void OnData(Slice data)
        {
            var contract = data.FutureChains.Values.SelectMany(c => c.Contracts.Values)
                .OrderBy(c => c.Symbol.ID.Date)
                .FirstOrDefault()?
                .Symbol;

            if (contract == null)
            {
                return;
            }

            if (_contract != contract || (_fast == null && _slow == null))
            {
                _fast = EMA(contract, 10);
                _slow = EMA(contract, 20);
                _contract = contract;
            }

            if (!_fast.IsReady || !_slow.IsReady)
            {
                return;
            }

            if (!Portfolio.ContainsKey(contract) || (Portfolio[contract].Quantity <= 0 && _fast > _slow))
            {
                SetHoldings(contract, 1);
            }
            else if (Portfolio.ContainsKey(contract) && Portfolio[contract].Quantity >= 0 && _fast < _slow)
            {
                SetHoldings(contract, -1);
            }
        }
    }
}
