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
using System.Net;
using System.IO;
using System.IO.Compression;
using QuantConnect.Logging;
using QuantConnect.ToolBox;

namespace QuantConnect.YahooDownloader
{
    class Program
    {
        /// <summary>
        /// Yahoo Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// Original by @chrisdk2015, tidied by @jaredbroad
        /// </summary>
        static void Main(string[] args)
        {
            var urlPrototype = @"http://ichart.finance.yahoo.com/table.csv?s={0}&a={1}&b={2}&c={3}&d={4}&e={5}&f={6}&g={7}&ignore=.csv";
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: YahooDownloader SYMBOL");
                Console.WriteLine("Usage: Place the data into your LEAN Data directory: /data/equity/usa/daily/SYMBOL.zip");
                Console.WriteLine("SYMBOL = eg SPY");
                Environment.Exit(1);
            }

            //Command line inputs: symbol resolution
            var symbol = args[0];

            // Starting date for the Yahoo data:
            var startMonth = 01;
            var startDay = 01;
            var startYear = 1990;

            // We subtract one day to make sure we have data from yahoo
            var finishMonth = DateTime.Today.Month;
            var finishDay = DateTime.Today.Subtract(TimeSpan.FromDays(1)).Day;
            var finishYear = DateTime.Today.Year;

            // The Yahoo Finance URL for each parameter
            var url = string.Format(urlPrototype, symbol, startMonth, startDay, startYear, finishMonth, finishDay, finishYear, "d");
            try
            {
                var cl = new WebClient();
                var data = cl.DownloadString(url);
                var lines = data.Split('\n');
                var temppath = Path.GetTempPath();
                var file = symbol + ".csv";
                var fullpath = Path.Combine(temppath, file);
                File.Delete(fullpath);
                using (var swfile = new StreamWriter(fullpath))
                {
                    for (var i = lines.Length - 1; i >= 1; i--)
                    {
                        var str = lines[i].Split(',');
                        if (str.Length < 6) continue;
                        var ymd = str[0].Split('-');
                        var year = ymd[0];
                        var month = ymd[1];
                        var day = ymd[2];
                        decimal open;
                        decimal high;
                        decimal low;
                        decimal close;
                        int volume;
                        decimal.TryParse(str[1], out open);
                        decimal.TryParse(str[2], out high);
                        decimal.TryParse(str[3], out low);
                        decimal.TryParse(str[4], out close);
                        int.TryParse(str[5], out volume);

                        //Scale into ints
                        var openScaled = decimal.ToInt32(10000 * open);
                        var highScaled = decimal.ToInt32(10000 * high);
                        var lowScaled = decimal.ToInt32(10000 * low);
                        var closeScaled = decimal.ToInt32(10000 * close);
                        var sf = string.Format("{0}{1}{2} 00:00,{3},{4},{5},{6},{7}", year, month, day, openScaled, highScaled, lowScaled, closeScaled, volume);
                        swfile.WriteLine(sf);
                    }
                }

                //Create and save zip into current directory.
                var zipfile = symbol.ToLower() + ".zip";
                var curdir = Directory.GetCurrentDirectory();
                var zipfullpath = Path.Combine(curdir, zipfile);
                File.Delete(zipfullpath);
                using (var zip = ZipFile.Open(zipfullpath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(fullpath, file);
                }

                Log.Trace("YahooDownloader: Success. Saved to "+zipfullpath);
            }
            catch (Exception err)
            {
                Log.Error("YahooDownloader(): Error: " + err.Message);
            }
        }
    }
}
