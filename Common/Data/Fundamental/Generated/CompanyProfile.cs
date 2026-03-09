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
    /// Definition of the CompanyProfile class
    /// </summary>
    public class CompanyProfile : FundamentalTimeDependentProperty
    {
        /// <summary>
        /// The headquarter address as given in the latest report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2100
        /// </remarks>
        [JsonProperty("2100")]
        public string HeadquarterAddressLine1 => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_HeadquarterAddressLine1);

        /// <summary>
        /// The headquarter address as given in the latest report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2101
        /// </remarks>
        [JsonProperty("2101")]
        public string HeadquarterAddressLine2 => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_HeadquarterAddressLine2);

        /// <summary>
        /// The headquarter address as given in the latest report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2102
        /// </remarks>
        [JsonProperty("2102")]
        public string HeadquarterAddressLine3 => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_HeadquarterAddressLine3);

        /// <summary>
        /// The headquarter address as given in the latest report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2103
        /// </remarks>
        [JsonProperty("2103")]
        public string HeadquarterAddressLine4 => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_HeadquarterAddressLine4);

        /// <summary>
        /// The headquarter address as given in the latest report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2104
        /// </remarks>
        [JsonProperty("2104")]
        public string HeadquarterAddressLine5 => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_HeadquarterAddressLine5);

        /// <summary>
        /// The headquarter city as given in the latest report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2105
        /// </remarks>
        [JsonProperty("2105")]
        public string HeadquarterCity => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_HeadquarterCity);

        /// <summary>
        /// The headquarter state or province as given in the latest report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2106
        /// </remarks>
        [JsonProperty("2106")]
        public string HeadquarterProvince => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_HeadquarterProvince);

        /// <summary>
        /// The headquarter country as given in the latest report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2107
        /// </remarks>
        [JsonProperty("2107")]
        public string HeadquarterCountry => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_HeadquarterCountry);

        /// <summary>
        /// The headquarter postal code as given in the latest report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2108
        /// </remarks>
        [JsonProperty("2108")]
        public string HeadquarterPostalCode => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_HeadquarterPostalCode);

        /// <summary>
        /// The headquarter phone number as given in the latest report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2109
        /// </remarks>
        [JsonProperty("2109")]
        public string HeadquarterPhone => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_HeadquarterPhone);

        /// <summary>
        /// The headquarter fax number as given in the latest report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2110
        /// </remarks>
        [JsonProperty("2110")]
        public string HeadquarterFax => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_HeadquarterFax);

        /// <summary>
        /// The headquarters' website address as given in the latest report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2111
        /// </remarks>
        [JsonProperty("2111")]
        public string HeadquarterHomepage => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_HeadquarterHomepage);

        /// <summary>
        /// The number of employees as indicated on the latest Annual Report, 10-K filing, Form 20-F or equivalent report indicating the employee count at the end of latest fiscal year.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2113
        /// </remarks>
        [JsonProperty("2113")]
        public int TotalEmployeeNumber => FundamentalService.Get<int>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_TotalEmployeeNumber);

        /// <summary>
        /// Company's contact email address
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2114
        /// </remarks>
        [JsonProperty("2114")]
        public string ContactEmail => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_ContactEmail);

        /// <summary>
        /// Average number of employees from Annual Report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2115
        /// </remarks>
        [JsonProperty("2115")]
        public int AverageEmployeeNumber => FundamentalService.Get<int>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_AverageEmployeeNumber);

        /// <summary>
        /// Details for registered office contact information including address full details, phone and
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2116
        /// </remarks>
        [JsonProperty("2116")]
        public string RegisteredAddressLine1 => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_RegisteredAddressLine1);

        /// <summary>
        /// Address for registered office
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2117
        /// </remarks>
        [JsonProperty("2117")]
        public string RegisteredAddressLine2 => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_RegisteredAddressLine2);

        /// <summary>
        /// Address for registered office
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2118
        /// </remarks>
        [JsonProperty("2118")]
        public string RegisteredAddressLine3 => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_RegisteredAddressLine3);

        /// <summary>
        /// Address for registered office
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2119
        /// </remarks>
        [JsonProperty("2119")]
        public string RegisteredAddressLine4 => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_RegisteredAddressLine4);

        /// <summary>
        /// City for registered office
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2120
        /// </remarks>
        [JsonProperty("2120")]
        public string RegisteredCity => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_RegisteredCity);

        /// <summary>
        /// Province for registered office
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2121
        /// </remarks>
        [JsonProperty("2121")]
        public string RegisteredProvince => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_RegisteredProvince);

        /// <summary>
        /// Country for registered office
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2122
        /// </remarks>
        [JsonProperty("2122")]
        public string RegisteredCountry => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_RegisteredCountry);

        /// <summary>
        /// Postal Code for registered office
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2123
        /// </remarks>
        [JsonProperty("2123")]
        public string RegisteredPostalCode => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_RegisteredPostalCode);

        /// <summary>
        /// Phone number for registered office
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2124
        /// </remarks>
        [JsonProperty("2124")]
        public string RegisteredPhone => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_RegisteredPhone);

        /// <summary>
        /// Fax number for registered office
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2125
        /// </remarks>
        [JsonProperty("2125")]
        public string RegisteredFax => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_RegisteredFax);

        /// <summary>
        /// Flag to denote whether head and registered offices are the same
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 2126
        /// </remarks>
        [JsonProperty("2126")]
        public bool IsHeadOfficeSameWithRegisteredOfficeFlag => FundamentalService.Get<bool>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_IsHeadOfficeSameWithRegisteredOfficeFlag);

        /// <summary>
        /// The latest total shares outstanding reported by the company; most common source of this information is from the cover of the 10K, 10Q, or 20F filing. This figure is an aggregated shares outstanding number for a company. It can be used to calculate the most accurate market cap, based on each individual share's trading price and the total aggregated shares outstanding figure.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 40000
        /// </remarks>
        [JsonProperty("40000")]
        public long SharesOutstanding => FundamentalService.Get<long>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_SharesOutstanding);

        /// <summary>
        /// Price * Total SharesOutstanding. The most current market cap for example, would be the most recent closing price x the most recent reported shares outstanding. For ADR share classes, market cap is price * (ordinary shares outstanding / adr ratio).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 40001
        /// </remarks>
        [JsonProperty("40001")]
        public long MarketCap => FundamentalService.Get<long>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_MarketCap);

        /// <summary>
        /// This number tells you what cash return you would get if you bought the entire company, including its debt. Enterprise Value = Market Cap + Preferred stock + Long-Term Debt And Capital Lease + Short Term Debt And Capital Lease + Securities Sold But Not Yet Repurchased - Cash, Cash Equivalent And Market Securities - Securities Purchased with Agreement to Resell - Securities Borrowed.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 40002
        /// </remarks>
        [JsonProperty("40002")]
        public long EnterpriseValue => FundamentalService.Get<long>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_EnterpriseValue);

        /// <summary>
        /// The latest shares outstanding reported by the company of a particular share class; most common source of this information is from the cover of the 10K, 10Q, or 20F filing. This figure is an aggregated shares outstanding number for a particular share class of the company.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 40003
        /// </remarks>
        [JsonProperty("40003")]
        public long ShareClassLevelSharesOutstanding => FundamentalService.Get<long>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_ShareClassLevelSharesOutstanding);

        /// <summary>
        /// Total shares outstanding reported by the company as of the balance sheet period ended date. The most common source of this information is from the 10K, 10Q, or 20F filing. This figure is an aggregated shares outstanding number for a company.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 40007
        /// </remarks>
        [JsonProperty("40007")]
        public long SharesOutstandingWithBalanceSheetEndingDate => FundamentalService.Get<long>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_SharesOutstandingWithBalanceSheetEndingDate);

        /// <summary>
        /// The reason for the change in a company's total shares outstanding from the previous record. Examples could be share issuances or share buy-back. This field will only be populated when total shares outstanding is collected from a press release.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 40010
        /// </remarks>
        [JsonProperty("40010")]
        public string ReasonofSharesChange => FundamentalService.Get<string>(_timeProvider.GetUtcNow(), _securityIdentifier, FundamentalProperty.CompanyProfile_ReasonofSharesChange);

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public CompanyProfile(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier)
            : base(timeProvider, securityIdentifier)
        {
        }

        /// <summary>
        /// Clones this instance
        /// </summary>
        public override FundamentalTimeDependentProperty Clone(ITimeProvider timeProvider)
        {
            return new CompanyProfile(timeProvider, _securityIdentifier);
        }
    }
}
