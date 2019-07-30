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

        /// <summary>
        /// Initializes a new instance of the <see cref="ReturnsSymbolData"/> class
        /// </summary>
        /// <param name="symbol">The symbol of the data that updates the indicators</param>
        /// <param name="lookback">Look-back period for the RateOfChange indicator</param>
        /// <param name="period">Size of rolling window that contains historical RateOfChange</param>
        public ReturnsSymbolData(Symbol symbol, int lookback, int period)
        {
            _symbol = symbol;
            _roc = new RateOfChange($"{_symbol}.ROC({lookback})", lookback);
            _window = new RollingWindow<IndicatorDataPoint>(period);
            _roc.Updated += OnRateOfChangeUpdated;
        }

        /// <summary>
        /// Historical returns
        /// </summary>
        public Dictionary<DateTime, double> Returns => _window.ToDictionary(x => x.EndTime, x => (double) x.Value);

        /// <summary>
        /// Adds an item to this window and shifts all other elements
        /// </summary>
        /// <param name="time">The time associated with the value</param>
        /// <param name="value">The value to use to update this window</param>
        public void Add(DateTime time, decimal value)
        {
            if (_window.Samples > 0 && _window[0].EndTime == time)
            {
                return;
            }

            var item = new IndicatorDataPoint(_symbol, time, value);
            _window.Add(item);
        }

        /// <summary>
        /// Updates the state of the RateOfChange with the given value and returns true
        /// if this indicator is ready, false otherwise
        /// </summary>
        /// <param name="time">The time associated with the value</param>
        /// <param name="value">The value to use to update this indicator</param>
        /// <returns>True if this indicator is ready, false otherwise</returns>
        public bool Update(DateTime time, decimal value)
        {
            return _roc.Update(time, value);
        }

        /// <summary>
        /// Resets all indicators of this object to its initial state
        /// </summary>
        public void Reset()
        {
            _roc.Updated -= OnRateOfChangeUpdated;
            _roc.Reset();
            _window.Reset();
        }

        /// <summary>
        /// When the RateOfChange is updated, adds the new value to the RollingWindow
        /// </summary>
        /// <param name="roc"></param>
        /// <param name="updated"></param>
        private void OnRateOfChangeUpdated(object roc, IndicatorDataPoint updated)
        {
            if (_roc.IsReady)
            {
                _window.Add(updated);
            }
        }
    }

    /// <summary>
    /// Extension methods for <see cref="ReturnsSymbolData"/>
    /// </summary>
    public static class ReturnsSymbolDataExtensions
    {
        /// <summary>
        /// Converts a dictionary of <see cref="ReturnsSymbolData"/> keyed by <see cref="Symbol"/> into a matrix
        /// </summary>
        /// <param name="symbolData">Dictionary of <see cref="ReturnsSymbolData"/> keyed by <see cref="Symbol"/> to be converted into a matrix</param>
        /// <param name="symbols">List of <see cref="Symbol"/> to be included in the matrix</param>
        public static double[,] FormReturnsMatrix(this Dictionary<Symbol, ReturnsSymbolData> symbolData, IEnumerable<Symbol> symbols)
        {
            var returnsByDate = from s in symbols join sd in symbolData on s equals sd.Key select sd.Value.Returns;

            // Consolidate by date
            var alldates = returnsByDate.SelectMany(r => r.Keys).Distinct();
            var returns = Accord.Math.Matrix.Create(alldates
                .Select(d => returnsByDate.Select(s => s.GetValueOrDefault(d, double.NaN)).ToArray())
                .Where(r => !r.Select(Math.Abs).Sum().IsNaNOrZero()) // remove empty rows
                .ToArray());

            return returns;
        }
    }
}