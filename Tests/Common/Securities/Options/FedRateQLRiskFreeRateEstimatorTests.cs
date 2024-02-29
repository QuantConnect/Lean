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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class FedRateQLRiskFreeRateEstimatorTests
    {
        [TestCase("20200306", 0.0175)]      // Friday
        [TestCase("20200307", 0.0175)]      // Saturday, use Friday's value
        [TestCase("20200308", 0.0175)]      // Sunday, use Friday's value
        [TestCase("20200310", 0.0175)]      // Tuesday
        public void Estimate(string dateString, decimal rate)
        {
            var spx = Symbols.SPX;
            var tz = TimeZones.NewYork;
            var optionSymbol = Symbol.CreateOption(spx.Value, spx.ID.Market, OptionStyle.European, OptionRight.Put, 4200,
                new DateTime(2021, 1, 15));
            var evaluationDate = Parse.DateTimeExact(dateString, "yyyyMMdd");

            // setting up
            var equity = OptionPriceModelTests.GetEquity(spx, 100m, 0.25m, tz);
            var option = OptionPriceModelTests.GetOption(optionSymbol, equity, tz);
            var tick = new Tick { Time = evaluationDate, Value = 10m };

            // get the risk free rate
            var estimator = new FedRateQLRiskFreeRateEstimator();
            var result = estimator.Estimate(option, 
                new Slice(evaluationDate, new List<BaseData> { tick }, evaluationDate), 
                new OptionContract(option, spx));

            Assert.AreEqual(rate, result);
        }
    }
}
