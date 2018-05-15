using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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


        private const string OptionHelp = "-?|-h|--help";
        private const string OptionVersion = "-v|-V|--version";

        /// <summary>
        /// Configuration file path
        /// </summary>
        private const string OptionConfig = "-c|--config";

        private static readonly string[] AdvancedProperties =
        {
            "parameters",
            "environments"
        };

        private static readonly Dictionary<string, CommandOptionType> Options =
            new Dictionary<string, CommandOptionType>
            {
                {"config", CommandOptionType.SingleValue},
                {"output", CommandOptionType.SingleValue},

                // Options grabbed from config.json file

                {"environment", CommandOptionType.SingleValue},

                // algorithm class selector
                {"algorithm-type-name", CommandOptionType.SingleValue},

                // Algorithm language selector - options CSharp, FSharp, VisualBasic, Python, Java
                {"algorithm-language", CommandOptionType.SingleValue},

                //Physical DLL location
                {"algorithm-location", CommandOptionType.SingleValue},

                //Jupyter notebook
                {"composer-dll-directory", CommandOptionType.SingleValue},

                // engine
                {"data-folder", CommandOptionType.SingleValue},

                // handlers
                {"log-handler", CommandOptionType.SingleValue},
                {"messaging-handler", CommandOptionType.SingleValue},
                {"job-queue-handler", CommandOptionType.SingleValue},
                {"api-handler", CommandOptionType.SingleValue},
                {"map-file-provider", CommandOptionType.SingleValue},
                {"factor-file-provider", CommandOptionType.SingleValue},
                {"data-provider", CommandOptionType.SingleValue},
                {"alpha-handler", CommandOptionType.SingleValue},

                // limits on number of symbols to allow
                {"symbol-minute-limit", CommandOptionType.SingleValue},
                {"symbol-second-limit", CommandOptionType.SingleValue},
                {"symbol-tick-limit", CommandOptionType.SingleValue},

                // if one uses true in following token, market hours will remain open all hours and all days.
                // if one uses false will make lean operate only during regular market hours.
                {"force-exchange-always-open", CommandOptionType.NoValue},

                // save list of transactions to the specified csv file
                {"transaction-log", CommandOptionType.SingleValue},

                // To get your api access token go to quantconnect.com/account
                {"job-user-id", CommandOptionType.SingleValue},
                {"api-access-token", CommandOptionType.SingleValue},

                // live data configuration
                {"live-data-url", CommandOptionType.SingleValue},
                {"live-data-port", CommandOptionType.SingleValue},

                // interactive brokers configuration
                {"ib-account", CommandOptionType.SingleValue},
                {"ib-user-name", CommandOptionType.SingleValue},
                {"ib-password", CommandOptionType.SingleValue},
                {"ib-host", CommandOptionType.SingleValue},
                {"ib-port", CommandOptionType.SingleValue},
                {"ib-agent-description", CommandOptionType.SingleValue},
                {"ib-use-tws", CommandOptionType.NoValue},
                {"ib-tws-dir", CommandOptionType.SingleValue},
                {"ib-trading-mode", CommandOptionType.SingleValue},
                {"ib-controller-dir", CommandOptionType.SingleValue},

                // tradier configuration
                {"tradier-account-id", CommandOptionType.SingleValue},
                {"tradier-access-token", CommandOptionType.SingleValue},
                {"tradier-refresh-token", CommandOptionType.SingleValue},
                {"tradier-issued-at", CommandOptionType.SingleValue},
                {"tradier-lifespan", CommandOptionType.SingleValue},
                {"tradier-refresh-session", CommandOptionType.NoValue},

                // oanda configuration
                {"oanda-environment", CommandOptionType.SingleValue},
                {"oanda-access-token", CommandOptionType.SingleValue},
                {"oanda-account-id", CommandOptionType.SingleValue},

                // fxcm configuration
                {"fxcm-server", CommandOptionType.SingleValue},
                {"fxcm-terminal", CommandOptionType.SingleValue}, //Real or Demo
                {"fxcm-user-name", CommandOptionType.SingleValue},
                {"fxcm-password", CommandOptionType.SingleValue},
                {"fxcm-account-id", CommandOptionType.SingleValue},

                // iqfeed configuration
                {"iqfeed-username", CommandOptionType.SingleValue},
                {"iqfeed-password", CommandOptionType.SingleValue},
                {"iqfeed-productName", CommandOptionType.SingleValue},
                {"iqfeed-version", CommandOptionType.SingleValue},

                // gdax configuration
                {"gdax-api-secret", CommandOptionType.SingleValue},
                {"gdax-api-key", CommandOptionType.SingleValue},
                {"gdax-passphrase", CommandOptionType.SingleValue},

                // Required to access data from Quandl
                // To get your access token go to https://www.quandl.com/account/api
                {"quandl-auth-token", CommandOptionType.SingleValue},

                // parameters to set in the algorithm (the below are just samples)
                {"parameters", CommandOptionType.MultipleValue},
                {"environments", CommandOptionType.MultipleValue}
            };

        /// <summary>
        /// Argument parser contructor
        /// </summary>
        public static Dictionary<string, object> ParseArguments(string[] args)
        {
            var lean = new CommandLineApplication
            {
                Name = ApplicationName,
                Description = ApplicationDescription,
                ExtendedHelpText = ApplicationHelpText
            };


            lean.HelpOption(OptionHelp);

            // This is a helper/shortcut method to display version info - it is creating a regular Option, with some defaults.
            // The default help text is "Show version Information"
            lean.VersionOption(OptionVersion,
                () =>
                    $"Version {Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion}");

            var optionsObject = new Dictionary<string, object>();

            var listOfOptions = new List<CommandOption>();

            foreach (var optionsKey in Options.Keys)
            {
                listOfOptions.Add(lean.Option($"--{optionsKey}", "@TODO", Options[optionsKey]));
            }

            lean.OnExecute(() =>
            {
                foreach (var commandOption in listOfOptions.Where(option => option.HasValue()))
                {
                    var optionKey = commandOption.Template.Replace("--", "");
                    switch (Options[optionKey])
                    {
                         // Booleans, string and numbers
                        case CommandOptionType.NoValue:
                        case CommandOptionType.SingleValue:
                            optionsObject[optionKey] = ParseTypedArgument(commandOption.Value());
                            break;
                        
                        // Parsing nested objects
                        case CommandOptionType.MultipleValue:
                            var keyValuePairs = commandOption.Value().Split(',');
                            var subDictionary = new Dictionary<string, object>();
                            foreach (var keyValuePair in keyValuePairs)
                            {
                                var subKeys = keyValuePair.Split(':');
                                subDictionary[subKeys[0]] = ParseTypedArgument(subKeys[1]);
                            }

                            optionsObject[optionKey] = subDictionary;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return 0;
            });

            lean.Execute(args);

            return optionsObject;
        }

        private static object ParseTypedArgument(string value)
        {
            if (value == "true" || value == "false")
            {
                return value == "true";
            }

            if (double.TryParse(value, out var numericValue))
            {
                return numericValue;
            }

            return value;
        }
    }
}