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
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;

namespace QuantConnect.Tests.Algorithm.Framework.Risk
{
    [TestFixture]
    public class MaximumDrawdownPercentPortfolioTests
    {
        [Test]
        [TestCase(Language.CSharp, false, 0, false)]
        [TestCase(Language.CSharp, true, -1000, false)]
        [TestCase(Language.CSharp, true, -10000, false)]
        [TestCase(Language.CSharp, true, -10001, true)]
        [TestCase(Language.Python, false, 0, false)]
        [TestCase(Language.Python, true, -1000, false)]
        [TestCase(Language.Python, true, -10000, false)]
        [TestCase(Language.Python, true, -10001, true)]
        public void ReturnsExpectedPortfolioTarget(
            Language language,
            bool invested,
            decimal absoluteHoldingsCost,
            bool shouldLiquidate
        )
        {
            var algorithm = CreateAlgorithm(language, 0.1m);
            var targets = algorithm
                .RiskManagement.ManageRisk(
                    algorithm,
                    new PortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, 10) }
                )
                .ToList();
            Assert.AreEqual(0, targets.Count);

            algorithm.Securities.Add(
                Symbols.AAPL,
                GetSecurity(Symbols.AAPL, invested, absoluteHoldingsCost)
            );
            algorithm.Portfolio.InvalidateTotalPortfolioValue();
            targets = algorithm
                .RiskManagement.ManageRisk(
                    algorithm,
                    new PortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, 10) }
                )
                .ToList();

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

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void ReturnsExpectedPortfolioTargetsAfterReset(Language language)
        {
            var algorithm = CreateAlgorithm(language, 0.1m);
            var targets = algorithm
                .RiskManagement.ManageRisk(
                    algorithm,
                    new PortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, 10) }
                )
                .ToList();
            algorithm.Securities.Add(Symbols.AAPL, GetSecurity(Symbols.AAPL, true, -10001));
            algorithm.Portfolio.InvalidateTotalPortfolioValue();
            targets = algorithm
                .RiskManagement.ManageRisk(
                    algorithm,
                    new PortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, 10) }
                )
                .ToList();

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(Symbols.AAPL, targets[0].Symbol);
            Assert.AreEqual(0, targets[0].Quantity);

            algorithm.Securities.Add(Symbols.AAPL, GetSecurity(Symbols.AAPL, true, 10001));
            targets = algorithm
                .RiskManagement.ManageRisk(
                    algorithm,
                    new PortfolioTarget[] { new PortfolioTarget(Symbols.AAPL, 10) }
                )
                .ToList();
            Assert.AreEqual(0, targets.Count);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Python)]
        public void ReturnsMoreThanOnePortfolioTarget(Language language)
        {
            var targetSymbols = new PortfolioTarget[]
            {
                new PortfolioTarget(Symbols.AAPL, 10),
                new PortfolioTarget(Symbols.SPY, 100),
                new PortfolioTarget(Symbols.MSFT, 1000),
                new PortfolioTarget(Symbols.GOOG, 10000),
                new PortfolioTarget(Symbols.IBM, 100000)
            };

            var algorithm = CreateAlgorithm(language, 0.1m);
            var returnedTargets = algorithm
                .RiskManagement.ManageRisk(algorithm, targetSymbols)
                .ToList();

            targetSymbols
                .ToList()
                .ForEach(x =>
                    algorithm.Securities.Add(x.Symbol, GetSecurity(x.Symbol, true, -x.Quantity))
                );
            algorithm.Portfolio.InvalidateTotalPortfolioValue();
            returnedTargets = algorithm
                .RiskManagement.ManageRisk(algorithm, targetSymbols)
                .ToList();

            Assert.AreEqual(targetSymbols.Length, returnedTargets.Count);
            Assert.AreEqual(
                targetSymbols.Select(x => x.Symbol),
                returnedTargets.Select(x => x.Symbol)
            );
            Assert.IsTrue(returnedTargets.All(x => x.Quantity == 0));
        }

        private QCAlgorithm CreateAlgorithm(Language language, decimal maxDrawdownPercent)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetPandasConverter();

            if (language == Language.Python)
            {
                using (Py.GIL())
                {
                    const string name = nameof(MaximumDrawdownPercentPortfolio);
                    var instance = Py.Import(name)
                        .GetAttr(name)
                        .Invoke(maxDrawdownPercent.ToPython());
                    var model = new RiskManagementModelPythonWrapper(instance);
                    algorithm.SetRiskManagement(model);
                }
            }
            else
            {
                var model = new MaximumDrawdownPercentPortfolio(maxDrawdownPercent);
                algorithm.SetRiskManagement(model);
            }

            return algorithm;
        }

        private Security GetSecurity(Symbol symbol, bool invested, decimal absoluteHoldingsCost)
        {
            // Add security
            var security = new Mock<Equity>(
                symbol,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 0, 1),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache(),
                Exchange.UNKNOWN
            );
            var holding = new Mock<EquityHolding>(
                security.Object,
                new IdentityCurrencyConverter(Currencies.USD)
            );
            holding.Setup(m => m.Invested).Returns(invested);
            holding.Setup(m => m.HoldingsValue).Returns(absoluteHoldingsCost);

            security.Object.Holdings = holding.Object;
            return security.Object;
        }
    }
}
