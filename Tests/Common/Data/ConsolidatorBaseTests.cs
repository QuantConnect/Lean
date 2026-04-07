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
