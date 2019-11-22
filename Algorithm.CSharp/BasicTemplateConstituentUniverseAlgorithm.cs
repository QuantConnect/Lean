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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm which showcases <see cref="ConstituentsUniverse"/> simple use case
    /// </summary>
    public class BasicTemplateConstituentUniverseAlgorithm : QCAlgorithm
    {
        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);

            // by default will use algorithms UniverseSettings
            AddUniverse(Universe.Constituent.Steel());

            // we specify the UniverseSettings it should use
            AddUniverse(Universe.Constituent.AggressiveGrowth(
                new UniverseSettings(Resolution.Hour,
                    2,
                    false,
                    false,
                    UniverseSettings.MinimumTimeInUniverse)));

            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromDays(1)));
            SetExecution(new ImmediateExecutionModel());
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
        }
    }
}