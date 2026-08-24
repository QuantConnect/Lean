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
using QuantConnect.Configuration;
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
        public void WarnsOnLargeOptionUniverseSelection()
        {
            // option universe subscriptions are added at minute resolution
            Config.Set("universe-selection-size-warning-thresholds", "{\"Minute\": 10}");
            try
            {
                var algorithm = new AlgorithmStub(new MockDataFeed());
                algorithm.SetStartDate(2014, 6, 6);
                var option = algorithm.AddOption("AAPL");
                // OnEndOfTimeStep will add all pending universe additions
                algorithm.OnEndOfTimeStep();
                var universe = algorithm.UniverseManager.Values.OfType<OptionChainUniverse>().Single();

                // below the threshold: no warning
                universe.Selected = CreateOptionSelection(option.Symbol.Underlying, 5);
                algorithm.DataManager.UniverseSelection.WarnOnLargeUniverseSelection(universe);
                Assert.AreEqual(0, UniverseSizeWarningCount(algorithm));

                // above the threshold: warns
                universe.Selected = CreateOptionSelection(option.Symbol.Underlying, 15);
                algorithm.DataManager.UniverseSelection.WarnOnLargeUniverseSelection(universe);
                Assert.AreEqual(1, UniverseSizeWarningCount(algorithm));
                Assert.AreEqual(1, algorithm.DebugMessages.Count(x => x.Contains("SetFilter")));

                // only warns once per algorithm
                algorithm.DataManager.UniverseSelection.WarnOnLargeUniverseSelection(universe);
                Assert.AreEqual(1, UniverseSizeWarningCount(algorithm));
            }
            finally
            {
                Config.Reset();
            }
        }

        [Test]
        public void LargeUniverseSelectionWarningIsResolutionAware()
        {
            Config.Set("universe-selection-size-warning-thresholds", "{\"Minute\": 10, \"Daily\": 50}");
            try
            {
                var algorithm = new AlgorithmStub(new MockDataFeed());
                algorithm.SetEndDate(new DateTime(2024, 12, 13));
                algorithm.SetStartDate(algorithm.EndDate.Subtract(TimeSpan.FromDays(10)));
                algorithm.UniverseSettings.Resolution = Resolution.Daily;
                algorithm.AddUniverse(coarse => Enumerable.Empty<Symbol>());
                // OnEndOfTimeStep will add all pending universe additions
                algorithm.OnEndOfTimeStep();
                var universe = algorithm.UniverseManager.Values.First();

                // over the minute threshold but under this universe's daily threshold: no warning
                universe.Selected = CreateEquitySelection(20);
                algorithm.DataManager.UniverseSelection.WarnOnLargeUniverseSelection(universe);
                Assert.AreEqual(0, UniverseSizeWarningCount(algorithm));

                // over the daily threshold: warns with the generic suggestion
                universe.Selected = CreateEquitySelection(60);
                algorithm.DataManager.UniverseSelection.WarnOnLargeUniverseSelection(universe);
                Assert.AreEqual(1, UniverseSizeWarningCount(algorithm));
                Assert.AreEqual(1, algorithm.DebugMessages.Count(x => x.Contains("Daily") && x.Contains("UniverseSettings.Resolution")));
            }
            finally
            {
                Config.Reset();
            }
        }

        [Test]
        public void LargeUniverseSelectionWarningAggregatesAcrossResolutions()
        {
            Config.Set("universe-selection-size-warning-thresholds", "{\"Minute\": 10, \"Daily\": 50}");
            try
            {
                var algorithm = new AlgorithmStub(new MockDataFeed());
                algorithm.SetStartDate(2014, 6, 6);
                var option = algorithm.AddOption("AAPL");
                algorithm.AddUniverse(fundamentals => Enumerable.Empty<Symbol>());
                // OnEndOfTimeStep will add all pending universe additions
                algorithm.OnEndOfTimeStep();
                var optionUniverse = algorithm.UniverseManager.Values.OfType<OptionChainUniverse>().Single();
                var equityUniverse = algorithm.UniverseManager.Values.Single(x => x is FundamentalUniverseFactory);
                optionUniverse.UniverseSettings = new UniverseSettings(optionUniverse.UniverseSettings) { Resolution = Resolution.Minute };
                equityUniverse.UniverseSettings = new UniverseSettings(equityUniverse.UniverseSettings) { Resolution = Resolution.Daily };

                // 8/10 minute contracts: under budget
                optionUniverse.Selected = CreateOptionSelection(option.Symbol.Underlying, 8);
                algorithm.DataManager.UniverseSelection.WarnOnLargeUniverseSelection(optionUniverse);
                Assert.AreEqual(0, UniverseSizeWarningCount(algorithm));

                // 8/10 minute contracts + 30/50 daily symbols = 1.4 of the shared budget: warns,
                // even though each universe is under its own resolution threshold
                equityUniverse.Selected = CreateEquitySelection(30);
                algorithm.DataManager.UniverseSelection.WarnOnLargeUniverseSelection(equityUniverse);
                Assert.AreEqual(1, UniverseSizeWarningCount(algorithm));
                Assert.AreEqual(1, algorithm.DebugMessages.Count(x => x.Contains("~8 symbols at Minute resolution") &&
                    x.Contains("~30 symbols at Daily resolution")));
            }
            finally
            {
                Config.Reset();
            }
        }

        [Test]
        public void LargeUniverseSelectionWarningSkipsSmallSelections()
        {
            Config.Set("universe-selection-size-warning-thresholds", "{\"Minute\": 10, \"Daily\": 100}");
            try
            {
                var algorithm = new AlgorithmStub(new MockDataFeed());
                algorithm.SetStartDate(2014, 6, 6);
                var option = algorithm.AddOption("AAPL");
                algorithm.AddUniverse(fundamentals => Enumerable.Empty<Symbol>());
                // OnEndOfTimeStep will add all pending universe additions
                algorithm.OnEndOfTimeStep();
                var optionUniverse = algorithm.UniverseManager.Values.OfType<OptionChainUniverse>().Single();
                var equityUniverse = algorithm.UniverseManager.Values.Single(x => x is FundamentalUniverseFactory);
                optionUniverse.UniverseSettings = new UniverseSettings(optionUniverse.UniverseSettings) { Resolution = Resolution.Minute };
                equityUniverse.UniverseSettings = new UniverseSettings(equityUniverse.UniverseSettings) { Resolution = Resolution.Daily };

                // the option universe alone exceeds the shared budget, but the selection being checked
                // is too small (under a tenth of its threshold) to run the check
                optionUniverse.Selected = CreateOptionSelection(option.Symbol.Underlying, 20);
                equityUniverse.Selected = CreateEquitySelection(5);
                algorithm.DataManager.UniverseSelection.WarnOnLargeUniverseSelection(equityUniverse);
                Assert.AreEqual(0, UniverseSizeWarningCount(algorithm));

                // a significant selection runs the check and warns
                equityUniverse.Selected = CreateEquitySelection(10);
                algorithm.DataManager.UniverseSelection.WarnOnLargeUniverseSelection(equityUniverse);
                Assert.AreEqual(1, UniverseSizeWarningCount(algorithm));
            }
            finally
            {
                Config.Reset();
            }
        }

        private static int UniverseSizeWarningCount(AlgorithmStub algorithm)
        {
            return algorithm.DebugMessages.Count(x => x.Contains("universe selections"));
        }

        private static HashSet<Symbol> CreateEquitySelection(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => Symbol.Create($"SYM{i}", SecurityType.Equity, Market.USA))
                .ToHashSet();
        }

        private static HashSet<Symbol> CreateOptionSelection(Symbol underlying, int contractCount)
        {
            // option universe selections have the underlying symbol prepended
            var selected = new HashSet<Symbol> { underlying };
            var expiry = new DateTime(2014, 7, 19);
            for (var i = 0; i < contractCount; i++)
            {
                selected.Add(Symbol.CreateOption(underlying, Market.USA, OptionStyle.American, OptionRight.Call, 100 + i, expiry));
            }
            return selected;
        }

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
