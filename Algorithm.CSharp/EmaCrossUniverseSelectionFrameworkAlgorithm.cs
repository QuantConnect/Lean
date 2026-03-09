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

using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Framework algorithm that uses the <see cref="EmaCrossUniverseSelectionModel"/> to
    /// select the universe based on a moving average cross.
    /// </summary>
    public class EmaCrossUniverseSelectionFrameworkAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2013, 01, 01);
            SetEndDate(2015, 01, 01);
            SetCash(100000);

            var fastPeriod = 100;
            var slowPeriod = 300;
            var count = 10;

            UniverseSettings.Leverage = 2.0m;
            UniverseSettings.Resolution = Resolution.Daily;

            SetUniverseSelection(new EmaCrossUniverseSelectionModel(fastPeriod, slowPeriod, count));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, Resolution.Daily.ToTimeSpan()));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
        }
    }
}