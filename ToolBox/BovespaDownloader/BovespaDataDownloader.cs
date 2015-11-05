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

using Ionic.Zip;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

namespace QuantConnect.ToolBox.BovespaDownloader
{
    /// <summary>
    /// Bovespa Data Downloader class
    /// </summary>
    public class BovespaDataDownloader : IDataDownloader
    {
        private string _ftpsite = string.Empty;
        private string _inputDirectory = string.Empty;
        private const string InstrumentsFileName = "instruments_bovespa.txt";
        private Dictionary<string, LeanInstrument> _instruments = new Dictionary<string, LeanInstrument>();
        private static readonly Dictionary<SecurityType, string> FtpSiteDic = new Dictionary<SecurityType, string>() 
        { 
            { SecurityType.Future, "ftp://ftp.bmf.com.br/MarketData/BMF" },
            { SecurityType.Equity, "ftp://ftp.bmf.com.br/MarketData/Bovespa-Vista" },
            { SecurityType.Option, "ftp://ftp.bmf.com.br/MarketData/Bovespa-Opcoes" } 
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="BovespaDataDownloader"/> class
        /// </summary>
        public BovespaDataDownloader()
        {
            LoadInstruments();
        }

        /// <summary>
        /// Loads the instrument list from the instruments.txt file
        /// </summary>
        /// <returns></returns>
        private void LoadInstruments()
        {
            if (!File.Exists(InstrumentsFileName))
                throw new FileNotFoundException(InstrumentsFileName + " file not found.");

            _instruments = new Dictionary<string, LeanInstrument>();

            var lines = File.ReadAllLines(InstrumentsFileName);
            foreach (var line in lines)
            {
                var tokens = line.Split(',');
                if (tokens.Length >= 3)
                {
                    var instrument = new LeanInstrument
                    {
                        Symbol = tokens[0],
                        Name = tokens[1],
                        Type = (SecurityType)Enum.Parse(typeof(SecurityType), tokens[2]),

                    };

                    if (tokens.Length >= 4) instrument.PointValue = double.Parse(tokens[3]);
                    _instruments.Add(tokens[0], instrument);
                }
            }
        }

        /// <summary>
        /// Checks if downloader can get the data for the symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns>Returns true if the symbol is available</returns>
        public bool HasSymbol(Symbol symbol)
        {
            return _instruments.ContainsKey(symbol);
        }

        /// <summary>
        /// Gets the security type for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol</param>
        /// <returns>The security type</returns>
        public SecurityType GetSecurityType(Symbol symbol)
        {
            return _instruments[symbol].Type;
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="type">Security type</param>
        /// <param name="resolution">Resolution of the data request</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, SecurityType type, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            if (!_instruments.ContainsKey(symbol))
                throw new ArgumentException("Invalid symbol requested: " + symbol);

            if (type != SecurityType.Equity && type != SecurityType.Option && type != SecurityType.Future)
                throw new NotSupportedException("SecurityType not available: " + type);

            if (endUtc < startUtc)
                throw new ArgumentException("The end date must be greater or equal to the start date.");

            _inputDirectory = Directory.CreateDirectory(String.Format("{0}/{1}", Config.Get("input-directory", "./Data"), type)).FullName;
            _ftpsite = FtpSiteDic[type];

            #region For equity daily files, use equity daily data!
            if (type == SecurityType.Equity && resolution == Resolution.Daily)
            {
                for (var year = startUtc.Year; year <= endUtc.Year; year++)
                {
                    foreach (var bar in GetDailyData(symbol, year))
                    {
                        startUtc = bar.Time;
                        yield return bar;
                    }
                }
            }
            #endregion

            foreach (var file in ListTickDataFiles(type, resolution, startUtc, endUtc))
            {
                // Request all ticks for a specific date
                var ticks = GetTickData(symbol, file);

                if (resolution == Resolution.Tick)
                {
                    foreach (var tick in ticks)
                    {
                        yield return tick;
                    }
                }
                else
                {
                    foreach (var bar in AggregateTicks(symbol, ticks, resolution))
                    {
                        yield return bar;
                    }
                }
            }
        }

        /// <summary>
        /// Get daily bars for the specified symbol from given file.
        /// If we do not have the file locally we download it from Dropbox
        /// </summary>
        /// <param name="symbol">The requested symbol</param>
        /// <param name="date">The requested date</param>
        /// <returns>An enumerable of ticks</returns>
        private IEnumerable<BaseData> GetDailyData(Symbol symbol, int year)
        {
            var localfile = String.Format("{0}/COTAHIST_A{1}.zip", _inputDirectory, year);
            var remotefile = String.Format("https://dl.dropboxusercontent.com/u/44311500/Data/Equity/COTAHIST_A{0}.zip", year);
        
            #region Download file from Dropbox if it does not exist locally
            if (!File.Exists(localfile) || new FileInfo(localfile).Length == 0 || year == DateTime.Now.Year)
            {
                using (var client = new WebClient())
                {                    
                    try { client.DownloadFile(remotefile, localfile); }
                    catch (Exception) { }
                }
            }
            #endregion

            if (!File.Exists(localfile))
            {
                Console.WriteLine("Do not have nor could download COTAHIST_A{0}.zip", year);
            }
            else
            {
                ZipFile zip;
                using (var reader = Compression.Unzip(localfile, out zip))
                {
                    var bars = new List<TradeBar>();

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        
                        if (line.Contains(symbol + " "))
                        {
                            bars.Add(new TradeBar(
                                line.Substring(2).ToDateTime(),
                                symbol,
                                Convert.ToInt64(line.Substring(56, 13)) / 100m,
                                Convert.ToInt64(line.Substring(69, 13)) / 100m,
                                Convert.ToInt64(line.Substring(82, 13)) / 100m,
                                Convert.ToInt64(line.Substring(108, 13)) / 100,
                                //Convert.ToInt64(line.Substring(152, 18)),   // QUATOT
                                Convert.ToInt64(line.Substring(170, 16)),   // VOLTOT
                                Resolution.Daily.ToTimeSpan()));
                        }
                    }

                    foreach (var bar in bars.OrderBy(x => x.Time))
                    {
                        yield return bar;
                    }
                }
            }
        }

