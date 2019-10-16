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

using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;

namespace QuantConnect.Data.Custom.SmartInsider
{
    /// <summary>
    /// Smart Insider Transaction - Execution of a stock buyback and details about the event occurred
    /// </summary>
    public class SmartInsiderTransaction : SmartInsiderEvent
    {
        /// <summary>
        /// Date traded through the market
        /// </summary>
        public DateTime? BuybackDate { get; set; }

        /// <summary>
        /// Describes how transaction was executed
        /// </summary>
        public SmartInsiderExecution? Execution { get; set; }

        /// <summary>
        /// Describes which entity carried out the transaction
        /// </summary>
        public SmartInsiderExecutionEntity? ExecutionEntity { get; set; }

        /// <summary>
        /// Describes what will be done with those shares following repurchase
        /// </summary>
        public SmartInsiderExecutionHolding? ExecutionHolding { get; set; }

        /// <summary>
        /// Currency of transation (ISO Code)
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Denominated in Currency of Transaction
        /// </summary>
        public decimal? ExecutionPrice { get; set; }

        /// <summary>
        /// Number of shares traded
        /// </summary>
        public decimal? Amount { get; set; }

        /// <summary>
        /// Currency conversion rates are updated daily and values are calculated at rate prevailing on the trade date
        /// </summary>
        public decimal? GBPValue { get; set; }

        /// <summary>
        /// Currency conversion rates are updated daily and values are calculated at rate prevailing on the trade date
        /// </summary>
        public decimal? EURValue { get; set; }

        /// <summary>
        /// Currency conversion rates are updated daily and values are calculated at rate prevailing on the trade date
        /// </summary>
        public decimal? USDValue { get; set; }

        /// <summary>
        /// Free text which expains futher details about the trade
        /// </summary>
        public string NoteText { get; set; }

        /// <summary>
        /// Percentage of value of the trade as part of the issuers total Market Cap
        /// </summary>
        public decimal? BuybackPercentage { get; set; }

        /// <summary>
        /// Percentage of the volume traded on the day of the buyback.
        /// </summary>
        public decimal? VolumePercentage { get; set; }

        /// <summary>
        /// Rate used to calculate 'Value (GBP)' from 'Price' multiplied by 'Amount'. Will be 1 where Currency is also 'GBP'
        /// </summary>
        public decimal? ConversionRate { get; set; }

        /// <summary>
        /// Multiplier which can be applied to 'Amount' field to account for subsequent corporate action
        /// </summary>
        public decimal? AmountAdjustedFactor { get; set; }

        /// <summary>
        /// Multiplier which can be applied to 'Price' and 'LastClose' fields to account for subsequent corporate actions
        /// </summary>
        public decimal? PriceAdjustedFactor { get; set; }

        /// <summary>
        /// Post trade holding of the Treasury or Trust in the security traded
        /// </summary>
        public int? TreasuryHolding { get; set; }

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
            var tsv = line.Split('\t');

