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

using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;

namespace QuantConnect.Configuration
{
    /// <summary>
    /// Command Line arguments parser for Report Creator
    /// </summary>
    public static class ReportArgumentParser
    {
        private const string ApplicationName = "Report Creator";

        private const string ApplicationDescription =
            "LEAN Report Creator generates beautiful PDF reports from your backtesting strategies for sharing with prospective partners.";

        private const string ApplicationHelpText =
            "If you are looking for help, please go to https://www.quantconnect.com/docs";

        private static readonly List<CommandLineOption> Options = new List<CommandLineOption>
            {
                new CommandLineOption("strategy-name", CommandOptionType.SingleValue, "Strategy name"),
                new CommandLineOption("strategy-description", CommandOptionType.SingleValue, "Strategy description"),
                new CommandLineOption("live-data-source-file", CommandOptionType.SingleValue, "Live source data json file"),
                new CommandLineOption("backtest-data-source-file", CommandOptionType.SingleValue, "Backtest source data json file"),
                new CommandLineOption("report-destination", CommandOptionType.SingleValue, "Destination of processed report file")
            };

        /// <summary>
        /// Parse and construct the args.
        /// </summary>
        public static Dictionary<string, object> ParseArguments(string[] args)
        {
            return ApplicationParser.Parse(ApplicationName, ApplicationDescription, ApplicationHelpText, args, Options);
        }
    }
}
