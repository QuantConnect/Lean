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
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class ConsolidatorBaseTests
    {
        [TestCaseSource(nameof(WindowTestCases))]
        public void WindowStoresConsolidatedBars(IDataConsolidator consolidator, IBaseData[] bars, decimal expectedWindow0, decimal expectedWindow1)
        {
            var windowConsolidator = (ConsolidatorBase)consolidator;

            foreach (var bar in bars)
            {
                consolidator.Update(bar);
            }

            Assert.AreEqual(2, windowConsolidator.Window.Count);
            Assert.AreEqual(expectedWindow0, windowConsolidator.Window[0].Value);
            Assert.AreEqual(expectedWindow1, windowConsolidator.Window[1].Value);
            Assert.AreEqual(windowConsolidator.Window[0], windowConsolidator.Consolidated);
            Assert.AreEqual(expectedWindow0, windowConsolidator[0].Value);
            Assert.AreEqual(expectedWindow1, windowConsolidator.Previous.Value);

            consolidator.Dispose();
        }

        [Test]
        public void HandlerSeesPreviousConsolidatedBarWhileReceivingTheNewOne()
        {
            var reference = new DateTime(2015, 4, 13);
            var spy = Symbols.SPY;
            var consolidator = new IdentityDataConsolidator<TradeBar>();

            IBaseData eventArgument = null;
            IBaseData consolidatedInsideHandler = null;
            consolidator.DataConsolidated += (_, bar) =>
            {
                eventArgument = bar;
                consolidatedInsideHandler = ((ConsolidatorBase)consolidator).Consolidated;
            };

            // First bar: inside the handler Consolidated is still null (no previous bar yet)
            var first = new TradeBar { Symbol = spy, Time = reference, Close = 10m, Value = 10m, Period = Time.OneMinute };
            consolidator.Update(first);
            Assert.AreEqual(first, eventArgument);
            Assert.IsNull(consolidatedInsideHandler);

            // Second bar: the handler receives the new bar as argument while Consolidated still holds the previous one
            var second = new TradeBar { Symbol = spy, Time = reference.AddMinutes(1), Close = 20m, Value = 20m, Period = Time.OneMinute };
            consolidator.Update(second);
            Assert.AreEqual(second, eventArgument);
            Assert.AreEqual(first, consolidatedInsideHandler);

            // Once the handler returns, the window reflects the latest consolidated bar
            Assert.AreEqual(second, ((ConsolidatorBase)consolidator).Consolidated);

            consolidator.Dispose();
        }

        [Test]
        public void WindowHoldsTheNewBarInsideTypedHandler()
        {
            // regression for consolidators that fired their typed event before populating the window:
            // inside the handler consolidator[0] must be the bar that was just produced
            var reference = new DateTime(2015, 4, 13);
            var spy = Symbols.SPY;
            var consolidator = new RenkoConsolidator(1m);
            var windowConsolidator = (ConsolidatorBase)consolidator;

            var handlerCalls = 0;
            consolidator.DataConsolidated += (_, bar) =>
            {
                handlerCalls++;
                Assert.AreEqual(bar, windowConsolidator[0]);
                Assert.AreEqual(bar, windowConsolidator.Current);
                Assert.AreEqual(bar.Value, windowConsolidator.Current.Value);
            };

            consolidator.Update(new IndicatorDataPoint(spy, reference, 10m));
            consolidator.Update(new IndicatorDataPoint(spy, reference.AddMinutes(1), 12.1m));

            Assert.Greater(handlerCalls, 0);
            consolidator.Dispose();
        }

        [Test]
        public void InterfaceAndConcreteDataConsolidatedShareOneSubscriptionList()
        {
            // regression for the interface event and the concrete event being two separate handler lists:
            // subscribing through one and unsubscribing through the other must cancel out
            var reference = new DateTime(2015, 4, 13);
            var spy = Symbols.SPY;
            var consolidator = new IdentityDataConsolidator<TradeBar>();
            IDataConsolidator asInterface = consolidator;

            var calls = 0;
            DataConsolidatedHandler handler = (_, __) => calls++;

            asInterface.DataConsolidated += handler;
            consolidator.DataConsolidated -= handler;

            consolidator.Update(new TradeBar { Symbol = spy, Time = reference, Close = 10m, Value = 10m, Period = Time.OneMinute });

            Assert.AreEqual(0, calls);
            consolidator.Dispose();
        }

        [Test]
        public void OutOfOrderDataDoesNotClearWindow()
        {
            // regression for count mode emitting a null bar on an out of order data point, which
            // previously reset the whole rolling window through the Consolidated setter
            var reference = new DateTime(2015, 4, 13);
            var spy = Symbols.SPY;
            var consolidator = new TradeBarConsolidator(1);
            var windowConsolidator = (ConsolidatorBase)consolidator;

            consolidator.Update(new TradeBar { Symbol = spy, Time = reference, Close = 10m, Value = 10m, Period = Time.OneMinute });
            consolidator.Update(new TradeBar { Symbol = spy, Time = reference.AddMinutes(1), Close = 20m, Value = 20m, Period = Time.OneMinute });

            Assert.AreEqual(2, windowConsolidator.Window.Count);

            consolidator.Update(new TradeBar { Symbol = spy, Time = reference.AddMinutes(-5), Close = 30m, Value = 30m, Period = Time.OneMinute });

            Assert.AreEqual(2, windowConsolidator.Window.Count);
            Assert.AreEqual(20m, windowConsolidator.Window[0].Value);
            Assert.AreEqual(10m, windowConsolidator.Window[1].Value);

            consolidator.Dispose();
        }

        [Test]
        public void CurrentAndPreviousAreNullBeforeFirstConsolidation()
        {
            var consolidator = new TradeBarConsolidator(1);
            var windowConsolidator = (ConsolidatorBase)consolidator;

            Assert.IsNull(windowConsolidator.Consolidated);
            Assert.IsNull(windowConsolidator.Current);
            Assert.IsNull(windowConsolidator.Previous);
            Assert.IsNull(windowConsolidator[0]);
            Assert.AreEqual(0, windowConsolidator.Window.Count);

            consolidator.Dispose();
        }

        [Test]
        public void ResetClearsWindowAndConsolidated()
        {
            var reference = new DateTime(2015, 4, 13);
            var spy = Symbols.SPY;
            var consolidator = new TradeBarConsolidator(1);
            var windowConsolidator = (ConsolidatorBase)consolidator;

            consolidator.Update(new TradeBar { Symbol = spy, Time = reference, Close = 10m, Value = 10m, Period = Time.OneMinute });
            consolidator.Update(new TradeBar { Symbol = spy, Time = reference.AddMinutes(1), Close = 20m, Value = 20m, Period = Time.OneMinute });

            Assert.AreEqual(2, windowConsolidator.Window.Count);
            Assert.IsNotNull(windowConsolidator.Consolidated);

            windowConsolidator.Reset();

            Assert.AreEqual(0, windowConsolidator.Window.Count);
            Assert.IsNull(windowConsolidator.Consolidated);
            Assert.IsNull(windowConsolidator.Current);
            Assert.IsNull(windowConsolidator.Previous);

            consolidator.Dispose();
        }

        private static IEnumerable<TestCaseData> WindowTestCases()
        {
            var reference = new DateTime(2015, 4, 13);
            var spy = Symbols.SPY;
            var ibm = Symbols.IBM;

            yield return new TestCaseData(
                new TradeBarConsolidator(1),
                new IBaseData[]
                {
                    new TradeBar { Symbol = spy, Time = reference, Close = 10m, Value = 10m, Period = Time.OneMinute },
                    new TradeBar { Symbol = spy, Time = reference.AddMinutes(1), Close = 20m, Value = 20m, Period = Time.OneMinute }
                },
                20m, 10m
            ).SetName("TradeBarConsolidator");

            yield return new TestCaseData(
                new QuoteBarConsolidator(1),
                new IBaseData[]
                {
                    new QuoteBar { Symbol = spy, Time = reference, Value = 10m, Period = Time.OneMinute },
                    new QuoteBar { Symbol = spy, Time = reference.AddMinutes(1), Value = 20m, Period = Time.OneMinute }
                },
                20m, 10m
            ).SetName("QuoteBarConsolidator");

            yield return new TestCaseData(
                new TickConsolidator(1),
                new IBaseData[]
                {
                    new Tick { Symbol = spy, Time = reference, Value = 10m, TickType = TickType.Trade },
                    new Tick { Symbol = spy, Time = reference.AddMinutes(1), Value = 20m, TickType = TickType.Trade }
                },
                20m, 10m
            ).SetName("TickConsolidator");

            yield return new TestCaseData(
                new TickQuoteBarConsolidator(1),
                new IBaseData[]
                {
                    new Tick { Symbol = spy, Time = reference, Value = 10m, TickType = TickType.Quote, BidPrice = 10m, AskPrice = 10m },
                    new Tick { Symbol = spy, Time = reference.AddMinutes(1), Value = 20m, TickType = TickType.Quote, BidPrice = 20m, AskPrice = 20m }
                },
                20m, 10m
            ).SetName("TickQuoteBarConsolidator");

            yield return new TestCaseData(
                new BaseDataConsolidator(1),
                new IBaseData[]
                {
                    new TradeBar { Symbol = spy, Time = reference, Close = 10m, Value = 10m, Period = Time.OneMinute },
                    new TradeBar { Symbol = spy, Time = reference.AddMinutes(1), Close = 20m, Value = 20m, Period = Time.OneMinute }
                },
                20m, 10m
            ).SetName("BaseDataConsolidator");

            yield return new TestCaseData(
                new IdentityDataConsolidator<TradeBar>(),
                new IBaseData[]
                {
                    new TradeBar { Symbol = spy, Time = reference, Close = 10m, Value = 10m, Period = Time.OneMinute },
                    new TradeBar { Symbol = spy, Time = reference.AddMinutes(1), Close = 20m, Value = 20m, Period = Time.OneMinute }
                },
                20m, 10m
            ).SetName("IdentityDataConsolidator");

            yield return new TestCaseData(
                new ClassicRenkoConsolidator(10),
                new IBaseData[]
                {
                    new IndicatorDataPoint(spy, reference, 0m),
                    new IndicatorDataPoint(spy, reference.AddMinutes(1), 10m),
                    new IndicatorDataPoint(spy, reference.AddMinutes(2), 20m)
                },
                20m, 10m
            ).SetName("ClassicRenkoConsolidator");

            yield return new TestCaseData(
                new RenkoConsolidator(1m),
                new IBaseData[]
                {
                    new IndicatorDataPoint(spy, reference, 10m),
                    new IndicatorDataPoint(spy, reference.AddMinutes(1), 12.1m)
                },
                12m, 11m
            ).SetName("RenkoConsolidator");

            yield return new TestCaseData(
                new RangeConsolidator(100, x => x.Value, x => 0m),
                new IBaseData[]
                {
                    new IndicatorDataPoint(ibm, reference, 90m),
                    new IndicatorDataPoint(ibm, reference.AddMinutes(1), 94.5m)
                },
                94.03m, 93.02m
            ).SetName("RangeConsolidator");

            yield return new TestCaseData(
                new SequentialConsolidator(new TradeBarConsolidator(1), new TradeBarConsolidator(1)),
                new IBaseData[]
                {
                    new TradeBar { Symbol = spy, Time = reference, Close = 10m, Value = 10m, Period = Time.OneMinute },
                    new TradeBar { Symbol = spy, Time = reference.AddMinutes(1), Close = 20m, Value = 20m, Period = Time.OneMinute }
                },
                20m, 10m
            ).SetName("SequentialConsolidator");
        }
    }
}
