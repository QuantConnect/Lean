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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;
using System.Collections.Generic;
using QuantConnect.Data.Consolidators;
using System.IO;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    public class AlgoSeekOptionsProcessor
    {
        private readonly Resolution[] _resolutions;
        private readonly TickType _tickType;
        private readonly Dictionary<Symbol, TickRepository> _tickRepositoriesBySymbol;
        private readonly string _destinationDirectory;
        private readonly Func<BaseData, bool> _filter = x => true;

        public AlgoSeekOptionsProcessor(TickType tickType, Resolution[] resolutions, string destinationDirectory, Func<BaseData, bool> filter = null)
        {
            _tickType = tickType;
            _resolutions = resolutions;
            _tickRepositoriesBySymbol = new Dictionary<Symbol, TickRepository>();
            _destinationDirectory = destinationDirectory;
            _filter = filter ?? _filter;
        }

        public void Process(BaseData newTick)
        {
            if (!_filter(newTick))
            {
                return;
            }

            if (!_tickRepositoriesBySymbol.ContainsKey(newTick.Symbol))
            {
                _tickRepositoriesBySymbol[newTick.Symbol] = new TickRepository(_tickType, _resolutions);
            }

            _tickRepositoriesBySymbol[newTick.Symbol].Add(newTick);
        }

        public void FlushToDisk(DateTime frontierTime, bool finalFlush = false)
        {
            foreach (var repository in _tickRepositoriesBySymbol)
            {
                foreach (var resolution in _resolutions)
                {
                    var data = repository.Value.GetConsolidatedFor(resolution, frontierTime, finalFlush);
                    using (var writer = LeanOptionsWriter.CreateNew(_destinationDirectory, repository.Key, frontierTime, resolution, _tickType))
                    {
                        foreach (var entry in data)
                        {
                            writer.WriteEntry(entry);
                        }
                    }
                }
            }
        }

        public string GetStats()
        {
            var symbolCount = _tickRepositoriesBySymbol.Count;

            var totalEntries = _tickRepositoriesBySymbol.Values.Sum(repo => repo.Size);

            var avgSize = totalEntries / symbolCount;

            return string.Format("{0} symbols with average entries {1}", symbolCount, avgSize);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

    }

    /// <summary>
    /// Holds consolidators and consolidated data for multiple resolutions of a single symbol.
    /// </summary>
    internal class TickRepository
    {
        private readonly Dictionary<Resolution, Queue<BaseData>> _queues = new Dictionary<Resolution, Queue<BaseData>>();
        private readonly Dictionary<Resolution, IDataConsolidator> _consolidators = new Dictionary<Resolution, IDataConsolidator>();

        public int Size { get; private set; }

        public TickRepository(TickType tickType, Resolution[] resolutions)
        {
            resolutions.ForEach(res => InitializeResolution(res, tickType));
        }

        public void Add(BaseData newTick)
        {
            _consolidators.Values.ForEach(consolidator => consolidator.Update(newTick));
            Size++;
        }

        public IEnumerable<BaseData> GetConsolidatedFor(Resolution resolution, DateTime frontierTime, bool forceFlush = false)
        {
            var consolidator = _consolidators[resolution];
            consolidator.Scan(frontierTime);
            var queue = _queues[resolution];

            if (forceFlush && consolidator.WorkingData != null)
            {
                queue.Enqueue(consolidator.WorkingData);
            }

            while (queue.Count > 0)
            {
                yield return queue.Dequeue();
            }
        }

        private void InitializeResolution(Resolution resolution, TickType tickType)
        {
            var queue = new Queue<BaseData>();

            var consolidator = CreateConsolidator(resolution, tickType);
            consolidator.DataConsolidated += (sender, consolidated) => queue.Enqueue(consolidated);

            _queues[resolution] = queue;
            _consolidators[resolution] = consolidator;
        }

        private static IDataConsolidator CreateConsolidator(Resolution resolution, TickType tickType)
        {
            if (resolution == Resolution.Tick)
            {
                return new PassthroughConsolidator();
            }

            switch (tickType)
            {
                case TickType.Trade:
                    return new TickConsolidator(resolution.ToTimeSpan());
                case TickType.Quote:
                    return new TickQuoteBarConsolidator(resolution.ToTimeSpan());
            }

            throw new NotImplementedException("Consolidator creation is not defined for Tick type " + tickType);
        }
    }

    // This is a shim for handling Tick resolution data in TickRepository
    // Ordinary TickConsolidators presents Consolidated data as type TradeBars.
    // However, LeanData.GenerateLine expects Tick resolution data to be of type Tick.
    // This class lets tick data pass through without changing object type,
    // which simplifies the logic in TickRepository.
    internal class PassthroughConsolidator : IDataConsolidator
    {
        public BaseData Consolidated { get; private set; }

        public BaseData WorkingData
        {
            get { return null; }
        }

        public Type InputType
        {
            get { return typeof(BaseData); }
        }

        public Type OutputType
        {
            get { return typeof(BaseData); }
        }

        public event DataConsolidatedHandler DataConsolidated;

        public void Update(BaseData data)
        {
            Consolidated = data;
            if (DataConsolidated != null)
            {
                DataConsolidated(this, data);
            }
        }

        public void Scan(DateTime currentLocalTime)
        {
        }
    }

    // Ideally this should be merged into with LeanDataWriter.
    internal class LeanOptionsWriter : IDisposable
    {
        private readonly StreamWriter _streamWriter;
        private readonly Resolution _resolution;

        public LeanOptionsWriter(string path, Resolution resolution)
        {
            _streamWriter = new StreamWriter(path);
            _resolution = resolution;
        }

        public static LeanOptionsWriter CreateNew(string dataDirectory, Symbol symbol, DateTime date, Resolution resolution, TickType tickType)
        {
            var entry = LeanData.GenerateZipEntryName(symbol, date, resolution, tickType);
            var relativePath = LeanData.GenerateRelativeZipFilePath(symbol, date, resolution, tickType).Replace(".zip", string.Empty);
            var path = Path.Combine(Path.Combine(dataDirectory, relativePath), entry);
            var directory = new FileInfo(path).Directory.FullName;
            Directory.CreateDirectory(directory);

            return new LeanOptionsWriter(path, resolution);
        }

        public void WriteEntry(BaseData data)
        {
            var line = LeanData.GenerateLine(data, data.Symbol.ID.SecurityType, _resolution);
            _streamWriter.WriteLine(line);
        }

        public void Flush()
        {
            _streamWriter.Flush();
        }

        public void Dispose()
        {
            _streamWriter.Dispose();
        }
    }

    internal static partial class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var entry in enumerable)
            {
                action(entry);
            }
        }
    }
}