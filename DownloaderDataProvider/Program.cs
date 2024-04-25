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
    /// Represents the time interval of 5 seconds.
    /// </summary>
    private static TimeSpan _logDisplayInterval = TimeSpan.FromSeconds(5);

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

        // Calculate the total number of seconds between the EndDate and StartDate
        var totalDataSecond = (dataDownloadConfig.EndDate - dataDownloadConfig.StartDate).TotalSeconds;
        // Determine the multiplier based on the number of symbols minus one
        int dateMultiplier = dataDownloadConfig.Symbols.Count - 1;
        // Calculate the new endDate by adding the calculated seconds multiplied by the dateMultiplier to the original EndDate
        var endDate = dataDownloadConfig.EndDate.AddSeconds(totalDataSecond * dateMultiplier);
        foreach (var symbol in dataDownloadConfig.Symbols)
        {
            var downloadParameters = new DataDownloaderGetParameters(symbol, dataDownloadConfig.Resolution, dataDownloadConfig.StartDate, dataDownloadConfig.EndDate, dataDownloadConfig.TickType);

            if (writer == null)
            {
                writer = new LeanDataWriter(dataDownloadConfig.Resolution, symbol, Globals.DataFolder, dataDownloadConfig.TickType, mapSymbol: true, dataCacheProvider: _dataCacheProvider);
            }

            var startDownloadTime = DateTime.UtcNow;
            Log.Trace($"Starting download {downloadParameters}");
            var downloadedData = dataDownloader.Get(downloadParameters);

            if (downloadedData == null)
            {
                Log.Trace($"No data available for the following parameters: {downloadParameters}");
                continue;
            }

            var lastLogStatusTime = DateTime.UtcNow;
            writer.Write(downloadedData.Select(data =>
            {
                var utcNow = DateTime.UtcNow;
                if (utcNow - lastLogStatusTime >= _logDisplayInterval)
                {
                    lastLogStatusTime = utcNow;
                    var eta = CalculateETA(utcNow, startDownloadTime, endDate, data.EndTime, dataDownloadConfig.StartDate);
                    double progress = CalculateProgress(data.EndTime, dataDownloadConfig.StartDate, dataDownloadConfig.EndDate);
                    Log.Trace($"Downloading {downloadParameters.Symbol} data: {progress:F2}%. ETA: {eta}");
                }

                return data;
            }));
            // Correct endDate by removing the totalDataSecond to adjust for overlap periods.
            endDate = endDate.AddSeconds(-totalDataSecond);
            Log.Trace($"Download completed for {downloadParameters.Symbol} at {downloadParameters.Resolution} resolution, covering the period from {dataDownloadConfig.StartDate} to {dataDownloadConfig.EndDate}.");
        }
    }

    /// <summary>
    /// Calculates the Estimated Time of Arrival (ETA) based on the current progress of a download.
    /// </summary>
    /// <param name="utcNow">The current UTC DateTime.</param>
    /// <param name="startDownloadTime">The DateTime when the download started.</param>
    /// <param name="completelyLastEndDate">The actual end date of the download considering all downloading symbols.</param>
    /// <param name="currentDownloadedEndDate">The current end date time in downloading process.</param>
    /// <param name="configStartDateTime">The start date of the download.</param>
    /// <returns>A TimeSpan representing the Estimated Time of Arrival (ETA).</returns>
    /// <remarks>
    /// The method calculates the time elapsed since the start of downloading and estimates
    /// the remaining time based on the current progress. It uses the difference between
    /// the end time and the end date to calculate missing data time, and the difference
    /// between the end time and the start date to calculate the progress so far.
    /// The ETA is then calculated based on these values.
    /// </remarks>
    public static TimeSpan CalculateETA(DateTime utcNow, DateTime startDownloadTime, DateTime completelyLastEndDate, DateTime currentDownloadedEndDate, DateTime configStartDateTime)
    {
        // Calculate how much time has passed since the start of downloading
        TimeSpan howMuchItTookSoFar = utcNow - startDownloadTime;

        // Calculate ETA in seconds
        var missingDataSecond = (completelyLastEndDate - currentDownloadedEndDate).TotalSeconds; // multiple to symbol.count 

        var currentProgressSecond = (currentDownloadedEndDate - configStartDateTime).TotalSeconds; // howMuch it process so far 

        // Calculate ETA in seconds
        var etaSeconds = (missingDataSecond / currentProgressSecond) * howMuchItTookSoFar.TotalSeconds;

        // Convert ETA from seconds to TimeSpan
        TimeSpan etaTimeSpan = TimeSpan.FromSeconds(etaSeconds);

        return etaTimeSpan;
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
