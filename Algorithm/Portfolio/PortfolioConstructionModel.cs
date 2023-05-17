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
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Scheduling;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides a base class for portfolio construction models
    /// </summary>
    public class PortfolioConstructionModel : IPortfolioConstructionModel
    {
        private Func<DateTime, DateTime?> _rebalancingFunc;
        private DateTime? _rebalancingTime;
        private bool _securityChanges;

        /// <summary>
        /// True if should rebalance portfolio on security changes. True by default
        /// </summary>
        public virtual bool RebalanceOnSecurityChanges { get; set; } = true;

        /// <summary>
        /// True if should rebalance portfolio on new insights or expiration of insights. True by default
        /// </summary>
        public virtual bool RebalanceOnInsightChanges { get; set; } = true;
        
        /// <summary>
        /// The algorithm instance
        /// </summary>
        protected IAlgorithm Algorithm { get; private set; }

        /// <summary>
        /// This is required due to a limitation in PythonNet to resolved overriden methods.
        /// When Python calls a C# method that calls a method that's overriden in python it won't
        /// run the python implementation unless the call is performed through python too.
        /// </summary>
        protected PortfolioConstructionModelPythonWrapper PythonWrapper;

        /// <summary>
        /// Initialize a new instance of <see cref="PortfolioConstructionModel"/>
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance. If null will be ignored</param>
        public PortfolioConstructionModel(Func<DateTime, DateTime?> rebalancingFunc)
        {
            _rebalancingFunc = rebalancingFunc;
        }

        /// <summary>
        /// Initialize a new instance of <see cref="PortfolioConstructionModel"/>
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance UTC time.
        /// Returning current time will trigger rebalance. If null will be ignored</param>
        public PortfolioConstructionModel(Func<DateTime, DateTime> rebalancingFunc = null)
        : this(rebalancingFunc != null ? (Func<DateTime, DateTime?>)(timeUtc => rebalancingFunc(timeUtc)) : null)
        {
        }

        /// <summary>
        /// Used to set the <see cref="PortfolioConstructionModelPythonWrapper"/> instance if any
        /// </summary>
        protected void SetPythonWrapper(PortfolioConstructionModelPythonWrapper pythonWrapper)
        {
            PythonWrapper = pythonWrapper;
        }

        /// <summary>
        /// Create portfolio targets from the specified insights
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="insights">The insights to create portfolio targets from</param>
        /// <returns>An enumerable of portfolio targets to be sent to the execution model</returns>
        public virtual IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            Algorithm = algorithm;
            
            if (!(PythonWrapper?.IsRebalanceDue(insights, algorithm.UtcTime)
                  ?? IsRebalanceDue(insights, algorithm.UtcTime)))
            {
                return Enumerable.Empty<IPortfolioTarget>();
            }

            var targets = new List<IPortfolioTarget>();

            var lastActiveInsights = PythonWrapper?.GetTargetInsights()
                                                 ?? GetTargetInsights();

            var errorSymbols = new HashSet<Symbol>();

            // Determine target percent for the given insights
            var percents = PythonWrapper?.DetermineTargetPercent(lastActiveInsights)
                           ?? DetermineTargetPercent(lastActiveInsights);

            foreach (var insight in lastActiveInsights)
            {
                if (!percents.TryGetValue(insight, out var percent))
                {
                    continue;
                }

                var target = PortfolioTarget.Percent(algorithm, insight.Symbol, percent);
                if (target != null)
                {
                    targets.Add(target);
                }
                else
                {
                    errorSymbols.Add(insight.Symbol);
                }
            }

            // Get expired insights and create flatten targets for each symbol
            var expiredInsights = Algorithm.Insights.RemoveExpiredInsights(algorithm.UtcTime);

            var expiredTargets = from insight in expiredInsights
                                                          group insight.Symbol by insight.Symbol into g
                                                          where !Algorithm.Insights.HasActiveInsights(g.Key, algorithm.UtcTime) && !errorSymbols.Contains(g.Key)
                                                          select new PortfolioTarget(g.Key, 0);

            targets.AddRange(expiredTargets);

            return targets;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public virtual void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            Algorithm ??= algorithm;

            _securityChanges = changes != SecurityChanges.None;
            // Get removed symbol and invalidate them in the insight collection
            var removedSymbols = changes.RemovedSecurities.Select(x => x.Symbol);
            algorithm?.Insights.Expire(removedSymbols);
        }

        /// <summary>
        /// Gets the target insights to calculate a portfolio target percent for
        /// </summary>
        /// <returns>An enumerable of the target insights</returns>
        protected virtual List<Insight> GetTargetInsights()
        {
            // Validate we should create a target for this insight
            bool IsValidInsight(Insight insight) => PythonWrapper?.ShouldCreateTargetForInsight(insight)
                ?? ShouldCreateTargetForInsight(insight);

            // Get insight that haven't expired of each symbol that is still in the universe
            var activeInsights = Algorithm.Insights.GetActiveInsights(Algorithm.UtcTime).Where(IsValidInsight);
            
            // Get the last generated active insight for each symbol
            return (from insight in activeInsights
                group insight by insight.Symbol into g
                select g.OrderBy(x => x.GeneratedTimeUtc).Last()).ToList();
        }

        /// <summary>
        /// Method that will determine if the portfolio construction model should create a
        /// target for this insight
        /// </summary>
        /// <param name="insight">The insight to create a target for</param>
        /// <returns>True if the portfolio should create a target for the insight</returns>
        protected virtual bool ShouldCreateTargetForInsight(Insight insight)
        {
            return true;
        }

        /// <summary>
        /// Will determine the target percent for each insight
        /// </summary>
        /// <param name="activeInsights">The active insights to generate a target for</param>
        /// <returns>A target percent for each insight</returns>
        protected virtual Dictionary<Insight, double> DetermineTargetPercent(List<Insight> activeInsights)
        {
            throw new NotImplementedException("Types deriving from 'PortfolioConstructionModel' must implement the 'Dictionary<Insight, double> DetermineTargetPercent(ICollection<Insight>)' method.");
        }

        /// <summary>
        /// Python helper method to set the rebalancing function.
        /// This is required due to a python net limitation not being able to use the base type constructor, and also because
        /// when python algorithms use C# portfolio construction models, it can't convert python methods into func nor resolve
        /// the correct constructor for the date rules, timespan parameter.
        /// For performance we prefer python algorithms using the C# implementation
        /// </summary>
        /// <param name="rebalance">Rebalancing func or if a date rule, timedelta will be converted into func.
        /// For a given algorithm UTC DateTime the func returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance. If null will be ignored</param>
        protected void SetRebalancingFunc(PyObject rebalance)
        {
            IDateRule dateRules;
            TimeSpan timeSpan;
            if (rebalance.TryConvert(out dateRules))
            {
                _rebalancingFunc = dateRules.ToFunc();
            }
            else if (!rebalance.TryConvertToDelegate(out _rebalancingFunc))
            {
                try
                {
                    using (Py.GIL())
                    {
                        // try convert does not work for timespan
                        timeSpan = rebalance.As<TimeSpan>();
                        if (timeSpan != default(TimeSpan))
                        {
                            _rebalancingFunc = time => time.Add(timeSpan);
                        }
                    }
                }
                catch
                {
                    _rebalancingFunc = null;
                }
            }
        }

        /// <summary>
        /// Determines if the portfolio should be rebalanced base on the provided rebalancing func,
        /// if any security change have been taken place or if an insight has expired or a new insight arrived
        /// If the rebalancing function has not been provided will return true.
        /// </summary>
        /// <param name="insights">The insights to create portfolio targets from</param>
        /// <param name="algorithmUtc">The current algorithm UTC time</param>
        /// <returns>True if should rebalance</returns>
        protected virtual bool IsRebalanceDue(Insight[] insights, DateTime algorithmUtc)
        {
            // if there is no rebalance func set, just return true but refresh state
            // just in case the rebalance func is going to be set.
            if (_rebalancingFunc == null)
            {
                RefreshRebalance(algorithmUtc);
                return true;
            }

            // we always get the next expiry time
            // we don't know if a new insight was added or removed
            var nextInsightExpiryTime = Algorithm.Insights.GetNextExpiryTime();

            if (_rebalancingTime == null)
            {
                _rebalancingTime = _rebalancingFunc(algorithmUtc);

                if (_rebalancingTime != null && _rebalancingTime <= algorithmUtc)
                {
                    // if the rebalancing time stopped being null and is current time
                    // we will ask for the next rebalance time in the next loop.
                    // we don't want to call the '_rebalancingFunc' twice in the same loop,
                    // since its internal state machine will probably be in the same state.
                    _rebalancingTime = null;
                    _securityChanges = false;
                    return true;
                }
            }

            if (_rebalancingTime != null && _rebalancingTime <= algorithmUtc
                || RebalanceOnSecurityChanges && _securityChanges
                || RebalanceOnInsightChanges
                    && (insights.Length != 0
                        || nextInsightExpiryTime != null && nextInsightExpiryTime < algorithmUtc))
            {
                RefreshRebalance(algorithmUtc);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Refresh the next rebalance time and clears the security changes flag
        /// </summary>
        protected void RefreshRebalance(DateTime algorithmUtc)
        {
            if (_rebalancingFunc != null)
            {
                _rebalancingTime = _rebalancingFunc(algorithmUtc);
            }
            _securityChanges = false;
        }

        /// <summary>
        /// Helper class that can be used by the different <see cref="IPortfolioConstructionModel"/>
        /// implementations to filter <see cref="Insight"/> instances with an invalid
        /// <see cref="Insight.Magnitude"/> value based on the <see cref="IAlgorithmSettings"/>
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="insights">The insight collection to filter</param>
        /// <returns>Returns a new array of insights removing invalid ones</returns>
        protected static Insight[] FilterInvalidInsightMagnitude(IAlgorithm algorithm, Insight[] insights)
        {
            var result = insights.Where(insight =>
            {
                if (!insight.Magnitude.HasValue || insight.Magnitude == 0)
                {
                    return true;
                }

                var absoluteMagnitude = Math.Abs(insight.Magnitude.Value);
                if (absoluteMagnitude > (double)algorithm.Settings.MaxAbsolutePortfolioTargetPercentage
                    || absoluteMagnitude < (double)algorithm.Settings.MinAbsolutePortfolioTargetPercentage)
                {
                    algorithm.Error("PortfolioConstructionModel.FilterInvalidInsightMagnitude():" +
                        $"The insight target Magnitude: {insight.Magnitude}, will not comply with the current " +
                        $"'Algorithm.Settings' 'MaxAbsolutePortfolioTargetPercentage': {algorithm.Settings.MaxAbsolutePortfolioTargetPercentage}" +
                        $" or 'MinAbsolutePortfolioTargetPercentage': {algorithm.Settings.MinAbsolutePortfolioTargetPercentage}. Skipping insight."
                    );
                    return false;
                }

                return true;
            });
            return result.ToArray();
        }
    }
}
