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
    /// Command Line arguments parser for Lean configuration
    /// </summary>
    public static class LeanArgumentParser
    {
        private const string ApplicationName = "Lean Platform";

        private const string ApplicationDescription =
            "Lean Engine is an open-source algorithmic trading engine built for easy strategy research, backtesting and live trading. We integrate with common data providers and brokerages so you can quickly deploy algorithmic trading strategies.";

        private const string ApplicationHelpText =
            "If you are looking for help, please go to https://www.quantconnect.com/lean/docs";

        private static readonly List<CommandLineOption> Options = new List<CommandLineOption>
            {
                // the location of the configuration to use
                new CommandLineOption("config", CommandOptionType.SingleValue),

                // true will close lean console automatically without waiting for input
                new CommandLineOption("close-automatically", CommandOptionType.SingleValue),

                // the result destination folder this algorithm should use for logging and result.json
                new CommandLineOption("results-destination-folder", CommandOptionType.SingleValue),

                // the algorithm name
                new CommandLineOption("backtest-name", CommandOptionType.SingleValue),

                // the unique algorithm id
                new CommandLineOption("algorithm-id", CommandOptionType.SingleValue),

                // the unique optimization id
                new CommandLineOption("optimization-id", CommandOptionType.SingleValue),

                // Options grabbed from json file
                new CommandLineOption("environment", CommandOptionType.SingleValue),

                // algorithm class selector
                new CommandLineOption("algorithm-type-name", CommandOptionType.SingleValue),

                // Algorithm language selector - options CSharp, Python
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
                new CommandLineOption("history-provider", CommandOptionType.SingleValue),

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
                new CommandLineOption("job-organization-id", CommandOptionType.SingleValue),

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

                // coinbase configuration
                new CommandLineOption("coinbase-rest-api", CommandOptionType.SingleValue),
                new CommandLineOption("coinbase-url", CommandOptionType.SingleValue),
                new CommandLineOption("coinbase-api-key", CommandOptionType.SingleValue),
                new CommandLineOption("coinbase-api-secret", CommandOptionType.SingleValue),

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
