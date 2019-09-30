/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace QuantConnect.Tests.ToolBox
{
    [TestFixture]
    public class PsychSignalDataTests
    {
        [Test, Ignore("This test requires raw PsychSignal data")]
        public void FileHourMatchesDataTimeRealRawData()
        {
            var rawPath = Path.Combine("raw", "alternative", "psychsignal");

            foreach (var file in Directory.GetFiles(rawPath, "*.csv", SearchOption.TopDirectoryOnly).ToList())
            {
                var fileSplit = file.Split('_');

                var hour = fileSplit[fileSplit.Length - 4].ConvertInvariant<int>();

                // Read one line of the file, and compare the hour of the day to the hour on the file
                var line = File.ReadLines(file).Last().Split(',');

                // SOURCE[0],SYMBOL[1],TIMESTAMP_UTC[2],BULLISH_INTENSITY[3],BEARISH_INTENSITY[4],BULL_MINUS_BEAR[5],BULL_SCORED_MESSAGES[6],BEAR_SCORED_MESSAGES[7],BULL_BEAR_MSG_RATIO[8],TOTAL_SCANNED_MESSAGES[9]
                var date = Parse.DateTime(line[2]).ToUniversalTime();

                Assert.AreEqual(hour, date.Hour);
            }
        }

        [Test]
        public void FileHourMatchesFakeDataTime()
        {
            var line = File.ReadLines(Path.Combine("TestData", "00010101_05_example_psychsignal_testdata.csv")).Last().Split(',');
            var hour = 5;

            var date = Parse.DateTime(line[2]).ToUniversalTime();

            Assert.AreEqual(hour, date.Hour);
        }
    }
}
