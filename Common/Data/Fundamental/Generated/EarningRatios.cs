/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2023 QuantConnect Corporation.
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
 *
*/

using System;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Definition of the EarningRatios class
    /// </summary>
    public readonly struct EarningRatios
    {
        /// <summary>
        /// The growth in the company's diluted earnings per share (EPS) on a percentage basis. Morningstar calculates the annualized growth percentage based on the underlying diluted EPS reported in the Income Statement within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13015
        /// </remarks>
        [JsonProperty("13015")]
        public DilutedEPSGrowth DilutedEPSGrowth => new(_time, _securityIdentifier);

        /// <summary>
        /// The growth in the company's diluted EPS from continuing operations on a percentage basis. Morningstar calculates the annualized growth percentage based on the underlying diluted EPS from continuing operations reported in the Income Statement within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13016
        /// </remarks>
        [JsonProperty("13016")]
        public DilutedContEPSGrowth DilutedContEPSGrowth => new(_time, _securityIdentifier);

        /// <summary>
        /// The growth in the company's dividends per share (DPS) on a percentage basis. Morningstar calculates the annualized growth percentage based on the underlying DPS from its dividend database. Morningstar collects its DPS from company filings and reports, as well as from third party sources.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13017
        /// </remarks>
        [JsonProperty("13017")]
        public DPSGrowth DPSGrowth => new(_time, _securityIdentifier);

        /// <summary>
        /// The growth in the company's book value per share on a percentage basis. Morningstar calculates the annualized growth percentage based on the underlying equity and end of period shares outstanding reported in the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13018
        /// </remarks>
        [JsonProperty("13018")]
        public EquityPerShareGrowth EquityPerShareGrowth => new(_time, _securityIdentifier);

        /// <summary>
        /// The five-year growth rate of dividends per share, calculated using regression analysis.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13019
        /// </remarks>
        [JsonProperty("13019")]
        public RegressionGrowthofDividends5Years RegressionGrowthofDividends5Years => new(_time, _securityIdentifier);

        /// <summary>
        /// The growth in the company's free cash flow per share on a percentage basis. Morningstar calculates the growth percentage based on the free cash flow divided by average diluted shares outstanding reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13020
        /// </remarks>
        [JsonProperty("13020")]
        public FCFPerShareGrowth FCFPerShareGrowth => new(_time, _securityIdentifier);

        /// <summary>
        /// The growth in the company's book value per share on a percentage basis. Morningstar calculates the growth percentage based on the common shareholder's equity reported in the Balance Sheet divided by the diluted shares outstanding within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13021
        /// </remarks>
        [JsonProperty("13021")]
        public BookValuePerShareGrowth BookValuePerShareGrowth => new(_time, _securityIdentifier);

        /// <summary>
        /// The growth in the company's Normalized Diluted EPS on a percentage basis.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13022
        /// </remarks>
        [JsonProperty("13022")]
        public NormalizedDilutedEPSGrowth NormalizedDilutedEPSGrowth => new(_time, _securityIdentifier);

        /// <summary>
        /// The growth in the company's Normalized Basic EPS on a percentage basis.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13023
        /// </remarks>
        [JsonProperty("13023")]
        public NormalizedBasicEPSGrowth NormalizedBasicEPSGrowth => new(_time, _securityIdentifier);

        private readonly DateTime _time;
        private readonly SecurityIdentifier _securityIdentifier;

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public EarningRatios(DateTime time, SecurityIdentifier securityIdentifier)
        {
            _time = time;
            _securityIdentifier = securityIdentifier;
        }
    }
}
