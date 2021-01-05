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

using Newtonsoft.Json;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace QuantConnect.ToolBox.USTreasuryYieldCurve
{
    public class USTreasuryYieldCurveConverter
    {
        private readonly DirectoryInfo _sourceDirectory;
        private readonly DirectoryInfo _destinationDirectory;

        public USTreasuryYieldCurveConverter(string sourceDirectory, string destinationDirectory)
        {
            _sourceDirectory = new DirectoryInfo(sourceDirectory);
            _destinationDirectory = new DirectoryInfo(destinationDirectory);
            _destinationDirectory.Create();
        }

        /// <summary>
        /// Converts the U.S. Treasury yield curve data to CSV format
        /// </summary>
        public void Convert()
        {
            Log.Trace("USTreasuryYieldCurveRateConverter.Convert(): Begin converting U.S. Treasury yield curve rate data");

            var rawFile = new FileInfo(Path.Combine(_sourceDirectory.FullName, "yieldcurverates.xml"));
            var finalPath = new FileInfo(Path.Combine(_destinationDirectory.FullName, "yieldcurverates.csv"));

            if (!rawFile.Exists)
            {
                throw new FileNotFoundException($"Failed to find yield curve rates file: {rawFile.FullName}");
            }

            using (var stream = rawFile.OpenText())
            {
                Log.Trace("USTreasuryYieldCurveConverter.Convert(): Begin deserialization of raw XML data");
                var xmlData = (feed) new XmlSerializer(typeof(feed))
                    .Deserialize(stream);

                // I don't think this should happen, but let's make sure before we work with the type
                if (xmlData == null)
                {
                    throw new InvalidOperationException("XML data is null. Perhaps we're deserializing the wrong XML data?");
                }

                var lines = 0;
                var csvBuilder = new StringBuilder();
                var sortedFilteredData = xmlData.entry.SelectMany(x => x.content)
                    .OrderBy(x => Parse.DateTime(x.properties.NEW_DATE.Value))
                    .ToList();

                if (finalPath.Exists)
                {
                    Log.Trace("USTreasuryYieldCurveConverter.Convert(): File already exists in destination. Filtering so that we only add new data");
                    var csvData = File.ReadAllLines(finalPath.FullName).Last();
                    // Since the date is the first entry in the CSV file, we don't have to worry about null values
                    var csvDataDate = DateTime.ParseExact(csvData.Split(',').First(), DateFormat.EightCharacter, CultureInfo.InvariantCulture);

                    sortedFilteredData = sortedFilteredData.Where(x => Parse.DateTime(x.properties.NEW_DATE.Value) > csvDataDate).ToList();
                }

                foreach (var entry in sortedFilteredData)
                {
                    Log.Trace($"USTreasuryYieldCurveConverter.Convert(): Processing data for date: {entry.properties.NEW_DATE.Value}");
                    lines++;

                    var data = new List<string>
                    {
                        Parse.DateTime(entry.properties.NEW_DATE.Value).Date.ToStringInvariant(DateFormat.EightCharacter),
                        entry.properties.BC_1MONTH.Value,
                        entry.properties.BC_2MONTH.Value,
                        entry.properties.BC_3MONTH.Value,
                        entry.properties.BC_6MONTH.Value,
                        entry.properties.BC_1YEAR.Value,
                        entry.properties.BC_2YEAR.Value,
                        entry.properties.BC_3YEAR.Value,
                        entry.properties.BC_5YEAR.Value,
                        entry.properties.BC_7YEAR.Value,
                        entry.properties.BC_10YEAR.Value,
                        entry.properties.BC_20YEAR.Value,
                        entry.properties.BC_30YEAR.Value
                    };

                    // Date[0], 1 mo[1], 2 mo[2], 3 mo[3], 6 mo[4], 1 yr[5], 2 yr[6] 3 yr[7], 5 yr[8], 7 yr [9], 10 yr[10], 20 yr[11], 30 yr[12]
                    csvBuilder.AppendLine(string.Join(",", data));
                }

                Log.Trace($"USTreasuryYieldCurveConverter.Convert(): Appending {lines} lines to file: {finalPath.FullName}");
                using (var writeStream = new StreamWriter(finalPath.FullName, append: true))
                {
                    writeStream.Write(csvBuilder.ToString());
                }
            }

            Log.Trace($"USTreasuryYieldCurveConverter.Convert(): Data conversion complete!");
        }
    }
}
