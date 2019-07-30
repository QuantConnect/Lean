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
    /// Smart Insider Transaction - Contains information
    /// about insider trading transactions
    /// </summary>
    public class SmartInsiderTransaction : SmartInsiderEvent
    {
        /// <summary>
        /// Date traded through the market
        /// </summary>
        public DateTime? BuybackDate { get; private set; }

        /// <summary>
        /// Describes how transaction was executed
        /// </summary>
        public string BuybackVia { get; private set; }

        /// <summary>
        /// Describes which entity carried out the transaction
        /// </summary>
        public string BuybackBy { get; private set; }

        /// <summary>
        /// Describes what will be done with those shares following repurchase
        /// </summary>
        public string HoldingType { get; private set; }

        /// <summary>
        /// Currency of transation (ISO Code)
        /// </summary>
        public string Currency { get; private set; }

        /// <summary>
        /// Denominated in Currency of Transaction
        /// </summary>
        public new decimal? Price { get; private set; }

        /// <summary>
        /// Number of shares traded
        /// </summary>
        public int? TransactionAmount { get; private set; }

        /// <summary>
        /// Currency conversion rates are updated daily and values are calculated at rate prevailing on the trade date
        /// </summary>
        public int? GBPValue { get; private set; }

        /// <summary>
        /// Currency conversion rates are updated daily and values are calculated at rate prevailing on the trade date
        /// </summary>
        public int? EURValue { get; private set; }

        /// <summary>
        /// Currency conversion rates are updated daily and values are calculated at rate prevailing on the trade date
        /// </summary>
        public int? USDValue { get; private set; }

        /// <summary>
        /// Free text which expains futher details about the trade
        /// </summary>
        public string NoteText { get; private set; }

        /// <summary>
        /// Percentage of value of the trade as part of the issuers total Market Cap
        /// </summary>
        public decimal? BuybackPercentage { get; private set; }

        /// <summary>
        /// Percentage of the volume traded on the day of the buyback.
        /// </summary>
        public decimal? VolumePercentage { get; private set; }

        /// <summary>
        /// Rate used to calculate 'Value (GBP)' from 'Price' multiplied by 'Amount'. Will be 1 where Currency is also 'GBP'
        /// </summary>
        public decimal? ConversionRate { get; private set; }

        /// <summary>
        /// Multiplier which can be applied to 'Amount' field to account for subsequent corporate action
        /// </summary>
        public decimal? AmountAdjustedFactor { get; private set; }

        /// <summary>
        /// Multiplier which can be applied to 'Price' and 'LastClose' fields to account for subsequent corporate actions
        /// </summary>
        public decimal? PriceAdjustedFactor { get; private set; }

        /// <summary>
        /// Post trade holding of the Treasury or Trust in the security traded
        /// </summary>
        public int? TreasuryHolding { get; private set; }

        /// <summary>
        /// Empty contsructor required for <see cref="Slice.Get{T}()"/>
        /// </summary>
        public SmartInsiderTransaction()
        {
        }

        /// <summary>
        /// Creates an instance of the object by taking a formatted CSV line
        /// </summary>
        /// <param name="line">Line of formatted CSV</param>
        public SmartInsiderTransaction(string line) : base(line)
        {
            var csv = line.Split('\t');

            BuybackDate = string.IsNullOrWhiteSpace(csv[26]) ? (DateTime?)null : DateTime.ParseExact(csv[26], "yyyyMMdd", CultureInfo.InvariantCulture);
            BuybackVia = string.IsNullOrWhiteSpace(csv[27]) ? null : csv[27];
            BuybackBy = string.IsNullOrWhiteSpace(csv[28]) ? null : csv[28];
            HoldingType = string.IsNullOrWhiteSpace(csv[29]) ? null : csv[29];
            Currency = string.IsNullOrWhiteSpace(csv[30]) ? null : csv[30];
            Price = string.IsNullOrWhiteSpace(csv[31]) ? (decimal?)null : Convert.ToDecimal(csv[31], CultureInfo.InvariantCulture);
            TransactionAmount = string.IsNullOrWhiteSpace(csv[32]) ? (int?)null : Convert.ToInt32(csv[32], CultureInfo.InvariantCulture);
            GBPValue = string.IsNullOrWhiteSpace(csv[33]) ? (int?)null : Convert.ToInt32(csv[33], CultureInfo.InvariantCulture);
            EURValue = string.IsNullOrWhiteSpace(csv[34]) ? (int?)null : Convert.ToInt32(csv[34], CultureInfo.InvariantCulture);
            USDValue = string.IsNullOrWhiteSpace(csv[35]) ? (int?)null : Convert.ToInt32(csv[35], CultureInfo.InvariantCulture);
            NoteText = string.IsNullOrWhiteSpace(csv[36]) ? null : csv[36];
            BuybackPercentage = string.IsNullOrWhiteSpace(csv[37]) ? (decimal?)null : Convert.ToDecimal(csv[37], CultureInfo.InvariantCulture);
            VolumePercentage = string.IsNullOrWhiteSpace(csv[38]) ? (decimal?)null : Convert.ToDecimal(csv[38], CultureInfo.InvariantCulture);
            ConversionRate = string.IsNullOrWhiteSpace(csv[39]) ? (decimal?)null : Convert.ToDecimal(csv[39], CultureInfo.InvariantCulture);
            AmountAdjustedFactor = string.IsNullOrWhiteSpace(csv[40]) ? (decimal?)null : Convert.ToDecimal(csv[40], CultureInfo.InvariantCulture);
            PriceAdjustedFactor = string.IsNullOrWhiteSpace(csv[41]) ? (decimal?)null : Convert.ToDecimal(csv[41], CultureInfo.InvariantCulture);
            TreasuryHolding = string.IsNullOrWhiteSpace(csv[42]) ? (int?)null : Convert.ToInt32(csv[42], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Creates an instance of the object by taking a formatted CSV line
        /// </summary>
        /// <param name="line">Line of formatted CSV</param>
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

            BuybackDate = string.IsNullOrWhiteSpace(csv[20]) ? (DateTime?)null : DateTime.ParseExact(csv[20], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            BuybackVia = string.IsNullOrWhiteSpace(csv[21]) ? null : csv[21];
            BuybackBy = string.IsNullOrWhiteSpace(csv[22]) ? null : csv[22];
            HoldingType = string.IsNullOrWhiteSpace(csv[23]) ? null : csv[23];
            Currency = string.IsNullOrWhiteSpace(csv[24]) ? null : csv[24];
            Price = string.IsNullOrWhiteSpace(csv[25]) ? (decimal?)null : Convert.ToDecimal(csv[25], CultureInfo.InvariantCulture);
            TransactionAmount = string.IsNullOrWhiteSpace(csv[26]) ? (int?)null : Convert.ToInt32(csv[26], CultureInfo.InvariantCulture);
            GBPValue = string.IsNullOrWhiteSpace(csv[27]) ? (int?)null : Convert.ToInt32(csv[27], CultureInfo.InvariantCulture);
            EURValue = string.IsNullOrWhiteSpace(csv[28]) ? (int?)null : Convert.ToInt32(csv[28], CultureInfo.InvariantCulture);
            USDValue = string.IsNullOrWhiteSpace(csv[29]) ? (int?)null : Convert.ToInt32(csv[29], CultureInfo.InvariantCulture);
            NoteText = string.IsNullOrWhiteSpace(csv[30]) ? null : csv[30];
            BuybackPercentage = string.IsNullOrWhiteSpace(csv[31]) ? (decimal?)null : Convert.ToDecimal(csv[31], CultureInfo.InvariantCulture);
            VolumePercentage = string.IsNullOrWhiteSpace(csv[32]) ? (decimal?)null : Convert.ToDecimal(csv[32], CultureInfo.InvariantCulture);
            ConversionRate = string.IsNullOrWhiteSpace(csv[33]) ? (decimal?)null : Convert.ToDecimal(csv[33], CultureInfo.InvariantCulture);
            AmountAdjustedFactor = string.IsNullOrWhiteSpace(csv[34]) ? (decimal?)null : Convert.ToDecimal(csv[34], CultureInfo.InvariantCulture);
            PriceAdjustedFactor = string.IsNullOrWhiteSpace(csv[35]) ? (decimal?)null : Convert.ToDecimal(csv[35], CultureInfo.InvariantCulture);
            TreasuryHolding = string.IsNullOrWhiteSpace(csv[36]) ? (int?)null : Convert.ToInt32(csv[36], CultureInfo.InvariantCulture);

            AnnouncementDate = string.IsNullOrWhiteSpace(csv[37]) ? (DateTime?)null : DateTime.ParseExact(csv[37], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            TimeReleased = string.IsNullOrWhiteSpace(csv[38]) ? (DateTime?)null : DateTime.ParseExact(csv[38].Replace(" ", "").Trim(), "dd/MM/yyyyHH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessed = string.IsNullOrWhiteSpace(csv[39]) ? (DateTime?)null : DateTime.ParseExact(csv[39].Replace(" ", "").Trim(), "dd/MM/yyyyHH:mm:ss", CultureInfo.InvariantCulture);
            TimeReleasedUtc = string.IsNullOrWhiteSpace(csv[40]) ? (DateTime?)null : DateTime.ParseExact(csv[40].Replace(" ", "").Trim(), "dd/MM/yyyyHH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessedUtc = string.IsNullOrWhiteSpace(csv[41]) ? (DateTime?)null : DateTime.ParseExact(csv[41].Replace(" ", "").Trim(), "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);
            AnnouncedIn = string.IsNullOrWhiteSpace(csv[42]) ? null : csv[42];
        }

        /// <summary>
        /// Specifies the location of the data and directs LEAN where to load the data from
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="date">Date</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>Subscription data source object pointing LEAN to the data location</returns>
        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            return new SubscriptionDataSource(
                Path.Combine(
                    Globals.DataFolder,
                    "alternative",
                    "smartinsider",
                    "transactions",
                    $"{config.Symbol.Value.ToLower()}.csv"
                ),
                SubscriptionTransportMedium.LocalFile,
                FileFormat.Csv
            );
        }

        /// <summary>
        /// Reads the data into LEAN for use in algorithms
        /// </summary>
        /// <param name="config">Subscription configuration</param>
        /// <param name="line">Line of CSV</param>
        /// <param name="date">Algorithm date</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>Instance of the object</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var transaction = new SmartInsiderTransaction(line)
            {
                Symbol = config.Symbol
            };
            transaction.Time = transaction.Time.AddHours(17).ConvertTo(TimeZones.London, config.DataTimeZone);

            return transaction;
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
            return new SmartInsiderTransaction
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

                BuybackDate = BuybackDate,
                BuybackVia = BuybackVia,
                BuybackBy = BuybackBy,
                HoldingType = HoldingType,
                Currency = Currency,
                Price = Price,
                TransactionAmount = TransactionAmount,
                GBPValue = GBPValue,
                EURValue = EURValue,
                USDValue = USDValue,
                NoteText = NoteText,
                BuybackPercentage = BuybackPercentage,
                VolumePercentage = VolumePercentage,
                ConversionRate = ConversionRate,
                AmountAdjustedFactor = AmountAdjustedFactor,
                PriceAdjustedFactor = PriceAdjustedFactor,
                TreasuryHolding = TreasuryHolding,

                Symbol = Symbol,
                Value = Value,
                Time = Time
            };
        }

        /// <summary>
        /// Converts the data to CSV
        /// </summary>
        /// <returns>String of CSV</returns>
        /// <remarks>Parsable by the constructor should you need to recreate the object from CSV</remarks>
        public override string ToCsv()
        {
            return $"{TransactionID}\t{BuybackType}\t{LastUpdate:yyyyMMdd}\t{LastIDsUpdate:yyyyMMdd}\t{ISIN}\t{USDMarketCap}\t{CompanyID}\t{ICBIndustry}\t{ICBSuperSector}\t{ICBSector}\t{ICBSubSector}\t{ICBCode}\t{CompanyName}\t{PreviousResultsAnnouncementDate:yyyyMMdd}\t{NextResultsAnnouncementsDate:yyyyMMdd}\t{NextCloseBegin:yyyyMMdd}\t{LastCloseEnded:yyyyMMdd}\t{SecurityDescription}\t{TickerCountry}\t{TickerSymbol}\t{AnnouncementDate:yyyyMMdd}\t{TimeReleased:yyyyMMdd HH:mm:ss}\t{TimeProcessed:yyyyMMdd HH:mm:ss}\t{TimeReleasedUtc:yyyyMMdd HH:mm:ss}\t{TimeProcessedUtc:yyyyMMdd HH:mm:ss}\t{AnnouncedIn}\t{BuybackDate:yyyyMMdd}\t{BuybackVia}\t{BuybackBy}\t{HoldingType}\t{Currency}\t{Price}\t{TransactionAmount}\t{GBPValue}\t{EURValue}\t{USDValue}\t{NoteText}\t{BuybackPercentage}\t{VolumePercentage}\t{ConversionRate}\t{AmountAdjustedFactor}\t{PriceAdjustedFactor}\t{TreasuryHolding}";
        }

        /// <summary>
        /// Determines equality to another SmartInsiderTransaction instance
        /// </summary>
        /// <param name="other">Another SmartInsiderTransaction instance</param>
        /// <returns>Boolean value indicating equality</returns>
        public override bool Equals(SmartInsiderEvent other)
        {
            var otherTransaction = other as SmartInsiderTransaction;
            if (otherTransaction == null)
            {
                return false;
            }

            return otherTransaction.TransactionID == TransactionID &&
                otherTransaction.BuybackType == BuybackType &&
                otherTransaction.LastUpdate == LastUpdate &&
                otherTransaction.LastIDsUpdate == LastIDsUpdate &&
                otherTransaction.ISIN == ISIN &&
                otherTransaction.USDMarketCap == USDMarketCap &&
                otherTransaction.CompanyID == CompanyID &&
                otherTransaction.ICBIndustry == ICBIndustry &&
                otherTransaction.ICBSuperSector == ICBSuperSector &&
                otherTransaction.ICBSector == ICBSector &&
                otherTransaction.ICBSubSector == ICBSubSector &&
                otherTransaction.ICBCode == ICBCode &&
                otherTransaction.CompanyName == CompanyName &&
                otherTransaction.PreviousResultsAnnouncementDate == PreviousResultsAnnouncementDate &&
                otherTransaction.NextResultsAnnouncementsDate == NextResultsAnnouncementsDate &&
                otherTransaction.NextCloseBegin == NextCloseBegin &&
                otherTransaction.LastCloseEnded == LastCloseEnded &&
                otherTransaction.SecurityDescription == SecurityDescription &&
                otherTransaction.TickerCountry == TickerCountry &&
                otherTransaction.TickerSymbol == TickerSymbol &&
                otherTransaction.AnnouncementDate == AnnouncementDate &&
                otherTransaction.TimeReleased == TimeReleased &&
                otherTransaction.TimeProcessed == TimeProcessed &&
                otherTransaction.TimeReleasedUtc == TimeReleasedUtc &&
                otherTransaction.TimeProcessedUtc == TimeProcessedUtc &&
                otherTransaction.AnnouncedIn == AnnouncedIn &&

                otherTransaction.BuybackDate == BuybackDate &&
                otherTransaction.BuybackVia == BuybackVia &&
                otherTransaction.BuybackBy == BuybackBy &&
                otherTransaction.HoldingType == HoldingType &&
                otherTransaction.Currency == Currency &&
                otherTransaction.Price == Price &&
                otherTransaction.TransactionAmount == TransactionAmount &&
                otherTransaction.GBPValue == GBPValue &&
                otherTransaction.EURValue == EURValue &&
                otherTransaction.USDValue == USDValue &&
                otherTransaction.NoteText == NoteText &&
                otherTransaction.BuybackPercentage == BuybackPercentage &&
                otherTransaction.VolumePercentage == VolumePercentage &&
                otherTransaction.ConversionRate == ConversionRate &&
                otherTransaction.AmountAdjustedFactor == AmountAdjustedFactor &&
                otherTransaction.PriceAdjustedFactor == PriceAdjustedFactor &&
                otherTransaction.TreasuryHolding == TreasuryHolding;
        }
    }
}