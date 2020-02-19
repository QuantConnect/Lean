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
using QuantConnect.Scheduling;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of <see cref="IPortfolioConstructionModel"/> that gives equal weighting to all
    /// securities. The target percent holdings of each security is 1/N where N is the number of securities. For
    /// insights of direction <see cref="InsightDirection.Up"/>, long targets are returned and for insights of direction
    /// <see cref="InsightDirection.Down"/>, short targets are returned.
    /// </summary>
    public class EqualWeightingPortfolioConstructionModel : PortfolioConstructionModel
    {
        /// <summary>
        /// Initialize a new instance of <see cref="EqualWeightingPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="rebalancingDateRules">The date rules used to define the next expected rebalance time
        /// in UTC</param>
        public EqualWeightingPortfolioConstructionModel(IDateRule rebalancingDateRules)
            : this(rebalancingDateRules.ToFunc())
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="EqualWeightingPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance. If null will be ignored</param>
        public EqualWeightingPortfolioConstructionModel(Func<DateTime, DateTime?> rebalancingFunc)
            : base(rebalancingFunc)
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="EqualWeightingPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="rebalancingFunc">For a given algorithm UTC DateTime returns the next expected rebalance UTC time.
        /// Returning current time will trigger rebalance. If null will be ignored</param>
        public EqualWeightingPortfolioConstructionModel(Func<DateTime, DateTime> rebalancingFunc)
            : this(rebalancingFunc != null ? (Func<DateTime, DateTime?>)(timeUtc => rebalancingFunc(timeUtc)) : null)
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="EqualWeightingPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="rebalancingParam">Rebalancing func or if a date rule, timedelta will be converted into func.
        /// For a given algorithm UTC DateTime the func returns the next expected rebalance time
        /// or null if unknown, in which case the function will be called again in the next loop. Returning current time
        /// will trigger rebalance. If null will be ignored</param>
        /// <remarks>This is required since python net can not convert python methods into func nor resolve the correct
        /// constructor for the date rules parameter.
        /// For performance we prefer python algorithms using the C# implementation</remarks>
        public EqualWeightingPortfolioConstructionModel(PyObject rebalancingParam)
            : this((Func<DateTime, DateTime?>)null)
        {
            SetRebalancingFunc(rebalancingParam);
        }

        /// <summary>
        /// Initialize a new instance of <see cref="EqualWeightingPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="timeSpan">Rebalancing frequency</param>
        public EqualWeightingPortfolioConstructionModel(TimeSpan timeSpan)
            : this(dt => dt.Add(timeSpan))
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="EqualWeightingPortfolioConstructionModel"/>
        /// </summary>
        /// <param name="resolution">Rebalancing frequency</param>
        public EqualWeightingPortfolioConstructionModel(Resolution resolution = Resolution.Daily)
            : this(resolution.ToTimeSpan())
        {
        }

        /// <summary>
        /// Will determine the target percent for each insight
        /// </summary>
        /// <param name="activeInsights">The active insights to generate a target for</param>
        /// <returns>A target percent for each insight</returns>
        protected override Dictionary<Insight, double> DetermineTargetPercent(ICollection<Insight> activeInsights)
        {
            var result = new Dictionary<Insight, double>();

            // give equal weighting to each security
            var count = activeInsights.Count(x => x.Direction != InsightDirection.Flat);
            var percent = count == 0 ? 0 : 1m / count;
            foreach (var insight in activeInsights)
            {
                result[insight] = (double)((int)insight.Direction * percent);
            }
            return result;
        }
    }
}