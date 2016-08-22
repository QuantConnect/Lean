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
using System.Collections.Concurrent;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;
using System.Collections.Generic;
using QuantConnect.Data.Consolidators;
using System.IO;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
	/*
	 * Possible Replacement:
	 * https://gist.github.com/AnObfuscator/a619eea8e9b66ef4967bd2c679400d39
	 */
	public class InMemoryDataProcessor : IDataProcessor
	{
		private readonly TickType _tickType;
		private readonly ConcurrentDictionary<Symbol, TickContainer> _ticksBySymbol;
		private Func<BaseData, bool> _filter = (x) => true;

		public InMemoryDataProcessor (TickType tickType, Func<BaseData, bool> filter = null)
		{
			_tickType = tickType;
			_ticksBySymbol = new ConcurrentDictionary<Symbol, TickContainer>();
			_filter = filter ?? _filter;
		}

		public void Process(BaseData newTick)
		{
			if (!_filter(newTick))
			{
				return;
			}

			TickContainer ticksForSymbol;
			if (!_ticksBySymbol.TryGetValue(newTick.Symbol, out ticksForSymbol))
			{
				ticksForSymbol = _ticksBySymbol.GetOrAdd(newTick.Symbol, new TickContainer());
			}

			ticksForSymbol.Add(newTick);
		}

		public void FlushConsolidatedToDisk(string dataDirectory, IEnumerable<Resolution> resolutions, DateTime cutoffTime)
		{
			foreach (var ticksForSymbol in _ticksBySymbol.Values)
			{
				using (var csvWriter = DataProcessor.Zip(dataDirectory, resolutions, _tickType, true))
				{
					ticksForSymbol.FlushConsolidatedWith(csvWriter, cutoffTime);
				}
			}
		}

		public void FlushAllToDisk(string dataDirectory, IEnumerable<Resolution> resolutions)
		{
			foreach (var ticksForSymbol in _ticksBySymbol.Values)
			{
				using (var csvWriter = DataProcessor.Zip(dataDirectory, resolutions, _tickType, true))
				{
					ticksForSymbol.FlushAllWith(csvWriter);
				}
			}
		}

		public string GetStats()
		{
			var symbolCount = _ticksBySymbol.Count;

			double totalEntries = 0;
			foreach (var entryList in _ticksBySymbol.Values)
			{
				totalEntries += entryList.Size;
			}

			double avgSize = totalEntries / symbolCount;

			return string.Format("{0} symbols with average entries {1}", symbolCount, avgSize);
		}

		public void Dispose()
		{
		}
	}

	class TickContainer
	{
		private readonly object _lock = new object();

		private Queue<BaseData> _ticks = new Queue<BaseData>();

		public int Size { get; private set; }

		public void Add(BaseData newTick)
		{
			lock (_lock)
			{
				_ticks.Enqueue(newTick);
				Size++;
			}
		}

		public void FlushConsolidatedWith(IDataProcessor outputProcessor, DateTime cutoffTime)
		{
			lock (_lock)
			{
				while (!_ticks.IsEmpty() && _ticks.Peek().Time < cutoffTime)
				{
					outputProcessor.Process(_ticks.Dequeue());
				}
			}
		}

		public void FlushAllWith(IDataProcessor outputProcessor)
		{
			lock (_lock)
			{
				while (!_ticks.IsEmpty())
				{
					outputProcessor.Process(_ticks.Dequeue());
				}
			}
		}
	}
}