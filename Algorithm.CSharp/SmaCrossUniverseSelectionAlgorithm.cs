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

using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using System.Collections.Concurrent;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Provides an example where WarmUpIndicator method is used to warm up indicators
    /// after their security is added and before (Universe Selection scenario)
    /// </summary>
    public class SmaCrossUniverseSelectionAlgorithm : QCAlgorithm
    {
        private const int _count = 10;
        private const decimal _tolerance = 0.01m;
        private const decimal _targetPercent = 1m / _count;

        public override void Initialize()
        {
            UniverseSettings.Leverage = 2.0m;
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2018, 01, 01);
            SetEndDate(2019, 01, 01);
            SetCash(1000000);

            IsWarmUpIndicatorEnabled = true;

            var symbol = AddEquity("SPY", Resolution.Hour).Symbol;
            var ema = EMA(symbol, 10);
            WarmUpIndicator(symbol, ema);
            Log($"{ema.Name}: {ema.Current.Time} - {ema}. IsReady? {ema.IsReady}");

            AddUniverse(coarse =>
            {
                var averages = new ConcurrentDictionary<Symbol, IndicatorBase<IndicatorDataPoint>>();

                return (from cf in coarse
                        where cf.HasFundamentalData
                        // grab the SMA instance for this symbol
                        let avg = averages.GetOrAdd(cf.Symbol, sym => WarmUpIndicator(cf.Symbol, new SimpleMovingAverage(100), Resolution.Daily))
                        // Update returns true when the indicators are ready, so don't accept until they are
                        where avg.Update(cf.EndTime, cf.AdjustedPrice)
                        // only pick symbols who have their price over their 100 day sma
                        where avg > cf.AdjustedPrice * _tolerance
                        // prefer symbols with a larger delta by percentage between the two averages
                        orderby (avg - cf.AdjustedPrice) / ((avg + cf.AdjustedPrice) / 2m) descending
                        // we only need to return the symbol and return 'Count' symbols
                        select cf.Symbol).Take(_count);
            });

            // Since the SPY EMA is ready, we will receive error messages
            // reporting that the algorithm manager is trying to add old information
            SetWarmUp(10);
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            foreach (var security in changes.RemovedSecurities.Where(x => x.Invested))
            {
                Liquidate(security.Symbol);
            }

            foreach (var security in changes.AddedSecurities)
            {
                SetHoldings(security.Symbol, _targetPercent);
            }
        }
    }
}