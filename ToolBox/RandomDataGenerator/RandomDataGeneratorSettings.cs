using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using QuantConnect.Brokerages;

namespace QuantConnect.ToolBox.RandomDataGenerator
{
    public class RandomDataGeneratorSettings
    {
        private static int MarketCode = 100;
        private static readonly string[] DateFormats = {DateFormat.EightCharacter, DateFormat.YearMonth, "yyyy-MM-dd"};

        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public SecurityType SecurityType { get; set; } = SecurityType.Equity;
        public DataDensity DataDensity { get; set; } = DataDensity.Dense;
        public Resolution Resolution { get; set; } = Resolution.Minute;
        public string Market { get; set; }
        public bool IncludeCoarse { get; set; } = true;
        public int SymbolCount { get; set; }
        public double QuoteTradeRatio { get; set; } = 1;
        public int RandomSeed { get; set; }
        public bool RandomSeedSet { get; set; }
        public double HasIpoPercentage { get; set; }
        public double HasRenamePercentage { get; set; }
        public double HasSplitsPercentage { get; set; }
        public double HasDividendsPercentage { get; set; }
        public double DividendEveryQuarterPercentage { get; set; }

        public TickType[] TickTypes { get; set; }

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
            ConsoleLeveledOutput output
            )
        {
            bool randomSeedSet = true;

            int randomSeed;
            int symbolCount;
            bool includeCoarse;
            TickType[] tickTypes;
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

            // --start
            if (!DateTime.TryParseExact(startDateString, DateFormats, null, DateTimeStyles.None, out startDate))
            {
                output.Error.WriteLine($"Required parameter --from-date was incorrectly formatted. Please specify in yyyyMMdd format. Value provided: '{startDateString}'");
            }

            // --end
            if (!DateTime.TryParseExact(endDateString, DateFormats, null, DateTimeStyles.None, out endDate))
            {
                output.Error.WriteLine($"Required parameter --to-date was incorrectly formatted. Please specify in yyyyMMdd format. Value provided: '{endDateString}'");
            }

            // --symbol-count
            if (!int.TryParse(symbolCountString, out symbolCount) || symbolCount <= 0)
            {
                output.Error.WriteLine($"Required parameter --symbol-count was incorrectly formatted. Please specify a valid integer greater than zero. Value provided: '{symbolCountString}'");
            }

            // --resolution
            if (string.IsNullOrEmpty(resolutionString))
            {
                resolution = Resolution.Minute;
                output.Info.WriteLine($"Using default value of '{resolution}' for --resolution");
            }
            else if (!Enum.TryParse(resolutionString, true, out resolution))
            {
                var validValues = string.Join(", ", Enum.GetValues(typeof(Resolution)).Cast<Resolution>());
                output.Error.WriteLine($"Optional parameter --resolution was incorrectly formatted. Default is Minute. Please specify a valid Resolution. Value provided: '{resolutionString}' Valid values: {validValues}");
            }

            // --security-type
            if (string.IsNullOrEmpty(securityTypeString))
            {
                securityType = SecurityType.Equity;
                output.Info.WriteLine($"Using default value of '{securityType}' for --security-type");
            }
            else if (!Enum.TryParse(securityTypeString, true, out securityType))
            {
                var validValues = string.Join(", ", Enum.GetValues(typeof(SecurityType)).Cast<SecurityType>());
                output.Error.WriteLine($"Optional parameter --security-type is invalid. Default is Equity. Please specify a valid SecurityType. Value provided: '{securityTypeString}' Valid values: {validValues}");
            }

            if (securityType == SecurityType.Option && resolution != Resolution.Minute)
            {
                output.Error.WriteLine($"When using --security-type=Option you must specify --resolution=Minute");
            }

            // --market
            if (string.IsNullOrEmpty(market))
            {
                market = DefaultBrokerageModel.DefaultMarketMap[securityType];
                output.Info.WriteLine($"Using default value of '{market}' for --market and --security-type={securityType}");
            }
            else if (QuantConnect.Market.Encode(market) == null)
            {
                // be sure to add a reference to the unknown market, otherwise we won't be able to decode it coming out
                QuantConnect.Market.Add(market, Interlocked.Increment(ref MarketCode));
                output.Warn.WriteLine($"Please verify that the specified market value is correct: '{market}'   This value is not known has been added to the market value map. If this is an error, stop the application immediately using Ctrl+C");
            }

            // --include-coarse
            if (string.IsNullOrEmpty(includeCoarseString))
            {
                includeCoarse = securityType == SecurityType.Equity;
                if (securityType != SecurityType.Equity)
                {
                    output.Info.WriteLine($"Using default value of '{includeCoarse}' for --security-type={securityType}");
                }
            }
            else if (!bool.TryParse(includeCoarseString, out includeCoarse))
            {
                output.Error.WriteLine($"Optional parameter --include-coarse was incorrectly formated. Please specify a valid boolean. Value provided: '{includeCoarseString}'. Valid values: 'true' or 'false'");
            }
            else if (includeCoarse && securityType != SecurityType.Equity)
            {
                output.Warn.WriteLine("Optional parameter --include-coarse will be ignored because it only applies to --security-type=Equity");
            }

            // --data-density
            if (string.IsNullOrEmpty(dataDensityString))
            {
                dataDensity = DataDensity.Dense;
                output.Info.WriteLine($"Using default value of '{dataDensity}' for --data-density");
            }
            else if (!Enum.TryParse(dataDensityString, true, out dataDensity))
            {
                var validValues = string.Join(", ", Enum.GetValues(typeof(DataDensity))).Cast<DataDensity>();
                output.Error.WriteLine($"Optional parameter --data-density was incorrectly formated. Please specify a valid DataDensity. Value provided: '{dataDensityString}'. Valid values: {validValues}");
            }

            // --quote-trade-ratio
            if (string.IsNullOrEmpty(quoteTradeRatioString))
            {
                quoteTradeRatio = 1;
            }
            else if (!double.TryParse(quoteTradeRatioString, out quoteTradeRatio))
            {
                output.Error.WriteLine($"Optional parameter --quote-trade-ratio was incorrectly formatted. Please specify a valid double greater than or equal to zero. Value provided: '{quoteTradeRatioString}'");
            }

            // --random-seed
            if (string.IsNullOrEmpty(randomSeedString))
            {
                randomSeed = 0;
                randomSeedSet = false;
            }
            else if (!int.TryParse(randomSeedString, out randomSeed))
            {
                output.Error.WriteLine($"Optional parameter --random-seed was incorrectly formatted. Please specify a valid integer");
            }

            // --ipo-percentage
            if (string.IsNullOrEmpty(hasIpoPercentageString))
            {
                hasIpoPercentage = 5.0;
            }
            else if (!double.TryParse(hasIpoPercentageString, out hasIpoPercentage))
            {
                output.Error.WriteLine($"Optional parameter --ipo-percentage was incorrectly formatted. Please specify a valid double greater than or equal to zero. Value provided: '{hasIpoPercentageString}'");
            }
            
            // --rename-percentage
            if (string.IsNullOrEmpty(hasRenamePercentageString))
            {
                hasRenamePercentage = 30.0;
            }
            else if (!double.TryParse(hasRenamePercentageString, out hasRenamePercentage))
            {
                output.Error.WriteLine($"Optional parameter --rename-percentage was incorrectly formatted. Please specify a valid double greater than or equal to zero. Value provided: '{hasRenamePercentageString}'");
            }

            // --splits-percentage
            if (string.IsNullOrEmpty(hasSplitsPercentageString))
            {
                hasSplitsPercentage = 15.0;
            }
            else if (!double.TryParse(hasSplitsPercentageString, out hasSplitsPercentage))
            {
                output.Error.WriteLine($"Optional parameter --splits-percentage was incorrectly formatted. Please specify a valid double greater than or equal to zero. Value provided: '{hasSplitsPercentageString}'");
            }

            // --dividends-percentage
            if (string.IsNullOrEmpty(hasDividendsPercentageString))
            {
                hasDividendsPercentage = 60.0;
            }
            else if (!double.TryParse(hasDividendsPercentageString, out hasDividendsPercentage))
            {
                output.Error.WriteLine($"Optional parameter --dividends-percentage was incorrectly formatted. Please specify a valid double greater than or equal to zero. Value provided: '{hasDividendsPercentageString}'");
            }

            // --dividend-every-quarter-percentage
            if (string.IsNullOrEmpty(dividendEveryQuarterPercentageString))
            {
                dividendEveryQuarterPercentage = 30.0;
            }
            else if (!double.TryParse(dividendEveryQuarterPercentageString, out dividendEveryQuarterPercentage))
            {
                output.Error.WriteLine($"Optional parameter --dividend-ever-quarter-percentage was incorrectly formatted. Please specify a valid double greater than or equal to zero. Value provided: '{dividendEveryQuarterPercentageString}'");
            }

            if (output.ErrorMessageWritten)
            {
                output.Error.WriteLine("Please address the errors and run the application again.");
                Environment.Exit(-1);
            }

            switch (securityType)
            {
                case SecurityType.Base:
                case SecurityType.Equity:
                    tickTypes = new[] { TickType.Trade };
                    break;

                case SecurityType.Forex:
                case SecurityType.Cfd:
                    tickTypes = new[] { TickType.Quote };
                    break;

                case SecurityType.Option:
                case SecurityType.Future:
                    tickTypes = new[] { TickType.Trade, TickType.Quote, TickType.OpenInterest };
                    break;

                case SecurityType.Crypto:
                    tickTypes = new[] { TickType.Trade, TickType.Quote };
                    break;

                case SecurityType.Commodity:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            output.Info.WriteLine($"Selected tick types for {securityType}: {string.Join(", ", tickTypes)}");

            return new RandomDataGeneratorSettings
            {
                End = endDate,
                Start = startDate,

                Market = market,
                SymbolCount = symbolCount,
                SecurityType = securityType,
                QuoteTradeRatio = quoteTradeRatio,

                TickTypes = tickTypes,
                Resolution = resolution,

                DataDensity = dataDensity,
                IncludeCoarse = includeCoarse,
                RandomSeed = randomSeed,
                RandomSeedSet = randomSeedSet,

                HasIpoPercentage = hasIpoPercentage,
                HasRenamePercentage = hasRenamePercentage,
                HasSplitsPercentage = hasSplitsPercentage,
                HasDividendsPercentage = hasDividendsPercentage,
                DividendEveryQuarterPercentage = dividendEveryQuarterPercentage
            };
        }

        public IEnumerable<TickAggregator> CreateAggregators()
        {
            // create default aggregators for tick type/resolution
            foreach (var tickAggregator in TickAggregator.ForTickTypes(Resolution, TickTypes))
            {
                yield return tickAggregator;
            }


            // ensure we have a daily consolidator when coarse is enabled
            if (IncludeCoarse && Resolution != Resolution.Daily)
            {
                // prefer trades for coarse - in practice equity only does trades, but leaving this as configurable
                if (TickTypes.Contains(TickType.Trade))
                {
                    yield return TickAggregator.ForTickTypes(Resolution.Daily, TickType.Trade).Single();
                }
                else
                {
                    yield return TickAggregator.ForTickTypes(Resolution.Daily, TickType.Quote).Single();
                }
            }
        }
    }
}