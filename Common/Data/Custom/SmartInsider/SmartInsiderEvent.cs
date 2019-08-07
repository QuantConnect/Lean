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

using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Data.Custom.SmartInsider
{
    /// <summary>
    /// SmartInsider Intention and Transaction events structure
    /// </summary>
    public abstract class SmartInsiderEvent : BaseData
    {
        /// <summary>
        /// Proprietary unique field. Not nullable
        /// </summary>
        public string TransactionID { get; set; }

        /// <summary>
        /// Row definition. Nullable
        /// </summary>
        public string BuybackType { get; set; }

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
        /// ???
        /// </summary>
        public DateTime? NextCloseBegin { get; set; }

        /// <summary>
        /// ???
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
        /// Parses a line of CSV (tab delimited) from Smart Insider data
        /// </summary>
        /// <param name="csvLine">Tab delimited line of data</param>
        public SmartInsiderEvent(string csvLine)
        {
            var csv = csvLine.Split('\t');
            var count = csv.Length;

            TransactionID = csv[0];
            BuybackType = string.IsNullOrWhiteSpace(csv[1]) ? null : csv[1];
            LastUpdate = DateTime.ParseExact(csv[2], "yyyyMMdd", CultureInfo.InvariantCulture);
            LastIDsUpdate = string.IsNullOrWhiteSpace(csv[3]) ? (DateTime?)null : DateTime.ParseExact(csv[3], "yyyyMMdd", CultureInfo.InvariantCulture);
            ISIN = string.IsNullOrWhiteSpace(csv[4]) ? null : csv[4];
            USDMarketCap = string.IsNullOrWhiteSpace(csv[5]) ? (decimal?)null : Convert.ToDecimal(csv[5], CultureInfo.InvariantCulture);
            CompanyID = string.IsNullOrWhiteSpace(csv[6]) ? (int?)null : Convert.ToInt32(csv[6], CultureInfo.InvariantCulture);
            ICBIndustry = string.IsNullOrWhiteSpace(csv[7]) ? null : csv[7];
            ICBSuperSector = string.IsNullOrWhiteSpace(csv[8]) ? null : csv[8];
            ICBSector = string.IsNullOrWhiteSpace(csv[9]) ? null : csv[9];
            ICBSubSector = string.IsNullOrWhiteSpace(csv[10]) ? null : csv[10];
            ICBCode = string.IsNullOrWhiteSpace(csv[11]) ? (int?)null : Convert.ToInt32(csv[11], CultureInfo.InvariantCulture);
            CompanyName = string.IsNullOrWhiteSpace(csv[12]) ? null : csv[12];
            PreviousResultsAnnouncementDate = string.IsNullOrWhiteSpace(csv[13]) ? (DateTime?)null : DateTime.ParseExact(csv[13], "yyyyMMdd", CultureInfo.InvariantCulture);
            NextResultsAnnouncementsDate = string.IsNullOrWhiteSpace(csv[14]) ? (DateTime?)null : DateTime.ParseExact(csv[14], "yyyyMMdd", CultureInfo.InvariantCulture);
            NextCloseBegin = string.IsNullOrWhiteSpace(csv[15]) ? (DateTime?)null : DateTime.ParseExact(csv[15], "yyyyMMdd", CultureInfo.InvariantCulture);
            LastCloseEnded = string.IsNullOrWhiteSpace(csv[16]) ? (DateTime?)null : DateTime.ParseExact(csv[16], "yyyyMMdd", CultureInfo.InvariantCulture);
            SecurityDescription = string.IsNullOrWhiteSpace(csv[17]) ? null : csv[17];
            TickerCountry = string.IsNullOrWhiteSpace(csv[18]) ? null : csv[18];
            TickerSymbol = string.IsNullOrWhiteSpace(csv[19]) ? null : csv[19];
            AnnouncementDate = string.IsNullOrWhiteSpace(csv[20]) ? (DateTime?)null : DateTime.ParseExact(csv[20], "yyyyMMdd", CultureInfo.InvariantCulture);
            TimeReleased = string.IsNullOrWhiteSpace(csv[21]) ? (DateTime?)null : DateTime.ParseExact(csv[21], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessed = string.IsNullOrWhiteSpace(csv[22]) ? (DateTime?)null : DateTime.ParseExact(csv[22], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            TimeReleasedUtc = string.IsNullOrWhiteSpace(csv[23]) ? (DateTime?)null : DateTime.ParseExact(csv[23], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            TimeProcessedUtc = string.IsNullOrWhiteSpace(csv[24]) ? (DateTime?)null : DateTime.ParseExact(csv[24], "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);
            AnnouncedIn = string.IsNullOrWhiteSpace(csv[25]) ? null : csv[25];

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
