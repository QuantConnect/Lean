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
using System.IO;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Type representing the various pieces of information emebedded into a lean data file path
    /// </summary>
    public class LeanDataPathComponents
    {
        public readonly DateTime Date;
        public readonly SecurityType SecurityType;
        public readonly string Market;
        public readonly Resolution Resolution;
        public readonly string Filename;
        public readonly Symbol Symbol; // for options this is a 'canonical' symbol using info derived from the path

        /// <summary>
        /// Initializes a new instance of the <see cref="LeanDataPathComponents"/> class
        /// </summary>
        public LeanDataPathComponents(SecurityType securityType, string market, Resolution resolution, Symbol symbol, string filename, DateTime date)
        {
            Date = date;
            SecurityType = securityType;
            Market = market;
            Resolution = resolution;
            Filename = filename;
            Symbol = symbol;
        }

        /// <summary>
        /// Parses the specified path into a new instance of the <see cref="LeanDataPathComponents"/> class
        /// </summary>
        /// <param name="path">The path to be parsed</param>
        /// <returns>A new instance of the <see cref="LeanDataPathComponents"/> class representing the specified path</returns>
        public static LeanDataPathComponents Parse(string path)
        {
            //"../Data/equity/usa/hour/spy.zip"
            //"../Data/equity/usa/hour/spy/20160218_trade.zip"
            var fileinfo = new FileInfo(path);
            var filename = fileinfo.Name;
            var parts = path.Split('/', '\\');
            if (parts.Length < 4)
            {
                throw new FormatException("Unexpected path format: " + path);
            }

            var offset = 4;
            SecurityType securityType;
            var rawValue = parts[parts.Length - offset];
            if (!Enum.TryParse(rawValue, true, out securityType))
            {
                offset++;
                rawValue = parts[parts.Length - offset];
                if (!Enum.TryParse(rawValue, true, out securityType))
                {
                    throw new FormatException("Unexpected path format: " + path);
                }
            }

            var market = parts[parts.Length - offset + 1];
            var resolution = (Resolution) Enum.Parse(typeof (Resolution), parts[parts.Length - offset + 2], true);
            var ticker = offset == 4 ? Path.GetFileNameWithoutExtension(path) : parts[parts.Length - offset + 3];
            var date = offset == 4 ? DateTime.MinValue : DateTime.ParseExact(filename.Substring(0, filename.IndexOf("_", StringComparison.Ordinal)), DateFormat.EightCharacter, null);

            Symbol symbol;
            if (securityType == SecurityType.Option)
            {
                var withoutExtension = Path.GetFileNameWithoutExtension(filename);
                rawValue = withoutExtension.Substring(withoutExtension.LastIndexOf("_", StringComparison.Ordinal) + 1);
                var style = (OptionStyle) Enum.Parse(typeof (OptionStyle), rawValue, true);
                symbol = Symbol.CreateOption(ticker, market, style, OptionRight.Call | OptionRight.Put, 0, SecurityIdentifier.DefaultDate);
            }
            else
            {
                symbol = Symbol.Create(ticker, securityType, market);
            }

            return new LeanDataPathComponents(securityType, market, resolution, symbol, filename, date);
        }
    }
}