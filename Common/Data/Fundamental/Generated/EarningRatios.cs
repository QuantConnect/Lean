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
using Python.Runtime;
using Newtonsoft.Json;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;

namespace QuantConnect.Data.Fundamental
{
    /// <summary>
    /// Definition of the EarningRatios class
    /// </summary>
    public class EarningRatios : FundamentalTimeDependentProperty
    {
        /// <summary>
        /// The growth in the company's diluted earnings per share (EPS) on a percentage basis. Morningstar calculates the annualized growth percentage based on the underlying diluted EPS reported in the Income Statement within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13015
        /// </remarks>
        [JsonProperty("13015")]
        public DilutedEPSGrowth DilutedEPSGrowth => _dilutedEPSGrowth ??= new(_timeProvider, _securityIdentifier);
        private DilutedEPSGrowth _dilutedEPSGrowth;

        /// <summary>
        /// The growth in the company's diluted EPS from continuing operations on a percentage basis. Morningstar calculates the annualized growth percentage based on the underlying diluted EPS from continuing operations reported in the Income Statement within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13016
        /// </remarks>
        [JsonProperty("13016")]
        public DilutedContEPSGrowth DilutedContEPSGrowth => _dilutedContEPSGrowth ??= new(_timeProvider, _securityIdentifier);
        private DilutedContEPSGrowth _dilutedContEPSGrowth;

        /// <summary>
        /// The growth in the company's dividends per share (DPS) on a percentage basis. Morningstar calculates the annualized growth percentage based on the underlying DPS from its dividend database. Morningstar collects its DPS from company filings and reports, as well as from third party sources.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13017
        /// </remarks>
        [JsonProperty("13017")]
        public DPSGrowth DPSGrowth => _dPSGrowth ??= new(_timeProvider, _securityIdentifier);
        private DPSGrowth _dPSGrowth;

        /// <summary>
        /// The growth in the company's book value per share on a percentage basis. Morningstar calculates the annualized growth percentage based on the underlying equity and end of period shares outstanding reported in the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13018
        /// </remarks>
        [JsonProperty("13018")]
        public EquityPerShareGrowth EquityPerShareGrowth => _equityPerShareGrowth ??= new(_timeProvider, _securityIdentifier);
        private EquityPerShareGrowth _equityPerShareGrowth;

        /// <summary>
        /// The five-year growth rate of dividends per share, calculated using regression analysis.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13019
        /// </remarks>
        [JsonProperty("13019")]
        public RegressionGrowthofDividends5Years RegressionGrowthofDividends5Years => _regressionGrowthofDividends5Years ??= new(_timeProvider, _securityIdentifier);
        private RegressionGrowthofDividends5Years _regressionGrowthofDividends5Years;

        /// <summary>
        /// The growth in the company's free cash flow per share on a percentage basis. Morningstar calculates the growth percentage based on the free cash flow divided by average diluted shares outstanding reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13020
        /// </remarks>
        [JsonProperty("13020")]
        public FCFPerShareGrowth FCFPerShareGrowth => _fCFPerShareGrowth ??= new(_timeProvider, _securityIdentifier);
        private FCFPerShareGrowth _fCFPerShareGrowth;

        /// <summary>
        /// The growth in the company's book value per share on a percentage basis. Morningstar calculates the growth percentage based on the common shareholder's equity reported in the Balance Sheet divided by the diluted shares outstanding within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13021
        /// </remarks>
        [JsonProperty("13021")]
        public BookValuePerShareGrowth BookValuePerShareGrowth => _bookValuePerShareGrowth ??= new(_timeProvider, _securityIdentifier);
        private BookValuePerShareGrowth _bookValuePerShareGrowth;

        /// <summary>
        /// The growth in the company's Normalized Diluted EPS on a percentage basis.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13022
        /// </remarks>
        [JsonProperty("13022")]
        public NormalizedDilutedEPSGrowth NormalizedDilutedEPSGrowth => _normalizedDilutedEPSGrowth ??= new(_timeProvider, _securityIdentifier);
        private NormalizedDilutedEPSGrowth _normalizedDilutedEPSGrowth;

        /// <summary>
        /// The growth in the company's Normalized Basic EPS on a percentage basis.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 13023
        /// </remarks>
        [JsonProperty("13023")]
        public NormalizedBasicEPSGrowth NormalizedBasicEPSGrowth => _normalizedBasicEPSGrowth ??= new(_timeProvider, _securityIdentifier);
        private NormalizedBasicEPSGrowth _normalizedBasicEPSGrowth;

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public EarningRatios(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier)
            : base(timeProvider, securityIdentifier)
        {
        }

        /// <summary>
        /// Clones this instance
        /// </summary>
        public override FundamentalTimeDependentProperty Clone(ITimeProvider timeProvider)
        {
            return new EarningRatios(timeProvider, _securityIdentifier);
        }
    }
}
