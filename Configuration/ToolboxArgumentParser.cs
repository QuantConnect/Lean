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

using System;
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
                                                   + "Each require a different set of parameters. Example: --app=YahooDownloader --tickers="
                                                   + "SPY,AAPL --resolution=Daily --from-date=yyyyMMdd-HH:mm:ss --to-date=yyyyMMdd-HH:mm:ss";
        private static readonly List<CommandLineOption> Options = new List<CommandLineOption>
            {
                new CommandLineOption("app", CommandOptionType.SingleValue,
                                                     "[REQUIRED] Target tool, CASE INSENSITIVE: GDAXDownloader or GDAXDL/CryptoiqDownloader or CDL"
                                                     + "/DukascopyDownloader or DDL/IEXDownloader or IEXDL"
                                                     + "/FxcmDownloader or FDL/FxcmVolumeDownload or FVDL/GoogleDownloader or GDL/IBDownloader or IBDL"
                                                     + "/KrakenDownloader or KDL/OandaDownloader or ODL/QuandlBitfinexDownloader or QBDL or SECDataDownloader or SECDL"
                                                     + "/SECDataConverter or SECCV/YahooDownloader or YDL/AlgoSeekFuturesConverter or ASFC/AlgoSeekOptionsConverter or ASOC"
                                                     + "/SmartInsiderConverter or SICV"
                                                     + "/IVolatilityEquityConverter or IVEC/KaikoDataConverter or KDC/NseMarketDataConverter or NMDC"
                                                     + "/QuantQuoteConverter or QQC/CoarseUniverseGenerator or CUG/PSDL or PsychSignalDownloader\n"
                                                     + "PsychSignalConverter or PSDC/RandomDataGenerator or RDG/USTYCDL or USTreasuryYieldCurveDownloader"
                                                     + "/USTYCCV or USTreasuryYieldCurveConverter/TIINC or TiingoNewsConverter/BZCV or BenzingaNewsDataConverter\n"
                                                     + "Example 1: --app=DDL\n"
                                                     + "Example 2: --app=NseMarketDataConverter\n"
                                                     + "Example 3: --app=RDG"),
                new CommandLineOption("tickers", CommandOptionType.MultipleValue, "[REQUIRED ALL downloaders (except QBDL, SECDL)] "
                                                                                  + "--tickers=SPY,AAPL,etc"),
                new CommandLineOption("resolution", CommandOptionType.SingleValue, "[REQUIRED ALL downloaders (except QBDL, CDL, SECDL) and IVolatilityEquityConverter,"
                                                                                   + " QuantQuoteConverter] *Not all downloaders support all resolutions. Send empty for more information.*"
                                                                                   + " CASE SENSITIVE: --resolution=Tick/Second/Minute/Hour/Daily/All" +Environment.NewLine+
                                                                                   "[OPTIONAL for RandomDataGenerator - same format as downloaders, Options only support Minute"),
                new CommandLineOption("from-date", CommandOptionType.SingleValue, "[REQUIRED ALL downloaders] --from-date=yyyyMMdd-HH:mm:ss"),
                new CommandLineOption("to-date", CommandOptionType.SingleValue, "[OPTIONAL for downloaders] If not provided 'DateTime.UtcNow' will "
                                                                                + "be used. --to-date=yyyyMMdd-HH:mm:ss"),
                new CommandLineOption("exchange", CommandOptionType.SingleValue, "[REQUIRED for CryptoiqDownloader] [Optional for KaikoDataConverter] The exchange to process, if not defined, all exchanges will be processed."),
                new CommandLineOption("api-key", CommandOptionType.SingleValue, "[REQUIRED for QuandlBitfinexDownloader, IEXDownloader, PsychSignalDataDownloader, BenzingaNewsDataDownloader]"),
                new CommandLineOption("date", CommandOptionType.SingleValue, "[REQUIRED for AlgoSeekFuturesConverter, AlgoSeekOptionsConverter, KaikoDataConverter, SECDataConverter, PsychSignalDataConverter, SmartInsiderConverter, BenzingaNewsDataConverter]"
                                                                             + "Date for the option bz files: --date=yyyyMMdd"),
                new CommandLineOption("source-dir", CommandOptionType.SingleValue, "[REQUIRED for IVolatilityEquityConverter, KaikoDataConverter,"
                                                                                   + " CoinApiDataConverter, NseMarketDataConverter, QuantQuoteConverter, SECDataConverter, PsychSignalDataConverter, USTreasuryYieldCurveConverter, SmartInsiderConverter, TiingoNewsConverter, BenzingaNewsDataConverter]"),
                new CommandLineOption("destination-dir", CommandOptionType.SingleValue, "[REQUIRED for IVolatilityEquityConverter, "
                                                                                        + "NseMarketDataConverter, QuantQuoteConverter, SECDataDownloader, SECDataConverter, PsychSignalDataDownloader, PsychSignalDataConverter, USTreasuryYieldCurveDownloader, USTreasuryYieldCurveConverter, SmartInsiderConverter, TiingoNewsConverter, BenzingaNewsDataDownloader, BenzingaNewsDataConverter]"),
                new CommandLineOption("source-meta-dir", CommandOptionType.SingleValue, "[REQUIRED for IVolatilityEquityConverter, BenzingaNewsDataConverter. OPTIONAL for SmartInsiderConverter]"),
                new CommandLineOption("start", CommandOptionType.SingleValue, "[REQUIRED for RandomDataGenerator. Format yyyyMMdd Example: --start=20010101]"),
                new CommandLineOption("end", CommandOptionType.SingleValue, "[REQUIRED for RandomDataGenerator. Format yyyyMMdd Example: --end=20020101]"),
                new CommandLineOption("market", CommandOptionType.SingleValue, "[OPTIONAL for RandomDataGenerator. Market of generated symbols. Defaults to default market for security type: Example: --market=usa]"),
                new CommandLineOption("symbol-count", CommandOptionType.SingleValue, "[REQUIRED for RandomDataGenerator. Number of symbols to generate data for: Example: --symbol-count=10]"),
                new CommandLineOption("security-type", CommandOptionType.SingleValue, "[OPTIONAL for RandomDataGenerator. Security type of generated symbols, defaults to Equity: Example: --security-type=Equity/Option/Forex/Future/Cfd/Crypto]"),
                new CommandLineOption("data-density", CommandOptionType.SingleValue, "[OPTIONAL for RandomDataGenerator. Defaults to Dense. Valid values: --data-density=Dense/Sparse/VerySparse ]"),
                new CommandLineOption("include-coarse", CommandOptionType.SingleValue, "[OPTIONAL for RandomDataGenerator. Only used for Equity, defaults to true: Example: --include-coarse=true]"),
                new CommandLineOption("quote-trade-ratio", CommandOptionType.SingleValue, "[OPTIONAL for RandomDataGenerator. Sets the ratio of generated quotes to generated trades. Values larger than 1 mean more quotes than trades. Only used for Option, Future and Crypto, defaults to 1: Example: --quote-trade-ratio=1.75 ]"),
                new CommandLineOption("random-seed", CommandOptionType.SingleValue, "[OPTIONAL for RandomDataGenerator. Sets the random number generator seed. Defaults to null (random seed). Example: --random-seed=11399 ]"),
                new CommandLineOption("ipo-percentage", CommandOptionType.SingleValue, "[OPTIONAL for RandomDataGenerator. Sets the probability each equity generated will have an IPO event. Note that this is not the total probability for all symbols generated. Only used for Equity. Defaults to 5.0: Example: --ipo-percentage=43.25 ]"),
                new CommandLineOption("rename-percentage", CommandOptionType.SingleValue, "[OPTIONAL for RandomDataGenerator. Sets the probability each equity generated will have a rename event. Note that this is not the total probability for all symbols generated. Only used for Equity. Defaults to 30.0: Example: --rename-percentage=20.0 ]"),
                new CommandLineOption("splits-percentage", CommandOptionType.SingleValue, "[OPTIONAL for RandomDataGenerator. Sets the probability each equity generated will have a stock split event. Note that this is not the total probability for all symbols generated. Only used for Equity. Defaults to 15.0: Example: --splits-percentage=10.0 ]"),
                new CommandLineOption("dividends-percentage", CommandOptionType.SingleValue, "[OPTIONAL for RandomDataGenerator. Sets the probability each equity generated will have dividends. Note that this is not the probability for all symbols genearted. Only used for Equity. Defaults to 60.0: Example: --dividends-percentage=25.5 ]"),
                new CommandLineOption("dividend-every-quarter-percentage", CommandOptionType.SingleValue, "[OPTIONAL for RandomDataGenerator. Sets the probability each equity generated will have a dividend event every quarter. Note that this is not the total probability for all symbols generated. Only used for Equity. Defaults to 30.0: Example: --dividend-every-quarter-percentage=15.0 ]"),
                new CommandLineOption("data-source", CommandOptionType.SingleValue, "[OPTIONAL for PsychSignalDataDownloader. This is the kind of data you want to get from PsychSignal's API. Defaults to 'twitter_enhanced_withretweets,stocktwits']"),
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
