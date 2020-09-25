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
using NodaTime;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Tests.Common.Data.Fundamental
{
    [TestFixture]
    public class CoarseFundamentalTests
    {
        [TestCase("AAWW TJ2ZLXWLWA5H,AAWW,33.78,126629,4277527,True,1,1")]
        [TestCase("AAXJ U55LDE3TN4O5,AAXJ,57.36,224926,12901755,False,0.9186624,1")]
        [TestCase("ABB S3MVQ2U3Z59H,ABB,24.91,894520,22282493,True,0.837195,1")]
        [TestCase("ABB S3MVQ2U3Z59H,ABB,24.91,894520,22282493,True,10.837195,1")]
        [TestCase("ABB S3MVQ2U3Z59H,ABB,24.91,894520,22282493,True,0.837195,1")]
        public void ToRow(string coarseLine)
        {
            var config = new SubscriptionDataConfig(typeof(CoarseFundamental),
                Symbols.SPY,
                Resolution.Second,
                DateTimeZone.Utc,
                DateTimeZone.Utc,
                false,
                false,
                false);
            var factory = new CoarseFundamental();
            var coarseRead = (CoarseFundamental)factory.Reader(config, coarseLine, DateTime.UtcNow, false);

            var row = CoarseFundamental.ToRow(coarseRead);
            Assert.AreEqual(coarseLine, row);
        }
    }
}
