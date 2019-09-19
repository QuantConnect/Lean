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

namespace QuantConnect.Data.Custom.SmartInsider
{
    /// <summary>
    /// Smart Insider Intentions - Contains information about
    /// intentions for an insider to purchase stock
    /// </summary>
    public class SmartInsiderIntention : SmartInsiderEvent
    {
        /// <summary>
        /// Describes how the transaction was executed
        /// </summary>
        public string IntentionVia { get; set; }

        /// <summary>
        /// Describes which entity carried out the transaction
        /// </summary>
        public string IntentionBy { get; set; }

        /// <summary>
        /// Describes what will be done with those shares following repurchase
        /// </summary>
        public string BuybackIntentionHoldingType { get; set; }

        /// <summary>
        /// Number of shares to be or authorised to be traded
        /// </summary>
        public int? IntentionAmount { get; set; }

        /// <summary>
        /// Currency of the value of shares to be/Authorised to be traded (ISO Code)
        /// </summary>
        public string ValueCurrency { get; set; }

        /// <summary>
        /// Valueof shares to be authorised to be traded
        /// </summary>
        public long? IntentionValue { get; set; }

        /// <summary>
        /// Percentage of oustanding shares to be authorised to be traded
        /// </summary>
        public decimal? IntentionPercentage { get; set; }

        /// <summary>
        /// start of the period the intention/authorisation applies to
        /// </summary>
        public DateTime? IntentionAuthorisationStartDate { get; set; }

        /// <summary>
        /// End of the period the intention/authorisation applies to
        /// </summary>
        public DateTime? IntentionAuthorisationEndDate { get; set; }

        /// <summary>
        /// Currency of min/max prices (ISO Code)
        /// </summary>
        public string PriceCurrency { get; set; }

        /// <summary>
        /// Minimum price shares will or may be purchased at
        /// </summary>
        public decimal? MinimumPrice { get; set; }

        /// <summary>
        /// Maximum price shares will or may be purchased at
        /// </summary>
        public decimal? MaximumPrice { get; set; }

        /// <summary>
        /// Free text which explains further details about the trade
        /// </summary>
        public string BuybackIntentionNoteText { get; set; }

        /// <summary>
        /// Empty constructor required for <see cref="Slice.Get{T}()"/>
        /// </summary>
        public SmartInsiderIntention()
        {
        }

        /// <summary>
        /// Constructs instance of this via a *formatted* CSV line (tab delimited)
        /// </summary>
        /// <param name="line">Line of formatted CSV data</param>
        public SmartInsiderIntention(string line) : base(line)
        {
            var tsv = line.Split('\t');

            IntentionVia = string.IsNullOrWhiteSpace(tsv[26]) ? null : tsv[26];
            IntentionBy = string.IsNullOrWhiteSpace(tsv[27]) ? null : tsv[27];
            BuybackIntentionHoldingType = string.IsNullOrWhiteSpace(tsv[28]) ? null : tsv[28];
            IntentionAmount = string.IsNullOrWhiteSpace(tsv[29]) ? (int?)null : Convert.ToInt32(tsv[29], CultureInfo.InvariantCulture);
            ValueCurrency = string.IsNullOrWhiteSpace(tsv[30]) ? null : tsv[30];
            IntentionValue = string.IsNullOrWhiteSpace(tsv[31]) ? (long?)null : Convert.ToInt64(tsv[31], CultureInfo.InvariantCulture);
            IntentionPercentage = string.IsNullOrWhiteSpace(tsv[32]) ? (decimal?)null : Convert.ToDecimal(tsv[32], CultureInfo.InvariantCulture);
            IntentionAuthorisationStartDate = string.IsNullOrWhiteSpace(tsv[33]) ? (DateTime?)null : DateTime.ParseExact(tsv[33], "yyyyMMdd", CultureInfo.InvariantCulture);
            IntentionAuthorisationEndDate = string.IsNullOrWhiteSpace(tsv[34]) ? (DateTime?)null : DateTime.ParseExact(tsv[34], "yyyyMMdd", CultureInfo.InvariantCulture);
            PriceCurrency = string.IsNullOrWhiteSpace(tsv[35]) ? null : tsv[35];
            MinimumPrice = string.IsNullOrWhiteSpace(tsv[36]) ? (decimal?)null : Convert.ToDecimal(tsv[36], CultureInfo.InvariantCulture);
            MaximumPrice = string.IsNullOrWhiteSpace(tsv[37]) ? (decimal?)null : Convert.ToDecimal(tsv[37], CultureInfo.InvariantCulture);
            BuybackIntentionNoteText = tsv.Length == 39? (string.IsNullOrWhiteSpace(tsv[38]) ? null : tsv[38]) : null;
        }

