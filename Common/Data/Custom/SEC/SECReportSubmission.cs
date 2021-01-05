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
using Newtonsoft.Json;
using QuantConnect.Util;

namespace QuantConnect.Data.Custom.SEC
{
    public class SECReportSubmission
    {
        /// <summary>
        /// Number used to access document filings on the SEC website
        /// </summary>
        [JsonProperty("ACCESSION-NUMBER")]
        public string AccessionNumber;

        /// <summary>
        /// SEC form type
        /// </summary>
        [JsonProperty("TYPE")]
        public string FormType;

        /// <summary>
        /// Number of documents made public by the SEC
        /// </summary>
        [JsonProperty("PUBLIC-DOCUMENT-COUNT")]
        public string PublicDocumentCount;

        /// <summary>
        /// End date of reporting period of filing. Optional.
        /// </summary>
        [JsonProperty("PERIOD"), JsonConverter(typeof(SECReportDateTimeConverter))]
        public DateTime Period;

        /// <summary>
        /// Identifies 1 or more items declared in 8-K filings. Optional &amp; Repeatable.
        /// </summary>
        [JsonProperty("ITEMS"), JsonConverter(typeof(SingleValueListConverter<string>))]
        public List<string> Items;

        /// <summary>
        /// Date report was filed with the SEC
        /// </summary>
        [JsonProperty("FILING-DATE"), JsonConverter(typeof(SECReportDateTimeConverter))]
        public DateTime FilingDate;

        /// <summary>
        /// Date when the last Post Acceptance occurred. Optional.
        /// </summary>
        [JsonProperty("DATE-OF-FILING-CHANGE"), JsonConverter(typeof(SECReportDateTimeConverter))]
        public DateTime FilingDateChange;

        /// <summary>
        /// Exact time the report was filed with the SEC and made available to the public (plus 10 minute delay).
        /// This field is NOT included with the raw SEC report, and should be added during post processing of the data
        /// </summary>
        [JsonProperty("MADE-AVAILABLE-AT")]
        public DateTime MadeAvailableAt;

        /// <summary>
        /// Contains information regarding who the filer of the report is.
        /// </summary>
        [JsonProperty("FILER"), JsonConverter(typeof(SingleValueListConverter<SECReportFiler>))]
        public List<SECReportFiler> Filers;

        /// <summary>
        /// Attachments/content associated with the report
        /// </summary>
        [JsonProperty("DOCUMENT"), JsonConverter(typeof(SingleValueListConverter<SECReportDocument>))]
        public List<SECReportDocument> Documents;
    }
}