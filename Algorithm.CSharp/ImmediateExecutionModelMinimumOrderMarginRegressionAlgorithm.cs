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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm that will test that <see cref="IAlgorithmSettings.MinimumOrderMarginPortfolioPercentage"/>
    /// is respected by the <see cref="ImmediateExecutionModel"/>
    /// </summary>
    public class ImmediateExecutionModelMinimumOrderMarginRegressionAlgorithm
        : BasicTemplateFrameworkAlgorithm
    {
        public override void Initialize()
        {
            base.Initialize();
            // this setting is the difference between doing 3 trades and > 60
            Settings.MinimumOrderMarginPortfolioPercentage = 0.001m;
            SetPortfolioConstruction(new CustomPortfolioConstructionModel(TimeKeeper));
        }

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        private class CustomPortfolioConstructionModel : EqualWeightingPortfolioConstructionModel
        {
            private ITimeKeeper _timeKeeper;

            public CustomPortfolioConstructionModel(ITimeKeeper timeKeeper)
            {
                _timeKeeper = timeKeeper;
            }

            protected override Dictionary<Insight, double> DetermineTargetPercent(
                List<Insight> activeInsights
            )
            {
                var baseResult = base.DetermineTargetPercent(activeInsights);

                // we generate some fake noise in the percentage allocation
                var adjustPercentage = _timeKeeper.UtcTime.Minute % 2 == 0;
                return baseResult.ToDictionary(
                    pair => pair.Key,
                    pair => adjustPercentage ? pair.Value - 0.001 : pair.Value
                );
            }
        }
    }
}
