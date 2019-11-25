using System.Collections.Generic;
using Microsoft.Extensions.CommandLineUtils;

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