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

using QuantConnect.Brokerages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class RandomDataGeneratorSettings
    {
        private static int MarketCode = 100;
        private static readonly string[] DateFormats = { DateFormat.EightCharacter, DateFormat.YearMonth, "yyyy-MM-dd" };

        public DateTime Start { get; init; }
        public DateTime End { get; init; }
        public SecurityType SecurityType { get; init; } = SecurityType.Equity;
        public DataDensity DataDensity { get; init; } = DataDensity.Dense;
        public Resolution Resolution { get; init; } = Resolution.Minute;
        public string Market { get; init; }
        public bool IncludeCoarse { get; init; } = true;
        public int SymbolCount { get; set; }
        public double QuoteTradeRatio { get; init; } = 1;
        public int RandomSeed { get; init; }
        public bool RandomSeedSet { get; init; }
        public double HasIpoPercentage { get; init; }
        public double HasRenamePercentage { get; init; }
        public double HasSplitsPercentage { get; init; }
        public double MonthSplitPercentage { get; init; }
        public double HasDividendsPercentage { get; init; }
        public double DividendEveryQuarterPercentage { get; init; }
        public string OptionPriceEngineName { get; init; }
        public int ChainSymbolCount { get; init; } = 1;
        public Resolution VolatilityModelResolution { get; init; } = Resolution.Daily;
        public List<string> Tickers { get; init; }
        public static RandomDataGeneratorSettings FromCommandLineArguments(
            string startDateString,
            string endDateString,
            string symbolCountString,
            string market,
            string securityTypeString,
            string resolutionString,
            string dataDensityString,
            string includeCoarseString,
            string quoteTradeRatioString,
            string randomSeedString,
            string hasIpoPercentageString,
            string hasRenamePercentageString,
            string hasSplitsPercentageString,
            string hasDividendsPercentageString,
            string dividendEveryQuarterPercentageString,
            string optionPriceEngineName,
            string volatilityModelResolutionString,
            string chainSymbolCountString,
            List<string> tickers,
            double monthSplitPercentage = 5.0
            )
        {
            var randomSeedSet = true;

            int randomSeed;
            int symbolCount;
            int chainSymbolCount;
            bool includeCoarse;
            Resolution resolution;
            double quoteTradeRatio;
            DataDensity dataDensity;
            SecurityType securityType;
            DateTime startDate, endDate;
            double hasIpoPercentage;
            double hasRenamePercentage;
            double hasSplitsPercentage;
            double hasDividendsPercentage;
            double dividendEveryQuarterPercentage;
            Resolution volatilityModelResolution;

            var failed = false;
            // --start
            if (!DateTime.TryParseExact(startDateString, DateFormats, null, DateTimeStyles.None, out startDate))
            {
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Required parameter --from-date was incorrectly formatted. Please specify in yyyyMMdd format. Value provided: '{startDateString}'");
            }

            // --end
            if (!DateTime.TryParseExact(endDateString, DateFormats, null, DateTimeStyles.None, out endDate))
            {
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Required parameter --to-date was incorrectly formatted. Please specify in yyyyMMdd format. Value provided: '{endDateString}'");
            }

            // --tickers
            if (!tickers.IsNullOrEmpty())
            {
                symbolCount = tickers.Count;
                Log.Trace("RandomDataGeneratorSettings(): Ignoring symbol count will use provided tickers");
            }
            // --symbol-count
            else if (!int.TryParse(symbolCountString, out symbolCount) || symbolCount <= 0)
            {
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Required parameter --symbol-count was incorrectly formatted. Please specify a valid integer greater than zero. Value provided: '{symbolCountString}'");
            }

            // --chain-symbol-count
            if (!int.TryParse(chainSymbolCountString, out chainSymbolCount) || chainSymbolCount <= 0)
            {
                chainSymbolCount = 10;
                Log.Trace($"RandomDataGeneratorSettings(): Using default value of '{chainSymbolCount}' for --chain-symbol-count");
            }

            // --resolution
            if (string.IsNullOrEmpty(resolutionString))
            {
                resolution = Resolution.Minute;
                Log.Trace($"RandomDataGeneratorSettings(): Using default value of '{resolution}' for --resolution");
            }
            else if (!Enum.TryParse(resolutionString, true, out resolution))
            {
                var validValues = string.Join(", ", Enum.GetValues(typeof(Resolution)).Cast<Resolution>());
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Optional parameter --resolution was incorrectly formatted. Default is Minute. Please specify a valid Resolution. Value provided: '{resolutionString}' Valid values: {validValues}");
            }

            // --standard deviation volatility period span
            if (string.IsNullOrEmpty(volatilityModelResolutionString))
            {
                volatilityModelResolution = Resolution.Daily;
                Log.Trace($"RandomDataGeneratorSettings():Using default value of '{resolution}' for --resolution");
            }
            else if (!Enum.TryParse(volatilityModelResolutionString, true, out volatilityModelResolution))
            {
                var validValues = string.Join(", ", Enum.GetValues(typeof(Resolution)).Cast<Resolution>());
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Optional parameter --volatility-model-resolution was incorrectly formatted. Default is Daily. Please specify a valid Resolution. Value provided: '{volatilityModelResolutionString}' Valid values: {validValues}");
            }

            // --security-type
            if (string.IsNullOrEmpty(securityTypeString))
            {
                securityType = SecurityType.Equity;
                Log.Trace($"RandomDataGeneratorSettings(): Using default value of '{securityType}' for --security-type");
            }
            else if (!Enum.TryParse(securityTypeString, true, out securityType))
            {
                var validValues = string.Join(", ", Enum.GetValues(typeof(SecurityType)).Cast<SecurityType>());
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Optional parameter --security-type is invalid. Default is Equity. Please specify a valid SecurityType. Value provided: '{securityTypeString}' Valid values: {validValues}");
            }

            if (securityType == SecurityType.Option && resolution != Resolution.Minute)
            {
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): When using --security-type=Option you must specify --resolution=Minute");
            }

            // --market
            if (string.IsNullOrEmpty(market))
            {
                market = DefaultBrokerageModel.DefaultMarketMap[securityType];
                Log.Trace($"RandomDataGeneratorSettings(): Using default value of '{market}' for --market and --security-type={securityType}");
            }
            else if (QuantConnect.Market.Encode(market) == null)
            {
                // be sure to add a reference to the unknown market, otherwise we won't be able to decode it coming out
                QuantConnect.Market.Add(market, Interlocked.Increment(ref MarketCode));
                Log.Trace($"RandomDataGeneratorSettings(): Please verify that the specified market value is correct: '{market}'   This value is not known has been added to the market value map. If this is an error, stop the application immediately using Ctrl+C");
            }

            // --include-coarse
            if (string.IsNullOrEmpty(includeCoarseString))
            {
                includeCoarse = securityType == SecurityType.Equity;
                if (securityType != SecurityType.Equity)
                {
                    Log.Trace($"RandomDataGeneratorSettings(): Using default value of '{includeCoarse}' for --security-type={securityType}");
                }
            }
            else if (!bool.TryParse(includeCoarseString, out includeCoarse))
            {
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Optional parameter --include-coarse was incorrectly formatted. Please specify a valid boolean. Value provided: '{includeCoarseString}'. Valid values: 'true' or 'false'");
            }
            else if (includeCoarse && securityType != SecurityType.Equity)
            {
                Log.Trace("RandomDataGeneratorSettings(): Optional parameter --include-coarse will be ignored because it only applies to --security-type=Equity");
            }

            // --data-density
            if (string.IsNullOrEmpty(dataDensityString))
            {
                dataDensity = DataDensity.Dense;
                if (securityType == SecurityType.Option)
                {
                    dataDensity = DataDensity.Sparse;
                }
                Log.Trace($"RandomDataGeneratorSettings(): Using default value of '{dataDensity}' for --data-density");
            }
            else if (!Enum.TryParse(dataDensityString, true, out dataDensity))
            {
                var validValues = string.Join(", ", Enum.GetValues(typeof(DataDensity))).Cast<DataDensity>();
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Optional parameter --data-density was incorrectly formatted. Please specify a valid DataDensity. Value provided: '{dataDensityString}'. Valid values: {validValues}");
            }

            // --quote-trade-ratio
            if (string.IsNullOrEmpty(quoteTradeRatioString))
            {
                quoteTradeRatio = 1;
            }
            else if (!double.TryParse(quoteTradeRatioString, out quoteTradeRatio))
            {
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Optional parameter --quote-trade-ratio was incorrectly formatted. Please specify a valid double greater than or equal to zero. Value provided: '{quoteTradeRatioString}'");
            }

            // --random-seed
            if (string.IsNullOrEmpty(randomSeedString))
            {
                randomSeed = 0;
                randomSeedSet = false;
            }
            else if (!int.TryParse(randomSeedString, out randomSeed))
            {
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Optional parameter --random-seed was incorrectly formatted. Please specify a valid integer");
            }

            // --ipo-percentage
            if (string.IsNullOrEmpty(hasIpoPercentageString))
            {
                hasIpoPercentage = 5.0;
            }
            else if (!double.TryParse(hasIpoPercentageString, out hasIpoPercentage))
            {
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Optional parameter --ipo-percentage was incorrectly formatted. Please specify a valid double greater than or equal to zero. Value provided: '{hasIpoPercentageString}'");
            }

            // --rename-percentage
            if (string.IsNullOrEmpty(hasRenamePercentageString))
            {
                hasRenamePercentage = 30.0;
            }
            else if (!double.TryParse(hasRenamePercentageString, out hasRenamePercentage))
            {
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Optional parameter --rename-percentage was incorrectly formatted. Please specify a valid double greater than or equal to zero. Value provided: '{hasRenamePercentageString}'");
            }

            // --splits-percentage
            if (string.IsNullOrEmpty(hasSplitsPercentageString))
            {
                hasSplitsPercentage = 15.0;
            }
            else if (!double.TryParse(hasSplitsPercentageString, out hasSplitsPercentage))
            {
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Optional parameter --splits-percentage was incorrectly formatted. Please specify a valid double greater than or equal to zero. Value provided: '{hasSplitsPercentageString}'");
            }

            // --dividends-percentage
            if (string.IsNullOrEmpty(hasDividendsPercentageString))
            {
                hasDividendsPercentage = 60.0;
            }
            else if (!double.TryParse(hasDividendsPercentageString, out hasDividendsPercentage))
            {
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Optional parameter --dividends-percentage was incorrectly formatted. Please specify a valid double greater than or equal to zero. Value provided: '{hasDividendsPercentageString}'");
            }

            // --dividend-every-quarter-percentage
            if (string.IsNullOrEmpty(dividendEveryQuarterPercentageString))
            {
                dividendEveryQuarterPercentage = 30.0;
            }
            else if (!double.TryParse(dividendEveryQuarterPercentageString, out dividendEveryQuarterPercentage))
            {
                failed = true;
                Log.Error($"RandomDataGeneratorSettings(): Optional parameter --dividend-ever-quarter-percentage was incorrectly formatted. Please specify a valid double greater than or equal to zero. Value provided: '{dividendEveryQuarterPercentageString}'");
            }

            if (failed)
            {
                Log.Error("RandomDataGeneratorSettings(): Please address the errors and run the application again.");
                Environment.Exit(-1);
            }

            return new RandomDataGeneratorSettings
            {
                End = endDate,
                Start = startDate,

                Market = market,
                SymbolCount = symbolCount,
                SecurityType = securityType,
                QuoteTradeRatio = quoteTradeRatio,
                ChainSymbolCount = chainSymbolCount,

                Resolution = resolution,

                DataDensity = dataDensity,
                IncludeCoarse = includeCoarse,
                RandomSeed = randomSeed,
                RandomSeedSet = randomSeedSet,

                HasIpoPercentage = hasIpoPercentage,
                HasRenamePercentage = hasRenamePercentage,
                HasSplitsPercentage = hasSplitsPercentage,
                MonthSplitPercentage = monthSplitPercentage,
                HasDividendsPercentage = hasDividendsPercentage,
                DividendEveryQuarterPercentage = dividendEveryQuarterPercentage,
                OptionPriceEngineName = optionPriceEngineName,
                VolatilityModelResolution = volatilityModelResolution,
                Tickers = tickers
            };
        }
    }
}
