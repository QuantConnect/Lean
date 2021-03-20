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
using QuantConnect.Securities.Future;

namespace QuantConnect.Tests.Common.Securities.Futures
{
    [TestFixture]
    public class FuturesListingsTests
    {
        [TestCaseSource(nameof(ListedContractsCME_2021_01_11))]
        public void ListedContractsMatchesCME(string ticker, string market, DateTime[] expectedListedExpiries)
        {
            // Test was created on 2021-01-11, we're using CME's data here to validate the test, hence the fixed date.
            var contractsListed = FuturesListings.ListedContracts(ticker, new DateTime(2021, 1, 11));
            var contractsMissing = new HashSet<Symbol>();

            foreach (var expectedExpiry in expectedListedExpiries)
            {
                var expectedFuture = Symbol.CreateFuture(ticker, market, expectedExpiry);
                if (!contractsListed.Contains(expectedFuture))
                {
                    contractsMissing.Add(expectedFuture);
                }
            }

            var missingContractsMessage = $"The following contracts are missing from the listed contracts: {string.Join("\n", contractsMissing.Select(s => "  " + s.Value + " " + s.ID.Date.ToStringInvariant("yyyy-MM-dd")))}";

            Assert.AreEqual(0, contractsMissing.Count, missingContractsMessage);
            Assert.AreEqual(expectedListedExpiries.Length, contractsListed.Count, $"The length of expected listed contracts does not match the returned contract count.");
        }

        public static TestCaseData[] ListedContractsCME_2021_01_11()
        {
            return new TestCaseData[]
            {
                new TestCaseData(
                    "ZB",
                    Market.CBOT,
                    new[]
                    {
                        new DateTime(2021, 3, 22),
                        new DateTime(2021, 6, 21),
                        new DateTime(2021, 9, 21),
                    }
                ),
                new TestCaseData(
                    "ZC",
                    Market.CBOT,
                    new[]
                    {
                        new DateTime(2021, 3, 12),
                        new DateTime(2021, 5, 14),
                        new DateTime(2021, 7, 14),
                        new DateTime(2021, 9, 14),
                        new DateTime(2021, 12, 14),
                        new DateTime(2022, 3, 14),
                        new DateTime(2022, 5, 13),
                        new DateTime(2022, 7, 14),
                        new DateTime(2022, 9, 14),
                        new DateTime(2022, 12, 14),
                        new DateTime(2023, 3, 14),
                        new DateTime(2023, 5, 12),
                        new DateTime(2023, 7, 14),
                        new DateTime(2023, 9, 14),
                        new DateTime(2023, 12, 14),
                        new DateTime(2024, 7, 12),
                        new DateTime(2024, 12, 13),
                    }
                ),
                new TestCaseData(
                    "ZS",
                    Market.CBOT,
                    new[]
                    {
                        new DateTime(2021, 1, 14),
                        new DateTime(2021, 3, 12),
                        new DateTime(2021, 5, 14),
                        new DateTime(2021, 7, 14),
                        new DateTime(2021, 8, 13),
                        new DateTime(2021, 9, 14),
                        new DateTime(2021, 11, 12),
                        new DateTime(2022, 1, 14),
                        new DateTime(2022, 3, 14),
                        new DateTime(2022, 5, 13),
                        new DateTime(2022, 7, 14),
                        new DateTime(2022, 8, 12),
                        new DateTime(2022, 9, 14),
                        new DateTime(2022, 11, 14),
                        new DateTime(2023, 1, 13),
                        new DateTime(2023, 3, 14),
                        new DateTime(2023, 5, 12),
                        new DateTime(2023, 7, 14),
                        new DateTime(2023, 8, 14),
                        new DateTime(2023, 9, 14),
                        new DateTime(2023, 11, 14),
                        new DateTime(2024, 7, 12),
                        new DateTime(2024, 11, 14)
                    }
                ),
                new TestCaseData(
                    "ZT",
                    Market.CBOT,
                    new[]
                    {
                        new DateTime(2021, 3, 31),
                        new DateTime(2021, 6, 30),
                        new DateTime(2021, 9, 30)
                    }
                ),
                new TestCaseData(
                    "ZW",
                    Market.CBOT,
                    new[]
                    {
                        new DateTime(2021, 3, 12),
                        new DateTime(2021, 5, 14),
                        new DateTime(2021, 7, 14),
                        new DateTime(2021, 9, 14),
                        new DateTime(2021, 12, 14),
                        new DateTime(2022, 3, 14),
                        new DateTime(2022, 5, 13),
                        new DateTime(2022, 7, 14),
                        new DateTime(2022, 9, 14),
                        new DateTime(2022, 12, 14),
                        new DateTime(2023, 3, 14),
                        new DateTime(2023, 5, 12),
                        new DateTime(2023, 7, 14)
                    }
                ),
            };
        }
    }
}
