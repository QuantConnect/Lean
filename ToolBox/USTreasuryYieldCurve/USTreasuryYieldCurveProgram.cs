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

namespace QuantConnect.ToolBox.USTreasuryYieldCurve
{
    public class USTreasuryYieldCurveProgram
    {
        public static void USTreasuryYieldCurveRateDownloader(DateTime fromDate, DateTime toDate, string destinationDirectory)
        {
            var downloader = new USTreasuryYieldCurveDownloader(destinationDirectory);
            downloader.Download();
        }

        /// <summary>
        /// Converts US Treasury yield curve data
        /// </summary>
        /// <param name="sourceDirectory">Directory where the data is contained</param>
        /// <param name="destinationDirectory">Directory where the data will be written to</param>
        public static void USTreasuryYieldCurveConverter(string sourceDirectory, string destinationDirectory)
        {
            var converter = new USTreasuryYieldCurveConverter(sourceDirectory, destinationDirectory);
            converter.Convert();
        }
    }
}
