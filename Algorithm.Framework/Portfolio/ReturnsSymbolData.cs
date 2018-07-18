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
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Indicators;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Contains returns specific to a symbol required for optimization model
    /// </summary>
    public class ReturnsSymbolData
    {
        public Symbol Symbol { get; }
        public RateOfChange ROC { get; }
        public IDataConsolidator Consolidator { get; }
        private RollingWindow<IndicatorDataPoint> Window { get; }

        public ReturnsSymbolData(QCAlgorithmFramework algorithm, Symbol symbol, int lookback, Resolution resolution)
        {
            Symbol = symbol;
            ROC = new RateOfChange($"{Symbol}.ROC(1)", 1);
            Window = new RollingWindow<IndicatorDataPoint>(lookback);

            Consolidator = algorithm.ResolveConsolidator(Symbol, resolution);
            algorithm.SubscriptionManager.AddConsolidator(Symbol, Consolidator);
            algorithm.RegisterIndicator(Symbol, ROC, Consolidator);

            ROC.Updated += (sender, updated) =>
            {
                if (ROC.IsReady)
                {
                    Window.Add(updated);
                }
            };
        }

        public void Add(DateTime time, decimal value)
        {
            var item = new IndicatorDataPoint(Symbol, time, value);
            Window.Add(item);
        }

        public Dictionary<DateTime, double> Returns => Window.Select(x => new { Date = x.EndTime, Return = (double)x.Value }).ToDictionary(r => r.Date, r => r.Return);

        public bool IsReady => Window.IsReady;

        public void RemoveConsolidators(QCAlgorithmFramework algorithm)
        {
            algorithm.SubscriptionManager.RemoveConsolidator(Symbol, Consolidator);
        }

        public void WarmUpIndicators(IEnumerable<Slice> history)
        {
            foreach (var slice in history)
            {
                var symbolData = slice[Symbol];
                var idp = new IndicatorDataPoint(Symbol, symbolData.EndTime, symbolData.Price);
                ROC.Update(idp);
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
