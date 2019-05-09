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
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    [TestFixture]
    public class InsightCollectionTests
    {
        private static string _expectedResultCopyTo = "[{\"id\":\"6d077d17f22943009b04919cdc2b897f\",\"generated-time\":1546300800.0,\"close-time\":1546300800.0,\"symbol\":\"AAPL R735QTJ8XC9X\",\"ticker\":\"AAPL\",\"type\":\"price\",\"reference\":0.0,\"direction\":\"up\",\"period\":86400.0},{\"id\":\"47f61ba2ff9c4c859ce06f29124d9f7f\",\"generated-time\":1546387200.0,\"close-time\":1546387200.0,\"symbol\":\"AAPL R735QTJ8XC9X\",\"ticker\":\"AAPL\",\"type\":\"volatility\",\"reference\":0.0,\"direction\":\"up\",\"period\":86400.0},{\"id\":\"ae689c34697b496e9ffcda2adc395b17\",\"generated-time\":1546646400.0,\"close-time\":1546646400.0,\"symbol\":\"AAPL R735QTJ8XC9X\",\"ticker\":\"AAPL\",\"type\":\"price\",\"reference\":0.0,\"direction\":\"up\",\"period\":86400.0},{\"id\":\"f06d2464d1f94361bcd000c18c7d2554\",\"generated-time\":1546732800.0,\"close-time\":1546732800.0,\"symbol\":\"AAPL R735QTJ8XC9X\",\"ticker\":\"AAPL\",\"type\":\"price\",\"reference\":0.0,\"direction\":\"flat\",\"period\":86400.0},{\"id\":\"d47f22e06e58434c9f71f71dd4691673\",\"generated-time\":1546819200.0,\"close-time\":1546819200.0,\"symbol\":\"AAPL R735QTJ8XC9X\",\"ticker\":\"AAPL\",\"type\":\"price\",\"reference\":0.0,\"direction\":\"down\",\"period\":86400.0},{\"id\":\"9a37bd59a37847599bd48cfe209e2887\",\"generated-time\":1546905600.0,\"close-time\":1546905600.0,\"symbol\":\"AAPL R735QTJ8XC9X\",\"ticker\":\"AAPL\",\"type\":\"volatility\",\"reference\":0.0,\"direction\":\"up\",\"period\":86400.0},{\"id\":\"66f1c3a7b3504b039834cecdc003a1ba\",\"generated-time\":1546992000.0,\"close-time\":1546992000.0,\"symbol\":\"AAPL R735QTJ8XC9X\",\"ticker\":\"AAPL\",\"type\":\"volatility\",\"reference\":0.0,\"direction\":\"down\",\"period\":86400.0}]";
        
        [Test]
        public static void InsightCollectionShouldBeAbleToBeConvertedToListWithoutStackOverflow()
        {
            var aapl = Symbol.Create("AAPL", SecurityType.Equity, "usa");
            var insightCollection = new InsightCollection();

            insightCollection.Add(new Insight(aapl, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up)
            {
                CloseTimeUtc = new DateTime(2019, 1, 1),
            });
            insightCollection.Add(new Insight(aapl, new TimeSpan(1, 0, 0, 0), InsightType.Volatility, InsightDirection.Up)
            {
                CloseTimeUtc = new DateTime(2019, 1, 2),
            });
            
            Assert.DoesNotThrow(() => insightCollection.OrderBy(x => x.CloseTimeUtc).ToList());
        }

        [Test]
        public static void InsightCollectionCopyTo()
        {
            var aapl = Symbol.Create("AAPL", SecurityType.Equity, "usa");
            var insightCollection = new InsightCollection();

            insightCollection.Add(new Insight(aapl, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up)
            {
                GeneratedTimeUtc = new DateTime(2019, 1, 1),
                CloseTimeUtc = new DateTime(2019, 1, 1),
            });
            insightCollection.Add(new Insight(aapl, new TimeSpan(1, 0, 0, 0), InsightType.Volatility, InsightDirection.Up)
            {
                GeneratedTimeUtc = new DateTime(2019, 1, 2),
                CloseTimeUtc = new DateTime(2019, 1, 2),
            });

            var insights = new Insight[7] {
                new Insight(aapl, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Up)
                {
                    GeneratedTimeUtc = new DateTime(2019, 1, 5),
                    CloseTimeUtc = new DateTime(2019, 1, 5),
                },
                new Insight(aapl, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Flat)
                {
                    GeneratedTimeUtc = new DateTime(2019, 1, 6),
                    CloseTimeUtc = new DateTime(2019, 1, 6),
                },
                new Insight(aapl, new TimeSpan(1, 0, 0, 0), InsightType.Price, InsightDirection.Down)
                {
                    GeneratedTimeUtc = new DateTime(2019, 1, 7),
                    CloseTimeUtc = new DateTime(2019, 1, 7),
                },
                new Insight(aapl, new TimeSpan(1, 0, 0, 0), InsightType.Volatility, InsightDirection.Up)
                {
                    GeneratedTimeUtc = new DateTime(2019, 1, 8),
                    CloseTimeUtc = new DateTime(2019, 1, 8),
                },
                new Insight(aapl, new TimeSpan(1, 0, 0, 0), InsightType.Volatility, InsightDirection.Down)
                {
                    GeneratedTimeUtc = new DateTime(2019, 1, 9),
                    CloseTimeUtc = new DateTime(2019, 1, 9),
                }, 
                null,
                null
            };

            insightCollection.CopyTo(insights, 5);

            var orderedInsights = insights.OrderBy(x => x.CloseTimeUtc).ToList();
            var orderedInsightsDe = JsonConvert.DeserializeObject<InsightCollection>(_expectedResultCopyTo);

            var equals = orderedInsights.Zip(orderedInsightsDe, (i, de) =>
            {
                return i.CloseTimeUtc == de.CloseTimeUtc &&
                    i.GeneratedTimeUtc == de.GeneratedTimeUtc &&
                    i.Symbol.Value == de.Symbol.Value &&
                    i.Type == de.Type &&
                    i.ReferenceValue == de.ReferenceValue &&
                    i.Direction == de.Direction &&
                    i.Period == de.Period;
            });

            Assert.IsTrue(equals.All((x) => x));
        }
    }
}
