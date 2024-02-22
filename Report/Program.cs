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
using System.IO;
using Newtonsoft.Json;
using QuantConnect.Util;
using System.Diagnostics;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Lean.Engine;
using System.Collections.Generic;
using QuantConnect.Configuration;

namespace QuantConnect.Report
{
    /// <summary>
    /// Lean Report creates a PDF strategy summary from the backtest and live json objects.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Parse report arguments and merge with config to use in report creator:
            if (args.Length > 0)
            {
                Config.MergeCommandLineArgumentsWithConfiguration(ReportArgumentParser.ParseArguments(args));
            }

            // initialize required lean handlers
            LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
            var name = Config.Get("strategy-name");
            var description = Config.Get("strategy-description");
            var version = Config.Get("strategy-version");
            var backtestDataFile = Config.Get("backtest-data-source-file");
            var liveDataFile = Config.Get("live-data-source-file");
            var destination = Config.Get("report-destination");
            var reportFormat = Config.Get("report-format");
            var cssOverrideFile = Config.Get("report-css-override-file", "css/report_override.css");
            var htmlCustomFile = Config.Get("report-html-custom-file", "template.html");

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

            string cssOverrideContent = null;
            if (!string.IsNullOrEmpty(cssOverrideFile))
            {
                if (File.Exists(cssOverrideFile))
                {
                    cssOverrideContent = File.ReadAllText(cssOverrideFile);
                }
                else
                {
                    Log.Trace($"QuantConnect.Report.Main(): CSS override file {cssOverrideFile} was not found");
                }
            }

            string htmlCustomContent = null;
            if (!string.IsNullOrEmpty(htmlCustomFile))
            {
                if (File.Exists(htmlCustomFile))
                {
                    htmlCustomContent = File.ReadAllText(htmlCustomFile);
                }
                else
                {
                    Log.Trace($"QuantConnect.Report.Main(): HTML custom file {htmlCustomFile} was not found");
                }
            }

            //Create a new report
            Log.Trace("QuantConnect.Report.Main(): Instantiating report...");
            var report = new Report(name, description, version, backtest, live, cssOverride: cssOverrideContent, htmlCustom: htmlCustomContent);

            // Generate the html content
            Log.Trace("QuantConnect.Report.Main(): Starting content compile...");
            string html;
            string _;

            report.Compile(out html, out _);

            //Write it to target destination.
            if (destination != string.Empty)
            {
                Log.Trace($"QuantConnect.Report.Main(): Writing content to file {destination}");
                File.WriteAllText(destination, html);

                if (!String.IsNullOrEmpty(reportFormat) && reportFormat.ToUpperInvariant() == "PDF")
                {
                    try
                    {
                        Log.Trace("QuantConnect.Report.Main(): Starting conversion to PDF");
                        // Ensure wkhtmltopdf and xvfb are installed and accessible from the $PATH
                        var pdfDestination = destination.Replace(".html", ".pdf");
                        Process process = new();
                        process.StartInfo.FileName = "xvfb-run";
                        process.StartInfo.Arguments = $"--server-args=\"-screen 0, 1600x1200x24+32\" wkhtmltopdf {destination} {pdfDestination}";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                        process.OutputDataReceived += (sender, e) => Log.Trace($"QuantConnect.Report.Main(): {e.Data}");
                        process.ErrorDataReceived += (sender, e) => Log.Error($"QuantConnect.Report.Main(): {e.Data}");

                        process.Start();

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        var processExited = process.WaitForExit(1*60*1000); // wait for up to 1 minutes

                        if (processExited)
                        {
                            Log.Trace("QuantConnect.Report.Main(): Convert to PDF process exited with code " + process.ExitCode);
                        }
                        else
                        {
                            Log.Error("QuantConnect.Report.Main(): Process did not exit within the timeout period.");
                            process.Kill(); // kill the process if it's still running
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"QuantConnect.Report.Main(): {ex.Message}");
                    }
                }
            }
            else
            {
                Console.Write(html);
            }

            Log.Trace("QuantConnect.Report.Main(): Completed.");

            if (!Console.IsInputRedirected && !Config.GetBool("close-automatically"))
            {
                Console.ReadKey();
            }
        }
    }
}
