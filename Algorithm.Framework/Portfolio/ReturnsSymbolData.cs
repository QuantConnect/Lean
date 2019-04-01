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
using QuantConnect.Indicators;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Contains returns specific to a symbol required for optimization model
    /// </summary>
    public class ReturnsSymbolData
    {
        private readonly Symbol _symbol;
        private readonly RateOfChange _roc;
        private readonly RollingWindow<IndicatorDataPoint> _window;

        public ReturnsSymbolData(Symbol symbol, int lookback, int period)
        {
            _symbol = symbol;
            _roc = new RateOfChange($"{_symbol}.ROC({lookback})", lookback);
            _window = new RollingWindow<IndicatorDataPoint>(period);
            _roc.Updated += OnRateOfChangeUpdated;
        }

        public Dictionary<DateTime, double> Returns => _window.Select(x => new { Date = x.EndTime, Return = (double)x.Value }).ToDictionary(r => r.Date, r => r.Return);

        public void Add(DateTime time, decimal value)
        {
            var item = new IndicatorDataPoint(_symbol, time, value);
            _window.Add(item);
        }

        public bool Update(DateTime time, decimal value)
        {
            return _roc.Update(time, value);
        }

        public void Reset()
        {
            _roc.Updated -= OnRateOfChangeUpdated;
            _roc.Reset();
            _window.Reset();
        }

        private void OnRateOfChangeUpdated(object roc, IndicatorDataPoint updated)
        {
            if (_roc.IsReady)
            {
                _window.Add(updated);
            }
        }
    }

    public static class ReturnsSymbolDataExtensions
    { 
        public static double[,] FormReturnsMatrix(this Dictionary<Symbol, ReturnsSymbolData> symbolData, IEnumerable<Symbol> symbols)
        {
            var returnsByDate = from s in symbols join sd in symbolData on s equals sd.Key select sd.Value.Returns;

            // Consolidate by date
            var alldates = returnsByDate.SelectMany(r => r.Keys).Distinct();
            var returns = Accord.Math.Matrix.Create(alldates
                .Select(d => returnsByDate.Select(s => s.GetValueOrDefault(d, Double.NaN)).ToArray())
                .Where(r => !r.Select(v => Math.Abs(v)).Sum().IsNaNOrZero()) // remove empty rows
                .ToArray());

            return returns;
        }
    }
}
