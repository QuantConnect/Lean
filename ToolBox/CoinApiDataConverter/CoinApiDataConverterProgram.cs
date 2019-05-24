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

using System;
using System.Globalization;

namespace QuantConnect.ToolBox.CoinApiDataConverter
{
    /// <summary>
    /// Coin API Main entry point for ToolBox.
    /// </summary>
    public static class CoinApiDataConverterProgram
    {
        public static void CoinApiDataProgram(string date, string market, string rawDataFolder, string destinationFolder)
        {
            var processingDate = DateTime.ParseExact(date, DateFormat.EightCharacter, CultureInfo.InvariantCulture);
            var converter  = new CoinApiDataConverter(processingDate, market, rawDataFolder, destinationFolder);
            converter.Run();
        }
    }
}
