using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace QuantConnect.Lean.Engine
{
    /// <summary>
    /// Defines a single row in a factor_factor file. This is a csv file ordered as {date, price factor, split factor}
    /// </summary>
    public class FactorFileRow
    {
        /// <summary>
        /// Gets the date associated with this data
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Gets the price factor associated with this data
        /// </summary>
        public decimal PriceFactor { get; private set; }

        /// <summary>
        /// Gets te split factored associated with the date
        /// </summary>
        public decimal SplitFactor { get; private set; }

        /// <summary>
        /// Gets the combined factor used to create adjusted prices from raw prices
        /// </summary>
        public decimal TimePriceFactor
        {
            get { return PriceFactor*SplitFactor; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactorFileRow"/> class
        /// </summary>
        public FactorFileRow(DateTime date, decimal priceFactor, decimal splitFactor)
        {
            Date = date;
            PriceFactor = priceFactor;
            SplitFactor = splitFactor;
        }

        /// <summary>
        /// Reads in the factor file for the specified equity symbol
        /// </summary>
        public static IEnumerable<FactorFileRow> Read(string dataFolder, string symbol)
        {
            string format = dataFolder + "equity/factor_files/" + symbol.ToLower() + ".csv";
            foreach (var line in File.ReadAllLines(format))
            {
                var csv = line.Split(',');
                yield return new FactorFileRow(
                    DateTime.ParseExact(csv[0], DateFormat.EightCharacter, CultureInfo.InvariantCulture, DateTimeStyles.None),
                    decimal.Parse(csv[1]),
                    decimal.Parse(csv[2])
                    );
            }
        }
    }
}