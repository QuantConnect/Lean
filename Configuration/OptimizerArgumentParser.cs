using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;

namespace QuantConnect.Configuration
{
    /// <summary>
    /// Command Line arguments parser for Lean Optimizer
    /// </summary>
    public static class OptimizerArgumentParser
    {
        private const string ApplicationName = "Lean Optimizer";

        private const string ApplicationDescription = "Lean Optimizer is a strategy optimization engine for algorithms.";

        private const string ApplicationHelpText = "If you are looking for help, please go to https://www.quantconnect.com/lean/docs";

        private static readonly List<CommandLineOption> Options = new List<CommandLineOption>
            {
                new CommandLineOption("estimate", CommandOptionType.NoValue, "Estimate the optimization run time")
            };

        /// <summary>
        /// Parse and construct the args
        /// </summary>
        public static Dictionary<string, object> ParseArguments(string[] args)
        {
            return ApplicationParser.Parse(ApplicationName, ApplicationDescription, ApplicationHelpText, args, Options);
        }
    }
}
