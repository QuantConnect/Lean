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

namespace QuantConnect.Indicators
{
    public class BetaIndicator : TradeBarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private Dictionary<Symbol, RollingWindow<decimal>> _symbol;
        private Symbol _marketIndex;
        private Symbol _stock;
        private decimal _beta;

        public BetaIndicator(string name, int period, Symbol marketIndex, Symbol stock)
            : base(name)
        {
            WarmUpPeriod = period;
            _marketIndex = marketIndex;
            _stock = stock;
            _symbol[marketIndex] = new RollingWindow<decimal>(period);
            _symbol[stock] = new RollingWindow<decimal>(period);
            _beta = 0;
        }
        public int WarmUpPeriod { get; private set; }

        public override bool IsReady => _symbol[_stock].Count == _symbol[_marketIndex].Count && _symbol[_marketIndex].Count == WarmUpPeriod;

        protected override decimal ComputeNextValue(TradeBar input)
        {
            var symbol = input.Symbol;
            _symbol[symbol].Add(input.Close);

            if (_symbol[symbol].Count > 1)
            {

            }
            return _beta;
        }

        private List<decimal> GetReturns(RollingWindow<decimal> rollingWindow)
        {
            var returns = new List<decimal>();
            for(int i = rollingWindow.Count; i > 1 ; i--)
            {
                returns.Add((rollingWindow[i] / rollingWindow[i - 1]) - 1);
            }
            return returns;
        }
    }
}
