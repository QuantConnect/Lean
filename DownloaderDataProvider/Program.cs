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
 *
*/

using QuantConnect.Util;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using QuantConnect.Lean.DownloaderDataProvider.Models.Constants;
using McMaster.Extensions.CommandLineUtils;
using System.Globalization;

namespace QuantConnect.Lean.DownloaderDataProvider;
class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            ProcessCommand(args);
        }
        else
        {
            throw new ArgumentException($"{nameof(DownloaderDataProvider)}: The arguments array is empty. Please provide valid command line arguments.");
        }
    }
    public static void ProcessCommand(string[] args)
    {
        var dataProvider
            = Composer.Instance.GetExportedValueByTypeName<IDataProvider>(Config.Get("data-provider", "DefaultDataProvider"));
        var mapFileProvider
            = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"));
        var factorFileProvider
            = Composer.Instance.GetExportedValueByTypeName<IFactorFileProvider>(Config.Get("factor-file-provider", "LocalDiskFactorFileProvider"));

        mapFileProvider.Initialize(dataProvider);
        factorFileProvider.Initialize(mapFileProvider, dataProvider);

        var optionsObject = DownloaderDataProviderArgumentParser.ParseArguments(args);

        Log.Trace($"{nameof(ProcessCommand)}:Prompt Command: {string.Join(',', optionsObject)}");

        Log.DebuggingEnabled = Config.GetBool("debug-mode");
        Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

        if (!optionsObject.TryGetValue("data-provider", out var parsedDataProvider))
        {
            parsedDataProvider = "DefaultDataProvider";
        }

        var dataDownloader = Composer.Instance.GetExportedValueByTypeName<IDataDownloader>(parsedDataProvider.ToString());

        Log.Trace($"{nameof(ProcessCommand)}: dataProvider: {dataProvider}, {dataProvider.GetType()}");

        ParsedOptionObject(optionsObject);

        //dataProvider.Get(new DataDownloaderGetParameters());
    }

    public static void ParsedOptionObject(Dictionary<string, object> parsedArguments)
    {
        var dataProvider = parsedArguments[DownloaderCommandArguments.CommandDownloaderDataProvider];
        var destinationDirectory = parsedArguments[DownloaderCommandArguments.CommandDestinationDirectory];

        if (!Enum.TryParse<TickType>(
            parsedArguments[DownloaderCommandArguments.CommandDataType].ToString(), out var tickType) || !Enum.IsDefined(typeof(TickType), tickType))
        {
            throw new ArgumentException("Invalid TickType specified. Please provide a valid TickType.");
        }

        if (!Enum.TryParse<SecurityType>(
            parsedArguments[DownloaderCommandArguments.CommandSecurityType].ToString(), out var securityType) || !Enum.IsDefined(typeof(SecurityType), securityType))
        {
            throw new ArgumentException("Invalid SecurityType specified. Please provide a valid SecurityType.");
        }

        if (!Enum.TryParse<Resolution>(
    parsedArguments[DownloaderCommandArguments.CommandResolution].ToString(), out var resolution) || !Enum.IsDefined(typeof(Resolution), resolution))
        {
            throw new ArgumentException("Invalid SecurityType specified. Please provide a valid SecurityType.");
        }

        var startDate = DateTime.ParseExact(parsedArguments[DownloaderCommandArguments.CommandStartDate].ToString()!, "yyyyMMdd", CultureInfo.InvariantCulture);
        var endDate = DateTime.ParseExact(parsedArguments[DownloaderCommandArguments.CommandEndDate].ToString()!, "yyyyMMdd", CultureInfo.InvariantCulture);


        if(!parsedArguments.TryGetValue(DownloaderCommandArguments.CommandMarketName, out var marketNameObj))
        {
            marketNameObj = Market.USA;
        }

        var marketName = marketNameObj.ToString()?.ToLower();
        if (!Market.SupportedMarkets().Contains(marketName))
        {
            var supportedMarkets = string.Join(", ", Market.SupportedMarkets());
            throw new ArgumentException($"The specified market '{marketName}' is not supported. Supported markets are: {supportedMarkets}.");
        }

        var symbols = new List<Symbol>();
        foreach (var ticker in (parsedArguments[DownloaderCommandArguments.CommandTickers] as Dictionary<string, string>)!.Keys)
        {
            symbols.Add(Symbol.Create(ticker, securityType, marketName));
        }

        Log.Trace($"{nameof(ProcessCommand)}: dataProvider: {dataProvider}, {dataProvider.GetType()}");
    }
}
