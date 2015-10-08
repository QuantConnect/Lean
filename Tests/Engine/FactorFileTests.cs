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
using QuantConnect.Data.Auxiliary;

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

            Assert.AreEqual(1, file.GetPriceScaleFactor(reference));
            Assert.AreEqual(1, file.GetPriceScaleFactor(reference.AddDays(-6)));
            Assert.AreEqual(.9, file.GetPriceScaleFactor(reference.AddDays(-7)));
            Assert.AreEqual(.9, file.GetPriceScaleFactor(reference.AddDays(-13)));
            Assert.AreEqual(.8, file.GetPriceScaleFactor(reference.AddDays(-14)));
            Assert.AreEqual(.8, file.GetPriceScaleFactor(reference.AddDays(-20)));
            Assert.AreEqual(.8m * .5m, file.GetPriceScaleFactor(reference.AddDays(-21)));
            Assert.AreEqual(.8m * .5m, file.GetPriceScaleFactor(reference.AddDays(-22)));
            Assert.AreEqual(.8m * .5m, file.GetPriceScaleFactor(reference.AddDays(-89)));
            Assert.AreEqual(.8m * .25m, file.GetPriceScaleFactor(reference.AddDays(-91)));
        }

        [Test]
        public void HasDividendEventOnNextTradingDay()
        {
            var reference = DateTime.Today;

            const string symbol = "n/a";
            decimal priceFactorRatio;
            var file = GetTestFactorFile(symbol, reference);

            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference, out priceFactorRatio));

            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-6), out priceFactorRatio));
            Assert.IsTrue(file.HasDividendEventOnNextTradingDay(reference.AddDays(-7), out priceFactorRatio));
            Assert.AreEqual(.9m/1m, priceFactorRatio);
            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-8), out priceFactorRatio));

            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-13), out priceFactorRatio));
            Assert.IsTrue(file.HasDividendEventOnNextTradingDay(reference.AddDays(-14), out priceFactorRatio));
            Assert.AreEqual(.8m / .9m, priceFactorRatio);
            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-15), out priceFactorRatio));

            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-364), out priceFactorRatio));
            Assert.IsTrue(file.HasDividendEventOnNextTradingDay(reference.AddDays(-365), out priceFactorRatio));
            Assert.AreEqual(.7m / .8m, priceFactorRatio);
            Assert.IsFalse(file.HasDividendEventOnNextTradingDay(reference.AddDays(-366), out priceFactorRatio));
        }

        [Test]
        public void HasSplitEventOnNextTradingDay()
        {
            var reference = DateTime.Today;

            const string symbol = "n/a";
            decimal splitFactor;
            var file = GetTestFactorFile(symbol, reference);

            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference, out splitFactor));

            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-20), out splitFactor));
            Assert.IsTrue(file.HasSplitEventOnNextTradingDay(reference.AddDays(-21), out splitFactor));
            Assert.AreEqual(.5, splitFactor);
            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-22), out splitFactor));

            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-89), out splitFactor));
            Assert.IsTrue(file.HasSplitEventOnNextTradingDay(reference.AddDays(-90), out splitFactor));
            Assert.AreEqual(.5, splitFactor);
            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-91), out splitFactor));

            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-364), out splitFactor));
            Assert.IsTrue(file.HasSplitEventOnNextTradingDay(reference.AddDays(-365), out splitFactor));
            Assert.AreEqual(.5, splitFactor);
            Assert.IsFalse(file.HasSplitEventOnNextTradingDay(reference.AddDays(-366), out splitFactor));
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
                new FactorFileRow(reference.AddDays(-365), .7m, .125m) // split+dividend
            });
            return file;
        }
    }
}