        /// <summary>
        /// Constructs a new instance from unformatted CSV data
        /// </summary>
        /// <param name="line">Line of raw CSV (raw with fields 46, 36, 14, 7 removed in descending order)</param>
        /// <returns>Instance of the object</returns>
        public override void FromRawData(string line)
        {
            var tsv = line.Split('\t');

            TransactionID = string.IsNullOrWhiteSpace(tsv[0]) ? null : tsv[0];
            BuybackType = string.IsNullOrWhiteSpace(tsv[1]) ? null : tsv[1];
            LastUpdate = DateTime.ParseExact(tsv[2], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            LastIDsUpdate = string.IsNullOrWhiteSpace(tsv[3]) ? (DateTime?)null : DateTime.ParseExact(tsv[3], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            ISIN = string.IsNullOrWhiteSpace(tsv[4]) ? null : tsv[4];
            USDMarketCap = string.IsNullOrWhiteSpace(tsv[5]) ? (decimal?)null : Convert.ToDecimal(tsv[5], CultureInfo.InvariantCulture);
            CompanyID = string.IsNullOrWhiteSpace(tsv[6]) ? (int?)null : Convert.ToInt32(tsv[6], CultureInfo.InvariantCulture);
            ICBIndustry = string.IsNullOrWhiteSpace(tsv[7]) ? null : tsv[7];
            ICBSuperSector = string.IsNullOrWhiteSpace(tsv[8]) ? null : tsv[8];
            ICBSector = string.IsNullOrWhiteSpace(tsv[9]) ? null : tsv[9];
            ICBSubSector = string.IsNullOrWhiteSpace(tsv[10]) ? null : tsv[10];
            ICBCode = string.IsNullOrWhiteSpace(tsv[11]) ? (int?)null : Convert.ToInt32(tsv[11], CultureInfo.InvariantCulture);
            CompanyName = string.IsNullOrWhiteSpace(tsv[12]) ? null : tsv[12];
            PreviousResultsAnnouncementDate = string.IsNullOrWhiteSpace(tsv[13]) ? (DateTime?)null : DateTime.ParseExact(tsv[13], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            NextResultsAnnouncementsDate = string.IsNullOrWhiteSpace(tsv[14]) ? (DateTime?)null : DateTime.ParseExact(tsv[14], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            NextCloseBegin = string.IsNullOrWhiteSpace(tsv[15]) ? (DateTime?)null : DateTime.ParseExact(tsv[15], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            LastCloseEnded = string.IsNullOrWhiteSpace(tsv[16]) ? (DateTime?)null : DateTime.ParseExact(tsv[16], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            SecurityDescription = string.IsNullOrWhiteSpace(tsv[17]) ? null : tsv[17];
            TickerCountry = string.IsNullOrWhiteSpace(tsv[18]) ? null : tsv[18];
            TickerSymbol = string.IsNullOrWhiteSpace(tsv[19]) ? null : tsv[19];

            AnnouncementDate = string.IsNullOrWhiteSpace(tsv[37]) ? (DateTime?)null : DateTime.ParseExact(tsv[37], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            TimeReleased = string.IsNullOrWhiteSpace(tsv[38]) ? (DateTime?)null : DateTime.ParseExact(tsv[38].Replace(" ", "").Trim(), "dd/MM/yyyyHH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessed = string.IsNullOrWhiteSpace(tsv[39]) ? (DateTime?)null : DateTime.ParseExact(tsv[39].Replace(" ", "").Trim(), "dd/MM/yyyyHH:mm:ss", CultureInfo.InvariantCulture);
            TimeReleasedUtc = string.IsNullOrWhiteSpace(tsv[40]) ? (DateTime?)null : DateTime.ParseExact(tsv[40].Replace(" ", "").Trim(), "dd/MM/yyyyHH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessedUtc = string.IsNullOrWhiteSpace(tsv[41]) ? (DateTime?)null : DateTime.ParseExact(tsv[41].Replace(" ", "").Trim(), "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);
            AnnouncedIn = string.IsNullOrWhiteSpace(tsv[42]) ? null : tsv[42];

            IntentionVia = string.IsNullOrWhiteSpace(tsv[43]) ? null : tsv[43];
            IntentionBy = string.IsNullOrWhiteSpace(tsv[44]) ? null : tsv[44];
            BuybackIntentionHoldingType = string.IsNullOrWhiteSpace(tsv[45]) ? null : tsv[45];
            IntentionAmount = string.IsNullOrWhiteSpace(tsv[46]) ? (int?)null : Convert.ToInt32(tsv[46], CultureInfo.InvariantCulture);
            ValueCurrency = string.IsNullOrWhiteSpace(tsv[47]) ? null : tsv[47];
            IntentionValue = string.IsNullOrWhiteSpace(tsv[48]) ? (long?)null : Convert.ToInt64(tsv[48], CultureInfo.InvariantCulture);
            IntentionPercentage = string.IsNullOrWhiteSpace(tsv[49]) ? (decimal?)null : Convert.ToDecimal(tsv[49], CultureInfo.InvariantCulture);
            IntentionAuthorisationStartDate = string.IsNullOrWhiteSpace(tsv[50]) ? (DateTime?)null : DateTime.ParseExact(tsv[50], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            IntentionAuthorisationEndDate = string.IsNullOrWhiteSpace(tsv[51]) ? (DateTime?)null : DateTime.ParseExact(tsv[51], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            PriceCurrency = string.IsNullOrWhiteSpace(tsv[52]) ? null : tsv[52];
            MinimumPrice = string.IsNullOrWhiteSpace(tsv[53]) ? (decimal?)null : Convert.ToDecimal(tsv[53], CultureInfo.InvariantCulture);
            MaximumPrice = string.IsNullOrWhiteSpace(tsv[54]) ? (decimal?)null : Convert.ToDecimal(tsv[54], CultureInfo.InvariantCulture);
            BuybackIntentionNoteText = tsv.Length == 56 ? (string.IsNullOrWhiteSpace(tsv[55]) ? null : tsv[55]) : null;
        }

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
                    "smartinsider",
                    "intentions",
                    $"{config.Symbol.Value.ToLowerInvariant()}.tsv"
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Csv
            );
        }

        /// <summary>
        /// Loads and reads the data to be used in LEAN
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="line">CSV line</param>
        /// <param name="date">Algorithm date</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>Instance of the object</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var intention = new SmartInsiderIntention(line)
            {
                Symbol = config.Symbol
            };
            // Files are made available at the earliest @ 17:00 U.K. time
            intention.Time = intention.Time.AddHours(17).ConvertTo(TimeZones.London,config.DataTimeZone);

            return intention;
        }

        /// <summary>
        /// Clones the object to a new instance. This method
        /// is required for custom data sources that make use
        /// of properties with more complex types since otherwise
        /// the values will default to null using the default clone method
        /// </summary>
        /// <returns>A new cloned instance of this object</returns>
        public override BaseData Clone()
        {
            return new SmartInsiderIntention()
            {
                TransactionID = TransactionID,
                BuybackType = BuybackType,
                LastUpdate = LastUpdate,
                LastIDsUpdate = LastIDsUpdate,
                ISIN = ISIN,
                USDMarketCap = USDMarketCap,
                CompanyID = CompanyID,
                ICBIndustry = ICBIndustry,
                ICBSuperSector = ICBSuperSector,
                ICBSector = ICBSector,
                ICBSubSector = ICBSubSector,
                ICBCode = ICBCode,
                CompanyName = CompanyName,
                PreviousResultsAnnouncementDate = PreviousResultsAnnouncementDate,
                NextResultsAnnouncementsDate = NextResultsAnnouncementsDate,
                NextCloseBegin = NextCloseBegin,
                LastCloseEnded = LastCloseEnded,
                SecurityDescription = SecurityDescription,
                TickerCountry = TickerCountry,
                TickerSymbol = TickerSymbol,
                AnnouncementDate = AnnouncementDate,
                TimeReleased = TimeReleased,
                TimeProcessed = TimeProcessed,
                TimeReleasedUtc = TimeReleasedUtc,
                TimeProcessedUtc = TimeProcessedUtc,
                AnnouncedIn = AnnouncedIn,

                IntentionVia = IntentionVia,
                IntentionBy = IntentionBy,
                BuybackIntentionHoldingType = BuybackIntentionHoldingType,
                IntentionAmount = IntentionAmount,
                ValueCurrency = ValueCurrency,
                IntentionValue = IntentionValue,
                IntentionPercentage = IntentionPercentage,
                IntentionAuthorisationStartDate = IntentionAuthorisationStartDate,
                IntentionAuthorisationEndDate = IntentionAuthorisationEndDate,
                PriceCurrency = PriceCurrency,
                MinimumPrice = MinimumPrice,
                MaximumPrice = MaximumPrice,
                BuybackIntentionNoteText = BuybackIntentionNoteText
            };
        }

        /// <summary>
        /// Converts the data to CSV
        /// </summary>
        /// <returns>String of CSV</returns>
        /// <remarks>Parsable by the constructor should you need to recreate the object from CSV</remarks>
        public override string ToLine()
        {
            return string.Join("\t",
                TransactionID,
                BuybackType,
                LastUpdate.ToStringInvariant("yyyyMMdd"),
                LastIDsUpdate?.ToStringInvariant("yyyyMMdd"),
                ISIN,
                USDMarketCap,
                CompanyID,
                ICBIndustry,
                ICBSuperSector,
                ICBSector,
                ICBSubSector,
                ICBCode,
                CompanyName,
                PreviousResultsAnnouncementDate?.ToStringInvariant("yyyyMMdd"),
                NextResultsAnnouncementsDate?.ToStringInvariant("yyyyMMdd"),
                NextCloseBegin?.ToStringInvariant("yyyyMMdd"),
                LastCloseEnded?.ToStringInvariant("yyyyMMdd"),
                SecurityDescription,
                TickerCountry,
                TickerSymbol,
                AnnouncementDate?.ToStringInvariant("yyyyMMdd"),
                TimeReleased?.ToStringInvariant("yyyyMMdd HH:mm:ss"),
                TimeProcessed?.ToStringInvariant("yyyyMMdd HH:mm:ss"),
                TimeReleasedUtc?.ToStringInvariant("yyyyMMdd HH:mm:ss"),
                TimeProcessedUtc?.ToStringInvariant("yyyyMMdd HH:mm:ss"),
                AnnouncedIn,
                IntentionVia,
                IntentionBy,
                BuybackIntentionHoldingType,
                IntentionAmount,
                ValueCurrency,
                IntentionValue,
                IntentionPercentage,
                IntentionAuthorisationStartDate?.ToStringInvariant("yyyyMMdd"),
                IntentionAuthorisationEndDate?.ToStringInvariant("yyyyMMdd"),
                PriceCurrency,
                MinimumPrice,
                MaximumPrice,
                BuybackIntentionNoteText);
        }
    }
}
