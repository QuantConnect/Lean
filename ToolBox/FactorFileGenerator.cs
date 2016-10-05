using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;

namespace QuantConnect.ToolBox
{
    /// <summary>
    /// Generates a factor file from a list of splits and dividends for a specified equity
    /// </summary>
    public class FactorFileGenerator
    {
        /// <summary>
        /// The symbol for which the factor file is being generated
        /// </summary>
        public Symbol Symbol { get; set; }
        /// <summary>
        /// The daily data for an equity
        /// </summary>
        public List<TradeBar> DailyDataForEquity { get; private set; }

        private readonly DateTime _lastDateFromEquityData;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="symbol">The equity for which the factor file respresents</param>
        /// <param name="pathForDailyEquityData">The path to the daily data for the specified equity</param>
        public FactorFileGenerator(Symbol symbol, string pathForDailyEquityData)
        {
            Symbol = symbol;
            DailyDataForEquity = ReadDailyData(pathForDailyEquityData);
            _lastDateFromEquityData = DailyDataForEquity.Last().Time;
        }

        /// <summary>
        /// Create FactorFile object
        /// </summary>
        /// <param name="EquityEvent">Dividends and Splits according</param>
        /// <returns>A factor file object</returns>
        public FactorFile CreateFactorFileFromData(Queue<EquityEvent> EquityEvent)
        {
            var factorFileRows = new List<FactorFileRow>()
            {
                // First Factor Row is set far into the future
                new FactorFileRow(DateTime.ParseExact("20501231",
                                                      DateFormat.EightCharacter,
                                                      CultureInfo.InvariantCulture),
                                  1, // Price Factor
                                  1) // Split Factor
            };

            return RecursivlyGenerateFactorFile(EquityEvent, factorFileRows);
        }

        /// <summary>
        /// Recursively generate the factor file
        /// </summary>
        /// <param name="EquityEvents">Queue of dividends and splits as reported</param>
        /// <param name="factorFileRows">The list of factor file rows</param>
        /// <returns>FactorFile object</returns>
        private FactorFile RecursivlyGenerateFactorFile(Queue<EquityEvent> EquityEvents, List<FactorFileRow> factorFileRows)
        {
            // If there is no more data return
            if (!EquityEvents.Any())
            {
                factorFileRows.Add(CreateLastFactorFileRow(factorFileRows));
                return new FactorFile(Symbol.ID.Symbol, factorFileRows);
            }
                
            var nextEvent = EquityEvents.Dequeue();

            // If there is no more daily equity data to use return
            if (_lastDateFromEquityData > nextEvent.Date)
            {
                factorFileRows.Add(CreateLastFactorFileRow(factorFileRows));
                return new FactorFile(Symbol.ID.Symbol, factorFileRows);
            }

            var nextFactorFileRow = CalculateNextFactorFileRow(factorFileRows, nextEvent);

            if (nextFactorFileRow != null)
                factorFileRows.Add(nextFactorFileRow);

            return RecursivlyGenerateFactorFile(EquityEvents, factorFileRows);
        }

        /// <summary>
        /// Creates the first row in the factor file. 
        /// Represents the earliest date that the daily equity data contains.
        /// </summary>
        /// <param name="factorFileRows">The list of factor file rows</param>
        /// <returns>FactorFileRow</returns>
        private FactorFileRow CreateLastFactorFileRow(List<FactorFileRow> factorFileRows)
        {
            return new FactorFileRow(DailyDataForEquity.Last().Time, 
                                     factorFileRows.Last().PriceFactor, 
                                     factorFileRows.Last().SplitFactor);
        }

        /// <summary>
        /// Calculates the values for the next row in the factor file
        /// </summary>
        /// <param name="factorFileRows">The current list of factorFileRows</param>
        /// <param name="nextMarketEvent">The next dividend or split</param>
        /// <returns>A single factor file row</returns>
        private FactorFileRow CalculateNextFactorFileRow(List<FactorFileRow> factorFileRows, EquityEvent nextMarketEvent)
        {
            FactorFileRow nextFactorFileRow;
            if (nextMarketEvent.EventType == EquityEventType.Split)
                nextFactorFileRow = CalculateNextSplitFactor(nextMarketEvent, factorFileRows.Last());
            else
                nextFactorFileRow = CalculateNextDividendFactor(nextMarketEvent, factorFileRows.Last());
            return nextFactorFileRow;
        }

