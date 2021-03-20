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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Data.Auxiliary
{
    /// <summary>
    /// Defines a single row in a factor_factor file. This is a csv file ordered as {date, price factor, split factor, reference price}
    /// </summary>
    public class FactorFileRow
    {
        private decimal _splitFactor;
        private decimal _priceFactor;

        /// <summary>
        /// Gets the date associated with this data
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Gets the price factor associated with this data
        /// </summary>
        public decimal PriceFactor
        {
            get
            {
                return _priceFactor;

            }
            set
            {
                _priceFactor = value;
                UpdatePriceScaleFactor();
            }
        }

        /// <summary>
        /// Gets the split factor associated with the date
        /// </summary>
        public decimal SplitFactor
        {
            get
            {
                return _splitFactor;
            }
            set
            {
                _splitFactor = value;
                UpdatePriceScaleFactor();
            }
        }

        /// <summary>
        /// Gets the combined factor used to create adjusted prices from raw prices
        /// </summary>
        public decimal PriceScaleFactor { get; private set; }

        /// <summary>
        /// Gets the raw closing value from the trading date before the updated factor takes effect
        /// </summary>
        public decimal ReferencePrice { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FactorFileRow"/> class
        /// </summary>
        public FactorFileRow(DateTime date, decimal priceFactor, decimal splitFactor, decimal referencePrice = 0)
        {
            Date = date;
            ReferencePrice = referencePrice;
            PriceFactor = priceFactor;
            SplitFactor = splitFactor;
        }

        /// <summary>
        /// Reads in the factor file for the specified equity symbol
        /// </summary>
        public static IEnumerable<FactorFileRow> Read(string permtick, string market, out DateTime? factorFileMinimumDate)
        {
            factorFileMinimumDate = null;

            var path = Path.Combine(Globals.CacheDataFolder, "equity", market, "factor_files", permtick.ToLowerInvariant() + ".csv");
            var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l));

            return Parse(lines, out factorFileMinimumDate);
        }

        /// <summary>
        /// Parses the lines as factor files rows while properly handling inf entries
        /// </summary>
        /// <param name="lines">The lines from the factor file to be parsed</param>
        /// <param name="factorFileMinimumDate">The minimum date from the factor file</param>
        /// <returns>An enumerable of factor file rows</returns>
        public static List<FactorFileRow> Parse(IEnumerable<string> lines, out DateTime? factorFileMinimumDate)
        {
            factorFileMinimumDate = null;

            var rows = new List<FactorFileRow>();

            // parse factor file lines
            foreach (var line in lines)
            {
                // Exponential notation is treated as inf is because of the loss of precision. In
                // all cases, the significant part has fewer decimals than the needed for a correct
                // representation, E.g., 1.6e+6 when the correct factor is 1562500.
                if (line.Contains("inf") || line.Contains("e+"))
                {
                    continue;
                }

                var row = Parse(line);

                // ignore zero factor rows
                if (row.PriceScaleFactor > 0)
                {
                    rows.Add(row);
                }
            }

            if (factorFileMinimumDate == null && rows.Count > 0)
            {
                factorFileMinimumDate = rows.Min(ffr => ffr.Date).AddDays(-1);
            }

            return rows;
        }

        /// <summary>
        /// Applies the dividend to this factor file row.
        /// This dividend date must be on or before the factor
        /// file row date
        /// </summary>
        /// <param name="dividend">The dividend to apply with reference price and distribution specified</param>
        /// <param name="exchangeHours">Exchange hours used for resolving the previous trading day</param>
        /// <returns>A new factor file row that applies the dividend to this row's factors</returns>
        public FactorFileRow Apply(Dividend dividend, SecurityExchangeHours exchangeHours)
        {
            if (dividend.ReferencePrice == 0m)
            {
                throw new ArgumentException("Unable to apply dividend with reference price of zero.");
            }

            var previousTradingDay = exchangeHours.GetPreviousTradingDay(dividend.Time);

            // this instance must be chronologically at or in front of the dividend
            // this is because the factors are defined working from current to past
            if (Date < previousTradingDay)
            {
                throw new ArgumentException(Invariant(
                    $"Factor file row date '{Date:yyy-MM-dd}' is before dividend previous trading date '{previousTradingDay.Date:yyyy-MM-dd}'."
                ));
            }

            // pfi - new price factor pf(i+1) - this price factor D - distribution C - previous close
            // pfi = pf(i+1) * (C-D)/C
            var priceFactor = PriceFactor * (dividend.ReferencePrice - dividend.Distribution) / dividend.ReferencePrice;

            return new FactorFileRow(
                previousTradingDay,
                priceFactor,
                SplitFactor,
                dividend.ReferencePrice
            );
        }

        /// <summary>
        /// Applies the split to this factor file row.
        /// This split date must be on or before the factor
        /// file row date
        /// </summary>
        /// <param name="split">The split to apply with reference price and split factor specified</param>
        /// <param name="exchangeHours">Exchange hours used for resolving the previous trading day</param>
        /// <returns>A new factor file row that applies the split to this row's factors</returns>
        public FactorFileRow Apply(Split split, SecurityExchangeHours exchangeHours)
        {
            if (split.Type == SplitType.Warning)
            {
                throw new ArgumentException("Unable to apply split with type warning. Only actual splits may be applied");
            }

            if (split.ReferencePrice == 0m)
            {
                throw new ArgumentException("Unable to apply split with reference price of zero.");
            }

            var previousTradingDay = exchangeHours.GetPreviousTradingDay(split.Time);

            // this instance must be chronologically at or in front of the split
            // this is because the factors are defined working from current to past
            if (Date < previousTradingDay)
            {
                throw new ArgumentException(Invariant(
                    $"Factor file row date '{Date:yyy-MM-dd}' is before split date '{split.Time.Date:yyyy-MM-dd}'."
                ));
            }

            return new FactorFileRow(
                previousTradingDay,
                PriceFactor,
                SplitFactor * split.SplitFactor,
                split.ReferencePrice
            );
        }

        /// <summary>
        /// Creates a new dividend from this factor file row and the one chronologically in front of it
        /// This dividend may have a distribution of zero if this row doesn't represent a dividend
        /// </summary>
        /// <param name="futureFactorFileRow">The next factor file row in time</param>
        /// <param name="symbol">The symbol to use for the dividend</param>
        /// <param name="exchangeHours">Exchange hours used for resolving the previous trading day</param>
        /// <param name="decimalPlaces">The number of decimal places to round the dividend's distribution to, defaulting to 2</param>
        /// <returns>A new dividend instance</returns>
        public Dividend GetDividend(FactorFileRow futureFactorFileRow, Symbol symbol, SecurityExchangeHours exchangeHours, int decimalPlaces=2)
        {
            if (futureFactorFileRow.PriceFactor == 0m)
            {
                throw new InvalidOperationException(Invariant(
                    $"Unable to resolve dividend for '{symbol.ID}' at {Date:yyyy-MM-dd}. Price factor is zero."
                ));
            }

            // find previous trading day
            var previousTradingDay = exchangeHours.GetNextTradingDay(Date);

            return Dividend.Create(
                symbol,
                previousTradingDay,
                ReferencePrice,
                PriceFactor / futureFactorFileRow.PriceFactor,
                decimalPlaces
            );
        }

        /// <summary>
        /// Creates a new split from this factor file row and the one chronologically in front of it
        /// This split may have a split factor of one if this row doesn't represent a split
        /// </summary>
        /// <param name="futureFactorFileRow">The next factor file row in time</param>
        /// <param name="symbol">The symbol to use for the split</param>
        /// <param name="exchangeHours">Exchange hours used for resolving the previous trading day</param>
        /// <returns>A new split instance</returns>
        public Split GetSplit(FactorFileRow futureFactorFileRow, Symbol symbol, SecurityExchangeHours exchangeHours)
        {
            if (futureFactorFileRow.SplitFactor == 0m)
            {
                throw new InvalidOperationException(Invariant(
                    $"Unable to resolve split for '{symbol.ID}' at {Date:yyyy-MM-dd}. Split factor is zero."
                ));
            }

            // find previous trading day
            var previousTradingDay = exchangeHours.GetNextTradingDay(Date);

            return new Split(
                symbol,
                previousTradingDay,
                ReferencePrice,
                SplitFactor / futureFactorFileRow.SplitFactor,
                SplitType.SplitOccurred
            );
        }

        /// <summary>
        /// Parses the specified line as a factor file row
        /// </summary>
        private static FactorFileRow Parse(string line)
        {
            var csv = line.Split(',');
            return new FactorFileRow(
                QuantConnect.Parse.DateTimeExact(csv[0], DateFormat.EightCharacter, DateTimeStyles.None),
                QuantConnect.Parse.Decimal(csv[1]),
                QuantConnect.Parse.Decimal(csv[2]),
                csv.Length > 3 ? QuantConnect.Parse.Decimal(csv[3]) : 0m
            );
        }

        /// <summary>
        /// Writes this row to csv format
        /// </summary>
        public string ToCsv(string source = null)
        {
            source = source == null ? "" : $",{source}";
            return $"{Date.ToStringInvariant(DateFormat.EightCharacter)}," +
                   Invariant($"{Math.Round(PriceFactor, 7)},") +
                   Invariant($"{Math.Round(SplitFactor, 8)},") +
                   Invariant($"{Math.Round(ReferencePrice, 4).Normalize()}") +
                   $"{source}";
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return Invariant($"{Date:yyyy-MM-dd}: {PriceScaleFactor:0.0000} {SplitFactor:0.0000}");
        }

        /// <summary>
        /// For performance we update <see cref="PriceScaleFactor"/> when underlying
        /// values are updated to avoid decimal multiplication on each get operation.
        /// </summary>
        private void UpdatePriceScaleFactor()
        {
            PriceScaleFactor = _priceFactor * _splitFactor;
        }
    }
}