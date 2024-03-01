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
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Statistics;

namespace QuantConnect.Tests.Common.Securities.Options
{
    [TestFixture]
    public class OptionSecurityTests
    {
        [Test]
        public void FutureOptionSecurityUsesFutureOptionMarginModel()
        {
            var underlyingFuture = Symbol.CreateFuture(
                QuantConnect.Securities.Futures.Indices.SP500EMini,
                Market.CME,
                new DateTime(2021, 3, 19));

            var futureOption = Symbol.CreateOption(underlyingFuture,
                Market.CME,
                OptionStyle.American,
                OptionRight.Call,
                2550m,
                new DateTime(2021, 3, 19));

            var futureOptionSecurity = new QuantConnect.Securities.FutureOption.FutureOption(
                futureOption,
                MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.CME, futureOption, futureOption.SecurityType),
                new Cash("USD", 100000m, 1m),
                new OptionSymbolProperties(string.Empty, "USD", 1m, 0.01m, 1m),
                new CashBook(),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache(),
                null);

            Assert.IsTrue(futureOptionSecurity.BuyingPowerModel is FuturesOptionsMarginModel);
        }

        [Test]
        public void EquityOptionSecurityUsesOptionMarginModel()
        {
            var underlyingEquity = Symbol.Create("TWX", SecurityType.Equity, Market.USA);
            var equityOption = Symbol.CreateOption(underlyingEquity,
                Market.USA,
                OptionStyle.American,
                OptionRight.Call,
                42.5m,
                new DateTime(2014, 6, 21));

            var equityOptionSecurity = new Option(
                equityOption,
                MarketHoursDatabase.FromDataFolder().GetExchangeHours(Market.USA, equityOption, equityOption.SecurityType),
                new Cash("USD", 100000m, 1m),
                new OptionSymbolProperties(string.Empty, "USD", 100m, 0.0001m, 1m),
                new CashBook(),
                new RegisteredSecurityDataTypesProvider(),
                new SecurityCache(),
                null);

            Assert.IsTrue(equityOptionSecurity.BuyingPowerModel is OptionMarginModel);
        }

        [Test]
        public void AlgorithmSendsOneTimeWarningAboutOptionModelsConsistency(
            [Values(nameof(OptionModelsConsistencyRegressionAlgorithm), nameof(IndexOptionModelsConsistencyRegressionAlgorithm))] string algorithmName,
            [Values(Language.CSharp, Language.Python)] Language language)
        {
            var parameter = new RegressionTests.AlgorithmStatisticsTestParameters(
                algorithmName,
                new Dictionary<string, string> {
                    {PerformanceMetrics.TotalOrders, "0"},
                    {"Average Win", "0%"},
                    {"Average Loss", "0%"},
                    {"Compounding Annual Return", "0%"},
                    {"Drawdown", "0%"},
                    {"Expectancy", "0"},
                    {"Net Profit", "0%"},
                    {"Sharpe Ratio", "0"},
                    {"Probabilistic Sharpe Ratio", "0%"},
                    {"Loss Rate", "0%"},
                    {"Win Rate", "0%"},
                    {"Profit-Loss Ratio", "0"},
                    {"Alpha", "0"},
                    {"Beta", "0"},
                    {"Annual Standard Deviation", "0"},
                    {"Annual Variance", "0"},
                    {"Information Ratio", "0"},
                    {"Tracking Error", "0"},
                    {"Treynor Ratio", "0"},
                    {"Total Fees", "$0.00"},
                    {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
                },
                language,
                AlgorithmStatus.Completed);

            var result = AlgorithmRunner.RunLocalBacktest(parameter.Algorithm,
                parameter.Statistics,
                parameter.Language,
                parameter.ExpectedFinalStatus,
                returnLogs: true);

            Assert.IsTrue(result.Logs.Any(message => message.Contains("Debug: Warning: Security ") &&
                message.EndsWith("To avoid this, consider using a security initializer to set the right models to each security type according to your algorithm's requirements.")));
        }
    }
}
