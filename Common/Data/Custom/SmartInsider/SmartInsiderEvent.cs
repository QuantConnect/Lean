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
        /// <param name="csvLine">Tab delimited line of data</param>
        public SmartInsiderEvent(string tsvLine)
        {
            var tsv = tsvLine.Split('\t');

            TransactionID = tsv[0];
            EventType = string.IsNullOrWhiteSpace(tsv[1]) ? (SmartInsiderEventType?)null : JsonConvert.DeserializeObject<SmartInsiderEventType>($"\"{tsv[1]}\"");
            LastUpdate = DateTime.ParseExact(tsv[2], "yyyyMMdd", CultureInfo.InvariantCulture);
            LastIDsUpdate = string.IsNullOrWhiteSpace(tsv[3]) ? (DateTime?)null : DateTime.ParseExact(tsv[3], "yyyyMMdd", CultureInfo.InvariantCulture);
            ISIN = string.IsNullOrWhiteSpace(tsv[4]) ? null : tsv[4];
            USDMarketCap = string.IsNullOrWhiteSpace(tsv[5]) ? (decimal?)null : Convert.ToDecimal(tsv[5], CultureInfo.InvariantCulture);
            CompanyID = string.IsNullOrWhiteSpace(tsv[6]) ? (int?)null : Convert.ToInt32(tsv[6], CultureInfo.InvariantCulture);
            ICBIndustry = string.IsNullOrWhiteSpace(tsv[7]) ? null : tsv[7];
            ICBSuperSector = string.IsNullOrWhiteSpace(tsv[8]) ? null : tsv[8];
            ICBSector = string.IsNullOrWhiteSpace(tsv[9]) ? null : tsv[9];
            ICBSubSector = string.IsNullOrWhiteSpace(tsv[10]) ? null : tsv[10];
            ICBCode = string.IsNullOrWhiteSpace(tsv[11]) ? (int?)null : Convert.ToInt32(tsv[11], CultureInfo.InvariantCulture);
            CompanyName = string.IsNullOrWhiteSpace(tsv[12]) ? null : tsv[12];
            PreviousResultsAnnouncementDate = string.IsNullOrWhiteSpace(tsv[13]) ? (DateTime?)null : DateTime.ParseExact(tsv[13], "yyyyMMdd", CultureInfo.InvariantCulture);
            NextResultsAnnouncementsDate = string.IsNullOrWhiteSpace(tsv[14]) ? (DateTime?)null : DateTime.ParseExact(tsv[14], "yyyyMMdd", CultureInfo.InvariantCulture);
            NextCloseBegin = string.IsNullOrWhiteSpace(tsv[15]) ? (DateTime?)null : DateTime.ParseExact(tsv[15], "yyyyMMdd", CultureInfo.InvariantCulture);
            LastCloseEnded = string.IsNullOrWhiteSpace(tsv[16]) ? (DateTime?)null : DateTime.ParseExact(tsv[16], "yyyyMMdd", CultureInfo.InvariantCulture);
            SecurityDescription = string.IsNullOrWhiteSpace(tsv[17]) ? null : tsv[17];
            TickerCountry = string.IsNullOrWhiteSpace(tsv[18]) ? null : tsv[18];
            TickerSymbol = string.IsNullOrWhiteSpace(tsv[19]) ? null : tsv[19];
            AnnouncementDate = string.IsNullOrWhiteSpace(tsv[20]) ? (DateTime?)null : DateTime.ParseExact(tsv[20], "yyyyMMdd", CultureInfo.InvariantCulture);
            TimeReleased = string.IsNullOrWhiteSpace(tsv[21]) ? (DateTime?)null : DateTime.ParseExact(tsv[21], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessed = string.IsNullOrWhiteSpace(tsv[22]) ? (DateTime?)null : DateTime.ParseExact(tsv[22], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            TimeReleasedUtc = string.IsNullOrWhiteSpace(tsv[23]) ? (DateTime?)null : DateTime.ParseExact(tsv[23], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessedUtc = string.IsNullOrWhiteSpace(tsv[24]) ? (DateTime?)null : DateTime.ParseExact(tsv[24], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            AnnouncedIn = string.IsNullOrWhiteSpace(tsv[25]) ? null : tsv[25];

            // Files are made available at the earliest @ 17:00 U.K. time. Adjust this in the Reader to reflect this fact
            Time = LastUpdate;
        }

        /// <summary>
        /// Converts data to CSV
        /// </summary>
        /// <returns>String of CSV</returns>
        public abstract string ToLine();

        /// <summary>
        /// Derived class instances populate their fields from raw CSV
        /// </summary>
        /// <param name="line">Line of raw CSV (raw with fields 46, 36, 14, 7 removed in descending order)</param>
        public abstract void FromRawData(string line);
    }
}
