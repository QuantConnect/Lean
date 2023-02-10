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
using NUnit.Framework;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Positions;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Securities.Option.StrategyMatcher;

namespace QuantConnect.Tests.Common.Securities.Positions
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class OptionStrategyPositionGroupResolverTests
    {
        [Test]
        public void DoesNotMatchDifferentOptionSharingUnderlying()
        {
            var symbol1 = SymbolRepresentation.ParseOptionTickerOSI("SPX   230217P04015000", SecurityType.IndexOption);
            var symbol2 = SymbolRepresentation.ParseOptionTickerOSI("SPXW  230215P04015000", SecurityType.IndexOption);

            var algorithm = new AlgorithmStub();
            algorithm.AddOptionContract(symbol1);
            algorithm.AddOptionContract(symbol2);
            var groupResolver = new OptionStrategyPositionGroupResolver(algorithm.Securities);

            var positionsCollection = new PositionCollection(new[] {
                new Position(symbol1, +2, 1),
                new Position(symbol2, -2, 1)
            });
            var result = groupResolver.Resolve(positionsCollection);

            // different option contracts
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FindsIndexOptionGroup()
        {
            var symbol1 = SymbolRepresentation.ParseOptionTickerOSI("SPXW  230215P04115000", SecurityType.IndexOption);
            var symbol2 = SymbolRepresentation.ParseOptionTickerOSI("SPXW  230215P04015000", SecurityType.IndexOption);

            var algorithm = new AlgorithmStub();
            algorithm.AddOptionContract(symbol1);
            algorithm.AddOptionContract(symbol2);
            var groupResolver = new OptionStrategyPositionGroupResolver(algorithm.Securities);

            var positionsCollection = new PositionCollection(new[] {
                new Position(symbol1, +2, 1),
                new Position(symbol2, -2, 1)
            });
            var result = groupResolver.Resolve(positionsCollection);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(OptionStrategyDefinitions.BearPutSpread.Name, ((OptionStrategyPositionGroupBuyingPowerModel)result.Single().BuyingPowerModel).ToString());
        }
    }
}
