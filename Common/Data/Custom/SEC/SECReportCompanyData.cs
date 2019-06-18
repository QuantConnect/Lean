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

namespace QuantConnect.Data.Custom.SEC
{
    public class SECReportCompanyData
    {
        /// <summary>
        /// Current company name
        /// </summary>
        [JsonProperty("CONFORMED-NAME")]
        public string ConformedName;

        /// <summary>
        /// Company's Central Index Key. Used to uniquely identify company filings in SEC's EDGAR system
        /// </summary>
        [JsonProperty("CIK")]
        public string Cik;

        /// <summary>
        /// Standard Industrial Classification
        /// </summary>
        [JsonProperty("ASSIGNED-SIC")]
        public string AssignedSic;

        /// <summary>
        /// Employer Identification Number
        /// </summary>
        [JsonProperty("IRS-NUMBER")]
        public string IrsNumber;

        /// <summary>
        /// State of incorporation
        /// </summary>
        [JsonProperty("STATE-OF-INCORPORATION")]
        public string StateOfIncorporation;

        /// <summary>
        /// Day fiscal year ends for given company. Formatted as MMdd
        /// </summary>
        [JsonProperty("FISCAL-YEAR-END")]
        public string FiscalYearEnd;

    }
}