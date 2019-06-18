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
    public class SECReportFilingValues 
    {
        /// <summary>
        /// SEC Form Type (e.g. 10-Q, 8-K, S-1, etc.)
        /// </summary>
        [JsonProperty("FORM-TYPE")]
        public string FormType;

        /// <summary>
        /// Identification of the act(s) under which certain IM filings are made. The form type may be filed under more than one act. Required in each filing values tag nest.
        /// </summary>
        [JsonProperty("ACT")]
        public string Act;

        /// <summary>
        /// SEC filing number
        /// </summary>
        [JsonProperty("FILE-NUMBER")]
        public string FileNumber;

        /// <summary>
        /// Used to access documents in the SEC's Virtual Private Reference Room (VPRR)
        /// </summary>
        [JsonProperty("FILM-NUMBER")]
        public string FilmNumber;

    }
}