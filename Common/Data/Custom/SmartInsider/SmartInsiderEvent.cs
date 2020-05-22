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
using NodaTime;
using System;
using System.Globalization;

namespace QuantConnect.Data.Custom.SmartInsider
{
    /// <summary>
    /// SmartInsider Intention and Transaction events. These are fields
    /// that are shared between intentions and transactions.
    /// </summary>
    public abstract class SmartInsiderEvent : BaseData
    {
        /// <summary>
        /// Proprietary unique field. Not nullable
        /// </summary>
        public string TransactionID { get; set; }

        /// <summary>
        /// Description of what has or will take place in an execution
        /// </summary>
        public SmartInsiderEventType? EventType { get; set; }

        /// <summary>
        /// The date when a transaction is updated after it has been reported. Not nullable
        /// </summary>
        public DateTime LastUpdate { get; set; }

        // All fields below are nullable values

        /// <summary>
        /// Date that company identifiers were changed. Can be a name, Ticker Symbol or ISIN change
        /// </summary>
        public DateTime? LastIDsUpdate { get; set; }

        /// <summary>
        /// Industry classification number
        /// </summary>
        public string ISIN { get; set; }

        /// <summary>
        /// The market capitalization at the time of the transaction stated in US Dollars
        /// </summary>
        public decimal? USDMarketCap { get; set; }

        /// <summary>
        /// Smart Insider proprietary identifier for the company
        /// </summary>
        public int? CompanyID { get; set; }

        /// <summary>
        /// FTSE Russell Sector Classification
        /// </summary>
        public string ICBIndustry { get; set; }

        /// <summary>
        /// FTSE Russell Sector Classification
        /// </summary>
        public string ICBSuperSector { get; set; }

        /// <summary>
        /// FTSE Russell Sector Classification
        /// </summary>
        public string ICBSector { get; set; }

        /// <summary>
        /// FTSE Russell Sector Classification
        /// </summary>
        public string ICBSubSector { get; set; }

        /// <summary>
        /// Numeric code that is the most granular level in ICB classification
        /// </summary>
        public int? ICBCode { get; set; }

        /// <summary>
        /// Company name. PLC is always excluded
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Announcement date of last results, this will be the end date of the last "Close Period"
        /// </summary>
        public DateTime? PreviousResultsAnnouncementDate { get; set; }

        /// <summary>
        /// Announcement date of next results, this will be the end date of the next "Close Period"
        /// </summary>
        public DateTime? NextResultsAnnouncementsDate { get; set; }

        /// <summary>
        /// Start date of next trading embargo ahead of scheduled results announcment
        /// </summary>
        public DateTime? NextCloseBegin { get; set; }

        /// <summary>
        /// Date trading embargo (Close Period) is lifted as results are made public
        /// </summary>
        public DateTime? LastCloseEnded { get; set; }

        /// <summary>
        /// Type of security. Does not contain nominal value
        /// </summary>
        public string SecurityDescription { get; set; }

        /// <summary>
        /// Country of local identifier, denoting where the trade took place
        /// </summary>
        public string TickerCountry { get; set; }

        /// <summary>
        /// Local market identifier
        /// </summary>
        public string TickerSymbol { get; set; }

        /// <summary>
        /// Date Transaction was entered onto our system. Where a transaction is after the London market close (usually 4.30pm) this will be stated as the next day
        /// </summary>
        public DateTime? AnnouncementDate { get; set; }

        /// <summary>
        /// Time the announcement first appeared on a Regulatory News Service or other disclosure system and became available to the market, time stated is local market time
        /// </summary>
        public DateTime? TimeReleased { get; set; }

        /// <summary>
        /// Time the transaction was entered into Smart Insider systems and appeared on their website, time stated is local to London, UK
        /// </summary>
        public DateTime? TimeProcessed { get; set; }

        /// <summary>
        /// Time the announcement first appeared on a Regulatory News Service or other disclosure system and became available to the market. Time stated is GMT standard
        /// </summary>
        public DateTime? TimeReleasedUtc { get; set; }

