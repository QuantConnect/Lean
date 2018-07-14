using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Contains data specific to a symbol required by this model
    /// </summary>
    public class SymbolData
    {
        public Symbol Symbol { get; }
        public RateOfChange ROC { get; }
        public IDataConsolidator Consolidator { get; }
        public RollingWindow<IndicatorDataPoint> Window { get; }

        public SymbolData(QCAlgorithmFramework algorithm, Symbol symbol, int lookback, Resolution resolution)
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

        public Dictionary<DateTime, double> Return()
        {
            var returns = Window.Select(x => new Tuple<double, DateTime>(Math.Pow((1.0+(double)x.Value), 252)-1.0, x.EndTime));
            return returns.ToDictionary(r => r.Item2, r =>r.Item1);
        }

        public Dictionary<DateTime, double> Returns => Window.Select(x => new { Date = x.EndTime, Return = (double) x.Value}).ToDictionary(r=>r.Date, r=>r.Return);

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
}
