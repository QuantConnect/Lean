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

using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

using QuantConnect.Interfaces;

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
        /// The Committee on Uniform Securities Identification Procedures (CUSIP) number of a security
        /// </summary>
        /// <remarks>For more information on CUSIP numbers: https://en.wikipedia.org/wiki/CUSIP</remarks>
        public string CUSIP { get; set; }

        /// <summary>
        /// The composite Financial Instrument Global Identifier (FIGI) of a security
        /// </summary>
        /// <remarks>
        /// The composite FIGI differs from an exchange-level FIGI, in that it identifies
        /// an asset across all exchanges in a single country that the asset trades in.
        /// For more information about the FIGI standard: https://en.wikipedia.org/wiki/Financial_Instrument_Global_Identifier
        /// </remarks>
        public string CompositeFIGI { get; set; }

        /// <summary>
        /// The Stock Exchange Daily Official List (SEDOL) security identifier of a security
        /// </summary>
        /// <remarks>For more information about SEDOL security identifiers: https://en.wikipedia.org/wiki/SEDOL</remarks>
        public string SEDOL { get; set; }

        /// <summary>
        /// The International Securities Identification Number (ISIN) of a security
        /// </summary>
        /// <remarks>For more information about the ISIN standard: https://en.wikipedia.org/wiki/International_Securities_Identification_Number</remarks>
        public string ISIN { get; set; }

        /// <summary>
        /// A Central Index Key or CIK number is a unique number assigned to an individual, company, filing agent or foreign government by the United States
        /// Securities and Exchange Commission (SEC). The number is used to identify its filings in several online databases, including EDGAR.
        /// </summary>
        /// <remarks>For more information about CIK: https://en.wikipedia.org/wiki/Central_Index_Key</remarks>
        public int? CIK { get; set; }

        /// <summary>
        /// Reads data from the specified file and converts it to a list of SecurityDefinition
        /// </summary>
        /// <param name="dataProvider">Data provider used to obtain symbol mappings data</param>
        /// <param name="securitiesDefinitionKey">Location to read the securities definition data from</param>
        /// <returns>List of security definitions</returns>
        public static List<SecurityDefinition> Read(IDataProvider dataProvider, string securitiesDefinitionKey)
        {
            using var stream = dataProvider.Fetch(securitiesDefinitionKey);
            using var reader = new StreamReader(stream);

            var securityDefinitions = new List<SecurityDefinition>();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.InvariantCulture))
                {
                    continue;
                }

                securityDefinitions.Add(FromCsvLine(line));
            }

            return securityDefinitions;
        }

        /// <summary>
        /// Attempts to read data from the specified file and convert it into a list of SecurityDefinition
        /// </summary>
        /// <param name="dataProvider">Data provider used to obtain symbol mappings data</param>
        /// <param name="securitiesDatabaseKey">Location of the file to read from</param>
        /// <param name="securityDefinitions">Security definitions read</param>
        /// <returns>true if data was read successfully, false otherwise</returns>
        public static bool TryRead(IDataProvider dataProvider, string securitiesDatabaseKey, out List<SecurityDefinition> securityDefinitions)
        {
            try
            {
                securityDefinitions = Read(dataProvider, securitiesDatabaseKey);
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
                CompositeFIGI = string.IsNullOrWhiteSpace(csv[2]) ? null : csv[2],
                SEDOL = string.IsNullOrWhiteSpace(csv[3]) ? null : csv[3],
                ISIN = string.IsNullOrWhiteSpace(csv[4]) ? null : csv[4],
                CIK = (csv.Length <= 5 || string.IsNullOrWhiteSpace(csv[5])) ? null : int.Parse(csv[5], CultureInfo.InvariantCulture)
            };
        }
    }
}
