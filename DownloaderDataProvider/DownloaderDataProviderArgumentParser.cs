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

using McMaster.Extensions.CommandLineUtils;
using QuantConnect.Configuration;

namespace QuantConnect.Lean.DownloaderDataProvider;

public static class DownloaderDataProviderArgumentParser
{
    private const string ApplicationName = "QuantConnect.DownloaderDataProvider.exe";
    private const string ApplicationDescription = "Welcome to Lean Downloader Data Provider! 🚀 Easily download historical data from various sources with our user-friendly application. Start exploring financial data effortlessly!";
    private const string ApplicationHelpText = "Hm...";

    private static readonly List<CommandLineOption> Options = new List<CommandLineOption>
    {
        new CommandLineOption("data-provider", CommandOptionType.SingleValue),
        new CommandLineOption("destination-dir", CommandOptionType.SingleValue),
        new CommandLineOption("data-type", CommandOptionType.SingleValue),
        new CommandLineOption("tickers", CommandOptionType.MultipleValue),
        new CommandLineOption("security-type", CommandOptionType.SingleValue),
        new CommandLineOption("resolution", CommandOptionType.SingleValue),
        new CommandLineOption("start-date", CommandOptionType.SingleValue),
        new CommandLineOption("end-date", CommandOptionType.SingleValue)
    };

    /// <summary>
    /// Parses the given array of strings representing arguments into a dictionary of key-value pairs.
    /// </summary>
    /// <param name="args">An array of strings representing the command line arguments.</param>
    /// <returns>A dictionary containing parsed arguments with keys as argument names and values as argument values.</returns>
    /// <remarks>
    /// The arguments are expected to be in the format "--key value". For example, "--data-provider Polygon --tickers AAPL,NVDA".
    /// </remarks>
    public static Dictionary<string, object> ParseArguments(string[] args)
    {
        return ApplicationParser.Parse(ApplicationName, ApplicationDescription, ApplicationHelpText, args, Options);
    }
}