        /// <summary>
        /// Time the transaction was entered onto our systems and appeared on our website. Time stated is GMT standard
        /// </summary>
        public DateTime? TimeProcessedUtc { get; set; }

        /// <summary>
        /// Market in which the transaction was announced, this can reference more than one country
        /// </summary>
        public string AnnouncedIn { get; set; }

        #region Reserved Fields
        /*
         * These fields are provided for future proofing of the data provided by Smart Insider
         * public string UserField61 { get; set; }
         * public string UserField62 { get; set; }
         * public string UserField63 { get; set; }
         * public string UserField64 { get; set; }
         * public string UserField65 { get; set; }
         * public string SystemField66 { get; set; }
         * public string SystemField67 { get; set; }
         * public string SystemField68 { get; set; }
         * public string SystemField69 { get; set; }
         * public string SystemField70 { get; set; }
         * public decimal? UserField71 { get; set; }
         * public decimal? UserField72 { get; set; }
         * public decimal? UserField73 { get; set; }
         * public decimal? UserField74 { get; set; }
         * public decimal? UserField75 { get; set; }
         * public decimal? SystemField76 { get; set; }
         * public decimal? SystemField77 { get; set; }
         * public decimal? SystemField78 { get; set; }
         * public decimal? SystemField79 { get; set; }
         * public decimal? SystemField80 { get; set; }
         * public int? Field81 { get; set; }
         * public int? Field82 { get; set; }
         * public int? Field83 { get; set; }
         * public int? Field84 { get; set; }
         * public int? Field85 { get; set; }
         * public int? Field86 { get; set; }
         * public int? Field87 { get; set; }
         * public int? Field88 { get; set; }
         * public int? Field89 { get; set; }
         * public int? Field90 { get; set; }
         * public DateTime? Field91 { get; set; }
         * public DateTime? Field92 { get; set; }
         * public DateTime? Field93 { get; set; }
         * public DateTime? Field94 { get; set; }
         * public DateTime? Field95 { get; set; }
         * public DateTime? Field96 { get; set; }
         * public DateTime? Field97 { get; set; }
         * public DateTime? Field98 { get; set; }
         * public DateTime? Field99 { get; set; }
         * public DateTime? Field100 { get; set; }
         */

        #endregion

        /// <summary>
        /// Empty constructor required for cloning
        /// </summary>
        public SmartInsiderEvent()
        {
        }

