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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    /// <summary>
    ///     Processor for caching and consolidating ticks;
    ///     then flushing the ticks in memory to disk when triggered.
    /// </summary>
    public class AlgoSeekOptionsProcessor
    {
        private readonly bool _isWindows = OS.IsWindows;
        private readonly IDataConsolidator _consolidator;
        private readonly string _dataDirectory;
        private string _entryPath;
        private readonly DateTime _referenceDate;
        public string SecurityRawIdentifier { get; }

        private readonly string[] _windowsRestrictedNames =
        {
            "con", "prn", "aux", "nul"
        };

        private string _zipPath;

        /// <summary>
        ///     Create a new AlgoSeekOptionsProcessor for enquing consolidated bars and flushing them to disk
        /// </summary>
        /// <param name="symbol">Symbol for the processor</param>
        /// <param name="date">Reference date for the processor</param>
        /// <param name="tickType">TradeBar or QuoteBar to generate</param>
        /// <param name="resolution">Resolution to consolidate</param>
        /// <param name="dataDirectory">Data directory for LEAN</param>
        public AlgoSeekOptionsProcessor(string securityRawIdentifier, DateTime date, TickType tickType, Resolution resolution, string dataDirectory)
        {
            SecurityRawIdentifier = securityRawIdentifier;
            TickType = tickType;
            _referenceDate = date;
            Resolution = resolution;
            Queue = new Queue<IBaseData>();
            _dataDirectory = dataDirectory;

            // Setup the consolidator for the requested resolution
            if (resolution == Resolution.Tick) throw new NotSupportedException();

            switch (tickType)
            {
                case TickType.Trade:
                    _consolidator = new TickConsolidator(resolution.ToTimeSpan());
                    break;
                case TickType.Quote:
                    _consolidator = new TickQuoteBarConsolidator(resolution.ToTimeSpan());
                    break;
                case TickType.OpenInterest:
                    _consolidator = new OpenInterestConsolidator(resolution.ToTimeSpan());
                    break;
            }

            // On consolidating the bars put the bar into a queue in memory to be written to disk later.
            _consolidator.DataConsolidated += (sender, consolidated) => { Queue.Enqueue(consolidated); };
        }

        public FileInfo GetEntryPath(Symbol symbol)
        {
            return  new FileInfo(LeanData.GenerateZipEntryName(symbol, _referenceDate, Resolution, TickType));
        }

        public FileInfo GetZipPath(Symbol symbol)
        {
            return new FileInfo(Path.Combine(_dataDirectory, SafeName(LeanData.GenerateRelativeZipFilePath(Safe(symbol), _referenceDate, Resolution, TickType).Replace(".zip", string.Empty))));
        }

        /// <summary>
        ///     Output base data queue for processing in memory
        /// </summary>
        public Queue<IBaseData> Queue { get; }

        /// <summary>
        ///     Accessor for the final enumerator
        /// </summary>
        public Resolution Resolution { get; }

        /// <summary>
        ///     Type of this option processor.
        ///     ASOP's are grouped trade type for file writing.
        /// </summary>
        public TickType TickType { get; set; }

        /// <summary>
        ///     Process the tick; add to the con
        /// </summary>
        /// <param name="data"></param>
        public void Process(Tick data)
        {
            if (data.TickType != TickType) return;

            _consolidator.Update(data);
        }

        /// <summary>
        ///     Write the in memory queues to the disk.
        /// </summary>
        /// <param name="frontierTime">Current foremost tick time</param>
        /// <param name="finalFlush">Indicates is this is the final push to disk at the end of the data</param>
        public void FlushBuffer(DateTime frontierTime, bool finalFlush)
        {
            //Force the consolidation if time has past the bar
            _consolidator.Scan(frontierTime);

            // If this is the final packet dump it to the queue
            if (finalFlush && _consolidator.WorkingData != null) Queue.Enqueue(_consolidator.WorkingData);
        }

        /// <summary>
        ///     Add filtering to safe check the symbol for windows environments
        /// </summary>
        /// <param name="symbol">Symbol to rename if required</param>
        /// <returns>Renamed symbol for reserved names</returns>
        private Symbol Safe(Symbol symbol)
        {
            if (_isWindows)
                if (_windowsRestrictedNames.Contains(symbol.Value.ToLower()) ||
                    _windowsRestrictedNames.Contains(symbol.Underlying.Value.ToLower()))
                    symbol = Symbol.CreateOption(SafeName(symbol.Underlying.Value), Market.USA, OptionStyle.American, symbol.ID.OptionRight, symbol.ID.StrikePrice, symbol.ID.Date);
            return symbol;
        }

        private string SafeName(string fileName)
        {
            if (_isWindows)
                if (_windowsRestrictedNames.Contains(fileName.ToLower()))
                    return "_" + fileName;
            return fileName;
        }
    }
}