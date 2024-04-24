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
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using QuantConnect.Lean.DownloaderDataProvider.Models.Constants;

namespace QuantConnect.Lean.DownloaderDataProvider;
class Program
{
    /// <summary>
    /// Synchronizer in charge of guaranteeing a single operation per file path
    /// </summary>
    private readonly static KeyStringSynchronizer DiskSynchronizer = new();

    /// <summary>
    /// The provider used to cache history data files
    /// </summary>
    private static readonly IDataCacheProvider _dataCacheProvider = new DiskDataCacheProvider(DiskSynchronizer);

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static void Main(string[] args)
    {
        // Parse report arguments and merge with config to use in the optimizer
        if (args.Length > 0)
        {
            Config.MergeCommandLineArgumentsWithConfiguration(DownloaderDataProviderArgumentParser.ParseArguments(args));
        }

        InitializeConfigurations();

        var dataDownloader = Composer.Instance.GetExportedValueByTypeName<IDataDownloader>(Config.Get(DownloaderCommandArguments.CommandDownloaderDataDownloader));

        var dataDownloadConfig = new DataDownloadConfig();

        var writer = default(LeanDataWriter);
        foreach (var symbol in dataDownloadConfig.Symbols)
        {
            var downloadParameters = new DataDownloaderGetParameters(symbol, dataDownloadConfig.Resolution, dataDownloadConfig.StartDate, dataDownloadConfig.EndDate, dataDownloadConfig.TickType);

            if (writer == null)
            {
                writer = new LeanDataWriter(dataDownloadConfig.Resolution, symbol, Globals.DataFolder, dataDownloadConfig.TickType, mapSymbol: true, dataCacheProvider: _dataCacheProvider);
            }

            Log.Trace($"Starting download {downloadParameters}");
            var downloadedData = dataDownloader.Get(downloadParameters);

            if (downloadedData == null)
            {
                Log.Trace($"No data available for the following parameters: {downloadParameters}");
                continue;
            }

            var downloadedFirstDate = default(DateTime);
            DateTime lastTenSecDisplayTime = DateTime.MinValue;
            // Save the data
            writer.Write(downloadedData.Select(data =>
            {
                // Some data sources may have restrictions and might not return the exact requested data.Time.
                // For instance, if we request data for 2010/01/02, the DataSource might return 2020/01/01 instead.
                if (downloadedFirstDate == default)
                {
                    downloadedFirstDate = data.Time;
                }

                if (DateTime.Now - lastTenSecDisplayTime >= TimeSpan.FromSeconds(5))
                {
                    lastTenSecDisplayTime = DateTime.Now;
                    double progress = CalculateProgress(data.EndTime, downloadedFirstDate, dataDownloadConfig.EndDate);
                    Log.Trace($"Downloading {downloadParameters.Symbol} data: {progress:F2}% / 100%");
                }

                return data;
            }));

            Log.Trace($"Download completed for {downloadParameters.Symbol} at {downloadParameters.Resolution} resolution, covering the period from {downloadedFirstDate} to {dataDownloadConfig.EndDate}.");
        }
    }

    /// <summary>
    /// Calculates the progress as a percentage based on the elapsed time between the start and end dates.
    /// </summary>
    /// <param name="currentTime">The current date and time.</param>
    /// <param name="start">The start date and time.</param>
    /// <param name="end">The end date and time.</param>
    /// <returns>A double representing the progress as a percentage.</returns>
    private static double CalculateProgress(DateTime currentTime, DateTime start, DateTime end)
    {
        double totalDays = (end - start).TotalDays;
        double elapsedDays = (currentTime - start).TotalDays;

        return (elapsedDays / totalDays) * 100;
    }

    /// <summary>
    /// Initializes various configurations for the application.
    /// This method sets up logging, data providers, map file providers, and factor file providers.
    /// </summary>
    /// <remarks>
    /// The method reads configuration values to determine whether debugging is enabled, 
    /// which log handler to use, and which data, map file, and factor file providers to initialize.
    /// </remarks>
    /// <seealso cref="Log"/>
    /// <seealso cref="Config"/>
    /// <seealso cref="Composer"/>
    /// <seealso cref="ILogHandler"/>
    /// <seealso cref="IDataProvider"/>
    /// <seealso cref="IMapFileProvider"/>
    /// <seealso cref="IFactorFileProvider"/>
    public static void InitializeConfigurations()
    {
        Log.DebuggingEnabled = Config.GetBool("debug-mode", false);
        Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));

        var dataProvider = Composer.Instance.GetExportedValueByTypeName<IDataProvider>("DefaultDataProvider");
        var mapFileProvider = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"));
        var factorFileProvider = Composer.Instance.GetExportedValueByTypeName<IFactorFileProvider>(Config.Get("factor-file-provider", "LocalDiskFactorFileProvider"));

        mapFileProvider.Initialize(dataProvider);
        factorFileProvider.Initialize(mapFileProvider, dataProvider);
    }
}
