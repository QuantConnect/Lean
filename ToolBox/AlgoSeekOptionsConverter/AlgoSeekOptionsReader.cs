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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;


namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
	public class AlgoSeekOptionsReader : IEnumerator<BaseData>
	{
		private readonly string _file;
		private readonly Stream _stream;
		private readonly StreamReader _streamReader;
		private readonly DateTime _referenceDate;

	    public bool HasNext { get { return Current != null; } }

	    object IEnumerator.Current { get { return Current; } }

	    public BaseData Current { get; private set; }

	    public long Count { get; private set; }

		public long InvalidLines { get; private set; }

		private bool IsEOF { get { return _streamReader.Peek() == -1; } }

		public AlgoSeekOptionsReader(string file, DateTime referenceDate)
		{
		    Current = null;
		    _file = file;
			_referenceDate = referenceDate;

			var streamProvider = StreamProvider.ForExtension(Path.GetExtension(file));
			_stream = streamProvider.Open(file).First();
			_streamReader = new StreamReader(_stream);

			MoveNext();
		}

		public void Dispose()
		{
			_stream.Close();
			_stream.Dispose();
			_streamReader.Close();
			_streamReader.Dispose();
		}

		public BaseData Take()
		{
			var thisLine = Current;
			MoveNext();
			return thisLine;
		}

		public bool MoveNext()
		{
			Current = NextValidEntry();
			return Current != null;
		}

	    public void Reset()
	    {
	        throw new NotImplementedException("Reset not implemented for AlgoSeekOptionsReader.");
	    }

		private BaseData NextValidEntry()
		{
			BaseData nextValidEntry = null;
			while (nextValidEntry == null && !IsEOF)
			{
				InvalidLines++;
				nextValidEntry = ParseNextLine();
			}
			return nextValidEntry;
		}

		private BaseData ParseNextLine()
		{
			if (IsEOF)
			{
				return null;
			}

			Tick tick = null;
			try
			{
				var line = _streamReader.ReadLine();
				tick = ParseIntoTick(line);
			}
			catch (Exception err)
			{
				Log.Error(err);
			}

			return tick;
		}

		private Tick ParseIntoTick(string line)
		{
			// filter out bad lines as fast as possible
			EventType eventType;
			if (!EventType.TryParse(line, out eventType))
			{
				return null;
			}

			// parse csv check column count
			const int columns = 11;
			var csv = line.ToCsv(columns);
			if (csv.Count < columns) 
			{
				return null;
			}
			Count++;

			// ignoring time zones completely -- this is all in the 'data-time-zone'
			var timeString = csv[0];
			var hours = timeString.Substring(0, 2).ToInt32();
			var minutes = timeString.Substring(3, 2).ToInt32();
			var seconds = timeString.Substring(6, 2).ToInt32();
			var millis = timeString.Substring(9, 3).ToInt32();
			var time = _referenceDate.Add(new TimeSpan(0, hours, minutes, seconds, millis));

			// detail: PUT at 30.0000 on 2014-01-18
			var underlying = csv[4];

			var optionRight = csv[5][0] == 'P' ? OptionRight.Put : OptionRight.Call;
			var expiry = DateTime.ParseExact(csv[6], "yyyyMMdd", null);
			var strike = csv[7].ToDecimal()/10000m;
			var optionStyle = OptionStyle.American; // couldn't see this specified in the file, maybe need a reference file
			var sid = SecurityIdentifier.GenerateOption(expiry, underlying, Market.USA, strike, optionRight, optionStyle);
			var symbol = new Symbol(sid, underlying);

			var price = csv[9].ToDecimal() / 10000m;
			var quantity = csv[8].ToInt32();

			var tick = new Tick
			{
				Symbol = symbol,
				Time = time,
				TickType = eventType.TickType,
				Exchange = csv[10],
				Value = price
			};
			if (eventType.TickType == TickType.Quote)
			{
				if (eventType.IsAsk)
				{
					tick.AskPrice = price;
					tick.AskSize = quantity;
				}
				else
				{
					tick.BidPrice = price;
					tick.BidSize = quantity;
				}
			}
			else
			{
				tick.Quantity = quantity;
			}

			return tick;
		}
	}

	class EventType
	{
		public static readonly EventType Trade = new EventType(false, TickType.Trade);
		public static readonly EventType Bid = new EventType(false, TickType.Quote);
		public static readonly EventType Ask = new EventType(true, TickType.Quote);

		public bool IsAsk { get; private set; }
		public TickType TickType { get; private set; }

		private EventType(bool isAsk, TickType tickType)
		{
			IsAsk = isAsk;
			TickType = tickType;
		}

		public static bool TryParse(string line, out EventType eventType)
		{
			switch (line[13])
			{
			case 'T':
				eventType = EventType.Trade;
				break;
			case 'F':
				switch (line[15])
				{
				case 'B':
					eventType = EventType.Bid;
					break;
				case 'O':
					eventType = EventType.Ask;
					break;
				default:
					eventType = null;
					return false;
				}
				break;
			default:
				eventType = null;
				return false;
			}

			return true;
		}
	}
}
