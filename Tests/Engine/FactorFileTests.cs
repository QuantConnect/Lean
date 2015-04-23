﻿/*
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
using System.Collections.Generic;
using NUnit.Framework;
using QuantConnect.Lean.Engine;

namespace QuantConnect.Tests.Engine
{
    [TestFixture]
    public class FactorFileTests
    {
        [Test]
        public void CorrectlyDeterminesTimePriceFactors()
        {
            var reference = DateTime.Today;

            const string symbol = "n/a";
            var file = GetTestFactorFile(symbol, reference);

            // time price factors should be the price factor * split factor

            Assert.AreEqual(1, file.GetTimePriceFactor(reference));
            Assert.AreEqual(1, file.GetTimePriceFactor(reference.AddDays(-6)));
            Assert.AreEqual(.9, file.GetTimePriceFactor(reference.AddDays(-7)));
            Assert.AreEqual(.9, file.GetTimePriceFactor(reference.AddDays(-13)));
            Assert.AreEqual(.8, file.GetTimePriceFactor(reference.AddDays(-14)));
            Assert.AreEqual(.8, file.GetTimePriceFactor(reference.AddDays(-20)));
            Assert.AreEqual(.8m * .5m, file.GetTimePriceFactor(reference.AddDays(-21)));
            Assert.AreEqual(.8m * .5m, file.GetTimePriceFactor(reference.AddDays(-22)));
            Assert.AreEqual(.8m * .5m, file.GetTimePriceFactor(reference.AddDays(-89)));
            Assert.AreEqual(.8m * .25m, file.GetTimePriceFactor(reference.AddDays(-91)));
        }

        [Test]
        public void HasDividendEventOnNextTradingDay()
        {
            var reference = DateTime.Today;

            const string symbol = "n/a";
            var file = GetTestFactorFile(symbol, reference);
            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-6)));
            Assert.IsTrue (file.HasDividendEventOnNextTradingDay(reference.AddDays(-7)));
            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-8)));

            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-13)));
            Assert.IsTrue (file.HasDividendEventOnNextTradingDay(reference.AddDays(-14)));
            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-15)));

            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-364)));
            Assert.IsTrue (file.HasDividendEventOnNextTradingDay(reference.AddDays(-365)));
            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-366)));
        }

        [Test]
        public void HasSplitEventOnNextTradingDay()
        {
            var reference = DateTime.Today;

            const string symbol = "n/a";
            var file = GetTestFactorFile(symbol, reference);
            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-20)));
            Assert.IsTrue (file.HasSplitEventOnNextTradingDay(reference.AddDays(-21)));
            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-22)));

            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-89)));
            Assert.IsTrue (file.HasSplitEventOnNextTradingDay(reference.AddDays(-90)));
            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-91)));

            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-364)));
            Assert.IsTrue (file.HasSplitEventOnNextTradingDay(reference.AddDays(-365)));
            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-366)));
        }

        private static FactorFile GetTestFactorFile(string symbol, DateTime reference)
        {
            var file = new FactorFile(symbol, new List<FactorFileRow>
            {
                new FactorFileRow(reference, 1, 1),
                new FactorFileRow(reference.AddDays(-7), .9m, 1),       // dividend
                new FactorFileRow(reference.AddDays(-14), .8m, 1),      // dividend
                new FactorFileRow(reference.AddDays(-21), .8m, .5m),    // split
                new FactorFileRow(reference.AddDays(-90), .8m, .25m),   // split
                new FactorFileRow(reference.AddDays(-365), .7m, .0125m) // split+dividend
            });
            return file;
        }
    }
}