        /// <summary>
        /// List files that contains the desired data 
        /// </summary>
        /// <param name="type">Security type</param>
        /// <param name="resolution">Resolution of input data</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of string with data for this symbol</returns>
        private IEnumerable<string> ListTickDataFiles(SecurityType type, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            try
            {
                var request = (FtpWebRequest)WebRequest.Create(_ftpsite);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential("anonymous", "me@home.com");

                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(responseStream))
                        {
                            var startdate = startUtc.AddDays(1 - startUtc.Day);
                            return reader.ReadToEnd().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                                .Where(x =>
                                {
                                    try
                                    {
                                        // No support for bid/ask data
                                        if (x.Contains("OFER")) return false;
                                        
                                        // No bid/ask data for daily resolution
                                        if (x.Contains("OFER") && resolution == Resolution.Daily) return false;

                                        // No files with odd lot
                                        if (x.Contains("FRAC")) return false;

                                        // Only zip files contain data
                                        if (!x.Contains(".zip")) return false;

                                        var date = DateTime.ParseExact(x.Replace(".zip", "").Trim().Split('_')[(x.Contains("NEG") ? 0 : 1) + (type == SecurityType.Equity ? 1 : 2)], "yyyyMMdd", null);
                                        return date >= startdate && date <= endUtc;
                                    }
                                    catch (Exception)
                                    {
                                        Log.Error(x + " is out of place!");
                                        return false;
                                    }
                                });
                        }
                    }
                }
            }
            catch (Exception)
            {
                return new DirectoryInfo(_inputDirectory).EnumerateFiles("*.zip").Where(x => x.Length > 0 && !x.Name.Contains("COTAHIST_A") && !x.Name.Contains("OFER")).Select(x => x.Name);
            }
        }
        
        /// <summary>
        /// Get ticks for the specified symbool from a given file.
        /// If we do not have the file locally we download it.
        /// </summary>
        /// <param name="symbol">The requested symbol</param>
        /// <param name="date">The requested date</param>
        /// <returns>An enumerable of ticks</returns>
        private IEnumerable<BaseData> GetTickData(Symbol symbol, string file)
        {
            var localfile = String.Format("{0}/{1}", _inputDirectory, file);

            #region Download file from FTP site if it does not exist locally
            if (!File.Exists(localfile) || new FileInfo(localfile).Length == 0)
            {
                try
                {
                    var request = (FtpWebRequest)WebRequest.Create(String.Format("{0}/{1}", _ftpsite, file));
                    request.Credentials = new NetworkCredential("anonymous", "me@home.com");
                    request.Method = WebRequestMethods.Ftp.DownloadFile;

                    using (var response = (FtpWebResponse)request.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            var readCount = 0;
                            var buffer = new byte[2048];

                            // Create new local file
                            using (var newFile = new FileStream(localfile, FileMode.Create))
                            {
                                while ((readCount = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                                    newFile.Write(buffer, 0, readCount);
                            }
                        }
                    }
                }
                catch (Exception) { }
            }
            #endregion

            if (!File.Exists(localfile))
            {
                Console.WriteLine("Do not have nor could download " + file);
            }
            else
            {
                ZipFile zip;
                var isBid = localfile.Contains("OFER_CPA");
                var isAsk = localfile.Contains("OFER_VDA");
                var tickType = localfile.Contains("NEG") ? TickType.Trade : TickType.Quote;

                using (var toplevelreader = Compression.Unzip(localfile, out zip))
                {
                    for (var i = 0; i < zip.Entries.Count; i++)
                    {
                        using (var reader = new StreamReader(zip[i].OpenReader()))
                        {
                            while (!reader.EndOfStream)
                            {
                                var line = reader.ReadLine();
                                if (!line.Contains(symbol)) continue;

                                var csv = line.Split(';');

                                if (tickType == TickType.Trade)
                                {
                                    yield return new Tick
                                    {
                                        Symbol = symbol,
                                        TickType = tickType,
                                        Time = DateTime.ParseExact(csv[0], "yyyy-MM-dd", null).Add(TimeSpan.Parse(csv[5])),
                                        Value = decimal.Parse(csv[3], NumberStyles.Any, CultureInfo.InvariantCulture),
                                        Quantity = int.Parse(csv[4])
                                    };
                                }

                                // For QuoteBar, we want best buy and sell quote, 
                                // therefore Priority indicator (csv[7]) ought to be zero. 
                                if (long.Parse(csv[7]) > 0) continue;

                                if (isBid)
                                {
                                    yield return new Tick
                                    {
                                        Symbol = symbol,
                                        TickType = tickType,
                                        Time = DateTime.ParseExact(csv[0], "yyyy-MM-dd", null).Add(TimeSpan.Parse(csv[6])),
                                        BidPrice = decimal.Parse(csv[8], NumberStyles.Any, CultureInfo.InvariantCulture),
                                        BidSize = int.Parse(csv[9])
                                    };
                                }

                                if (isAsk)
                                {
                                    yield return new Tick
                                    {
                                        Symbol = symbol,
                                        TickType = tickType,
                                        Time = DateTime.ParseExact(csv[0], "yyyy-MM-dd", null).Add(TimeSpan.Parse(csv[6])),
                                        AskPrice = decimal.Parse(csv[8], NumberStyles.Any, CultureInfo.InvariantCulture),
                                        AskSize = int.Parse(csv[9])
                                    };
                                }
                            }
                        }
                    }
                    // Manually dispose the ZipFile object
                    zip.Dispose();
                }
            }
        }

        /// <summary>
        /// Aggregates a list of ticks at the requested resolution
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="ticks">Input tick data</param>
        /// <param name="resolution">Output resolution</param>
        /// <returns>Enumerable of trade bars or quote bars</returns>
        private static IEnumerable<BaseData> AggregateTicks(Symbol symbol, IEnumerable<BaseData> data, Resolution resolution)
        {
            var ticks = data.Cast<Tick>().OrderBy(t => t.Time).ToList();

            if (ticks.All(t => t.TickType == TickType.Trade))
            {
                return
                    (from t in ticks
                     group t by t.Time.RoundDown(resolution.ToTimeSpan())
                         into g
                         select new TradeBar
                         {
                             DataType = MarketDataType.TradeBar,
                             Symbol = symbol,
                             Time = g.Key,
                             Open = g.First().LastPrice,
                             High = g.Max(t => t.LastPrice),
                             Low = g.Min(t => t.LastPrice),
                             Close = g.Last().LastPrice,
                             Volume = g.Sum(t => t.Quantity),
                         });
            }
            else
            {
                return 
                    (from t in ticks
                     group t by t.Time.RoundDown(resolution.ToTimeSpan()) into g
                     
                     select new QuoteBar
                     {
                         DataType = MarketDataType.QuoteBar,
                         Symbol = symbol,
                         //Time = g.Key,
                         //AvgBidSize = (long)g.Where(t => t.BidSize > 0).Average(t => t.BidSize),
                         //AvgAskSize = (long)g.Where(t => t.AskSize > 0).Average(t => t.AskSize),
                         //Bid = new Bar
                         //(
                         //    g.Where(t => t.BidSize > 0).First().LastPrice,
                         //    g.Where(t => t.BidSize > 0).Max(t => t.LastPrice),
                         //    g.Where(t => t.BidSize > 0).Min(t => t.LastPrice),
                         //    g.Where(t => t.BidSize > 0).Last().LastPrice
                         //),
                         //Ask = new Bar
                         //(
                         //    g.Where(t => t.AskSize > 0).First().LastPrice,
                         //    g.Where(t => t.AskSize > 0).Max(t => t.LastPrice),
                         //    g.Where(t => t.AskSize > 0).Min(t => t.LastPrice),
                         //    g.Where(t => t.AskSize > 0).Last().LastPrice
                         //),         
                     });
            }
        }
    }
}
