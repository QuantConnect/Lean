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

using QuantConnect.Algorithm.Framework;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp.Benchmarks
{
    public class EmaCrossUniverseSelectionFrameworkBenchmark : QCAlgorithmFramework
    {
        public override void Initialize()
        {
            SetStartDate(2013, 01, 01);
            SetEndDate(2015, 01, 01);
            SetCash(100000);

            UniverseSettings.Leverage = 2.0m;
            UniverseSettings.Resolution = Resolution.Daily;

            SetUniverseSelection(new EmaCrossUniverseSelectionModel(100, 300, 10));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, Resolution.Daily.ToTimeSpan()));
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel());
        }
    }
}