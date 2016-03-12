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
using System.Threading;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.AlgoSeekOptionsConverter
{
    /// <summary>
    /// Provides an implementation of <see cref="IStreamParser"/> that parses raw algo seek options data
    /// </summary>
    public class AlgoSeekOptionsParser : IStreamParser
    {
        private const int LogInterval = 1000000;

        static AlgoSeekOptionsParser()
        {
            Log.Error("WARNING:: TEST MODE:: AWAITING FINAL FILE NAMING CONVENTION");
        }

        /// <summary>
        /// Parses the specified input stream into an enumerable of data
        /// </summary>
        /// <param name="source">The source of the stream</param>
        /// <param name="stream">The input stream to be parsed</param>
        /// <returns>An enumerable of base data</returns>
        public IEnumerable<BaseData> Parse(string source, Stream stream)
        {
            var count = 0L;
            var referenceDate = DateTime.ParseExact(new FileInfo(source).Directory.Name, DateFormat.EightCharacter, null);
            
            using (var reader = new StreamReader(stream))
            {
                // skip the header row
                reader.ReadLine();

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    count++;

                    if (count%LogInterval == 0)
                    {
                        Log.Trace("AlgoSeekOptionsParser.Parse({0}): Parsed {1,3}M lines.", source, count/LogInterval);
                    }

                    Tick tick;
                    try
                    {
                        // filter out bad lines as fast as possible
                        EventType eventType;
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
                                        continue;
                                }
                                break;
                            default:
                                continue;
                        }
                        
                        // parse csv check column count
                        const int columns = 11;
                        var csv = line.ToCsv(columns);
                        if (csv.Count < columns) continue;

                        // ignoring time zones completely -- this is all in the 'data-time-zone'
                        var timeString = csv[0];
                        var hours = timeString.Substring(0, 2).ToInt32();
                        var minutes = timeString.Substring(3, 2).ToInt32();
                        var seconds = timeString.Substring(6, 2).ToInt32();
                        var millis = timeString.Substring(9, 3).ToInt32();
                        var time = referenceDate.Add(new TimeSpan(0, hours, minutes, seconds, millis));

                        // detail: PUT at 30.0000 on 2014-01-18
                        var underlying = csv[4];

                        //FOR WINDOWS TESTING
                        //if (underlying.Equals("AUX", StringComparison.OrdinalIgnoreCase)
                         //|| underlying.Equals("CON", StringComparison.OrdinalIgnoreCase)
                         //|| underlying.Equals("PRN", StringComparison.OrdinalIgnoreCase))
                        //{
                            //continue;
                        //}

                        var optionRight = csv[5][0] == 'P' ? OptionRight.Put : OptionRight.Call;
                        var expiry = DateTime.ParseExact(csv[6], "yyyyMMdd", null);
                        var strike = csv[7].ToDecimal()/10000m;
                        var optionStyle = OptionStyle.American; // couldn't see this specified in the file, maybe need a reference file
                        var sid = SecurityIdentifier.GenerateOption(expiry, underlying, Market.USA, strike, optionRight, optionStyle);
                        var symbol = new Symbol(sid, underlying);
                        
                        var price = csv[9].ToDecimal() / 10000m;
                        var quantity = csv[8].ToInt32();

                        tick = new Tick
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
                    }
                    catch (Exception err)
                    {
                        Log.Error(err);
                        continue;
                    }

                    yield return tick;
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Specifies the event types to be parsed from the raw data, all other data is ignored
        /// </summary>
        class EventType
        {
            public static readonly EventType Trade = new EventType(false, TickType.Trade);
            public static readonly EventType Bid = new EventType(false, TickType.Quote);
            public static readonly EventType Ask = new EventType(true, TickType.Quote);
            public readonly bool IsAsk;
            public readonly TickType TickType;
            private EventType(bool isAsk, TickType tickType)
            {
                IsAsk = isAsk;
                TickType = tickType;
            }
        }
    }
}