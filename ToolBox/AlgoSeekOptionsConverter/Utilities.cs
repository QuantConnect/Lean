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
using System.Linq;
using System.Diagnostics;
using Ionic.BZip2;
using System.IO;
using System.Threading.Tasks;
using QuantConnect.Logging;
using QuantConnect.Data;
using QuantConnect.Util;


namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
	public static class Extensions
	{
		public static AlgoSeekOptionsReader EarliestTick(this IEnumerable<AlgoSeekOptionsReader> readers)
		{
			return readers.Aggregate((earliestTick, nextCandidate) => earliestTick.Peek.Time <= nextCandidate.Peek.Time ? earliestTick : nextCandidate);
		}

		public static bool IsEmpty<T>(this Queue<T> queue)
		{
			return queue.Count == 0;
		}
	}

	public class Resolutions
	{
		public static readonly Resolution[] Fine = new[] {Resolution.Tick, Resolution.Second, Resolution.Minute};
		public static readonly Resolution[] Coarse = new[] {Resolution.Hour, Resolution.Daily};
	}

	public class Info
	{
		private static readonly Process thisProcess = System.Diagnostics.Process.GetCurrentProcess();

		public static long CurrentMemoryUsage
		{
			get 
			{
				return thisProcess.WorkingSet64 / (1024*1024);
			}
		}
	}
}

