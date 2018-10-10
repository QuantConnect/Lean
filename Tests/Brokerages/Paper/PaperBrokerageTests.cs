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

using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Brokerages.Paper;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Brokerages.Paper
{
    [TestFixture]
    public class PaperBrokerageTests
    {
        [Test]
        public void AppliesDividendDistributionDirectlyToPortfolioCashBook()
        {
            // init algo
            var algo = new AlgorithmStub();
            algo.AddSecurities(equities: new List<string> {"SPY"});
            algo.PostInitialize();

            // init holdings
            var SPY = algo.Securities[Symbols.SPY];
            SPY.SetMarketPrice(new Tick {Value = 100m});
            SPY.Holdings.SetHoldings(100m, 1000);

            // resolve expected outcome
            var USD = algo.Portfolio.CashBook["USD"];
            var preDistributionCash = USD.Amount;
            var distributionPerShare = 10m;
            var expectedTotalDistribution = distributionPerShare * SPY.Holdings.Quantity;

            // create slice w/ dividend
            var slice = new Slice(algo.Time, new List<BaseData>());
            slice.Dividends.Add(new Dividend(Symbols.SPY, algo.Time, distributionPerShare, 100m));
            algo.SetCurrentSlice(slice);

            // invoke brokerage
            var brokerage = new PaperBrokerage(algo, null);
            brokerage.Scan();

            // verify results
            var postDistributionCash = USD.Amount;
            Assert.AreEqual(preDistributionCash + expectedTotalDistribution, postDistributionCash);
        }
    }
}
