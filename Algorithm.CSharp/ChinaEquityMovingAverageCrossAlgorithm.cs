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
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Canonical moving average cross example using a Wind full-code China A-share symbol.
    /// </summary>
    public class ChinaEquityMovingAverageCrossAlgorithm : QCAlgorithm
    {
        private Symbol _symbol;
        private DateTime _previous;
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;
        private int _tradeBars;
        private int _filledOrders;

        public override void Initialize()
        {
            SetStartDate(2024, 01, 02);
            SetEndDate(2024, 04, 30);
            SetCash(100000m);

            var equity = AddEquity("600000.SH", Resolution.Daily);
            equity.SetFeeModel(new ConstantFeeModel(0));
            _symbol = equity.Symbol;

            _fast = EMA(_symbol, 15, Resolution.Daily);
            _slow = EMA(_symbol, 30, Resolution.Daily);
        }

        public override void OnData(Slice data)
        {
            if (!data.Bars.ContainsKey(_symbol))
            {
                return;
            }

            _tradeBars++;

            if (!_slow.IsReady)
            {
                return;
            }

            if (_previous.Date == Time.Date)
            {
                return;
            }

            const decimal tolerance = 0.00015m;
            var holdings = Portfolio[_symbol].Quantity;

            if (holdings <= 0 && _fast > _slow * (1 + tolerance))
            {
                SetHoldings(_symbol, 0.95);
            }

            if (holdings > 0 && _fast < _slow)
            {
                Liquidate(_symbol);
            }

            _previous = Time;
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                _filledOrders++;
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (_tradeBars < 60)
            {
                throw new RegressionTestException($"Expected at least 60 China equity daily bars but received {_tradeBars}.");
            }
            if (!_fast.IsReady || !_slow.IsReady)
            {
                throw new RegressionTestException("China equity moving averages were not ready.");
            }
            if (_filledOrders == 0)
            {
                throw new RegressionTestException("China equity moving average cross did not produce a filled order.");
            }
        }
    }
}
