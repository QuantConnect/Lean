/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Configuration;
using McMaster.Extensions.CommandLineUtils;
using QuantConnect.DownloaderDataProvider.Launcher.Models.Constants;

namespace QuantConnect.DownloaderDataProvider.Launcher;

public static class DownloaderDataProviderArgumentParser
{
    private const string ApplicationName = "QuantConnect.DownloaderDataProvider.exe";
    private const string ApplicationDescription = "Welcome to Lean Downloader Data Provider! 🚀 Easily download historical data from various sources with our user-friendly application. Start exploring financial data effortlessly!";
    private const string ApplicationHelpText = "Hm...";

    private static readonly List<CommandLineOption> Options = new List<CommandLineOption>
    {
        new CommandLineOption(DownloaderCommandArguments.CommandDownloaderDataDownloader, CommandOptionType.SingleValue),
        new CommandLineOption(DownloaderCommandArguments.CommandDataType, CommandOptionType.SingleValue),
        new CommandLineOption(DownloaderCommandArguments.CommandTickers, CommandOptionType.MultipleValue),
        new CommandLineOption(DownloaderCommandArguments.CommandSecurityType, CommandOptionType.SingleValue),
        new CommandLineOption(DownloaderCommandArguments.CommandMarketName, CommandOptionType.SingleValue),
        new CommandLineOption(DownloaderCommandArguments.CommandResolution, CommandOptionType.SingleValue),
        new CommandLineOption(DownloaderCommandArguments.CommandStartDate, CommandOptionType.SingleValue),
        new CommandLineOption(DownloaderCommandArguments.CommandEndDate, CommandOptionType.SingleValue)
    };

    /// <summary>
    /// Parses the command-line arguments and returns a dictionary containing parsed values.
    /// </summary>
    /// <param name="args">An array of command-line arguments.</param>
    /// <returns>A dictionary containing parsed values from the command-line arguments.</returns>
    /// <remarks>
    /// The <paramref name="args"/> parameter should contain the command-line arguments to be parsed.
    /// The method uses the ApplicationParser class to parse the arguments based on the ApplicationName, 
    /// ApplicationDescription, ApplicationHelpText, and Options properties.
    /// </remarks>
    public static Dictionary<string, object> ParseArguments(string[] args)
    {
        return ApplicationParser.Parse(ApplicationName, ApplicationDescription, ApplicationHelpText, args, Options);
    }
}
