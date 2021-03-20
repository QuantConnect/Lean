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
using QuantConnect.Report;

namespace QuantConnect.Tests.Report
{
    [TestFixture]
    public class DrawdownCollectionTests
    {
        [Test]
        public void MaxDrawdown()
        {
            var series = new Deedle.Series<DateTime, double>(new []
            {
                new KeyValuePair<DateTime, double>(new DateTime(2020, 1, 1), 100000),
                new KeyValuePair<DateTime, double>(new DateTime(2020, 1, 2), 90000),
                new KeyValuePair<DateTime, double>(new DateTime(2020, 1, 3), 100000),
                new KeyValuePair<DateTime, double>(new DateTime(2020, 1, 4), 100000),
                new KeyValuePair<DateTime, double>(new DateTime(2020, 1, 5), 80000)
            });

            var collection = DrawdownCollection.GetDrawdownPeriods(series, 1).ToList();

            Assert.AreEqual(1, collection.Count);
            Assert.AreEqual(0.2, collection.First().Drawdown, 0.0001);
        }
    }
}
