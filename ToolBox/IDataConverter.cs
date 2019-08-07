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

using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using System;
using System.Collections.Generic;
using System.IO;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Interface defines common patterns that are involved in implementing a ToolBox converter
    /// for custom data sources. We provide many generic implementable methods so that your converter
    /// is capable of processing multiple data sources per instance
    public interface IDataConverter
    {
        /// <summary>
        /// Responsible for handling discovery of map files that deal with
        /// mapping events (e.g. delistings, renames, listing after delisting, etc.)
        /// </summary>
        MapFileResolver MapFileResolver { get; }

        /// <summary>
        /// Source directory to get raw data from. We provide this variable to make
        /// constructing paths to the source directory easier and cheaper.
        /// </summary>
        DirectoryInfo SourceDirectory { get; }

        /// <summary>
        /// Destination directory to write files to. This is usually the Data folder.
        /// We provide these variables to make constructing paths to the final destination
        /// easier and cheaper.
        ///
        /// In many cases, the destination directory can contain sub-folders that we
        /// organize data into, such as:
        /// `Globals.DataFolder/alternative/datavendor/sentiment/symbol.csv`
        /// `Globals.DataFolder/alternative/datavendor/messages/symbol.csv`
        /// </summary>
        DirectoryInfo DestinationDirectory { get; }

        /// Main entry point for data conversion
        /// </summary>
        /// <returns>Boolean value indicating success or failure</returns>
        bool Convert(DateTime date);

        /// <summary>
        /// Processes the data into custom data objects
        /// </summary>
        /// <param name="date">Date we're converting data for</param>
        /// <param name="sourceStream">Source stream of data</param>
        /// <returns>Dictionary keyed by ticker containing enumerable of <see cref="BaseData"/></returns>
        /// <remarks>We use a stream instead of a file to allow for extra flexability with zipped data sources</remarks>
        IEnumerable<T> Process<T>(DateTime date, Stream sourceStream)
            where T : BaseData, new();

        /// <summary>
        /// Takes raw data and constructs a new instance of type T with the data loaded into it
        /// </summary>
        /// <typeparam name="T">BaseData type</typeparam>
        /// <param name="line">Line of raw CSV</param>
        /// <returns>New instance of T</returns>
        T GetDataInstanceFromRaw<T>(string line)
            where T : BaseData, new();

        /// <summary>
        /// Instantiates generic type with formatted data loaded into the instance
        /// </summary>
        /// <typeparam name="T">BaseData derived type</typeparam>
        /// <param name="ticker">Ticker of the data we're processing</param>
        /// <param name="line">Line of formatted CSV in its final form</param>
        /// <returns>New BaseData instance of typeparam T</returns>
        /// <remarks>
        /// We must pass in the ticker from external sources because usually, the reader + data are dumb and
        /// are not able to determine what the ticker is from those two sources. We can fake a
        /// <see cref="SubscriptionDataConfig"/> object in order to satisfy the <see cref="BaseData.Reader(SubscriptionDataConfig, string, DateTime, bool)"/>
        /// method and pass in our desired <see cref="Symbol"/> should you choose to use Reader.
        ///
        /// We leave the user to implement this instead of calling the Reader so that the user can handle any
        /// complexities that may arise out of calling Reader and bypass it entirely if they wish
        /// </remarks>
        T GetDataInstance<T>(string ticker, string line)
            where T : BaseData, new();

        /// <summary>
        /// Serializes to string the given type parameter to a single line
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns>string of serialized object</returns>
        string Serialize<T>(T instance)
            where T : BaseData;

        /// <summary>
        /// Can be used to implement mapping for tickers
        /// </summary>
        /// <param name="ticker"></param>
        /// <param name="date"></param>
        /// <returns>Mapped ticker</returns>
        /// <remarks>
        /// If you don't want to map the ticker, implement this to
        /// return the same ticker you passed in
        /// </remarks>
        string MapTicker(string ticker, DateTime date, int lineCount);

        /// <summary>
        /// Writes the data to a file in daily style format
        /// </summary>
        /// <param name="data">Enumerable of data</param>
        void WriteToFile<T>(IEnumerable<T> data, DirectoryInfo finalDirectory)
            where T : BaseData, new();
    }
}
