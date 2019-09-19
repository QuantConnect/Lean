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

using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace QuantConnect.Data.Custom.USTreasury
{
    /// <summary>
    /// U.S. Treasury yield curve data
    /// </summary>
    public class USTreasuryYieldCurveRate : BaseData
    {
        /// <summary>
        /// One month yield curve
        /// </summary>
        public decimal? OneMonth { get; private set; }

        /// <summary>
        /// Two month yield curve
        /// </summary>
        public decimal? TwoMonth { get; private set; }

        /// <summary>
        /// Three month yield curve
        /// </summary>
        public decimal? ThreeMonth { get; private set; }

        /// <summary>
        /// Six month yield curve
        /// </summary>
        public decimal? SixMonth { get; private set; }

        /// <summary>
        /// One year yield curve
        /// </summary>
        public decimal? OneYear { get; private set; }

        /// <summary>
        /// Two year yield curve
        /// </summary>
        public decimal? TwoYear { get; private set; }

        /// <summary>
        /// Three year yield curve
        /// </summary>
        public decimal? ThreeYear { get; private set; }

        /// <summary>
        /// Five year yield curve
        /// </summary>
        public decimal? FiveYear { get; private set; }

        /// <summary>
        /// Seven year yield curve
        /// </summary>
        public decimal? SevenYear { get; private set; }

        /// <summary>
        /// Ten year yield curve
        /// </summary>
        public decimal? TenYear { get; private set; }

        /// <summary>
        /// Twenty year yield curve
        /// </summary>
        public decimal? TwentyYear { get; private set; }

        /// <summary>
        /// Thirty year yield curve
        /// </summary>
        public decimal? ThirtyYear { get; private set; }

        /// <summary>
        /// Specifies the location of the data and directs LEAN where to load the data from
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="date">Algorithm date</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>Subscription data source object pointing LEAN to the data location</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource(
                Path.Combine(
                    Globals.DataFolder,
                    "alternative",
                    "ustreasury",
                    "yieldcurverates.csv"
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Csv
            );
        }

        /// <summary>
        /// Reads and parses yield curve data from a csv file
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="line">CSV line containing yield curve data</param>
        /// <param name="date">Date request was made for</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>YieldCurve instance</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            // Date[0], 1 mo[1], 2 mo[2], 3 mo[3], 6 mo[4], 1 yr[5], 2 yr[6] 3 yr[7], 5 yr[8], 7 yr [9], 10 yr[10], 20 yr[11], 30 yr[12]
            var csv = line.Split(new[] { ',' }, StringSplitOptions.None);
            var csvDecimals = csv.Skip(1)
                .Select(x => string.IsNullOrEmpty(x) ? (decimal?) null : Convert.ToDecimal(x, CultureInfo.InvariantCulture))
                .ToList();

            DateTime csvDate;
            if (!DateTime.TryParseExact(csv[0], DateFormat.EightCharacter, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out csvDate))
            {
                return null;
            }

            return new USTreasuryYieldCurveRate
            {
                // "These market yields are calculated from composites of indicative, bid-side
                // market quotations (not actual transactions) obtained by the
                // Federal Reserve Bank of New York at or near 3:30 PM each trading day"
                //
                // Remarks: Publication time was about an hour delayed on 2019-07-24 - verified by manual observation
                // 20:30 UTC == 16:30 ET
                Time = csvDate.Date.AddHours(16).AddMinutes(30),
                OneMonth = csvDecimals[0],
                TwoMonth = csvDecimals[1],
                ThreeMonth = csvDecimals[2],
                SixMonth = csvDecimals[3],
                OneYear = csvDecimals[4],
                TwoYear = csvDecimals[5],
                ThreeYear = csvDecimals[6],
                FiveYear = csvDecimals[7],
                SevenYear = csvDecimals[8],
                TenYear = csvDecimals[9],
                TwentyYear = csvDecimals[10],
                ThirtyYear = csvDecimals[11],
                Symbol = config.Symbol
            };
        }

        /// <summary>
        /// Clones the object. This method implementation is required
        /// so that we don't have any null values for our properties
        /// when the user attempts to use it in backtesting/live trading
        /// </summary>
        /// <returns>Cloned instance</returns>
        public override BaseData Clone()
        {
            return new USTreasuryYieldCurveRate
            {
                Time = Time,
                OneMonth = OneMonth,
                TwoMonth = TwoMonth,
                ThreeMonth = ThreeMonth,
                SixMonth = SixMonth,
                OneYear = OneYear,
                TwoYear = TwoYear,
                ThreeYear = ThreeYear,
                FiveYear = FiveYear,
                SevenYear = SevenYear,
                TenYear = TenYear,
                TwentyYear = TwentyYear,
                ThirtyYear = ThirtyYear,
                Symbol = Symbol
            };
        }
    }
}
