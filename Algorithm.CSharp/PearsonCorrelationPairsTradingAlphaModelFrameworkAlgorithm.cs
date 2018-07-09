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
using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Data.UniverseSelection;
using MathNet.Numerics.Statistics;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Framework algorithm that uses the <see cref="PearsonCorrelationPairsTradingAlphaModel"/>.
    /// This model extendes <see cref="BasePairsTradingAlphaModel"/> and uses Pearson correlation
    /// to rank the pairs trading candidates and use the best candidate to trade.
    /// </summary>
    public class PearsonCorrelationPairsTradingAlphaModelFrameworkAlgorithm : QCAlgorithmFramework
    {
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            SetUniverseSelection(new ManualUniverseSelectionModel(
                QuantConnect.Symbol.Create("AIG", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("BAC", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("IBM", SecurityType.Equity, Market.USA),
                QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA)));

            SetAlpha(new PearsonCorrelationPairsTradingAlphaModel(365, TimeSpan.FromMinutes(15)));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new NullRiskManagementModel());
        }

        /// <summary>
        /// This alpha model is designed to rank every pair combination by its pearson correlation 
        /// and trade the pair with the hightest correlation
        /// This model generates alternating long ratio/short ratio insights emitted as a group
        /// </summary>
        private class PearsonCorrelationPairsTradingAlphaModel : BasePairsTradingAlphaModel
        {
            private readonly int _lookback;
            private Tuple<Symbol, Symbol> _bestPair;

            /// <summary>
            /// Initializes a new instance of the <see cref="PearsonCorrelationPairsTradingAlphaModel"/> class
            /// </summary>
            /// <param name="lookback">lookback period to evaluate the historical correlation</param>
            /// <param name="period">Period over which this insight is expected to come to fruition</param>
            /// <param name="threshold">The percent [0, 100] deviation of the ratio from the mean before emitting an insight</param>
            public PearsonCorrelationPairsTradingAlphaModel(int lookback, TimeSpan period, decimal threshold = 1m)
                : base(period, threshold)
            {
                _lookback = lookback;
            }

            public override void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
            {
                NotifiedSecurityChanges.UpdateCollection(Securities, changes);

                var symbols = Securities.Select(x => x.Symbol).ToArray();

                var history = algorithm.History(symbols, _lookback, Resolution.Daily)
                    .SelectMany(x => x.Bars.Values)
                    .GroupBy(x => x.Symbol)
                    .Select(x =>
                    {
                        var array = x.Select(b => (double)b.Close).ToArray();

                        for (var i = array.Length - 1; i > 0; i--)
                        {
                            array[i] = Math.Log(array[i]) - Math.Log(array[i - 1]);
                        }
                        array[0] = 0;

                        return array;
                    });

                var pearsonMatrix = Correlation.PearsonMatrix(history).UpperTriangle();

                var corr = new Dictionary<Tuple<Symbol, Symbol>, double>();
                var maxValue = pearsonMatrix.Enumerate().Where(x => Math.Abs(x) < 1).Max();
                var maxTuple = pearsonMatrix.Find(x => x == maxValue);

                _bestPair = Tuple.Create(symbols[maxTuple.Item1], symbols[maxTuple.Item2]);

                base.OnSecuritiesChanged(algorithm, changes);
            }

            public override bool HasPassedTest(QCAlgorithm algorithm, Symbol asset1, Symbol asset2)
            {
                return asset1 == _bestPair.Item1 && asset2 == _bestPair.Item2;
            }
        }
    }
}