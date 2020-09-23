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
using QuantConnect.Logging;

namespace QuantConnect.Data.Custom.SmartInsider
{
    /// <summary>
    /// Smart Insider Intentions - Intention to execute a stock buyback and details about the future event
    /// </summary>
    public class SmartInsiderIntention : SmartInsiderEvent
    {
        /// <summary>
        /// Describes how the transaction was executed
        /// </summary>
        public SmartInsiderExecution? Execution { get; set; }

        /// <summary>
        /// Describes which entity intends to execute the transaction
        /// </summary>
        public SmartInsiderExecutionEntity? ExecutionEntity { get; set; }

        /// <summary>
        /// Describes what will be done with those shares following repurchase
        /// </summary>
        public SmartInsiderExecutionHolding? ExecutionHolding { get; set; }

        /// <summary>
        /// Number of shares to be or authorised to be traded
        /// </summary>
        public int? Amount { get; set; }

        /// <summary>
        /// Currency of the value of shares to be/Authorised to be traded (ISO Code)
        /// </summary>
        public string ValueCurrency { get; set; }

        /// <summary>
        /// Value of shares to be authorised to be traded
        /// </summary>
        public long? AmountValue { get; set; }

        /// <summary>
        /// Percentage of oustanding shares to be authorised to be traded
        /// </summary>
        public decimal? Percentage { get; set; }

        /// <summary>
        /// start of the period the intention/authorisation applies to
        /// </summary>
        public DateTime? AuthorizationStartDate { get; set; }

        /// <summary>
        /// End of the period the intention/authorisation applies to
        /// </summary>
        public DateTime? AuthorizationEndDate { get; set; }

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
        public string NoteText { get; set; }

        /// <summary>
        /// Empty constructor required for <see cref="Slice.Get{T}()"/>
        /// </summary>
        public SmartInsiderIntention()
        {
        }

        /// <summary>
        /// Constructs instance of this via a *formatted* TSV line (tab delimited)
        /// </summary>
        /// <param name="line">Line of formatted TSV data</param>
        public SmartInsiderIntention(string line) : base(line)
        {
            var tsv = line.Split('\t');
            Execution = string.IsNullOrWhiteSpace(tsv[26]) ? (SmartInsiderExecution?)null : JsonConvert.DeserializeObject<SmartInsiderExecution>($"\"{tsv[26]}\"");
            ExecutionEntity = string.IsNullOrWhiteSpace(tsv[27]) ? (SmartInsiderExecutionEntity?)null : JsonConvert.DeserializeObject<SmartInsiderExecutionEntity>($"\"{tsv[27]}\"");
            ExecutionHolding = string.IsNullOrWhiteSpace(tsv[28]) ? (SmartInsiderExecutionHolding?)null : JsonConvert.DeserializeObject<SmartInsiderExecutionHolding>($"\"{tsv[28]}\"");
            ExecutionHolding = ExecutionHolding == SmartInsiderExecutionHolding.Error ? SmartInsiderExecutionHolding.SatisfyStockVesting : ExecutionHolding;
            Amount = string.IsNullOrWhiteSpace(tsv[29]) ? (int?)null : Convert.ToInt32(tsv[29], CultureInfo.InvariantCulture);
            ValueCurrency = string.IsNullOrWhiteSpace(tsv[30]) ? null : tsv[30];
            AmountValue = string.IsNullOrWhiteSpace(tsv[31]) ? (long?)null : Convert.ToInt64(tsv[31], CultureInfo.InvariantCulture);
            Percentage = string.IsNullOrWhiteSpace(tsv[32]) ? (decimal?)null : Convert.ToDecimal(tsv[32], CultureInfo.InvariantCulture);
            AuthorizationStartDate = string.IsNullOrWhiteSpace(tsv[33]) ? (DateTime?)null : DateTime.ParseExact(tsv[33], "yyyyMMdd", CultureInfo.InvariantCulture);
            AuthorizationEndDate = string.IsNullOrWhiteSpace(tsv[34]) ? (DateTime?)null : DateTime.ParseExact(tsv[34], "yyyyMMdd", CultureInfo.InvariantCulture);
            PriceCurrency = string.IsNullOrWhiteSpace(tsv[35]) ? null : tsv[35];
            MinimumPrice = string.IsNullOrWhiteSpace(tsv[36]) ? (decimal?)null : Convert.ToDecimal(tsv[36], CultureInfo.InvariantCulture);
            MaximumPrice = string.IsNullOrWhiteSpace(tsv[37]) ? (decimal?)null : Convert.ToDecimal(tsv[37], CultureInfo.InvariantCulture);
            NoteText = tsv.Length == 39? (string.IsNullOrWhiteSpace(tsv[38]) ? null : tsv[38]) : null;
        }

