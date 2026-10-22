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

using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Trades gold (XAUUSD CFD) with a simple moving average crossover:
    /// long when the fast SMA is above the slow SMA, flat otherwise.
    /// </summary>
    public class GoldMovingAverageCrossAlgorithm : QCAlgorithm
    {
        private Symbol _gold;
        private SimpleMovingAverage _fast;
        private SimpleMovingAverage _slow;

        public override void Initialize()
        {
            SetStartDate(2010, 1, 1);
            SetEndDate(2019, 2, 25);
            SetCash(10000);
            SetBrokerageModel(BrokerageName.OandaBrokerage, AccountType.Margin);

            // Hourly data so orders fill as market orders; Oanda rejects the
            // market-on-open/close orders Lean would use with daily bars
            _gold = AddCfd("XAUUSD", Resolution.Hour, Market.Oanda).Symbol;

            _fast = SMA(_gold, 50, Resolution.Daily);
            _slow = SMA(_gold, 200, Resolution.Daily);
            SetWarmUp(200, Resolution.Daily);
        }

        public override void OnData(Slice slice)
        {
            if (IsWarmingUp || !_slow.IsReady) return;

            var invested = Portfolio[_gold].Invested;
            if (!invested && _fast > _slow)
            {
                SetHoldings(_gold, 1m);
            }
            else if (invested && _fast < _slow)
            {
                Liquidate(_gold);
            }
        }
    }
}
