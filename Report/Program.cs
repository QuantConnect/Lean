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
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Python;

namespace QuantConnect.Report
{
    /// <summary>
    /// Lean Report creates a PDF strategy summary from the backtest and live json objects.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Adds the current working directory to the PYTHONPATH env var.
            PythonInitializer.SetPythonPathEnvironmentVariable();

            // Parse report arguments and merge with config to use in report creator:
            if (args.Length > 0)
            {
                Config.MergeCommandLineArgumentsWithConfiguration(ReportArgumentParser.ParseArguments(args));
            }
            var name = Config.Get("strategy-name");
            var description = Config.Get("strategy-description");
            var version = Config.Get("strategy-version");
            var backtestDataFile = Config.Get("backtest-data-source-file");
            var liveDataFile = Config.Get("live-data-source-file");
            var destination = Config.Get("report-destination");

            // Parse content from source files into result objects
            Log.Trace($"QuantConnect.Report.Main(): Parsing source files...{backtestDataFile}, {liveDataFile}");
            var backtestSettings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new NullResultValueTypeJsonConverter<BacktestResult>() },
                FloatParseHandling = FloatParseHandling.Decimal
            };

            var backtest = JsonConvert.DeserializeObject<BacktestResult>(File.ReadAllText(backtestDataFile), backtestSettings);
            LiveResult live = null;

            if (liveDataFile != string.Empty)
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Converters = new List<JsonConverter> { new NullResultValueTypeJsonConverter<LiveResult>() }
                };

                live = JsonConvert.DeserializeObject<LiveResult>(File.ReadAllText(liveDataFile), settings);
            }

            //Create a new report
            Log.Trace("QuantConnect.Report.Main(): Instantiating report...");
            var report = new Report(name, description, version, backtest, live);

            // Generate the html content
            Log.Trace("QuantConnect.Report.Main(): Starting content compile...");
            var html = report.Compile();

            //Write it to target destination.
            if (destination != string.Empty)
            {
                Log.Trace($"QuantConnect.Report.Main(): Writing content to file {destination}");
                File.WriteAllText(destination, html);
            }
            else
            {
                Console.Write(html);
            }
            Log.Trace("QuantConnect.Report.Main(): Completed.");
            Console.ReadKey();
        }
    }
}
