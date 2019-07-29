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
using QuantConnect.Data;
using System.Collections.Generic;
using System.IO;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Util;
using System.Linq;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    /// <summary>
    /// Processor for caching and consolidating ticks;
    /// then flushing the ticks in memory to disk when triggered.
    /// </summary>
    public class AlgoSeekOptionsProcessor
    {
        private string _zipPath;
        private string _entryPath;
        private Symbol _symbol;
        private TickType _tickType;
        private Resolution _resolution;
        private Queue<IBaseData> _queue;
        private string _dataDirectory;
        private IDataConsolidator _consolidator;
        private DateTime _referenceDate;
        private static string[] _windowsRestrictedNames =
        {
            "con", "prn", "aux", "nul"
        };

        /// <summary>
        /// Zip entry name for the option contract
        /// </summary>
        public string EntryPath
        {
            get
            {
                if (_entryPath == null)
                {
                    _entryPath = SafeName(LeanData.GenerateZipEntryName(_symbol, _referenceDate, _resolution, _tickType));
                }
                return _entryPath;
            }
            set { _entryPath = value; }
        }

        /// <summary>
        /// Zip file path for the option contract collection
        /// </summary>
        public string ZipPath
        {
            get
            {
                if (_zipPath == null)
                {
                    _zipPath = Path.Combine(_dataDirectory, SafeName(LeanData.GenerateRelativeZipFilePath(Safe(_symbol), _referenceDate, _resolution, _tickType).Replace(".zip", string.Empty))) + ".zip";
                }
                return _zipPath;
            }
            set { _zipPath = value; }
        }

        /// <summary>
        /// Public access to the processor symbol
        /// </summary>
        public Symbol Symbol
        {
            get { return _symbol; }
        }

        /// <summary>
        /// Output base data queue for processing in memory
        /// </summary>
        public Queue<IBaseData> Queue
        {
            get { return _queue; }
        }

        /// <summary>
        /// Accessor for the final enumerator
        /// </summary>
        public Resolution Resolution
        {
            get { return _resolution; }
        }

        /// <summary>
        /// Type of this option processor.
        /// ASOP's are grouped trade type for file writing.
        /// </summary>
        public TickType TickType
        {
            get { return _tickType; }
            set { _tickType = value; }
        }

        /// <summary>
        /// Create a new AlgoSeekOptionsProcessor for enquing consolidated bars and flushing them to disk
        /// </summary>
        /// <param name="symbol">Symbol for the processor</param>
        /// <param name="date">Reference date for the processor</param>
        /// <param name="tickType">TradeBar or QuoteBar to generate</param>
        /// <param name="resolution">Resolution to consolidate</param>
        /// <param name="dataDirectory">Data directory for LEAN</param>
        public AlgoSeekOptionsProcessor(Symbol symbol, DateTime date, TickType tickType, Resolution resolution, string dataDirectory)
        {
            _symbol = Safe(symbol);
            _tickType = tickType;
            _referenceDate = date;
            _resolution = resolution;
            _queue = new Queue<IBaseData>();
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
            _consolidator.DataConsolidated += (sender, consolidated) =>
            {
                _queue.Enqueue(consolidated);
            };
        }

        /// <summary>
        /// Process the tick; add to the con
        /// </summary>
        /// <param name="data"></param>
        public void Process(Tick data)
        {
            if (data.TickType != _tickType)
            {
                return;
            }

            _consolidator.Update(data);
        }

        /// <summary>
        /// Write the in memory queues to the disk.
        /// </summary>
        /// <param name="frontierTime">Current foremost tick time</param>
        /// <param name="finalFlush">Indicates is this is the final push to disk at the end of the data</param>
        public void FlushBuffer(DateTime frontierTime, bool finalFlush)
        {
            //Force the consolidation if time has past the bar
            _consolidator.Scan(frontierTime);

            // If this is the final packet dump it to the queue
            if (finalFlush && _consolidator.WorkingData != null)
            {
                _queue.Enqueue(_consolidator.WorkingData);
            }
        }

        /// <summary>
        /// Add filtering to safe check the symbol for windows environments
        /// </summary>
        /// <param name="symbol">Symbol to rename if required</param>
        /// <returns>Renamed symbol for reserved names</returns>
        private static Symbol Safe(Symbol symbol)
        {
            if (OS.IsWindows)
            {
                if (_windowsRestrictedNames.Contains(symbol.Value.ToLowerInvariant()) ||
                    _windowsRestrictedNames.Contains(symbol.Underlying.Value.ToLowerInvariant()))
                {
                    symbol = Symbol.CreateOption(SafeName(symbol.Underlying.Value), Market.USA, OptionStyle.American, symbol.ID.OptionRight, symbol.ID.StrikePrice, symbol.ID.Date);
                }
            }
            return symbol;
        }

        private static string SafeName(string fileName)
        {
            if (OS.IsWindows)
            {
                if (_windowsRestrictedNames.Contains(fileName.ToLowerInvariant()))
                    return "_" + fileName;
            }
            return fileName;
        }
    }
}