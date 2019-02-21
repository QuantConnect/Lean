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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using QuantConnect.Logging;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// A wrapper class that keeps data points and provide high performance access
    /// </summary>
    public class DataPointDictionary
    {
        /// <summary>
        /// The reference array for index-based enumeration
        /// </summary>
        private readonly IList<KeyValuePair<DateTime, string>> _referenceArray;

        /// <summary>
        /// A sorted list that maintain time orders of data points and provide time-based access
        /// </summary>
        private readonly SortedList<DateTime, string> _dataPoints;

        /// <summary>
        /// The source file name where the data is from in this class
        /// </summary>
        private readonly string _fileName;

        /// <summary>
        /// The resolution of the data
        /// </summary>
        private readonly Resolution _resolution;

        /// <summary>
        /// A property for fine granularity data. Available only when resolution is lower than Hour.
        /// </summary>
        private readonly DateTime _dateOfFileIfAny;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPointCacheProvider"/> class.
        /// </summary>
        /// <param name="fileStream">The file stream to csv</param>
        /// <param name="fileName">The source file name</param>
        /// <param name="resolution">The resolution of data</param>
        public DataPointDictionary(Stream fileStream, string fileName, Resolution resolution)
        {
            _fileName = fileName;
            _resolution = resolution;
            _dataPoints = BuildDataPointDictionary(fileStream);
            _referenceArray = GetReferenceArray(_dataPoints);
            if (_resolution != Resolution.Daily && _resolution != Resolution.Hour)
            {
                _dateOfFileIfAny = DateTime.ParseExact(
                    Path.GetFileNameWithoutExtension(_fileName)
                        .Substring(0, DateFormat.EightCharacter.Length),
                    DateFormat.EightCharacter,
                    CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets the number of data points
        /// </summary>
        public int Count
        {
            get { return _dataPoints.Count; }
        }

        /// <summary>
        /// Extract <see cref="DataPointDictionary"/> from csv file stream
        /// </summary>
        /// <param name="fileStream">The csv file stream</param>
        /// <returns>A sorted dictionary including all data points in ascending order</returns>
        public SortedList<DateTime, string> BuildDataPointDictionary(Stream fileStream)
        {
            if (fileStream == null || !fileStream.CanRead)
            {
                throw new InvalidOperationException("Cannot read from the zip file stream. The stream is broken.");
            }

            StreamReader reader = new StreamReader(fileStream);
            SortedList<DateTime, string> result = new SortedList<DateTime, string>();
            string line = string.Empty;
            do
            {
                try
                {
                    line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    result[GetDateTime(line)] = line;
                }
                catch (Exception ex)
                {
                    Log.Trace($"DataPointDictionary was not able to parse/read below line: \"{line}\"; Details: {ex.ToString()}");
                }
            } while (!reader.EndOfStream);
            return result;
        }

        /// <summary>
        /// Extract datetime from a csv row
        /// </summary>
        /// <param name="line">The data row</param>
        /// <returns>The datetime of the data point</returns>
        public DateTime GetDateTime(string line)
        {
            // Get exchange timezone and the file name for date
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new ArgumentNullException(nameof(line), "Input cannot be null or empty.");
            }

            switch (_resolution)
            {
                case Resolution.Daily:
                case Resolution.Hour:
                    return DateTime.ParseExact(
                        line.Split(new char[] { ',' })[0],
                        DateFormat.TwelveCharacter,
                        CultureInfo.InvariantCulture);
                default:
                    return _dateOfFileIfAny +
                        TimeSpan.FromMilliseconds(long.Parse(line.Split(new char[] { ',' })[0]));
            }
        }

        /// <summary>
        /// Generate reference array from sorted dictionary of data points for enumeration
        /// </summary>
        /// <param name="dataPoints">The data point dictionary</param>
        /// <returns>A list of KV pairs in order</returns>
        public IList<KeyValuePair<DateTime, string>> GetReferenceArray(SortedList<DateTime, string> dataPoints)
        {
            if (dataPoints == null || dataPoints.Count == 0)
            {
                return new List<KeyValuePair<DateTime, string>>(0);
            }

            List<KeyValuePair<DateTime, string>> result = new List<KeyValuePair<DateTime, string>>(dataPoints.Count);
            foreach (KeyValuePair<DateTime, string> kv in dataPoints)
            {
                result.Add(kv);
            }

            return result;
        }

        /// <summary>
        /// Access data points by index. Equivalent to ElementAt()
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>A kv pair at index</returns>
        public KeyValuePair<DateTime, string> this[int index]
        {
            get
            {
                return _referenceArray[index];
            }
        }

        /// <summary>
        /// Access data points by datetime(key)
        /// </summary>
        /// <param name="key">The key of the entry</param>
        /// <returns>A data point in string</returns>
        public string this[DateTime key]
        {
            get
            {
                return _dataPoints[key];
            }
        }

        /// <summary>
        /// Whether the data point dictionary is empty
        /// </summary>
        /// <returns>True if empty; False otherwise</returns>
        public bool IsEmpty()
        {
            return _dataPoints == null || _dataPoints.Count == 0;
        }

        /// <summary>
        /// Return the first item of the list base on the predicate
        /// </summary>
        /// <param name="predicate">The selector</param>
        /// <returns>A KV pair of data point</returns>
        public KeyValuePair<DateTime, string> First(Func<KeyValuePair<DateTime, string>, bool> predicate)
        {
            return _dataPoints.FirstOrDefault(predicate);
        }

        /// <summary>
        /// Return the last item of the list base on the predicate
        /// </summary>
        /// <param name="predicate">The selector</param>
        /// <returns>A KV pair of data point</returns>
        public KeyValuePair<DateTime, string> Last(Func<KeyValuePair<DateTime, string>, bool> predicate)
        {
            return _dataPoints.LastOrDefault(predicate);
        }

        /// <summary>
        /// Get the index of the key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int IndexOfKey(DateTime key)
        {
            return _dataPoints.IndexOfKey(key);
        }
    }
}
