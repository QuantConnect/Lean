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
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.ToolBox.AlgoSeekFuturesConverter;
using QuantConnect.ToolBox.CoarseUniverseGenerator;
using QuantConnect.ToolBox.DukascopyDownloader;
using QuantConnect.ToolBox.IVolatilityEquityConverter;
using QuantConnect.ToolBox.KaikoDataConverter;
using QuantConnect.ToolBox.RandomDataGenerator;
using QuantConnect.Util;
using System;
using System.IO;
using static QuantConnect.Configuration.ApplicationParser;

namespace QuantConnect.ToolBox
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.DebuggingEnabled = Config.GetBool("debug-mode");
            var destinationDir = Config.Get("results-destination-folder");
            if (!string.IsNullOrEmpty(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
                Log.FilePath = Path.Combine(destinationDir, "log.txt");
            }
            Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

            var optionsObject = ToolboxArgumentParser.ParseArguments(args);
            if (optionsObject.Count == 0)
            {
                PrintMessageAndExit();
            }

            var dataProvider
                = Composer.Instance.GetExportedValueByTypeName<IDataProvider>(Config.Get("data-provider", "DefaultDataProvider"));
            var mapFileProvider
                = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"));
            var factorFileProvider
                = Composer.Instance.GetExportedValueByTypeName<IFactorFileProvider>(Config.Get("factor-file-provider", "LocalDiskFactorFileProvider"));

            mapFileProvider.Initialize(dataProvider);
            factorFileProvider.Initialize(mapFileProvider, dataProvider);

            var targetApp = GetParameterOrExit(optionsObject, "app").ToLowerInvariant();
            if (targetApp.Contains("download") || targetApp.EndsWith("dl"))
            {
                var fromDate = Parse.DateTimeExact(GetParameterOrExit(optionsObject, "from-date"), "yyyyMMdd-HH:mm:ss");
                var resolution = optionsObject.ContainsKey("resolution") ? optionsObject["resolution"].ToString() : "";
                var market = optionsObject.ContainsKey("market") ? optionsObject["market"].ToString() : "";
                var securityType = optionsObject.ContainsKey("security-type") ? optionsObject["security-type"].ToString() : "";
                var tickers = ToolboxArgumentParser.GetTickers(optionsObject);
                var toDate = optionsObject.ContainsKey("to-date")
                    ? Parse.DateTimeExact(optionsObject["to-date"].ToString(), "yyyyMMdd-HH:mm:ss")
                    : DateTime.UtcNow;
                switch (targetApp)
                {
                    case "ddl":
                    case "dukascopydownloader":
                        DukascopyDownloaderProgram.DukascopyDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    default:
                        PrintMessageAndExit(1, "ERROR: Unrecognized --app value");
                        break;
                }
            }
            else
            {
                switch (targetApp)
                {
                    case "asfc":
                    case "algoseekfuturesconverter":
                        AlgoSeekFuturesProgram.AlgoSeekFuturesConverter(GetParameterOrExit(optionsObject, "date"));
                        break;
                    case "ivec":
                    case "ivolatilityequityconverter":
                        IVolatilityEquityConverterProgram.IVolatilityEquityConverter(GetParameterOrExit(optionsObject, "source-dir"),
                                                                                     GetParameterOrExit(optionsObject, "source-meta-dir"),
                                                                                     GetParameterOrExit(optionsObject, "destination-dir"),
                                                                                     GetParameterOrExit(optionsObject, "resolution"));
                        break;
                    case "kdc":
                    case "kaikodataconverter":
                        KaikoDataConverterProgram.KaikoDataConverter(GetParameterOrExit(optionsObject, "source-dir"),
                                                                     GetParameterOrExit(optionsObject, "date"),
                                                                     GetParameterOrDefault(optionsObject, "exchange", string.Empty));
                        break;
                    case "cug":
                    case "coarseuniversegenerator":
                        CoarseUniverseGeneratorProgram.CoarseUniverseGenerator();
                        break;
                    case "rdg":
                    case "randomdatagenerator":
                        var tickers = ToolboxArgumentParser.GetTickers(optionsObject);
                        RandomDataGeneratorProgram.RandomDataGenerator(
                            GetParameterOrExit(optionsObject, "start"),
                            GetParameterOrExit(optionsObject, "end"),
                            GetParameterOrDefault(optionsObject, "symbol-count", null),
                            GetParameterOrDefault(optionsObject, "market", null),
                            GetParameterOrDefault(optionsObject, "security-type", "Equity"),
                            GetParameterOrDefault(optionsObject, "resolution", "Minute"),
                            GetParameterOrDefault(optionsObject, "data-density", "Dense"),
                            GetParameterOrDefault(optionsObject, "include-coarse", "true"),
                            GetParameterOrDefault(optionsObject, "quote-trade-ratio", "1"),
                            GetParameterOrDefault(optionsObject, "random-seed", null),
                            GetParameterOrDefault(optionsObject, "ipo-percentage", "5.0"),
                            GetParameterOrDefault(optionsObject, "rename-percentage", "30.0"),
                            GetParameterOrDefault(optionsObject, "splits-percentage", "15.0"),
                            GetParameterOrDefault(optionsObject, "dividends-percentage", "60.0"),
                            GetParameterOrDefault(optionsObject, "dividend-every-quarter-percentage", "30.0"),
                            GetParameterOrDefault(optionsObject, "option-price-engine", "BaroneAdesiWhaleyApproximationEngine"),
                            GetParameterOrDefault(optionsObject, "volatility-model-resolution", "Daily"),
                            GetParameterOrDefault(optionsObject, "chain-symbol-count", "1"),
                            tickers
                        );
                        break;

                    default:
                        PrintMessageAndExit(1, "ERROR: Unrecognized --app value");
                        break;
                }
            }
        }
    }
}
