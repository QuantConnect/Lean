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
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class StandardDeviationTests
    {
        [Test]
        public void ComputesCorrectly()
        {
            // Indicator output was compared against values from "std" function in Julia.
            var std = new StandardDeviation(3);
            var reference = DateTime.MinValue;

            std.Update(reference.AddDays(1), 1m);
            Assert.AreEqual(1m, std.Current.Value);

            std.Update(reference.AddDays(2), -1m);
            Assert.AreEqual(-1m, std.Current.Value);

            std.Update(reference.AddDays(3), 1m);
            Assert.AreEqual(1.15470053837925m, std.Current.Value);

            std.Update(reference.AddDays(4), -2m);
            Assert.AreEqual(1.52752523165195m, std.Current.Value);

            std.Update(reference.AddDays(5), 3m);
            Assert.AreEqual(2.51661147842358m, std.Current.Value);
        }
    }
}
