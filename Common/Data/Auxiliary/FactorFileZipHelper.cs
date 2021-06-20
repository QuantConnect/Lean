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
using System.Linq;
using System.Collections.Generic;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Provides methods for reading factor file zips
    /// </summary>
    public static class FactorFileZipHelper
    {
        /// <summary>
        /// Reads the zip bytes as text and parses as FactorFileRows to create FactorFiles
        /// </summary>
        public static IEnumerable<KeyValuePair<Symbol, FactorFile>> ReadFactorFileZip(Stream file, MapFileResolver mapFileResolver, string market)
        {
            if (file == null || file.Length == 0)
            {
                return new Dictionary<Symbol, FactorFile>();
            }

            var keyValuePairs = (
                    from kvp in Compression.Unzip(file)
                    let filename = kvp.Key
                    let lines = kvp.Value
                    let factorFile = SafeRead(filename, lines)
                    let mapFile = mapFileResolver.GetByPermtick(factorFile.Permtick)
                    where mapFile != null
                    let sid = SecurityIdentifier.GenerateEquity(mapFile.FirstDate, mapFile.FirstTicker, market)
                    let symbol = new Symbol(sid, mapFile.Permtick)
                    select new KeyValuePair<Symbol, FactorFile>(symbol, factorFile)
                );

            return keyValuePairs;
        }

        /// <summary>
        /// Parses the contents as a FactorFile, if error returns a new empty factor file
        /// </summary>
        public static FactorFile SafeRead(string filename, IEnumerable<string> contents)
        {
            var permtick = Path.GetFileNameWithoutExtension(filename);
            try
            {
                DateTime? minimumDate;
                // FactorFileRow.Parse handles entries with 'inf' and exponential notation and provides the associated minimum tradeable date for these cases
                // previously these cases were not handled causing an exception and returning an empty factor file
                return new FactorFile(permtick, FactorFileRow.Parse(contents, out minimumDate), minimumDate);
            }
            catch
            {
                return new FactorFile(permtick, Enumerable.Empty<FactorFileRow>());
            }
        }
    }
}
