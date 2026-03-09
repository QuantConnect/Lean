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

using System;
using System.Linq;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Data.Market;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Tests.Algorithm.Framework.Risk
{
    [TestFixture]
    public class TrailingStopRiskManagementModelTests
    {
        [Test, TestCaseSource(nameof(GenerateTestData))]
        public void ReturnsExpectedPortfolioTarget(
            TrailingStopRiskManagementModelTestParameters parameters)
        {
            var decimalPrices = System.Array.ConvertAll(parameters.Prices, x => (decimal) x);

            var security = new Mock<Security>(
                Symbols.AAPL,
                SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork),
                new Cash(Currencies.USD, 1000m, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );
            security.CallBase = true;
            security.Object.FeeModel = new ConstantFeeModel(0);

            var holding = new SecurityHolding(security.Object, new IdentityCurrencyConverter(Currencies.USD));
            holding.SetHoldings(parameters.InitialPrice, parameters.Quantity);
            security.Object.Holdings = holding;

            var algorithm = new QCAlgorithm();
            algorithm.SetPandasConverter();
            algorithm.Securities.Add(Symbols.AAPL, security.Object);

            if (parameters.Language == Language.Python)
            {
                using (Py.GIL())
                {
                    const string name = nameof(TrailingStopRiskManagementModel);
                    var instance = Py.Import(name).GetAttr(name).Invoke(parameters.MaxDrawdownPercent.ToPython());
                    var model = new RiskManagementModelPythonWrapper(instance);
                    algorithm.SetRiskManagement(model);
                }
            }
            else
            {
                var model = new TrailingStopRiskManagementModel(parameters.MaxDrawdownPercent);
                algorithm.SetRiskManagement(model);
            }

            var quantity = parameters.Quantity;

            for (int i = 0; i < decimalPrices.Length; i++)
            {
                var price = decimalPrices[i];
                security.Object.SetMarketPrice(new Tick(DateTime.Now, security.Object.Symbol, price, price));
                security.Setup((m => m.Invested)).Returns(parameters.InvestedArray[i]);

                var targets = algorithm.RiskManagement.ManageRisk(algorithm, null).ToList();
                var shouldLiquidate = parameters.ShouldLiquidateArray[i];

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

                if (shouldLiquidate || parameters.ChangePosition[i])
                {
                    // Go from long to short or viceversa
                    holding.SetHoldings(price, quantity = -quantity);
                }
            }
        }

        static IEnumerable<TestCaseData> GenerateTestData()
        {
            Language[] languages = new Language[] { Language.CSharp, Language.Python };
            TrailingStopRiskManagementModelTestParameters[] datasets = new TrailingStopRiskManagementModelTestParameters[]
            {
                new TrailingStopRiskManagementModelTestParameters(
                    "LiquidatesOnCorrectPriceChangeInLongPosition",
                    0.05m,
                    1m,
                    1m,
                    new decimal[] { 100m, 99.95m, 99.94m, 95m, 94.99m },
                    new bool[] { true, true, true, true, true },
                    new bool[] { false, false, false, false, false },
                    new bool[] { false, false, false, false, true }
                ),
                new TrailingStopRiskManagementModelTestParameters(
                    "LiquidatesOnCorrectPriceChangeInShortPosition",
                    0.1m,
                    100m,
                    -1m,
                    new decimal[] { 50m, 54.99m, 55m, 55.01m },
                    new bool[] { true, true, true, true },
                    new bool[] { false, false, false, false },
                    new bool[] { false, false, false, true }
                ),
                new TrailingStopRiskManagementModelTestParameters(
                    "DoesntLiquidateIfSecurityIsNotInvested",
                    0.05m,
                    1m,
                    1m,
                    new decimal[] { 100m, 94.99m, 90m },
                    new bool[] { false, false, false },
                    new bool[] { false, false, false },
                    new bool[] { false, false, false }
                ),
                new TrailingStopRiskManagementModelTestParameters(
                    "LiquidatesOnCorrectPriceChangeInLongPositionWithUnivestedSecurityInFirstPrices",
                    0.05m,
                    1m,
                    1m,
                    new decimal[] { 10m, 100m, 99.95m, 99.94m, 95m, 94.99m },
                    new bool[] { false, true, true, true, true, true },
                    new bool[] { false, false, false, false, false, false },
                    new bool[] { false, false, false, false, false, true }
                ),
                new TrailingStopRiskManagementModelTestParameters(
                    "LiquidatesOnCorrectPriceChangeInShortPositionWithUnivestedSecurityInFirstPrices",
                    0.1m,
                    100m,
                    -1m,
                    new decimal[] { 90m, 100m, 50m, 54.99m, 55m, 55.01m },
                    new bool[] { false, true, true, true, true, true },
                    new bool[] { false, false, false, false, false, false },
                    new bool[] { false, false, false, false, false, true }
                ),
                new TrailingStopRiskManagementModelTestParameters(
                    "DoesntLiquidateIfPricesDontChangeInLongPosition",
                    0.05m,
                    1m,
                    1m,
                    new decimal[] { 1m, 1m, 1m, 1m },
                    new bool[] { true, true, true, true },
                    new bool[] { false, false, false, false },
                    new bool[] { false, false, false, false }
                ),
                new TrailingStopRiskManagementModelTestParameters(
                    "DoesntLiquidateIfPricesDontChangeInShortPosition",
                    0.05m,
                    1m,
                    -1m,
                    new decimal[] { 1m, 1m, 1m, 1m },
                    new bool[] { true, true, true, true },
                    new bool[] { false, false, false, false },
                    new bool[] { false, false, false, false }
                ),
                new TrailingStopRiskManagementModelTestParameters(
                    "LiquidatesAfterSwitchingToShortPosition",
                    0.05m,
                    1m,
                    1m,
                    new decimal[] { 100m, 90m, 70m, 50m, 52.6m },
                    new bool[] { true, true, true, true, true },
                    new bool[] { true, false, false, false, false },
                    new bool[] { false, false, false, false, true }
                ),
                new TrailingStopRiskManagementModelTestParameters(
                    "LiquidatesOnFirstCallForLongPosition",
                    0.1m,
                    100m,
                    1m,
                    new decimal[] { 89.99m },
                    new bool[] { true },
                    new bool[] { false },
                    new bool[] { true }
                ),
                new TrailingStopRiskManagementModelTestParameters(
                    "LiquidatesOnFirstCallForShortPosition",
                    0.1m,
                    100m,
                    -1m,
                    new decimal[] { 110.01m },
                    new bool[] { true },
                    new bool[] { false },
                    new bool[] { true }
                )
            };

            return (
                from parameters in datasets
                from language in languages
                select new TrailingStopRiskManagementModelTestParameters(
                    parameters.Name,
                    parameters.MaxDrawdownPercent,
                    parameters.InitialPrice,
                    parameters.Quantity,
                    parameters.Prices,
                    parameters.InvestedArray,
                    parameters.ChangePosition,
                    parameters.ShouldLiquidateArray,
                    language
                )
            )
            .OrderBy(c => c.Language)
            // generate test cases from test parameters
            .Select(x => new TestCaseData(x).SetName(x.Language + "/" + x.Name))
            .ToArray();
        }

        public class TrailingStopRiskManagementModelTestParameters
        {
            public string Name { get; init; }
            public Language Language { get; init; }
            public decimal MaxDrawdownPercent { get; init; }
            public decimal InitialPrice { get; init; }
            public decimal Quantity { get; init; }
            public decimal[] Prices { get; init; }
            public bool[] InvestedArray { get; init; }
            public bool[] ChangePosition { get; init; }
            public bool[] ShouldLiquidateArray { get; init; }

            public TrailingStopRiskManagementModelTestParameters(
                string name,
                decimal maxDrawdownPercent,
                decimal initialPrice,
                decimal quantity,
                decimal[] prices,
                bool[] investedArray,
                bool[] changePosition,
                bool[] shouldLiquidateArray,
                Language language = Language.CSharp
                )
            {
                Name = name;
                Language = language;
                MaxDrawdownPercent = maxDrawdownPercent;
                InitialPrice = initialPrice;
                Quantity = quantity;
                Prices = prices;
                InvestedArray = investedArray;
                ChangePosition = changePosition;
                ShouldLiquidateArray = shouldLiquidateArray;
            }
        }
    }
}
