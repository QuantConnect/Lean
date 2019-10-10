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

namespace QuantConnect.ToolBox.Benzinga
{
    public static class BenzingaProgram
    {
        public static void BenzingaDataConverter(string source, string destination, string day)
        {
            var date = Parse.DateTimeExact(day, "yyyyMMdd");
            var converter = new BenzingaDataConverter(new DirectoryInfo(source), new DirectoryInfo(destination));

            converter.Convert(date);
        }
    }
}
