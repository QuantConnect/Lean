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
 *
*/

using System.Linq;
using Moq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Tests.Algorithm.Framework.Risk
{
    [TestFixture]
    public class MaximumDrawdownPercentPerSecurityTests
    {
        [Test]
        [TestCase(Language.CSharp, 0.1, false, 0, 0, false)]
        [TestCase(Language.CSharp, 0.1, true, -50, 1000, false)]
        [TestCase(Language.CSharp, 0.1, true, -100, 1000, false)]
        [TestCase(Language.CSharp, 0.1, true, -150, 1000, true)]
        [TestCase(Language.Python, 0.1, false, 0, 0, false)]
        [TestCase(Language.Python, 0.1, true, -50, 1000, false)]
        [TestCase(Language.Python, 0.1, true, -100, 1000, false)]
        [TestCase(Language.Python, 0.1, true, -150, 1000, true)]
        public void ReturnsExpectedPortfolioTarget(
            Language language,
            decimal maxDrawdownPercent,
            bool invested,
            decimal unrealizedProfit,
            decimal absoluteHoldingsCost,
            bool shouldLiquidate)
        {
            var security = new Mock<Equity>(
                Symbols.AAPL,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache(),
                PrimaryExchange.UNKNOWN
            );
            security.Setup(m => m.Invested).Returns(invested);

            var holding = new Mock<EquityHolding>(security.Object,
                new IdentityCurrencyConverter(Currencies.USD));
            holding.Setup(m => m.UnrealizedProfit).Returns(unrealizedProfit);
            holding.Setup(m => m.AbsoluteHoldingsCost).Returns(absoluteHoldingsCost);

            security.Object.Holdings = holding.Object;

            var algorithm = new QCAlgorithm();
            algorithm.SetPandasConverter();
            algorithm.Securities.Add(Symbols.AAPL, security.Object);

            if (language == Language.Python)
            {
                using (Py.GIL())
                {
                    const string name = nameof(MaximumDrawdownPercentPerSecurity);
                    var instance = Py.Import(name).GetAttr(name).Invoke(maxDrawdownPercent.ToPython());
                    var model = new RiskManagementModelPythonWrapper(instance);
                    algorithm.SetRiskManagement(model);
                }
            }
            else
            {
                var model = new MaximumDrawdownPercentPerSecurity(maxDrawdownPercent);
                algorithm.SetRiskManagement(model);
            }

            var targets = algorithm.RiskManagement.ManageRisk(algorithm, null).ToList();

            if (shouldLiquidate)
            {
                Assert.AreEqual(1, targets.Count);
                Assert.AreEqual(Symbols.AAPL, targets[0].Symbol);
                Assert.AreEqual(0, targets[0].Quantity);
            }
            else
            {
                Assert.AreEqual(0, targets.Count);
            }
        }
    }
}
