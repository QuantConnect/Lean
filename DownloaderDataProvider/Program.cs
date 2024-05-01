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

using NodaTime;
using QuantConnect.Util;
using QuantConnect.Data;
using QuantConnect.Logging;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using QuantConnect.Configuration;
using DataFeeds = QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.DownloaderDataProvider.Launcher.Models.Constants;

namespace QuantConnect.DownloaderDataProvider.Launcher;
public static class Program
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
    /// Provides access to exchange hours and raw data times zones in various markets
    /// </summary>
    private static readonly MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();

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

        RunDownload(dataDownloader, dataDownloadConfig, Globals.DataFolder, _dataCacheProvider);
    }

    /// <summary>
    /// Executes a data download operation using the specified data downloader.
    /// </summary>
    /// <param name="dataDownloader">An instance of an object implementing the <see cref="IDataDownloader"/> interface, responsible for downloading data.</param>
    /// <param name="dataDownloadConfig">Configuration settings for the data download operation.</param>
    /// <param name="dataDirectory">The directory where the downloaded data will be stored.</param>
    /// <param name="dataCacheProvider">The provider used to cache history data files</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataDownloader"/> is null.</exception>
    public static void RunDownload(IDataDownloader dataDownloader, DataDownloadConfig dataDownloadConfig, string dataDirectory, IDataCacheProvider dataCacheProvider)
    {
        if (dataDownloader == null)
        {
            throw new ArgumentNullException(nameof(dataDownloader), "The data downloader instance cannot be null. Please ensure that a valid instance of data downloader is provided.");
        }

        // Calculate the total number of seconds between the EndDate and StartDate
        var totalDataPerSymbolInSeconds = (dataDownloadConfig.EndDate - dataDownloadConfig.StartDate).TotalSeconds;
        var totalDataInSeconds = totalDataPerSymbolInSeconds * dataDownloadConfig.Symbols.Count;
        var completeSymbolCount = 0;
        var startDownloadUtcTime = DateTime.UtcNow;

        foreach (var symbol in dataDownloadConfig.Symbols)
        {
            var downloadParameters = new DataDownloaderGetParameters(symbol, dataDownloadConfig.Resolution, dataDownloadConfig.StartDate, dataDownloadConfig.EndDate, dataDownloadConfig.TickType);

            Log.Trace($"DownloaderDataProvider.Main(): Starting download {downloadParameters}");
            var downloadedData = dataDownloader.Get(downloadParameters);

            if (downloadedData == null)
            {
                completeSymbolCount++;
                Log.Trace($"DownloaderDataProvider.Main(): No data available for the following parameters: {downloadParameters}");
                continue;
            }

            var (dataTimeZone, exchangeTimeZone) = GetDataAndExchangeTimeZoneBySymbol(symbol);

            var writer = new LeanDataWriter(dataDownloadConfig.Resolution, symbol, dataDirectory, dataDownloadConfig.TickType, mapSymbol: true, dataCacheProvider: dataCacheProvider);

            var groupedData = DataFeeds.DownloaderDataProvider.FilterAndGroupDownloadDataBySymbol(
                downloadedData,
                symbol,
                LeanData.GetDataType(downloadParameters.Resolution, downloadParameters.TickType),
                exchangeTimeZone,
                dataTimeZone,
                downloadParameters.StartUtc,
                downloadParameters.EndUtc);

            var lastLogStatusTime = DateTime.UtcNow;

            foreach (var data in groupedData)
            {
                writer.Write(data.Select(data =>
                {
                    var utcNow = DateTime.UtcNow;
                    if (utcNow - lastLogStatusTime >= _logDisplayInterval)
                    {
                        lastLogStatusTime = utcNow;
                        var progressSoFar = (data.EndTime - dataDownloadConfig.StartDate).TotalSeconds + totalDataPerSymbolInSeconds * completeSymbolCount;
                        var eta = CalculateETA(utcNow, startDownloadUtcTime, totalDataInSeconds, progressSoFar);
                        var progress = CalculateProgress(data.EndTime, dataDownloadConfig.StartDate, dataDownloadConfig.EndDate);
                        Log.Trace($"DownloaderDataProvider.RunDownload(): Downloading {downloadParameters.Symbol} data: {progress:F2}%. ETA: {eta}");
                    }

                    return data;
                }));
            }

            completeSymbolCount++;

            Log.Trace($"DownloaderDataProvider.RunDownload(): Download completed for {downloadParameters.Symbol} at {downloadParameters.Resolution} resolution, " +
                $"covering the period from {dataDownloadConfig.StartDate} to {dataDownloadConfig.EndDate}.");
        }
        Log.Trace($"All downloads completed in {(DateTime.UtcNow - startDownloadUtcTime).TotalSeconds:F2} seconds.");
    }

    /// <summary>
    /// Retrieves the data time zone and exchange time zone associated with the specified symbol.
    /// </summary>
    /// <param name="symbol">The symbol for which to retrieve time zones.</param>
    /// <returns>
    /// A tuple containing the data time zone and exchange time zone.
    /// The data time zone represents the time zone for data related to the symbol.
    /// The exchange time zone represents the time zone for trading activities related to the symbol.
    /// </returns>
    private static (DateTimeZone dataTimeZone, DateTimeZone exchangeTimeZone) GetDataAndExchangeTimeZoneBySymbol(Symbol symbol)
    {
        var entry = _marketHoursDatabase.GetEntry(symbol.ID.Market, symbol, symbol.SecurityType);
        return (entry.DataTimeZone, entry.ExchangeHours.TimeZone);
    }

    /// <summary>
    /// Calculates the Estimated Time of Arrival (ETA) based on the current progress of a download.
    /// </summary>
    /// <param name="utcNow">The current UTC DateTime.</param>
    /// <param name="startUtcTime">The DateTime when the download started.</param>
    /// <param name="totalDataInSeconds"></param>
    /// <param name="currentProgressSecond"></param>
    /// <returns>A TimeSpan representing the Estimated Time of Arrival (ETA).</returns>
    /// <remarks>
    /// The method calculates the time elapsed since the start of downloading and estimates
    /// the remaining time based on the current progress. It uses the difference between
    /// the end time and the end date to calculate missing data time, and the difference
    /// between the end time and the start date to calculate the progress so far.
    /// The ETA is then calculated based on these values.
    /// </remarks>
    public static TimeSpan CalculateETA(DateTime utcNow, DateTime startUtcTime, double totalDataInSeconds, double currentProgressSecond)
    {
        // Calculate how much time has passed since the start of downloading
        TimeSpan howMuchItTookSoFar = utcNow - startUtcTime;

        var missingDataSecond = totalDataInSeconds - currentProgressSecond;

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
        if (totalDays == 0)
        {
            return 0;
        }
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
