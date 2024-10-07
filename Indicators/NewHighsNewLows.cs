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

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Hew Highs - New Lows (NH-NL) Indicator
    /// Displays the daily difference between the number of stocks reaching
    /// new 52-week highs and the number of stocks reaching new 52-week lows.
    /// </summary>
    public class NHNLIndicator : IndicatorBase<IBaseData>
    {
        private readonly Dictionary<Symbol, decimal> _highs = new();
        private readonly Dictionary<Symbol, decimal> _lows = new();
        private int _nhCount;
        private int _nlCount;
    
        public NHNLIndicator(string name) : base(name) {}
    
        protected override decimal ComputeNextValue(IBaseData input)
        {
            // Example logic: Assume we get data containing symbols' 52-week highs/lows
            var tradeBar = input as TradeBar;
            if (tradeBar == null)
              return 0;
      
            var symbol = tradeBar.Symbol;
      
            // Update 52-week highs and lows for each symbol
            if (!_highs.ContainsKey(symbol) || tradeBar.High > _highs[symbol])
              _highs[symbol] = tradeBar.High;
            if (!_lows.ContainsKey(symbol) || tradeBar.Low < _lows[symbol])
              _lows[symbol] = tradeBar.Low;
      
            // Count stocks reaching new 52-week highs or lows
            _nhCount = _highs.Values.Count(h => tradeBar.Close >= h);
            _nlCount = _lows.Values.Count(l => tradeBar.Close <= l);
      
            // NH-NL: Difference between the number of new highs and new lows
            return _nhCount - _nlCounts;
        }
  
        public override bool IsReady => _highs.Count > 0 && _lows.Count > 0;
    }
}
