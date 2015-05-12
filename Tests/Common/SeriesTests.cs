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
using System.Linq;
using NUnit.Framework;

namespace QuantConnect.Tests.Common
{
    [TestFixture]
    public class SeriesTests
    {
        [Test]
        public void RespectsMostRecentTimeOnDuplicatePoints()
        {
            var series = new Series();
            series.AddPoint(DateTime.Today, 1m);
            series.AddPoint(DateTime.Today, 2m);
            Assert.AreEqual(1, series.Values.Count);
            Assert.AreEqual(2m, series.Values.Single().y);
        }
    }
}
