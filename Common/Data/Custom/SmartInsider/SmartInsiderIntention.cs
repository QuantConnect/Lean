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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string IntentionVia { get; private set; }

        /// <summary>
        /// Describes which entity carried out the transaction
        /// </summary>
        public string IntentionBy { get; private set; }

        /// <summary>
        /// Describes what will be done with those shares following repurchase
        /// </summary>
        public string BuybackIntentionHoldingType { get; private set; }

        /// <summary>
        /// Number of shares to be or authorised to be traded
        /// </summary>
        public int? IntentionAmount { get; private set; }

        /// <summary>
        /// Currency of the value of shares to be/Authorised to be traded (ISO Code)
        /// </summary>
        public string ValueCurrency { get; private set; }

        /// <summary>
        /// Valueof shares to be authorised to be traded
        /// </summary>
        public long? IntentionValue { get; private set; }

        /// <summary>
        /// Percentage of oustanding shares to be authorised to be traded
        /// </summary>
        public decimal? IntentionPercentage { get; private set; }

        /// <summary>
        /// start of the period the intention/authorisation applies to
        /// </summary>
        public DateTime? IntentionAuthorisationStartDate { get; private set; }

        /// <summary>
        /// End of the period the intention/authorisation applies to
        /// </summary>
        public DateTime? IntentionAuthorisationEndDate { get; private set; }

        /// <summary>
        /// Currency of min/max prices (ISO Code)
        /// </summary>
        public string PriceCurrency { get; private set; }

        /// <summary>
        /// Minimum price shares will or may be purchased at
        /// </summary>
        public decimal? MinimumPrice { get; private set; }

        /// <summary>
        /// Maximum price shares will or may be purchased at
        /// </summary>
        public decimal? MaximumPrice { get; private set; }

        /// <summary>
        /// Free text which explains further details about the trade
        /// </summary>
        public string BuybackIntentionNoteText { get; private set; }

        /// <summary>
        /// Empty constructor required for <see cref="Slice.Get{T}()"/>
        /// </summary>
        public SmartInsiderIntention()
        {
        }

        /// <summary>
        /// Constructs instance of this via a *formatted* CSV line (tab delimited)
        /// </summary>
        /// <param name="csvLine">Line of formatted CSV data</param>
        public SmartInsiderIntention(string csvLine) : base(csvLine)
        {
            var csv = csvLine.Split('\t');

            IntentionVia = string.IsNullOrWhiteSpace(csv[26]) ? null : csv[26];
            IntentionBy = string.IsNullOrWhiteSpace(csv[27]) ? null : csv[27];
            BuybackIntentionHoldingType = string.IsNullOrWhiteSpace(csv[28]) ? null : csv[28];
            IntentionAmount = string.IsNullOrWhiteSpace(csv[29]) ? (int?)null : Convert.ToInt32(csv[29], CultureInfo.InvariantCulture);
            ValueCurrency = string.IsNullOrWhiteSpace(csv[30]) ? null : csv[30];
            IntentionValue = string.IsNullOrWhiteSpace(csv[31]) ? (long?)null : Convert.ToInt64(csv[31], CultureInfo.InvariantCulture);
            IntentionPercentage = string.IsNullOrWhiteSpace(csv[32]) ? (decimal?)null : Convert.ToDecimal(csv[32], CultureInfo.InvariantCulture);
            IntentionAuthorisationStartDate = string.IsNullOrWhiteSpace(csv[33]) ? (DateTime?)null : DateTime.ParseExact(csv[33], "yyyyMMdd", CultureInfo.InvariantCulture);
            IntentionAuthorisationEndDate = string.IsNullOrWhiteSpace(csv[34]) ? (DateTime?)null : DateTime.ParseExact(csv[34], "yyyyMMdd", CultureInfo.InvariantCulture);
            PriceCurrency = string.IsNullOrWhiteSpace(csv[35]) ? null : csv[35];
            MinimumPrice = string.IsNullOrWhiteSpace(csv[36]) ? (decimal?)null : Convert.ToDecimal(csv[36], CultureInfo.InvariantCulture);
            MaximumPrice = string.IsNullOrWhiteSpace(csv[37]) ? (decimal?)null : Convert.ToDecimal(csv[37], CultureInfo.InvariantCulture);
            BuybackIntentionNoteText = csv.Length == 39? (string.IsNullOrWhiteSpace(csv[38]) ? null : csv[38]) : null;
        }

        /// <summary>
        /// Constructs a new instance from unformatted CSV data
        /// </summary>
        /// <param name="line">Line of raw CSV (raw with fields 46, 36, 14, 7 removed in descending order)</param>
        /// <returns>Instance of the object</returns>
        public override void FromRawCsv(string line)
        {
            var csv = line.Split('\t');

            TransactionID = string.IsNullOrWhiteSpace(csv[0]) ? null : csv[0];
            BuybackType = string.IsNullOrWhiteSpace(csv[1]) ? null : csv[1];
            LastUpdate = DateTime.ParseExact(csv[2], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            LastIDsUpdate = string.IsNullOrWhiteSpace(csv[3]) ? (DateTime?)null : DateTime.ParseExact(csv[3], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            ISIN = string.IsNullOrWhiteSpace(csv[4]) ? null : csv[4];
            USDMarketCap = string.IsNullOrWhiteSpace(csv[5]) ? (decimal?)null : Convert.ToDecimal(csv[5], CultureInfo.InvariantCulture);
            CompanyID = string.IsNullOrWhiteSpace(csv[6]) ? (int?)null : Convert.ToInt32(csv[6], CultureInfo.InvariantCulture);
            ICBIndustry = string.IsNullOrWhiteSpace(csv[7]) ? null : csv[7];
            ICBSuperSector = string.IsNullOrWhiteSpace(csv[8]) ? null : csv[8];
            ICBSector = string.IsNullOrWhiteSpace(csv[9]) ? null : csv[9];
            ICBSubSector = string.IsNullOrWhiteSpace(csv[10]) ? null : csv[10];
            ICBCode = string.IsNullOrWhiteSpace(csv[11]) ? (int?)null : Convert.ToInt32(csv[11], CultureInfo.InvariantCulture);
            CompanyName = string.IsNullOrWhiteSpace(csv[12]) ? null : csv[12];
            PreviousResultsAnnouncementDate = string.IsNullOrWhiteSpace(csv[13]) ? (DateTime?)null : DateTime.ParseExact(csv[13], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            NextResultsAnnouncementsDate = string.IsNullOrWhiteSpace(csv[14]) ? (DateTime?)null : DateTime.ParseExact(csv[14], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            NextCloseBegin = string.IsNullOrWhiteSpace(csv[15]) ? (DateTime?)null : DateTime.ParseExact(csv[15], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            LastCloseEnded = string.IsNullOrWhiteSpace(csv[16]) ? (DateTime?)null : DateTime.ParseExact(csv[16], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            SecurityDescription = string.IsNullOrWhiteSpace(csv[17]) ? null : csv[17];
            TickerCountry = string.IsNullOrWhiteSpace(csv[18]) ? null : csv[18];
            TickerSymbol = string.IsNullOrWhiteSpace(csv[19]) ? null : csv[19];

            AnnouncementDate = string.IsNullOrWhiteSpace(csv[37]) ? (DateTime?)null : DateTime.ParseExact(csv[37], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            TimeReleased = string.IsNullOrWhiteSpace(csv[38]) ? (DateTime?)null : DateTime.ParseExact(csv[38].Replace(" ", "").Trim(), "dd/MM/yyyyHH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessed = string.IsNullOrWhiteSpace(csv[39]) ? (DateTime?)null : DateTime.ParseExact(csv[39].Replace(" ", "").Trim(), "dd/MM/yyyyHH:mm:ss", CultureInfo.InvariantCulture);
            TimeReleasedUtc = string.IsNullOrWhiteSpace(csv[40]) ? (DateTime?)null : DateTime.ParseExact(csv[40].Replace(" ", "").Trim(), "dd/MM/yyyyHH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessedUtc = string.IsNullOrWhiteSpace(csv[41]) ? (DateTime?)null : DateTime.ParseExact(csv[41].Replace(" ", "").Trim(), "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);
            AnnouncedIn = string.IsNullOrWhiteSpace(csv[42]) ? null : csv[42];

            IntentionVia = string.IsNullOrWhiteSpace(csv[43]) ? null : csv[43];
            IntentionBy = string.IsNullOrWhiteSpace(csv[44]) ? null : csv[44];
            BuybackIntentionHoldingType = string.IsNullOrWhiteSpace(csv[45]) ? null : csv[45];
            IntentionAmount = string.IsNullOrWhiteSpace(csv[46]) ? (int?)null : Convert.ToInt32(csv[46], CultureInfo.InvariantCulture);
            ValueCurrency = string.IsNullOrWhiteSpace(csv[47]) ? null : csv[47];
            IntentionValue = string.IsNullOrWhiteSpace(csv[48]) ? (long?)null : Convert.ToInt64(csv[48], CultureInfo.InvariantCulture);
            IntentionPercentage = string.IsNullOrWhiteSpace(csv[49]) ? (decimal?)null : Convert.ToDecimal(csv[49], CultureInfo.InvariantCulture);
            IntentionAuthorisationStartDate = string.IsNullOrWhiteSpace(csv[50]) ? (DateTime?)null : DateTime.ParseExact(csv[50], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            IntentionAuthorisationEndDate = string.IsNullOrWhiteSpace(csv[51]) ? (DateTime?)null : DateTime.ParseExact(csv[51], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            PriceCurrency = string.IsNullOrWhiteSpace(csv[52]) ? null : csv[52];
            MinimumPrice = string.IsNullOrWhiteSpace(csv[53]) ? (decimal?)null : Convert.ToDecimal(csv[53], CultureInfo.InvariantCulture);
            MaximumPrice = string.IsNullOrWhiteSpace(csv[54]) ? (decimal?)null : Convert.ToDecimal(csv[54], CultureInfo.InvariantCulture);
            BuybackIntentionNoteText = csv.Length == 56 ? (string.IsNullOrWhiteSpace(csv[55]) ? null : csv[55]) : null;
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
                    $"{config.Symbol.Value.ToLower()}.csv"
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
        public override string ToCsv()
        {
            return $"{TransactionID}\t{BuybackType}\t{LastUpdate:yyyyMMdd}\t{LastIDsUpdate:yyyyMMdd}\t{ISIN}\t{USDMarketCap}\t{CompanyID}\t{ICBIndustry}\t{ICBSuperSector}\t{ICBSector}\t{ICBSubSector}\t{ICBCode}\t{CompanyName}\t{PreviousResultsAnnouncementDate:yyyyMMdd}\t{NextResultsAnnouncementsDate:yyyyMMdd}\t{NextCloseBegin:yyyyMMdd}\t{LastCloseEnded:yyyyMMdd}\t{SecurityDescription}\t{TickerCountry}\t{TickerSymbol}\t{AnnouncementDate:yyyyMMdd}\t{TimeReleased:yyyyMMdd HH:mm:ss}\t{TimeProcessed:yyyyMMdd HH:mm:ss}\t{TimeReleasedUtc:yyyyMMdd HH:mm:ss}\t{TimeProcessedUtc:yyyyMMdd HH:mm:ss}\t{AnnouncedIn}\t{IntentionVia}\t{IntentionBy}\t{BuybackIntentionHoldingType}\t{IntentionAmount}\t{ValueCurrency}\t{IntentionValue}\t{IntentionPercentage}\t{IntentionAuthorisationStartDate:yyyyMMdd}\t{IntentionAuthorisationEndDate:yyyyMMdd}\t{PriceCurrency}\t{MinimumPrice}\t{MaximumPrice}\t{BuybackIntentionNoteText}";
        }

        /// <summary>
        /// Determines equality to another SmartInsiderIntention instance
        /// </summary>
        /// <param name="other">Another SmartInsiderIntention instance</param>
        /// <returns>Boolean value indicating equality</returns>
        public override bool Equals(SmartInsiderEvent other)
        {
            var otherIntention = other as SmartInsiderIntention;
            if (otherIntention == null)
            {
                return false;
            }

            return otherIntention.TransactionID == TransactionID &&
                otherIntention.BuybackType == BuybackType &&
                otherIntention.LastUpdate == LastUpdate &&
                otherIntention.LastIDsUpdate == LastIDsUpdate &&
                otherIntention.ISIN == ISIN &&
                otherIntention.USDMarketCap == USDMarketCap &&
                otherIntention.CompanyID == CompanyID &&
                otherIntention.ICBIndustry == ICBIndustry &&
                otherIntention.ICBSuperSector == ICBSuperSector &&
                otherIntention.ICBSector == ICBSector &&
                otherIntention.ICBSubSector == ICBSubSector &&
                otherIntention.ICBCode == ICBCode &&
                otherIntention.CompanyName == CompanyName &&
                otherIntention.PreviousResultsAnnouncementDate == PreviousResultsAnnouncementDate &&
                otherIntention.NextResultsAnnouncementsDate == NextResultsAnnouncementsDate &&
                otherIntention.NextCloseBegin == NextCloseBegin &&
                otherIntention.LastCloseEnded == LastCloseEnded &&
                otherIntention.SecurityDescription == SecurityDescription &&
                otherIntention.TickerCountry == TickerCountry &&
                otherIntention.TickerSymbol == TickerSymbol &&
                otherIntention.AnnouncementDate == AnnouncementDate &&
                otherIntention.TimeReleased == TimeReleased &&
                otherIntention.TimeProcessed == TimeProcessed &&
                otherIntention.TimeReleasedUtc == TimeReleasedUtc &&
                otherIntention.TimeProcessedUtc == TimeProcessedUtc &&
                otherIntention.AnnouncedIn == AnnouncedIn &&

                otherIntention.IntentionVia == IntentionVia &&
                otherIntention.IntentionBy == IntentionBy &&
                otherIntention.BuybackIntentionHoldingType == BuybackIntentionHoldingType &&
                otherIntention.IntentionAmount == IntentionAmount &&
                otherIntention.ValueCurrency == ValueCurrency &&
                otherIntention.IntentionValue == IntentionValue &&
                otherIntention.IntentionPercentage == IntentionPercentage &&
                otherIntention.IntentionAuthorisationStartDate == IntentionAuthorisationStartDate &&
                otherIntention.IntentionAuthorisationEndDate == IntentionAuthorisationEndDate &&
                otherIntention.PriceCurrency == PriceCurrency &&
                otherIntention.MinimumPrice == MinimumPrice &&
                otherIntention.MaximumPrice == MaximumPrice &&
                otherIntention.BuybackIntentionNoteText == BuybackIntentionNoteText;
        }
    }
}
