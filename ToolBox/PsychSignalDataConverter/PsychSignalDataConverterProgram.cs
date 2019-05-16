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

using System.IO;
using QuantConnect.Logging;
using DataConverter = QuantConnect.ToolBox.PsychSignalDataConverter.PsychSignalDataConverter;

namespace QuantConnect.ToolBox.PsychSignalDataConverter
{
    public static class PsychSignalDataConverterProgram
    {
        /// <summary>
        /// Entry point for ToolBox application PsychSignalDataConverter
        /// </summary>
        /// <param name="dataAbsolutePath"></param>
        /// <param name="securityType"></param>
        /// <param name="market"></param>
        public static void PsychSignalDataConverter(string dataAbsolutePath, string securityType, string market)
        {
            Directory.CreateDirectory(Path.Combine(Globals.DataFolder, "equity", market, "alternative", "psychsignal"));

            if (securityType != "equity")
            {
                Log.Error("Only equity data is supported. Exiting...");
                return;
            }

            var converter = new DataConverter();

            converter.Convert(dataAbsolutePath, SecurityType.Equity, market);
        }
    }
}