        /// <summary>
        /// Constructs a new instance from unformatted TSV data
        /// </summary>
        /// <param name="line">Line of raw TSV (raw with fields 46, 36, 14, 7 removed in descending order)</param>
        /// <returns>Instance of the object</returns>
        public override void FromRawData(string line)
        {
            var tsv = line.Split('\t');

            TransactionID = string.IsNullOrWhiteSpace(tsv[0]) ? null : tsv[0];

            EventType = SmartInsiderEventType.NotSpecified;
            if (!string.IsNullOrWhiteSpace(tsv[1]))
            {
                try
                {
                    EventType = JsonConvert.DeserializeObject<SmartInsiderEventType>($"\"{tsv[1]}\"");
                }
                catch (JsonSerializationException)
                {
                    Log.Error($"SmartInsiderIntention.FromRawData: New unexpected entry found {tsv[1]}. Parsed as NotSpecified.");
                }
            }

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
            TimeReleased = string.IsNullOrWhiteSpace(tsv[38]) ? (DateTime?)null : ParseDate(tsv[38]);
            TimeProcessed = string.IsNullOrWhiteSpace(tsv[39]) ? (DateTime?)null : ParseDate(tsv[39]);
            TimeReleasedUtc = string.IsNullOrWhiteSpace(tsv[40]) ? (DateTime?)null : ParseDate(tsv[40]);
            TimeProcessedUtc = string.IsNullOrWhiteSpace(tsv[41]) ? (DateTime?)null : ParseDate(tsv[41]);
            AnnouncedIn = string.IsNullOrWhiteSpace(tsv[42]) ? null : tsv[42];

            Execution = string.IsNullOrWhiteSpace(tsv[43]) ? (SmartInsiderExecution?)null : JsonConvert.DeserializeObject<SmartInsiderExecution>($"\"{tsv[43]}\"");
            ExecutionEntity = string.IsNullOrWhiteSpace(tsv[44]) ? (SmartInsiderExecutionEntity?)null : JsonConvert.DeserializeObject<SmartInsiderExecutionEntity>($"\"{tsv[44]}\"");
            ExecutionHolding = string.IsNullOrWhiteSpace(tsv[45]) ? (SmartInsiderExecutionHolding?)null : JsonConvert.DeserializeObject<SmartInsiderExecutionHolding>($"\"{tsv[45]}\"");
            ExecutionHolding = ExecutionHolding == SmartInsiderExecutionHolding.Error ? SmartInsiderExecutionHolding.SatisfyStockVesting : ExecutionHolding;
            Amount = string.IsNullOrWhiteSpace(tsv[46]) ? (int?)null : Convert.ToInt32(tsv[46], CultureInfo.InvariantCulture);
            ValueCurrency = string.IsNullOrWhiteSpace(tsv[47]) ? null : tsv[47];
            AmountValue = string.IsNullOrWhiteSpace(tsv[48]) ? (long?)null : Convert.ToInt64(tsv[48], CultureInfo.InvariantCulture);
            Percentage = string.IsNullOrWhiteSpace(tsv[49]) ? (decimal?)null : Convert.ToDecimal(tsv[49], CultureInfo.InvariantCulture);
            AuthorizationStartDate = string.IsNullOrWhiteSpace(tsv[50]) ? (DateTime?)null : DateTime.ParseExact(tsv[50], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            AuthorizationEndDate = string.IsNullOrWhiteSpace(tsv[51]) ? (DateTime?)null : DateTime.ParseExact(tsv[51], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            PriceCurrency = string.IsNullOrWhiteSpace(tsv[52]) ? null : tsv[52];
            MinimumPrice = string.IsNullOrWhiteSpace(tsv[53]) ? (decimal?)null : Convert.ToDecimal(tsv[53], CultureInfo.InvariantCulture);
            MaximumPrice = string.IsNullOrWhiteSpace(tsv[54]) ? (decimal?)null : Convert.ToDecimal(tsv[54], CultureInfo.InvariantCulture);
            NoteText = tsv.Length == 56 ? (string.IsNullOrWhiteSpace(tsv[55]) ? null : tsv[55]) : null;
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
        /// <param name="line">TSV line</param>
        /// <param name="date">Algorithm date</param>
        /// <param name="isLiveMode">Is live mode</param>
        /// <returns>Instance of the object</returns>
        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            return new SmartInsiderIntention(line)
            {
                Symbol = config.Symbol
            };
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

                Execution = Execution,
                ExecutionEntity = ExecutionEntity,
                ExecutionHolding = ExecutionHolding,
                Amount = Amount,
                ValueCurrency = ValueCurrency,
                AmountValue = AmountValue,
                Percentage = Percentage,
                AuthorizationStartDate = AuthorizationStartDate,
                AuthorizationEndDate = AuthorizationEndDate,
                PriceCurrency = PriceCurrency,
                MinimumPrice = MinimumPrice,
                MaximumPrice = MaximumPrice,
                NoteText = NoteText,

                Symbol = Symbol,
                Value = Value,
                Time = Time,
            };
        }

        /// <summary>
        /// Converts the data to TSV
        /// </summary>
        /// <returns>String of TSV</returns>
        /// <remarks>Parsable by the constructor should you need to recreate the object from TSV</remarks>
        public override string ToLine()
        {
            return string.Join("\t",
                TimeProcessedUtc?.ToStringInvariant("yyyyMMdd HH:mm:ss"),
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
                AnnouncedIn,
                JsonConvert.SerializeObject(Execution).Replace("\"", ""),
                JsonConvert.SerializeObject(ExecutionEntity).Replace("\"", ""),
                JsonConvert.SerializeObject(ExecutionHolding).Replace("\"", ""),
                Amount,
                ValueCurrency,
                AmountValue,
                Percentage,
                AuthorizationStartDate?.ToStringInvariant("yyyyMMdd"),
                AuthorizationEndDate?.ToStringInvariant("yyyyMMdd"),
                PriceCurrency,
                MinimumPrice,
                MaximumPrice,
                NoteText);
        }
    }
}