            BuybackDate = string.IsNullOrWhiteSpace(tsv[26]) ? (DateTime?)null : DateTime.ParseExact(tsv[26], "yyyyMMdd", CultureInfo.InvariantCulture);
            Execution = string.IsNullOrWhiteSpace(tsv[27]) ? (SmartInsiderExecution?)null : JsonConvert.DeserializeObject<SmartInsiderExecution>($"\"{tsv[27]}\"");
            ExecutionEntity = string.IsNullOrWhiteSpace(tsv[28]) ? (SmartInsiderExecutionEntity?)null : JsonConvert.DeserializeObject<SmartInsiderExecutionEntity>($"\"{tsv[28]}\"");
            ExecutionHolding = string.IsNullOrWhiteSpace(tsv[29]) ? (SmartInsiderExecutionHolding?)null : JsonConvert.DeserializeObject<SmartInsiderExecutionHolding>($"\"{tsv[29]}\"");
            ExecutionHolding = ExecutionHolding == SmartInsiderExecutionHolding.Error ? SmartInsiderExecutionHolding.SatisfyStockVesting : ExecutionHolding;
            Currency = string.IsNullOrWhiteSpace(tsv[30]) ? null : tsv[30];
            ExecutionPrice = string.IsNullOrWhiteSpace(tsv[31]) ? (decimal?)null : Convert.ToDecimal(tsv[31], CultureInfo.InvariantCulture);
            Amount = string.IsNullOrWhiteSpace(tsv[32]) ? (decimal?)null : Convert.ToDecimal(tsv[32], CultureInfo.InvariantCulture);
            GBPValue = string.IsNullOrWhiteSpace(tsv[33]) ? (decimal?)null : Convert.ToDecimal(tsv[33], CultureInfo.InvariantCulture);
            EURValue = string.IsNullOrWhiteSpace(tsv[34]) ? (decimal?)null : Convert.ToDecimal(tsv[34], CultureInfo.InvariantCulture);
            USDValue = string.IsNullOrWhiteSpace(tsv[35]) ? (decimal?)null : Convert.ToDecimal(tsv[35], CultureInfo.InvariantCulture);
            NoteText = string.IsNullOrWhiteSpace(tsv[36]) ? null : tsv[36];
            BuybackPercentage = string.IsNullOrWhiteSpace(tsv[37]) ? (decimal?)null : Convert.ToDecimal(tsv[37], CultureInfo.InvariantCulture);
            VolumePercentage = string.IsNullOrWhiteSpace(tsv[38]) ? (decimal?)null : Convert.ToDecimal(tsv[38], CultureInfo.InvariantCulture);
            ConversionRate = string.IsNullOrWhiteSpace(tsv[39]) ? (decimal?)null : Convert.ToDecimal(tsv[39], CultureInfo.InvariantCulture);
            AmountAdjustedFactor = string.IsNullOrWhiteSpace(tsv[40]) ? (decimal?)null : Convert.ToDecimal(tsv[40], CultureInfo.InvariantCulture);
            PriceAdjustedFactor = string.IsNullOrWhiteSpace(tsv[41]) ? (decimal?)null : Convert.ToDecimal(tsv[41], CultureInfo.InvariantCulture);
            TreasuryHolding = string.IsNullOrWhiteSpace(tsv[42]) ? (int?)null : Convert.ToInt32(tsv[42], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Creates an instance of the object by taking a formatted CSV line
        /// </summary>
        /// <param name="line">Line of formatted CSV</param>
        public override void FromRawData(string line)
        {
            var tsv = line.Split('\t');

            TransactionID = string.IsNullOrWhiteSpace(tsv[0]) ? null : tsv[0];
            EventType = string.IsNullOrWhiteSpace(tsv[1]) ? (SmartInsiderEventType?)null : JsonConvert.DeserializeObject<SmartInsiderEventType>($"\"{tsv[1]}\"");
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

            BuybackDate = string.IsNullOrWhiteSpace(tsv[20]) ? (DateTime?)null : DateTime.ParseExact(tsv[20], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            Execution = string.IsNullOrWhiteSpace(tsv[21]) ? (SmartInsiderExecution?)null : JsonConvert.DeserializeObject<SmartInsiderExecution>($"\"{tsv[21]}\"");
            ExecutionEntity = string.IsNullOrWhiteSpace(tsv[22]) ? (SmartInsiderExecutionEntity?)null : JsonConvert.DeserializeObject<SmartInsiderExecutionEntity>($"\"{tsv[22]}\"");
            ExecutionHolding = string.IsNullOrWhiteSpace(tsv[23]) ? (SmartInsiderExecutionHolding?)null : JsonConvert.DeserializeObject<SmartInsiderExecutionHolding>($"\"{tsv[23]}\"");
            ExecutionHolding = ExecutionHolding == SmartInsiderExecutionHolding.Error ? SmartInsiderExecutionHolding.SatisfyStockVesting : ExecutionHolding;
            Currency = string.IsNullOrWhiteSpace(tsv[24]) ? null : tsv[24];
            ExecutionPrice = string.IsNullOrWhiteSpace(tsv[25]) ? (decimal?)null : Convert.ToDecimal(tsv[25], CultureInfo.InvariantCulture);
            Amount = string.IsNullOrWhiteSpace(tsv[26]) ? (decimal?)null : Convert.ToDecimal(tsv[26], CultureInfo.InvariantCulture);
            GBPValue = string.IsNullOrWhiteSpace(tsv[27]) ? (decimal?)null : Convert.ToDecimal(tsv[27], CultureInfo.InvariantCulture);
            EURValue = string.IsNullOrWhiteSpace(tsv[28]) ? (decimal?)null : Convert.ToDecimal(tsv[28], CultureInfo.InvariantCulture);
            USDValue = string.IsNullOrWhiteSpace(tsv[29]) ? (decimal?)null : Convert.ToDecimal(tsv[29], CultureInfo.InvariantCulture);
            NoteText = string.IsNullOrWhiteSpace(tsv[30]) ? null : tsv[30];
            BuybackPercentage = string.IsNullOrWhiteSpace(tsv[31]) ? (decimal?)null : Convert.ToDecimal(tsv[31], CultureInfo.InvariantCulture);
            VolumePercentage = string.IsNullOrWhiteSpace(tsv[32]) ? (decimal?)null : Convert.ToDecimal(tsv[32], CultureInfo.InvariantCulture);
            ConversionRate = string.IsNullOrWhiteSpace(tsv[33]) ? (decimal?)null : Convert.ToDecimal(tsv[33], CultureInfo.InvariantCulture);
            AmountAdjustedFactor = string.IsNullOrWhiteSpace(tsv[34]) ? (decimal?)null : Convert.ToDecimal(tsv[34], CultureInfo.InvariantCulture);
            PriceAdjustedFactor = string.IsNullOrWhiteSpace(tsv[35]) ? (decimal?)null : Convert.ToDecimal(tsv[35], CultureInfo.InvariantCulture);
            TreasuryHolding = string.IsNullOrWhiteSpace(tsv[36]) ? (int?)null : Convert.ToInt32(tsv[36], CultureInfo.InvariantCulture);

            AnnouncementDate = string.IsNullOrWhiteSpace(tsv[37]) ? (DateTime?)null : DateTime.ParseExact(tsv[37], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            TimeReleased = string.IsNullOrWhiteSpace(tsv[38]) ? (DateTime?)null : DateTime.ParseExact(tsv[38].Replace(" ", "").Trim(), "dd/MM/yyyyHH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessed = string.IsNullOrWhiteSpace(tsv[39]) ? (DateTime?)null : DateTime.ParseExact(tsv[39].Replace(" ", "").Trim(), "dd/MM/yyyyHH:mm:ss", CultureInfo.InvariantCulture);
            TimeReleasedUtc = string.IsNullOrWhiteSpace(tsv[40]) ? (DateTime?)null : DateTime.ParseExact(tsv[40].Replace(" ", "").Trim(), "dd/MM/yyyyHH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessedUtc = string.IsNullOrWhiteSpace(tsv[41]) ? (DateTime?)null : DateTime.ParseExact(tsv[41].Replace(" ", "").Trim(), "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);
            AnnouncedIn = string.IsNullOrWhiteSpace(tsv[42]) ? null : tsv[42];
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
                    $"{config.Symbol.Value.ToLowerInvariant()}.tsv"
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
            // Files are made available at the earliest @ 17:00 U.K. time
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
                EventType = EventType,
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
                Execution = Execution,
                ExecutionEntity = ExecutionEntity,
                ExecutionHolding = ExecutionHolding,
                Currency = Currency,
                ExecutionPrice = ExecutionPrice,
                Amount = Amount,
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
        public override string ToLine()
        {
            return string.Join("\t",
                TransactionID,
                JsonConvert.SerializeObject(EventType).Replace("\"", ""),
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
                BuybackDate?.ToStringInvariant("yyyyMMdd"),
                JsonConvert.SerializeObject(Execution).Replace("\"", ""),
                JsonConvert.SerializeObject(ExecutionEntity).Replace("\"", ""),
                JsonConvert.SerializeObject(ExecutionHolding).Replace("\"", ""),
                Currency,
                ExecutionPrice,
                Amount,
                GBPValue,
                EURValue,
                USDValue,
                NoteText,
                BuybackPercentage,
                VolumePercentage,
                ConversionRate,
                AmountAdjustedFactor,
                PriceAdjustedFactor,
                TreasuryHolding);
        }
    }
}