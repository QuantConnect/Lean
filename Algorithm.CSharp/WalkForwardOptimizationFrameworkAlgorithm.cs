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
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;

namespace QuantConnect.Algorithm.CSharp
{
    public class WalkForwardOptimizationFrameworkAlgorithm : QCAlgorithm
    {
        private IReadOnlyList<Symbol> Symbols = new[] {"AIG", "BAC", "IBM", "SPY"}
            .Select(s => QuantConnect.Symbol.Create(s, SecurityType.Equity, Market.USA))
            .ToArray();

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetStartDate(2013, 10, 11);

            SetUniverseSelection(new CoarseFundamentalUniverseSelectionModel(coarse => Symbols));
            SetAlpha(new HistoricalReturnsAlphaModel(120, Resolution.Second));
            SetPortfolioConstruction(new WalkForwardOptimizationPortfolioConstructionModel(
                60, Resolution.Second,
                am => new EqualWeightingPortfolioConstructionModel(Resolution.Hour)
            ));
            SetRiskManagement(new NullRiskManagementModel());
            SetExecution(new ImmediateExecutionModel());
        }
    }
}