        /// <summary>
        /// Calculates next price factor after dividend occurs
        /// </summary>
        /// <param name="nextEvent">The next dividend event</param>
        /// <param name="lastFactorFileRow">The current last item in the factor file</param>
        /// <returns></returns>
        private FactorFileRow CalculateNextDividendFactor(EquityEvent nextEvent, FactorFileRow lastFactorFileRow)
        {
            var evenDayData = DailyDataForEquity.FirstOrDefault(x => x.Time.Date == nextEvent.Date);

            // If you don't have the equity data nothing can be done
            if (evenDayData == null)
                return null;

            TradeBar previousClosingPrice = FindNextPreviousClosingPrice(evenDayData.Time);

            var priceFactor = lastFactorFileRow.PriceFactor - (nextEvent.Amount / ((previousClosingPrice.Close / 10000) * lastFactorFileRow.SplitFactor));

            return new FactorFileRow(previousClosingPrice.Time, priceFactor.RoundToSignificantDigits(7), lastFactorFileRow.SplitFactor);
        }

        /// <summary>
        /// Calculates the split factors
        /// </summary>
        /// <param name="nextMarketEvent">The market split or dividend currently being calculated</param>
        /// <param name="lastFactorFileRow">The last factor file row processed</param>
        /// <returns>The next Factor file row</returns>
        private FactorFileRow CalculateNextSplitFactor(EquityEvent nextMarketEvent, FactorFileRow lastFactorFileRow)
        {
            return new FactorFileRow(
                    nextMarketEvent.Date,
                    lastFactorFileRow.PriceFactor,
                    (lastFactorFileRow.SplitFactor * nextMarketEvent.Amount).RoundToSignificantDigits(6)
                );
        }

        /// <summary>
        /// Gets the previous trading days closing price
        /// </summary>
        /// <param name="date">Date that is currently being processed</param>
        /// <returns>A tradebar representing that days data</returns>
        private TradeBar FindNextPreviousClosingPrice(DateTime date)
        {
            TradeBar previousDayData = null;

            while (previousDayData == null)
            {
                previousDayData = DailyDataForEquity.FirstOrDefault(x => x.Time == date.AddDays(-1));
                date = date.AddDays(-1);
            }

            return previousDayData;
        }

        /// <summary>
        /// Reads in the daily data for the specified equity symbol
        /// </summary>
        /// <param name="pathForDailyEquityData">Path to the daily equity data</param>
        /// <returns>List of trade bars representing the daily equity data</returns>
        public List<TradeBar> ReadDailyData(string pathForDailyEquityData)
        {
            string path = Path.Combine(pathForDailyEquityData);
            IEnumerable<TradeBar> dailyData = null;
            string[] lines;

            if (path.Contains(".zip"))
                lines = ReadZipFile(path); 
            else
                lines = File.ReadAllLines(path);

            dailyData = lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(Parse);

            return dailyData.OrderByDescending(x => x.Time).ToList();
        }

        /// <summary>
        /// Read zip file and return list of lines in file
        /// </summary>
        /// <param name="path">Path to zip file</param>
        /// <returns></returns>
        private string[] ReadZipFile(string path)
        {
            using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    var stream = entry.Open();
                    var streamReader = new StreamReader(stream);
                    return streamReader.ReadToEnd().Split('\n');
                }
            }

            return new string[0];
        }

        /// <summary>
        /// Parses the specified line into a tradebar
        /// </summary>
        private TradeBar Parse(string line)
        {
            var csv = line.Split(',');
            return new TradeBar(
                DateTime.ParseExact(csv[0], DateFormat.TwelveCharacter, CultureInfo.InvariantCulture, DateTimeStyles.None),
                Symbol,
                decimal.Parse(csv[1], CultureInfo.InvariantCulture),
                decimal.Parse(csv[2], CultureInfo.InvariantCulture),
                decimal.Parse(csv[3], CultureInfo.InvariantCulture),
                decimal.Parse(csv[4], CultureInfo.InvariantCulture),
                (long)0
                );
        }
    }
}
