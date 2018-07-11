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
using Accord.Statistics;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of Mean-Variance portfolio optimization based on modern portfolio theory.
    /// The interval of weights in optimization method can be changed based on the long-short algorithm.
    /// The default model uses the last three months daily price to calculate the optimal weight
    /// with the weight range from -1 to 1 and minimize the portfolio variance with a target return of 2%
    /// </summary>
    public class MeanVarianceOptimizationPortfolioConstructionModel : PortfolioConstructionModel
    {
        private readonly int _lookback = 1;
        private readonly int _period = 63;
        private readonly Resolution _resolution = Resolution.Daily;
        private readonly double _minimumWeight = -1;
        private readonly double _maximumWeight = 1;
        private readonly double _targetReturn = 0.02;
        
        private List<Symbol> _pendingRemoval = new List<Symbol>();
        private readonly Dictionary<Symbol, SymbolData> _symbolDataDict = new Dictionary<Symbol, SymbolData>();

        /// <summary>
        /// Initialize the model
        /// </summary>
        /// <param name="lookback">Historical return lookback period</param>
        /// <param name="resolution">The resolution of the history price</param>
        /// <param name="period">The time interval of history price to calculate the weight</param>
        /// <param name="minimumWeight">The lower bounds on portfolio weights</param>
        /// <param name="maximumWeight">The upper bounds on portfolio weights</param>
        /// <param name="targetReturn"> The target portfolio return</param>
        public MeanVarianceOptimizationPortfolioConstructionModel(
            int lookback = 1,
            int period = 63,
            Resolution resolution = Resolution.Daily,
            double minimumWeight = -1,
            double maximumWeight = 1,
            double targetReturn = 0.02
            )
        {
            _lookback = lookback;
            _period = period;
            _resolution = resolution;
            _minimumWeight = minimumWeight;
            _maximumWeight = maximumWeight;
            _targetReturn = targetReturn;
        }

        /// <summary>
        /// Create portfolio targets from the specified insights
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="insights">The insights to create portoflio targets from</param>
        /// <returns>An enumerable of portoflio targets to be sent to the execution model</returns>
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithmFramework algorithm, Insight[] insights)
        {
            var targets = new List<IPortfolioTarget>();

            foreach (var symbol in _pendingRemoval)
            {
                targets.Add(new PortfolioTarget(symbol, 0));
            }
            _pendingRemoval.Clear();

            var symbols = insights.Select(x => x.Symbol).Distinct();
            if (symbols.Count() == 0)
            {
                return Enumerable.Empty<IPortfolioTarget>();
            }

            foreach (var insight in insights)
            {
                SymbolData data;
                if (_symbolDataDict.TryGetValue(insight.Symbol, out data))
                {
                    if (!insight.Magnitude.HasValue)
                    {
                        algorithm.SetRunTimeError(new ArgumentNullException("MeanVarianceOptimizationPortfolioConstructionModel does not accept 'null' as Insight.Magnitude. Please checkout the selected Alpha Model specifications."));
                    }
                    data.Add(algorithm.Time, (decimal)insight.Magnitude.Value);
                }
            }

            // Get symbols' returns by date
            var returnsByDate = (from x in _symbolDataDict
                          where symbols.Contains(x.Key)
                          select new { Symbol = x.Key, Returns = x.Value.Return() }).ToDictionary(r => r.Symbol, r =>r.Returns);

            // Consolidate by date
            var alldates = returnsByDate.SelectMany(r => r.Value.Keys).Distinct();
            var returns = new Dictionary<Symbol, List<double>>();
            foreach (var symbol in returnsByDate.Keys)
                returns[symbol] = new List<double>();

            foreach (var d in alldates)
            {
                foreach (var s in symbols)
                {
                    double v;
                    returnsByDate[s].TryGetValue(d, out v);
                    returns[s].Add(v);
                }
            }
            
            // The optimization method processes the data frame
            var weights = MinimumVariance(returns.Values, symbols);
            algorithm.Log(" ### [" + string.Join(",", weights.Keys)+ "] = ["+ string.Join(",", weights.Values) + "]");

            // Create portfolio targets from the specified insights
            foreach (var insight in insights)
            {
                var weight = (decimal)weights[insight.Symbol];
                targets.Add(PortfolioTarget.Percent(algorithm, insight.Symbol, weight));
            }

            return targets;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithmFramework algorithm, SecurityChanges changes)
        {
            // clean up data for removed securities
            foreach (var removed in changes.RemovedSecurities)
            {
                _pendingRemoval.Add(removed.Symbol);
                SymbolData data;
                if (_symbolDataDict.TryGetValue(removed.Symbol, out data))
                {
                    _symbolDataDict.Remove(removed.Symbol);
                    data.RemoveConsolidators(algorithm);
                }
            }

            // initialize data for added securities
            var addedSymbols = changes.AddedSecurities.Select(s => s.Symbol);
            var history = algorithm.History(addedSymbols, _lookback * _period, _resolution);
            if (history.Count() == 0)
                return;

            foreach (var symbol in addedSymbols)
            {
                if (!_symbolDataDict.ContainsKey(symbol))
                {
                    var symbolData = new SymbolData(algorithm, symbol, _lookback * _period, _resolution);
                    symbolData.WarmUpIndicators(history);
                    _symbolDataDict[symbol] = symbolData;
                }
            }            
        }

        /// <summary>
        /// Perform mean variance optimization given the returns
        /// </summary>
        /// <param name="returns">Collections of returns by symbols</param>
        /// <param name="symbols">Collection of symbols</param>
        /// <returns></returns>
        public Dictionary<Symbol, double> MinimumVariance(IEnumerable<IEnumerable<double>> returns, IEnumerable<Symbol> symbols)
        {
            var weights = new Dictionary<Symbol, double>();
            var means = returns.Select(r => Measures.Mean(r.ToArray())).ToList();
            var size = means.Count;
            var data = Accord.Math.Matrix.Create(returns.Select(r => r.ToArray()).ToArray());
            var cov = Measures.Covariance(Accord.Math.Matrix.Transpose(data), means.ToArray());

            // initial point
            var x0 = new double[size];
            for (int i = 0; i < size; i++) { x0[i] = 1.0 / size; }
            // lower boundaries
            var bndl = new double[size];
            for (int i = 0; i < size; i++) { bndl[i] = _minimumWeight; }
            // upper boundaries
            var bndu = new double[size];
            for (int i = 0; i < size; i++) { bndu[i] = _maximumWeight; }
            // scale
            var s = new double[size];
            for (int i = 0; i < size; i++) { s[i] = 1.0; }
            // covariance
            double[,] a = cov;
            //double[] b = new double[size];

            alglib.minqpstate state;
            alglib.minqpreport rep;

            // create solver, set quadratic/linear terms
            alglib.minqpcreate(size, out state);
            alglib.minqpsetquadraticterm(state, a);
            //alglib.minqpsetlinearterm(state, b);
            alglib.minqpsetstartingpoint(state, x0);
            // set scale
            alglib.minqpsetscale(state, s);
            // upper and lower bounds
            alglib.minqpsetbc(state, bndl, bndu);

            // c1: sum(x) = 1
            // c2: R^T * x = mu
            means.Add(_targetReturn);
            var c1 = new double[means.Count];
            for (int i = 0; i < means.Count; i++) { c1[i] = 1.0; }
            var c2 = means.ToArray();
            var C =  Accord.Math.Matrix.Create(new double[][] { c1, c2 });
            int[] ct = new int[] { 0, 0 };
            alglib.minqpsetlc(state, C, ct);

            // Solve problem
            //if (size > 50)
            //{
            //    alglib.minqpsetalgodenseaul(state, 1.0e-9, 1.0e+4, 5);
            //}
            //else
            {
                alglib.minqpsetalgobleic(state, 0.0, 0.0, 0.0, 0);
            }
            alglib.minqpoptimize(state);

            // Get results
            double[] x;
            alglib.minqpresults(state, out x, out rep);
            
            // Solver succesfully
            if (rep.terminationtype > 0)
            {                
                foreach (var kv in symbols.Zip(x, (sym, w) => Tuple.Create(sym, w)))
                {
                    weights[kv.Item1] = kv.Item2;
                }
            }
            return weights;
        }
    }
}