        /// <summary>
        /// Parses a line of TSV (tab delimited) from Smart Insider data
        /// </summary>
        /// <param name="tsvLine">Tab delimited line of data</param>
        public SmartInsiderEvent(string tsvLine)
        {
            var tsv = tsvLine.Split('\t');

            TimeProcessedUtc = string.IsNullOrWhiteSpace(tsv[0]) ? (DateTime?)null : DateTime.ParseExact(tsv[0], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            TransactionID = tsv[1];
            EventType = string.IsNullOrWhiteSpace(tsv[2]) ? (SmartInsiderEventType?)null : JsonConvert.DeserializeObject<SmartInsiderEventType>($"\"{tsv[2]}\"");
            LastUpdate = DateTime.ParseExact(tsv[3], "yyyyMMdd", CultureInfo.InvariantCulture);
            LastIDsUpdate = string.IsNullOrWhiteSpace(tsv[4]) ? (DateTime?)null : DateTime.ParseExact(tsv[4], "yyyyMMdd", CultureInfo.InvariantCulture);
            ISIN = string.IsNullOrWhiteSpace(tsv[5]) ? null : tsv[5];
            USDMarketCap = string.IsNullOrWhiteSpace(tsv[6]) ? (decimal?)null : Convert.ToDecimal(tsv[6], CultureInfo.InvariantCulture);
            CompanyID = string.IsNullOrWhiteSpace(tsv[7]) ? (int?)null : Convert.ToInt32(tsv[7], CultureInfo.InvariantCulture);
            ICBIndustry = string.IsNullOrWhiteSpace(tsv[8]) ? null : tsv[8];
            ICBSuperSector = string.IsNullOrWhiteSpace(tsv[9]) ? null : tsv[9];
            ICBSector = string.IsNullOrWhiteSpace(tsv[10]) ? null : tsv[10];
            ICBSubSector = string.IsNullOrWhiteSpace(tsv[11]) ? null : tsv[11];
            ICBCode = string.IsNullOrWhiteSpace(tsv[12]) ? (int?)null : Convert.ToInt32(tsv[12], CultureInfo.InvariantCulture);
            CompanyName = string.IsNullOrWhiteSpace(tsv[13]) ? null : tsv[13];
            PreviousResultsAnnouncementDate = string.IsNullOrWhiteSpace(tsv[14]) ? (DateTime?)null : DateTime.ParseExact(tsv[14], "yyyyMMdd", CultureInfo.InvariantCulture);
            NextResultsAnnouncementsDate = string.IsNullOrWhiteSpace(tsv[15]) ? (DateTime?)null : DateTime.ParseExact(tsv[15], "yyyyMMdd", CultureInfo.InvariantCulture);
            NextCloseBegin = string.IsNullOrWhiteSpace(tsv[16]) ? (DateTime?)null : DateTime.ParseExact(tsv[16], "yyyyMMdd", CultureInfo.InvariantCulture);
            LastCloseEnded = string.IsNullOrWhiteSpace(tsv[17]) ? (DateTime?)null : DateTime.ParseExact(tsv[17], "yyyyMMdd", CultureInfo.InvariantCulture);
            SecurityDescription = string.IsNullOrWhiteSpace(tsv[18]) ? null : tsv[18];
            TickerCountry = string.IsNullOrWhiteSpace(tsv[19]) ? null : tsv[19];
            TickerSymbol = string.IsNullOrWhiteSpace(tsv[20]) ? null : tsv[20];
            AnnouncementDate = string.IsNullOrWhiteSpace(tsv[21]) ? (DateTime?)null : DateTime.ParseExact(tsv[21], "yyyyMMdd", CultureInfo.InvariantCulture);
            TimeReleased = string.IsNullOrWhiteSpace(tsv[22]) ? (DateTime?)null : DateTime.ParseExact(tsv[22], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessed = string.IsNullOrWhiteSpace(tsv[23]) ? (DateTime?)null : DateTime.ParseExact(tsv[23], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            TimeReleasedUtc = string.IsNullOrWhiteSpace(tsv[24]) ? (DateTime?)null : DateTime.ParseExact(tsv[24], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            AnnouncedIn = string.IsNullOrWhiteSpace(tsv[25]) ? null : tsv[25];

            // Value is never null. Use as time index.
            Time = TimeProcessedUtc.Value;
        }

        /// <summary>
        /// Converts data to TSV
        /// </summary>
        /// <returns>String of TSV</returns>
        public abstract string ToLine();

        /// <summary>
        /// Derived class instances populate their fields from raw TSV
        /// </summary>
        /// <param name="line">Line of raw TSV (raw with fields 46, 36, 14, 7 removed in descending order)</param>
        public abstract void FromRawData(string line);

        /// <summary>
        /// Specifies the timezone of this data source
        /// </summary>
        /// <returns>Timezone</returns>
        public override DateTimeZone DataTimeZone()
        {
            return TimeZones.Utc;
        }

        /// <summary>
        /// Attempts to normalize and parse SmartInsider dates that include a time component.
        /// </summary>
        /// <param name="date">Date string to parse</param>
        /// <returns>DateTime object</returns>
        /// <exception cref="ArgumentException">Date string was unable to be parsed</exception>
        public static DateTime ParseDate(string date)
        {
            date = date.Replace(" ", "");

            DateTime time;
            if (!Parse.TryParseExact(date, "yyyy-MM-ddHH:mm:ss", DateTimeStyles.None, out time) &&
                !Parse.TryParseExact(date, "dd/MM/yyyyHH:mm:ss", DateTimeStyles.None, out time))
            {
                throw new ArgumentException($"SmartInsider data contains unparsable DateTime: {date}");
            }

            return time;
        }
    }
}
