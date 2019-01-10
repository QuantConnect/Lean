using System.Collections.Generic;
using Microsoft.Extensions.CommandLineUtils;

namespace QuantConnect.Configuration
{
    /// <summary>
    /// Command Line arguments parser for Toolbox configuration
    /// </summary>
    public static class ToolboxArgumentParser
    {
        private const string ApplicationName = "QuantConnect.ToolBox.exe";
        private const string ApplicationDescription = "Lean Engine ToolBox";
        private const string ApplicationHelpText = "\nThe ToolBox is a wrapper of >15 tools. "
                                                   + "Each require a different set of parameters. Example: --app=GoogleDownloader --tickers="
                                                   + "SPY,AAPL --resolution=Minute --from-date=yyyyMMdd-HH:mm:ss --to-date=yyyyMMdd-HH:mm:ss";
        private static readonly List<CommandLineOption> Options = new List<CommandLineOption>
            {
                new CommandLineOption("app", CommandOptionType.SingleValue,
                                                     "[REQUIRED] Target tool, CASE INSENSITIVE: GDAXDownloader or GDAXDL/CryptoiqDownloader or CDL"
                                                     + "/DukascopyDownloader or DDL/IEXDownloader or IEXDL"
                                                     + "/FxcmDownloader or FDL/FxcmVolumeDownload or FVDL/GoogleDownloader or GDL/IBDownloader or IBDL"
                                                     + "/KrakenDownloader or KDL/OandaDownloader or ODL/QuandlBitfinexDownloader or QBDL"
                                                     + "/YahooDownloader or YDL/AlgoSeekFuturesConverter or ASFC/AlgoSeekOptionsConverter or ASOC"
                                                     + "/IVolatilityEquityConverter or IVEC/KaikoDataConverter or KDC/NseMarketDataConverter or NMDC"
                                                     + "/QuantQuoteConverter or QQC/CoarseUniverseGenerator or CUG\n"
                                                     + "Example 1: --app=DDL\n"
                                                     + "Example 2: --app=NseMarketDataConverter\n"),
                new CommandLineOption("tickers", CommandOptionType.MultipleValue, "[REQUIRED ALL downloaders (except QBDL)] "
                                                                                  + "--tickers=SPY,AAPL,etc"),
                new CommandLineOption("resolution", CommandOptionType.SingleValue, "[REQUIRED ALL downloaders (except QBDL, CDL) and IVolatilityEquityConverter,"
                                                                                   + " QuantQuoteConverter] *Not all downloaders support all resolutions. Send empty for more information.*"
                                                                                   + " CASE SENSITIVE: --resolution=Tick/Second/Minute/Hour/Daily/All"),
                new CommandLineOption("from-date", CommandOptionType.SingleValue, "[REQUIRED ALL downloaders] --from-date=yyyyMMdd-HH:mm:ss"),
                new CommandLineOption("to-date", CommandOptionType.SingleValue, "[OPTIONAL for downloaders] If not provided 'DateTime.UtcNow' will "
                                                                                + "be used. --to-date=yyyyMMdd-HH:mm:ss"),
                new CommandLineOption("exchange", CommandOptionType.SingleValue, "[REQUIRED for CryptoiqDownloader]"),
                new CommandLineOption("api-key", CommandOptionType.SingleValue, "[REQUIRED for QuandlBitfinexDownloader]"),
                new CommandLineOption("date", CommandOptionType.SingleValue, "[REQUIRED for AlgoSeekFuturesConverter, AlgoSeekOptionsConverter, KaikoDataConverter] "
                                                                             + "Date for the option bz files: --date=yyyyMMdd"),
                new CommandLineOption("source-dir", CommandOptionType.SingleValue, "[REQUIRED for IVolatilityEquityConverter, KaikoDataConverter,"
                                                                                   + " NseMarketDataConverter, QuantQuoteConverter]"),
                new CommandLineOption("destination-dir", CommandOptionType.SingleValue, "[REQUIRED for IVolatilityEquityConverter, "
                                                                                        + "NseMarketDataConverter, QuantQuoteConverter]"),
                new CommandLineOption("source-meta-dir", CommandOptionType.SingleValue, "[REQUIRED for IVolatilityEquityConverter]")
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
