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
 *
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Helper class containing various unique identifiers for a given
    /// <see cref="SecurityIdentifier"/>, such as FIGI, ISIN, CUSIP, SEDOL.
    /// </summary>
    public class SecurityDefinition
    {
        /// <summary>
        /// The unique <see cref="SecurityIdentifier"/> identified by
        /// the industry-standard security identifiers contained within this class.
        /// </summary>
        public SecurityIdentifier SecurityIdentifier { get; set; }
        
        /// <summary>
        /// The CUSIP of the security
        /// </summary>
        public string CUSIP { get; set; }
       
        /// <summary>
        /// The FIGI of the security
        /// </summary>
        public string FIGI { get; set; }
        
        /// <summary>
        /// SEDOL of the security
        /// </summary>
        public string SEDOL { get; set; }
       
        /// <summary>
        /// ISIN of the security
        /// </summary>
        public string ISIN { get; set; }
        
        /// <summary>
        /// Reads data from the specified file and converts it to a list of SecurityDefinition
        /// </summary>
        /// <param name="securitiesFile">File to read from</param>
        /// <returns>List of security definitions</returns>
        public static List<SecurityDefinition> FromCsvFile(FileInfo securitiesFile)
        {
            return File.ReadAllLines(securitiesFile.FullName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(FromCsvLine)
                .ToList();
        }

        /// <summary>
        /// Attempts to read data from the specified file and convert it into a list of SecurityDefinition
        /// </summary>
        /// <param name="securitiesFile">File to read from</param>
        /// <param name="securityDefinitions">Security definitions read</param>
        /// <returns>true if data was read successfully, false otherwise</returns>
        public static bool TryFromCsvFile(FileInfo securitiesFile, out List<SecurityDefinition> securityDefinitions)
        {
            try
            {
                securityDefinitions = FromCsvFile(securitiesFile);
                return true;
            }
            catch
            {
                securityDefinitions = null;
                return false;
            }
        }

        /// <summary>
        /// Parses a single line of CSV and converts it into an instance
        /// </summary>
        /// <param name="line">Line of CSV</param>
        /// <returns>SecurityDefinition instance</returns>
        public static SecurityDefinition FromCsvLine(string line)
        {
            var csv = line.Split(',');
            return new SecurityDefinition
            {
                SecurityIdentifier = SecurityIdentifier.Parse(csv[0]),
                CUSIP = string.IsNullOrWhiteSpace(csv[1]) ? null : csv[1],
                FIGI = string.IsNullOrWhiteSpace(csv[2]) ? null : csv[2],
                SEDOL = string.IsNullOrWhiteSpace(csv[3]) ? null : csv[3],
                ISIN = string.IsNullOrWhiteSpace(csv[4]) ? null : csv[4]
            };
        }
    }
}