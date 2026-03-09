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
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Tests.Common.Data.UniverseSelection
{
    [TestFixture]
    public class CoarseFundamentalTests
    {
        [Test, TestCaseSource(nameof(TestParameters))]
        public void ParsesCoarseCsvLine(string line, bool hasFundamentalData, decimal price, decimal priceFactor, decimal splitFactor, decimal adjustedPrice)
        {
            var cf = (CoarseFundamental)CoarseFundamentalDataProvider.Read(line, DateTime.MinValue);

            Assert.AreEqual(hasFundamentalData, cf.HasFundamentalData);
            Assert.AreEqual(price, cf.Price);
            Assert.AreEqual(priceFactor, cf.PriceFactor);
            Assert.AreEqual(splitFactor, cf.SplitFactor);
            Assert.AreEqual(adjustedPrice, cf.AdjustedPrice);
        }

        public static object[] TestParameters =
        {
            new object[] { "AAPL R735QTJ8XC9X,AAPL,537.46,5483955,3490219402,True", true, 537.46m, 1m, 1m, 537.46m },
            new object[] { "AAPL R735QTJ8XC9X,AAPL,645.57,7831583,5055835037,True,0.9304792,0.142857", true, 645.57m, 0.9304792m, 0.142857m, 85.812693779220408m },
            new object[] { "AAPL R735QTJ8XC9X,AAPL,93.7,37807206,3542535202,True,0.9304792,1", true, 93.7m, 0.9304792m, 1m, 87.18590104m },
        };

    }
}
