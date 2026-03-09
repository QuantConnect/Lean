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
using QuantConnect.Data.Consolidators;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;

namespace QuantConnect.Tests.Indicators
{
    public interface ITestMcClellanOscillator
    {
        public void TestUpdate(IndicatorDataPoint input);
    }

    /// <summary> 
    /// Miscellaneous tool for McClellan Indicator test
    /// </summary>
    public class McClellanIndicatorTestHelper
    {
        /// <summary> 
        /// Run test for McClellan Indicator
        /// </summary>
        /// <param name="indicator">McClellan Indicator instance</param>
        /// <param name="fileName">External source file name</param>
        /// <param name="columnName">External source reference column name</param>
        public static void RunTestIndicator<T>(T indicator, string fileName, string columnName)
            where T : TradeBarIndicator, ITestMcClellanOscillator
        {
            foreach (var parts in TestHelper.GetCsvFileStream(fileName))
            {
                parts.TryGetValue("a/d difference", out var adDifference);
                parts.TryGetValue("date", out var date);

                var data = new IndicatorDataPoint(Parse.DateTimeExact(date, "yyyyMMdd"), adDifference.ToDecimal());
                indicator.TestUpdate(data);

                if (!indicator.IsReady || !parts.TryGetValue(columnName, out var expected))
                {
                    continue;
                }

                // Source data has only 2 decimal places
                Assert.AreEqual(Parse.Double(expected), (double)indicator.Current.Value, 0.02d);
            }
        }

        /// <summary>
        /// Updates the given consolidator with the entries from the given external CSV file
        /// </summary>
        /// <param name="renkoConsolidator">RenkoConsoliadtor instance</param>
        /// <param name="fileName">External source file name</param>
        public static void UpdateRenkoConsolidator(IDataConsolidator renkoConsolidator, string fileName)
        {
            var closeValue = 1m;
            foreach (var parts in TestHelper.GetCsvFileStream(fileName))
            {
                parts.TryGetValue("a/d difference", out var adDifference);
                parts.TryGetValue("date", out var date);

                var data = new TradeBar() { Symbol = Symbols.SPY, Close = closeValue, Open = closeValue - 1, Volume = 1, Time = Parse.DateTimeExact(date, "yyyyMMdd") };
                closeValue++;
                renkoConsolidator.Update(data);
            }
        }

        /// <summary> 
        /// Get the simulated number of advance and decline asset
        /// </summary>
        /// <param name="adDifference">Number of advancing asset minus that of declining ones</param>
        /// <param name="advance">Simulated number of advancing asset</param>
        /// <param name="decline">Simulated number of declining asset</param>
        public static bool GetAdvanceDeclineNumber(decimal adDifference, out int advance, out int decline)
        {
            // x + (3000 - x) = adDifference
            var simulatedAdvance = (adDifference + 2530m) / 2m;

            // Both -0.5 if `simulatedAdvance` is not divisible by 2
            if (simulatedAdvance % 1 != 0)
            {
                advance = (int)Math.Floor(simulatedAdvance);
                decline = 2530 - advance - 1;
                return false;
            }

            advance = (int)simulatedAdvance;
            decline = 2530 - advance;
            return true;
        }
    }
}
