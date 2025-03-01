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
using Moq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    public class UniverseSelectionTests
    {
        [Test]
        public void CreatedEquityIsNotAddedToSymbolCache()
        {
            SymbolCache.Clear();
            var algorithm = new AlgorithmStub(new MockDataFeed());
            algorithm.SetEndDate(new DateTime(2024, 12, 13));
            algorithm.SetStartDate(algorithm.EndDate.Subtract(TimeSpan.FromDays(10)));
            algorithm.AddUniverse(CoarseSelectionFunction, FineSelectionFunction);
            // OnEndOfTimeStep will add all pending universe additions
            algorithm.OnEndOfTimeStep();
            var universe = algorithm.UniverseManager.Values.First();
            var securityChanges = algorithm.DataManager.UniverseSelection.ApplyUniverseSelection(
                universe,
                algorithm.EndDate.ConvertToUtc(algorithm.TimeZone).Subtract(TimeSpan.FromDays(1)),
                new BaseDataCollection(
                    DateTime.UtcNow,
                    Symbols.AAPL,
                    new[]
                    {
                        new CoarseFundamental
                        {
                            Symbol = Symbols.AAPL,
                            Time = DateTime.UtcNow
                        },
                        new CoarseFundamental
                        {
                            Symbol = Symbols.SPY,
                            Time = DateTime.UtcNow
                        }
                    }
                )
            );
            Symbol symbol;
            Assert.AreEqual(1, securityChanges.AddedSecurities.Count);
            Assert.AreEqual(Symbols.AAPL, securityChanges.AddedSecurities.First().Symbol);
            Assert.IsFalse(SymbolCache.TryGetSymbol("AAPL", out symbol));
            Assert.IsFalse(SymbolCache.TryGetSymbol("SPY", out symbol));
        }

        [Test]
        public void RemovalFromUniverseAndDataFeedMakesSecurityNotTradable()
        {
            SymbolCache.Clear();
            var algorithm = new AlgorithmStub(new MockDataFeedWithSubscription());
            var orderProcessorMock = new Mock<IOrderProcessor>();
            orderProcessorMock.Setup(m => m.GetOpenOrders(It.IsAny<Func<Order, bool>>())).Returns(new List<Order>());
            algorithm.Transactions.SetOrderProcessor(orderProcessorMock.Object);

            algorithm.SetStartDate(2012, 3, 27);
            algorithm.SetEndDate(2012, 3, 30);
            algorithm.AddUniverse("my-custom-universe", dt => dt.Day < 30 ? new List<string> { "CPRT" } : Enumerable.Empty<string>());
            // OnEndOfTimeStep will add all pending universe additions
            algorithm.OnEndOfTimeStep();
            var universe = algorithm.UniverseManager.Values.First();

            var securityChanges = algorithm.DataManager.UniverseSelection.ApplyUniverseSelection(
                universe,
                algorithm.EndDate.ConvertToUtc(algorithm.TimeZone).Subtract(TimeSpan.FromDays(2)),
                new BaseDataCollection(
                    algorithm.UtcTime,
                    Symbol.Create("CPRT", SecurityType.Equity, Market.USA),
                    new List<BaseData>()
                )
            );

            Assert.AreEqual(1, securityChanges.AddedSecurities.Count);
            Assert.AreEqual(0, securityChanges.RemovedSecurities.Count);

            var security = securityChanges.AddedSecurities.First();
            Assert.IsTrue(security.IsTradable);

            securityChanges = algorithm.DataManager.UniverseSelection.ApplyUniverseSelection(
                universe,
                algorithm.EndDate.ConvertToUtc(algorithm.TimeZone),
                new BaseDataCollection(
                    algorithm.UtcTime,
                    Symbol.Create("CPRT", SecurityType.Equity, Market.USA),
                    new List<BaseData>()
                )
            );

            Assert.AreEqual(0, securityChanges.AddedSecurities.Count);
            Assert.AreEqual(1, securityChanges.RemovedSecurities.Count);
            Assert.AreEqual(security.Symbol, securityChanges.RemovedSecurities.First().Symbol);

            Assert.IsFalse(security.IsTradable);
        }

        [Test]
        public void CoarseFundamentalHasFundamentalDataFalseExcludedInFineUniverseSelection()
        {
            var algorithm = new AlgorithmStub(new MockDataFeed());
            algorithm.SetEndDate(new DateTime(2024, 12, 13));
            algorithm.SetStartDate(algorithm.EndDate.Subtract(TimeSpan.FromDays(10)));

            algorithm.AddUniverse(
                coarse => coarse.Select(c => c.Symbol),
                fine => fine.Select(f => f.Symbol).Where(x => x.ID.Symbol == "AAPL")
            );
            // OnEndOfTimeStep will add all pending universe additions
            algorithm.OnEndOfTimeStep();

            var universe = algorithm.UniverseManager.Values.First();
            var securityChanges = algorithm.DataManager.UniverseSelection.ApplyUniverseSelection(
                universe,
                algorithm.EndDate.ConvertToUtc(algorithm.TimeZone).Subtract(TimeSpan.FromDays(1)),
                new BaseDataCollection(
                    DateTime.UtcNow,
                    Symbols.AAPL,
                    new[]
                    {
                        new CoarseFundamental
                        {
                            Symbol = Symbols.AAPL,
                            Time = DateTime.UtcNow
                        },
                        new CoarseFundamental
                        {
                            Symbol = Symbols.SPY,
                            Time = DateTime.UtcNow
                        }
                    }
                )
            );

            Assert.AreEqual(1, securityChanges.Count);
            Assert.AreEqual(Symbols.AAPL, securityChanges.AddedSecurities.First().Symbol);
        }

        [Test]
        public void DoesNotAddSelectedSecuritiesIfNoTradableDates()
        {
            var algorithm = new AlgorithmStub(new MockDataFeed());
            algorithm.SetStartDate(2023, 12, 01);
            algorithm.SetEndDate(2023, 12, 30); // Sunday

            algorithm.AddUniverse(
                coarse => coarse.Select(c => c.Symbol),
                fine => fine.Select(f => f.Symbol));
            algorithm.OnEndOfTimeStep();

            var universe = algorithm.UniverseManager.Values.First();

            var getUniverseData = (DateTime dt) => new BaseDataCollection(
                dt,
                Symbols.AAPL,
                [
                    new CoarseFundamental
                    {
                        Symbol = Symbols.AAPL,
                        Time = dt
                    },
                    new CoarseFundamental
                    {
                        Symbol = Symbols.SPY,
                        Time = dt
                    }
                ]
            );

            // Friday, one tradeale day left before end date
            var dateTime = new DateTime(2023, 12, 29).ConvertToUtc(algorithm.TimeZone);
            var universeData = getUniverseData(dateTime);

            var securityChanges = algorithm.DataManager.UniverseSelection.ApplyUniverseSelection(
                universe,
                dateTime,
                universeData);
            Assert.AreEqual(2, securityChanges.AddedSecurities.Count);
            CollectionAssert.AreEquivalent(universeData.Select(x => x.Symbol), securityChanges.AddedSecurities.Select(x => x.Symbol));

            // Saturday, no tradable days left before end date
            dateTime += TimeSpan.FromDays(1);
            universeData = getUniverseData(dateTime);

            securityChanges = algorithm.DataManager.UniverseSelection.ApplyUniverseSelection(
                universe,
                dateTime,
                universeData);
            Assert.AreEqual(0, securityChanges.AddedSecurities.Count);
        }

        private IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            return new List<Symbol> {Symbols.AAPL, Symbols.SPY};
        }

        private IEnumerable<Symbol> FineSelectionFunction(IEnumerable<FineFundamental> fine)
        {
            return new[] { fine.First(fundamental => fundamental.Symbol.Value == "AAPL").Symbol };
        }

        public class MockDataFeedWithSubscription : IDataFeed
        {
            public bool IsActive { get; }

            public void Initialize(
                IAlgorithm algorithm,
                AlgorithmNodePacket job,
                IResultHandler resultHandler,
                IMapFileProvider mapFileProvider,
                IFactorFileProvider factorFileProvider,
                IDataProvider dataProvider,
                IDataFeedSubscriptionManager subscriptionManager,
                IDataFeedTimeProvider dataFeedTimeProvider,
                IDataChannelProvider dataChannelProvider
            )
            {
            }

            public Subscription CreateSubscription(SubscriptionRequest request)
            {
                return new Subscription(request, Enumerable.Empty<SubscriptionData>().GetEnumerator(), null);
            }

            public void RemoveSubscription(Subscription subscription)
            {
            }

            public void Exit()
            {
            }
        }
    }
}
