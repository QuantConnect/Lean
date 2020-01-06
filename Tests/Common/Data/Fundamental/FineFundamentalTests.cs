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
using NUnit.Framework;
using QuantConnect.Data.Fundamental;

namespace QuantConnect.Tests.Common.Data.Fundamental
{
    [TestFixture]
    public class FineFundamentalTests
    {
        [Test]
        public void ComputesMarketCapCorrectly()
        {
            var fine = new FineFundamental
            {
                Symbol = Symbols.AAPL,
                EndTime = DateTime.Now,
                Value = 267.18m,
                CompanyReference = new CompanyReference
                {
                    CountryId = "USA",
                    PrimaryExchangeID = "NYS",
                    IndustryTemplateCode = "N"
                },
                SecurityReference = new SecurityReference
                {
                    IPODate = new DateTime(1980,12,12)
                },
                ValuationRatios = new ValuationRatios
                {
                    PERatio = 22.476871m
                },
                EarningReports = new EarningReports
                {
                    BasicAverageShares = new BasicAverageShares
                    {
                        ThreeMonths = 449081100m
                    },
                    BasicEPS = new BasicEPS
                    {
                        TwelveMonths = 11.97m
                    }
                }
            };

            Assert.AreEqual(119985488298m, fine.MarketCap);
        }

        [Test]
        public void ZeroMarketCapForDefaultBasicAverageShares()
        {
            var fine = new FineFundamental
            {
                Symbol = Symbols.AAPL,
                EndTime = DateTime.Now,
                Value = 267.18m,
            };
            fine.EarningReports.BasicAverageShares = null;

            Assert.AreEqual(267.18m, fine.Price);
            Assert.IsNull(fine.EarningReports.BasicAverageShares);
            Assert.AreEqual(0, fine.MarketCap);
        }

        [Test]
        public void ZeroMarketCapForDefaultObject()
        {
            var fine = new FineFundamental();
            fine.EarningReports.BasicAverageShares = null;

            Assert.AreEqual(0, fine.Price);
            Assert.IsNull(fine.EarningReports.BasicAverageShares);
            Assert.AreEqual(0, fine.MarketCap);
        }
    }
}