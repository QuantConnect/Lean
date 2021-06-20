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
using QuantConnect.Data.Custom.VIXCentral;
using System;
using System.IO;
using System.Linq;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class VIXCentralContangoTests
    {
        private readonly VIXCentralContango _factory = new VIXCentralContango();
        private readonly SubscriptionDataConfig _config = new SubscriptionDataConfig(
            typeof(VIXCentralContango),
            Symbol.Create("VIX", SecurityType.Base, QuantConnect.Market.USA, baseDataType: typeof(VIXCentralContango)),
            Resolution.Daily,
            TimeZones.NewYork,
            TimeZones.NewYork,
            false,
            false,
            false,
            true);

        [TestCase("Date,First Month,F1,F2,F3,F4,F5,F6,F7,F8,F9,F10,F11,F12,Contango 2/1,Contango 7/4,Con 7/4 div 3")]
        [TestCase("")]
        [TestCase("test")]
        public void DoesNotReadBadData(string header)
        {
            var instance = _factory.Reader(_config, header, default(DateTime), false);
            Assert.IsNull(instance);
        }

        [Test]
        public void ReadsIncompleteData()
        {
            var line = "2021-05-14,5.0,19.1312,21.3668,22.8053,23.3341,23.8138,24.0424,23.8489,23.5,24.425,,,,0.1169,0.0221,0.0074";
            var instance = (VIXCentralContango)_factory.Reader(_config, line, default(DateTime), false);

            Assert.AreEqual(new DateTime(2021, 5, 14), instance.Time);
            Assert.AreEqual(new DateTime(2021, 5, 15), instance.EndTime);
            Assert.AreEqual(_config.Symbol, instance.Symbol);

            Assert.AreEqual(5, instance.FrontMonth);
            Assert.AreEqual(19.1312m, instance.F1);
            Assert.AreEqual(21.3668m, instance.F2);
            Assert.AreEqual(22.8053m, instance.F3);
            Assert.AreEqual(23.3341m, instance.F4);
            Assert.AreEqual(23.8138m, instance.F5);
            Assert.AreEqual(24.0424m, instance.F6);
            Assert.AreEqual(23.8489m, instance.F7);
            Assert.AreEqual(23.5m, instance.F8);
            Assert.AreEqual(24.425m, instance.F9);
            Assert.AreEqual(null, instance.F10);
            Assert.AreEqual(null, instance.F11);
            Assert.AreEqual(null, instance.F12);
            Assert.AreEqual(0.1169m, instance.Contango_F2_Minus_F1);
            Assert.AreEqual(0.0221m, instance.Contango_F7_Minus_F4);
            Assert.AreEqual(0.0074m, instance.Contango_F7_Minus_F4_Div_3);
        }

        [Test]
        public void ReadsMissingF9Data()
        {
            var line = "2016-07-20,8.0,15.475,17.225,18.475,18.825,18.925,19.875,20.125,20.225,,,,,0.1131,0.06910000000000001,0.023";
            var instance = (VIXCentralContango)_factory.Reader(_config, line, default(DateTime), false);

            Assert.AreEqual(new DateTime(2016, 7, 20), instance.Time);
            Assert.AreEqual(new DateTime(2016, 7, 21), instance.EndTime);
            Assert.AreEqual(_config.Symbol, instance.Symbol);

            Assert.AreEqual(8, instance.FrontMonth);
            Assert.AreEqual(15.475m, instance.F1);
            Assert.AreEqual(17.225m, instance.F2);
            Assert.AreEqual(18.475m, instance.F3);
            Assert.AreEqual(18.825m, instance.F4);
            Assert.AreEqual(18.925m, instance.F5);
            Assert.AreEqual(19.875m, instance.F6);
            Assert.AreEqual(20.125m, instance.F7);
            Assert.AreEqual(20.225m, instance.F8);
            Assert.AreEqual(null, instance.F9);   // F9 is missing in some occasions
            Assert.AreEqual(null, instance.F10);
            Assert.AreEqual(null, instance.F11);
            Assert.AreEqual(null, instance.F12);
            Assert.AreEqual(0.1131m, instance.Contango_F2_Minus_F1);
            Assert.AreEqual(0.06910000000000001m, instance.Contango_F7_Minus_F4);
            Assert.AreEqual(0.0230m, instance.Contango_F7_Minus_F4_Div_3);
        }


        [Test]
        public void ReadsCompleteData()
        {
            var line = "2019-12-16,12.0,12.225,14.825,16.425,16.775,17.325,17.425,17.625,17.975,18.075,18.55,19.075,18.675,0.2127,0.0507,0.0169";
            var instance = (VIXCentralContango)_factory.Reader(_config, line, default(DateTime), false);

            Assert.AreEqual(new DateTime(2019, 12, 16), instance.Time);
            Assert.AreEqual(new DateTime(2019, 12, 17), instance.EndTime);
            Assert.AreEqual(_config.Symbol, instance.Symbol);

            Assert.AreEqual(12, instance.FrontMonth);
            Assert.AreEqual(12.225m, instance.F1);
            Assert.AreEqual(14.825m, instance.F2);
            Assert.AreEqual(16.425m, instance.F3);
            Assert.AreEqual(16.775m, instance.F4);
            Assert.AreEqual(17.325m, instance.F5);
            Assert.AreEqual(17.425m, instance.F6);
            Assert.AreEqual(17.625m, instance.F7);
            Assert.AreEqual(17.975m, instance.F8);
            Assert.AreEqual(18.075m, instance.F9);
            Assert.AreEqual(18.55m, instance.F10);
            Assert.AreEqual(19.075m, instance.F11);
            Assert.AreEqual(18.675m, instance.F12);
            Assert.AreEqual(0.2127m, instance.Contango_F2_Minus_F1);
            Assert.AreEqual(0.0507m, instance.Contango_F7_Minus_F4);
            Assert.AreEqual(0.0169m, instance.Contango_F7_Minus_F4_Div_3);
        }

        [Test]
        public void ReadsSampleFile()
        {
            VIXCentralContango instance = null;

            var lines = File.ReadLines(Path.Combine("TestData", "vix_contango.csv")).Skip(1);
            foreach (var line in lines)
            {
                Assert.DoesNotThrow(() => instance = (VIXCentralContango)_factory.Reader(_config, line, default(DateTime), false));
                Assert.NotNull(instance);
            }
        }
    }
}
