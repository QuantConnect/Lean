using System.Collections.Generic;
using Microsoft.Extensions.CommandLineUtils;

namespace QuantConnect.Configuration
{
    /// <summary>
    /// Command Line arguments parser for Lean configuration
    /// </summary>
    public static class LeanArgumentParser
    {
        private const string ApplicationName = "Lean Platform";

        private const string ApplicationDescription =
            "Lean Engine is an open-source algorithmic trading engine built for easy strategy research, backtesting and live trading. We integrate with common data providers and brokerages so you can quickly deploy algorithmic trading strategies.";

        private const string ApplicationHelpText =
            "If you are looking for help, please go to https://www.quantconnect.com/lean/docs";

        /// <summary>
        /// Configuration file path
        /// </summary>
        private const string OptionConfig = "-c|--config";

        private static readonly string[] AdvancedProperties =
        {
            "parameters",
            "environments"
        };

        private static readonly List<CommandLineOption> Options = new List<CommandLineOption>
            {
                new CommandLineOption("config", CommandOptionType.SingleValue),
                new CommandLineOption("results-destination-folder", CommandOptionType.SingleValue),
                new CommandLineOption("algorithm-id", CommandOptionType.SingleValue),

                // Options grabbed from json file
                new CommandLineOption("environment", CommandOptionType.SingleValue),

                // algorithm class selector
                new CommandLineOption("algorithm-type-name", CommandOptionType.SingleValue),

                // Algorithm language selector - options CSharp, FSharp, VisualBasic, Python, Java
                new CommandLineOption("algorithm-language", CommandOptionType.SingleValue),

                //Physical DLL location
                new CommandLineOption("algorithm-location", CommandOptionType.SingleValue),

                //Research notebook
                new CommandLineOption("composer-dll-directory", CommandOptionType.SingleValue),

                // engine
                new CommandLineOption("data-folder", CommandOptionType.SingleValue),

                // handlers
                new CommandLineOption("log-handler", CommandOptionType.SingleValue),
                new CommandLineOption("messaging-handler", CommandOptionType.SingleValue),
                new CommandLineOption("job-queue-handler", CommandOptionType.SingleValue),
                new CommandLineOption("api-handler", CommandOptionType.SingleValue),
                new CommandLineOption("map-file-provider", CommandOptionType.SingleValue),
                new CommandLineOption("factor-file-provider", CommandOptionType.SingleValue),
                new CommandLineOption("data-provider", CommandOptionType.SingleValue),
                new CommandLineOption("alpha-handler", CommandOptionType.SingleValue),

                // limits on number of symbols to allow
                new CommandLineOption("symbol-minute-limit", CommandOptionType.SingleValue),
                new CommandLineOption("symbol-second-limit", CommandOptionType.SingleValue),
                new CommandLineOption("symbol-tick-limit", CommandOptionType.SingleValue),

                // if one uses true in following token, market hours will remain open all hours and all days.
                // if one uses false will make lean operate only during regular market hours.
                new CommandLineOption("force-exchange-always-open", CommandOptionType.NoValue),

                // save list of transactions to the specified csv file
                new CommandLineOption("transaction-log", CommandOptionType.SingleValue),

                // To get your api access token go to quantconnect.com/account
                new CommandLineOption("job-user-id", CommandOptionType.SingleValue),
                new CommandLineOption("api-access-token", CommandOptionType.SingleValue),

                // live data configuration
                new CommandLineOption("live-data-url", CommandOptionType.SingleValue),
                new CommandLineOption("live-data-port", CommandOptionType.SingleValue),

                // interactive brokers configuration
                new CommandLineOption("ib-account", CommandOptionType.SingleValue),
                new CommandLineOption("ib-user-name", CommandOptionType.SingleValue),
                new CommandLineOption("ib-password", CommandOptionType.SingleValue),
                new CommandLineOption("ib-host", CommandOptionType.SingleValue),
                new CommandLineOption("ib-port", CommandOptionType.SingleValue),
                new CommandLineOption("ib-agent-description", CommandOptionType.SingleValue),
                new CommandLineOption("ib-tws-dir", CommandOptionType.SingleValue),
                new CommandLineOption("ib-trading-mode", CommandOptionType.SingleValue),

                // tradier configuration
                new CommandLineOption("tradier-account-id", CommandOptionType.SingleValue),
                new CommandLineOption("tradier-access-token", CommandOptionType.SingleValue),
                new CommandLineOption("tradier-refresh-token", CommandOptionType.SingleValue),
                new CommandLineOption("tradier-issued-at", CommandOptionType.SingleValue),
                new CommandLineOption("tradier-lifespan", CommandOptionType.SingleValue),
                new CommandLineOption("tradier-refresh-session", CommandOptionType.NoValue),

                // oanda configuration
                new CommandLineOption("oanda-environment", CommandOptionType.SingleValue),
                new CommandLineOption("oanda-access-token", CommandOptionType.SingleValue),
                new CommandLineOption("oanda-account-id", CommandOptionType.SingleValue),

                // fxcm configuration
                new CommandLineOption("fxcm-server", CommandOptionType.SingleValue),
                new CommandLineOption("fxcm-terminal", CommandOptionType.SingleValue), //Real or Demo
                new CommandLineOption("fxcm-user-name", CommandOptionType.SingleValue),
                new CommandLineOption("fxcm-password", CommandOptionType.SingleValue),
                new CommandLineOption("fxcm-account-id", CommandOptionType.SingleValue),

                // iqfeed configuration
                new CommandLineOption("iqfeed-username", CommandOptionType.SingleValue),
                new CommandLineOption("iqfeed-password", CommandOptionType.SingleValue),
                new CommandLineOption("iqfeed-productName", CommandOptionType.SingleValue),
                new CommandLineOption("iqfeed-version", CommandOptionType.SingleValue),

                // gdax configuration
                new CommandLineOption("gdax-api-secret", CommandOptionType.SingleValue),
                new CommandLineOption("gdax-api-key", CommandOptionType.SingleValue),
                new CommandLineOption("gdax-passphrase", CommandOptionType.SingleValue),

                // Required to access data from Quandl
                // To get your access token go to https://www.quandl.com/account/api
                new CommandLineOption("quandl-auth-token", CommandOptionType.SingleValue),

                // parameters to set in the algorithm (the below are just samples)
                new CommandLineOption("parameters", CommandOptionType.MultipleValue),
                new CommandLineOption("environments", CommandOptionType.MultipleValue)
            };

        /// <summary>
        /// Argument parser contructor
        /// </summary>
        public static Dictionary<string, object> ParseArguments(string[] args)
        {
            return ApplicationParser.Parse(ApplicationName, ApplicationDescription, ApplicationHelpText, args, Options);
        }
    }
}