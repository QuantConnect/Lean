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
using System.Linq;
using QuantConnect.Util;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Represents an entire factor file for a specified symbol
    /// </summary>
    public abstract class FactorFile<T> : IFactorProvider
        where T : IFactorRow
    {
        /// <summary>
        /// Keeping a reversed version is more performant that reversing it each time we need it
        /// </summary>
        protected List<DateTime> ReversedFactorFileDates { get; }

        /// <summary>
        /// The factor file data rows sorted by date
        /// </summary>
        public SortedList<DateTime, List<T>> SortedFactorFileData { get; set; }

        /// <summary>
        /// The minimum tradeable date for the symbol
        /// </summary>
        /// <remarks>
        /// Some factor files have INF split values, indicating that the stock has so many splits
        /// that prices can't be calculated with correct numerical precision.
        /// To allow backtesting these symbols, we need to move the starting date
        /// forward when reading the data.
        /// Known symbols: GBSN, JUNI, NEWL
        /// </remarks>
        public DateTime? FactorFileMinimumDate { get; set; }

        /// <summary>
        /// Gets the most recent factor change in the factor file
        /// </summary>
        public DateTime MostRecentFactorChange => ReversedFactorFileDates
            .FirstOrDefault(time => time != Time.EndOfTime);

        /// <summary>
        /// Gets the symbol this factor file represents
        /// </summary>
        public string Permtick { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactorFile"/> class.
        /// </summary>
        protected FactorFile(string permtick, IEnumerable<T> data, DateTime? factorFileMinimumDate = null)
        {
            Permtick = permtick.LazyToUpper();

            SortedFactorFileData = new SortedList<DateTime, List<T>>();
            foreach (var row in data)
            {
                if (!SortedFactorFileData.TryGetValue(row.Date, out var factorFileRows))
                {
                    SortedFactorFileData[row.Date] = factorFileRows = new List<T>();
                }

                factorFileRows.Add(row);
            }

            ReversedFactorFileDates = new List<DateTime>(SortedFactorFileData.Count);
            foreach (var time in SortedFactorFileData.Keys.Reverse())
            {
                ReversedFactorFileDates.Add(time);
            }

            FactorFileMinimumDate = factorFileMinimumDate;
        }

        /// <summary>
        /// Gets the price scale factor for the specified search date
        /// </summary>
        public abstract decimal GetPriceFactor(
            DateTime searchDate,
            DataNormalizationMode dataNormalizationMode,
            DataMappingMode? dataMappingMode = null,
            uint contractOffset = 0
            );

        /// <summary>
        /// Writes this factor file data to an enumerable of csv lines
        /// </summary>
        /// <returns>An enumerable of lines representing this factor file</returns>
        public IEnumerable<string> GetFileFormat()
        {
            return SortedFactorFileData.SelectMany(kvp => kvp.Value.Select(row => row.GetFileFormat()));
        }

        /// <summary>
        /// Write the factor file to the correct place in the default Data folder
        /// </summary>
        /// <param name="symbol">The symbol this factor file represents</param>
        public void WriteToFile(Symbol symbol)
        {
            var filePath = LeanData.GenerateRelativeFactorFilePath(symbol);
            File.WriteAllLines(filePath, GetFileFormat());
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<IFactorRow> GetEnumerator()
        {
            foreach (var kvp in SortedFactorFileData)
            {
                foreach (var factorRow in kvp.Value)
                {
                    yield return factorRow;
                }
            }
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
