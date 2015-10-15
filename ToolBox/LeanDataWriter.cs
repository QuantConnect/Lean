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
using System.Globalization;
using System.IO;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Data writer for saving an IEnumerable of BaseData into the LEAN data directory.
    /// </summary>
    public class LeanDataWriter
    {
        private readonly Symbol _symbol;
        private readonly string _market;
        private readonly string _dataDirectory;
        private readonly TickType _dataType;
        private readonly Resolution _resolution;
        private readonly SecurityType _securityType;
        
        /// <summary>
        /// Create a new lean data writer to this base data directory.
        /// </summary>
        /// <param name="symbol">Symbol string</param>
        /// <param name="dataDirectory">Base data directory</param>
        /// <param name="type">Security type</param>
        /// <param name="resolution">Resolution of the desired output data</param>
        /// <param name="market">Market for this security</param>
        /// <param name="dataType">Write the data to trade files</param>
        public LeanDataWriter(SecurityType type, Resolution resolution, Symbol symbol, string dataDirectory, string market, TickType dataType = TickType.Trade)
        {
            _securityType = type;
            _dataDirectory = dataDirectory;
            _resolution = resolution;
            _symbol = symbol.ToLower();
            _market = market.ToLower();
            _dataType = dataType;

            //All fx data is quote data.
            if (_securityType == SecurityType.Forex)
            {
                _dataType = TickType.Quote;
            }

            //Can only process Fx and equity for now
            if (_securityType != SecurityType.Equity && _securityType != SecurityType.Forex)
            {
                throw new Exception("Sorry this security type is not yet supported by the LEAN data writer: " + _securityType);
            }
        }

        /// <summary>
        /// Given the constructor parameters, write out the data in LEAN format.
        /// </summary>
        /// <param name="source">IEnumerable source of the data: sorted from oldest to newest.</param>
        public void Write(IEnumerable<BaseData> source)
        {
            var file = string.Empty;
            var lastTime = new DateTime();
            var lastOutputFile = string.Empty;
            
            //Determine file path:
            var baseDirectory = Path.Combine(_dataDirectory, _securityType.ToString().ToLower(), _market);

            //Loop through all the data and write to file as we go.
            foreach (var data in source)
            {
                //Ensure the data is sorted
                if (data.Time < lastTime) throw new Exception("The data must be pre-sorted from oldest to newest");
                lastTime = data.Time;

                //Based on the security type and resolution, write the data to the zip file.
                var outputFile = ZipOutputFile(baseDirectory, data);
                if (outputFile != lastOutputFile)
                {
                    //If an existing collection of data, write this before continue:
                    if (lastOutputFile != string.Empty)
                    {
                        //Write and reset the file:
                        WriteFile(lastOutputFile, file, lastTime);
                        file = string.Empty;
                    }
                    lastOutputFile = outputFile;
                }

                //Build the line and append it to the file:
                file += GenerateFileLine(data) + Environment.NewLine;
            }

            //Write the last file:
            if (lastOutputFile != string.Empty)
            {
                WriteFile(lastOutputFile, file, lastTime);
            }
        }

        /// <summary>
        /// Write this file to disk
        /// </summary>
        private void WriteFile(string data, string filename, DateTime time)
        {
            filename = filename.TrimEnd();
            if (File.Exists(data))
            {
                File.Delete(data);
                Log.Trace("LeanDataWriter.Write(): Existing deleted: " + data);
            }
            //Create the directory if it doesnt exist
            Directory.CreateDirectory(Directory.GetDirectoryRoot(data));

            //Write out this data string to a zip file.
            Compression.Zip(filename, data, Compression.CreateZipEntryName(_symbol, _securityType, time, _resolution, _dataType));
            Log.Trace("LeanDataWriter.Write(): Created: " + data);
        }

        /// <summary>
        /// Generate a single line of the data for this security type
        /// </summary>
        /// <param name="data">Data we're generating</param>
        /// <returns>String line for this basedata</returns>
        private string GenerateFileLine(IBaseData data)
        {
            var line = string.Empty;
            var format = "{0},{1},{2},{3},{4},{5}";
            var milliseconds = data.Time.TimeOfDay.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            var longTime = data.Time.ToString(DateFormat.TwelveCharacter);

            switch (_securityType)
            {
                case SecurityType.Equity:
                    switch (_resolution)
                    {
                        case Resolution.Tick:
                            var tick = data as Tick;
                            if (tick != null)
                            {
                                line = string.Format(format, milliseconds, Scale(tick.LastPrice), tick.Quantity, tick.Exchange, tick.SaleCondition, tick.Suspicious);
                            }
                            break;
                        case Resolution.Minute:
                        case Resolution.Second:
                            var bar = data as TradeBar;
                            if (bar != null)
                            {
                                line = string.Format(format, milliseconds, Scale(bar.Open), Scale(bar.High), Scale(bar.Low), Scale(bar.Close), bar.Volume);   
                            }
                            break;
                        case Resolution.Hour:
                        case Resolution.Daily:
                            var bigBar = data as TradeBar;
                            if (bigBar != null)
                            {
                                line = string.Format(format, longTime, Scale(bigBar.Open), Scale(bigBar.High), Scale(bigBar.Low), Scale(bigBar.Close), bigBar.Volume);
                            }
                            break;
                    }
                    break;

                case SecurityType.Forex:
                    switch (_resolution)
                    {
                        case Resolution.Tick:
                            var fxTick = data as Tick;
                            if (fxTick != null)
                            {
                                line = string.Format("{0},{1},{2}", milliseconds, fxTick.BidPrice, fxTick.AskPrice);
                            }
                            break;
                        case Resolution.Second:
                        case Resolution.Minute:
                            var fxBar = data as TradeBar;
                            if (fxBar != null)
                            {
                                line = string.Format("{0},{1},{2},{3},{4}", milliseconds, fxBar.Open, fxBar.High, fxBar.Low, fxBar.Close);
                            }
                            break;
                        case Resolution.Hour:
                        case Resolution.Daily:
                            var dailyBar = data as TradeBar;
                            if (dailyBar != null)
                            {
                                line = string.Format("{0},{1},{2},{3},{4}", longTime, dailyBar.Open, dailyBar.High, dailyBar.Low, dailyBar.Close);
                            }
                            break;
                    }
                    break;
            }
            return line;
        }

        /// <summary>
        /// Scale and convert the resulting number to deci-cents int.
        /// </summary>
        private static int Scale(decimal value)
        {
            return Convert.ToInt32(value*10000);
        }

        /// <summary>
        /// Get the output zip file
        /// </summary>
        /// <param name="baseDirectory">Base output directory for the zip file</param>
        /// <param name="data">Data we're writing</param>
        /// <returns></returns>
        private string ZipOutputFile(string baseDirectory, IBaseData data)
        {
            var file = string.Empty;
            //Further determine path based on the remaining data: security type.
            switch (_securityType)
            {
                case SecurityType.Equity:
                case SecurityType.Forex:
                    //Base directory includes the market
                    file = Path.Combine(baseDirectory, _resolution.ToString().ToLower(), _symbol.ToLower(), Compression.CreateZipFileName(_symbol, _securityType, data.Time, _resolution));

                    if (_resolution == Resolution.Daily || _resolution == Resolution.Hour)
                    {
                        file = Path.Combine(baseDirectory, _resolution.ToString().ToLower(), Compression.CreateZipFileName(_symbol, _securityType, data.Time, _resolution));
                    }
                    break;
                default:
                    throw new Exception("Sorry this security type is not yet supported by the LEAN data writer: " + _securityType);
            }
            return file;
        }

    }
}
