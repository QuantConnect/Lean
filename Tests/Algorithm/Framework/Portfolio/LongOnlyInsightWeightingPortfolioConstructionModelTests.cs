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

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Portfolio;
using System.Collections.Generic;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class LongOnlyInsightWeightingPortfolioConstructionModelTests : InsightWeightingPortfolioConstructionModelTests
    {
        public override PortfolioBias PortfolioBias => PortfolioBias.Long;

        public override IPortfolioConstructionModel GetPortfolioConstructionModel(Language language, dynamic paramenter = null)
        {
            if (language == Language.CSharp)
            {
                return new InsightWeightingPortfolioConstructionModel(paramenter, PortfolioBias.Long);
            }

            using (Py.GIL())
            {
                const string name = nameof(InsightWeightingPortfolioConstructionModel);
                var instance = Py.Import(name).GetAttr(name).Invoke(((object)paramenter).ToPython(), ((int)PortfolioBias.Long).ToPython());
                return new PortfolioConstructionModelPythonWrapper(instance);
            }
        }

        public override List<IPortfolioTarget> GetTargetsForSPY()
        {
            return new List<IPortfolioTarget> { PortfolioTarget.Percent(Algorithm, Symbols.SPY, 0m) };
        }
    }
}