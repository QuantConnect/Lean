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

using QuantConnect.Logging;
using System;
using System.Globalization;
using System.IO;

namespace QuantConnect.ToolBox.BrainDataConverter
{
    public class BrainDataProgram
    {
        /// <summary>
        /// Converts the raw data to a format usable by LEAN
        /// </summary>
        /// <param name="date">Date to convert</param>
        /// <param name="sourceDirectory">Source directory. This should be the top-level directory of the raw data folder</param>
        /// <param name="destinationDirectory">Destination directory. This should be your Data Folder</param>
        /// <param name="market">Market to convert data for</param>
        public static void BrainDataConverter(string date, string sourceDirectory, string destinationDirectory, string market)
        {
            var dateOf = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
            var sourceDirectoryInfo = new DirectoryInfo(Path.Combine(sourceDirectory, "alternative", "braindata"));
            var destinationDirectoryInfo = new DirectoryInfo(Path.Combine(destinationDirectory, "alternative", "braindata"));

            var converter = new BrainDataConverter(sourceDirectoryInfo, destinationDirectoryInfo, market);
            if (!converter.Convert(dateOf))
            {
                Log.Error("BrainDataProgram.BrainDataConverter(): Failed to convert BrainData data");
            }
        }
    }
}
