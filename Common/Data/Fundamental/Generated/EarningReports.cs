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
    /// Definition of the EarningReports class
    /// </summary>
    public class EarningReports : FundamentalTimeDependentProperty
    {
        /// <summary>
        /// The exact date that is given in the financial statements for each quarter's end.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20001
        /// </remarks>
        [JsonProperty("20001")]
        public EarningReportsPeriodEndingDate PeriodEndingDate => _periodEndingDate ??= new(_timeProvider, _securityIdentifier);
        private EarningReportsPeriodEndingDate _periodEndingDate;

        /// <summary>
        /// Specific date on which a company released its filing to the public.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20002
        /// </remarks>
        [JsonProperty("20002")]
        public EarningReportsFileDate FileDate => _fileDate ??= new(_timeProvider, _securityIdentifier);
        private EarningReportsFileDate _fileDate;

        /// <summary>
        /// The accession number is a unique number that EDGAR assigns to each submission as the submission is received.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20003
        /// </remarks>
        [JsonProperty("20003")]
        public EarningReportsAccessionNumber AccessionNumber => _accessionNumber ??= new(_timeProvider, _securityIdentifier);
        private EarningReportsAccessionNumber _accessionNumber;

        /// <summary>
        /// The type of filing of the report: for instance, 10-K (annual report) or 10-Q (quarterly report).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20004
        /// </remarks>
        [JsonProperty("20004")]
        public EarningReportsFormType FormType => _formType ??= new(_timeProvider, _securityIdentifier);
        private EarningReportsFormType _formType;

        /// <summary>
        /// The nature of the period covered by an individual set of financial results. The output can be: Quarter, Semi-annual or Annual. Assuming a 12-month fiscal year, quarter typically covers a three-month period, semi-annual a six-month period, and annual a twelve-month period. Annual could cover results collected either from preliminary results or an annual report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 28006
        /// </remarks>
        [JsonProperty("28006")]
        public EarningReportsPeriodType PeriodType => _periodType ??= new(_timeProvider, _securityIdentifier);
        private EarningReportsPeriodType _periodType;

        /// <summary>
        /// Basic EPS from Continuing Operations is the earnings from continuing operations reported by the company divided by the weighted average number of common shares outstanding.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29000
        /// </remarks>
        [JsonProperty("29000")]
        public BasicContinuousOperations BasicContinuousOperations => _basicContinuousOperations ??= new(_timeProvider, _securityIdentifier);
        private BasicContinuousOperations _basicContinuousOperations;

        /// <summary>
        /// Basic EPS from Discontinued Operations is the earnings from discontinued operations reported by the company divided by the weighted average number of common shares outstanding. This only includes gain or loss from discontinued operations.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29001
        /// </remarks>
        [JsonProperty("29001")]
        public BasicDiscontinuousOperations BasicDiscontinuousOperations => _basicDiscontinuousOperations ??= new(_timeProvider, _securityIdentifier);
        private BasicDiscontinuousOperations _basicDiscontinuousOperations;

        /// <summary>
        /// Basic EPS from the Extraordinary Gains/Losses is the earnings attributable to the gains or losses (during the reporting period) from extraordinary items divided by the weighted average number of common shares outstanding.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29002
        /// </remarks>
        [JsonProperty("29002")]
        public BasicExtraordinary BasicExtraordinary => _basicExtraordinary ??= new(_timeProvider, _securityIdentifier);
        private BasicExtraordinary _basicExtraordinary;

        /// <summary>
        /// Basic EPS from the Cumulative Effect of Accounting Change is the earnings attributable to the accounting change (during the reporting period) divided by the weighted average number of common shares outstanding.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29003
        /// </remarks>
        [JsonProperty("29003")]
        public BasicAccountingChange BasicAccountingChange => _basicAccountingChange ??= new(_timeProvider, _securityIdentifier);
        private BasicAccountingChange _basicAccountingChange;

        /// <summary>
        /// Basic EPS is the bottom line net income divided by the weighted average number of common shares outstanding.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29004
        /// </remarks>
        [JsonProperty("29004")]
        public BasicEPS BasicEPS => _basicEPS ??= new(_timeProvider, _securityIdentifier);
        private BasicEPS _basicEPS;

        /// <summary>
        /// Diluted EPS from Continuing Operations is the earnings from continuing operations divided by the common shares outstanding adjusted for the assumed conversion of all potentially dilutive securities. Securities having a dilutive effect may include convertible debentures, warrants, options, and convertible preferred stock.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29005
        /// </remarks>
        [JsonProperty("29005")]
        public DilutedContinuousOperations DilutedContinuousOperations => _dilutedContinuousOperations ??= new(_timeProvider, _securityIdentifier);
        private DilutedContinuousOperations _dilutedContinuousOperations;

        /// <summary>
        /// Diluted EPS from Discontinued Operations is the earnings from discontinued operations divided by the common shares outstanding adjusted for the assumed conversion of all potentially dilutive securities. Securities having a dilutive effect may include convertible debentures, warrants, options, and convertible preferred stock. This only includes gain or loss from discontinued operations.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29006
        /// </remarks>
        [JsonProperty("29006")]
        public DilutedDiscontinuousOperations DilutedDiscontinuousOperations => _dilutedDiscontinuousOperations ??= new(_timeProvider, _securityIdentifier);
        private DilutedDiscontinuousOperations _dilutedDiscontinuousOperations;

        /// <summary>
        /// Diluted EPS from Extraordinary Gain/Losses is the gain or loss from extraordinary items divided by the common shares outstanding adjusted for the assumed conversion of all potentially dilutive securities. Securities having a dilutive effect may include convertible debentures, warrants, options, and convertible preferred stock.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29007
        /// </remarks>
        [JsonProperty("29007")]
        public DilutedExtraordinary DilutedExtraordinary => _dilutedExtraordinary ??= new(_timeProvider, _securityIdentifier);
        private DilutedExtraordinary _dilutedExtraordinary;

        /// <summary>
        /// Diluted EPS from Cumulative Effect Accounting Changes is the earnings from accounting changes (in the reporting period) divided by the common shares outstanding adjusted for the assumed conversion of all potentially dilutive securities. Securities having a dilutive effect may include convertible debentures, warrants, options, and convertible preferred stock.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29008
        /// </remarks>
        [JsonProperty("29008")]
        public DilutedAccountingChange DilutedAccountingChange => _dilutedAccountingChange ??= new(_timeProvider, _securityIdentifier);
        private DilutedAccountingChange _dilutedAccountingChange;

        /// <summary>
        /// Diluted EPS is the bottom line net income divided by the common shares outstanding adjusted for the assumed conversion of all potentially dilutive securities. Securities having a dilutive effect may include convertible debentures, warrants, options, and convertible preferred stock. This value will be derived when not reported for the fourth quarter and will be less than or equal to Basic EPS.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29009
        /// </remarks>
        [JsonProperty("29009")]
        public DilutedEPS DilutedEPS => _dilutedEPS ??= new(_timeProvider, _securityIdentifier);
        private DilutedEPS _dilutedEPS;

        /// <summary>
        /// The shares outstanding used to calculate Basic EPS, which is the weighted average common share outstanding through the whole accounting period. Note: If Basic Average Shares are not presented by the firm in the Income Statement, this data point will be null.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29010
        /// </remarks>
        [JsonProperty("29010")]
        public BasicAverageShares BasicAverageShares => _basicAverageShares ??= new(_timeProvider, _securityIdentifier);
        private BasicAverageShares _basicAverageShares;

        /// <summary>
        /// The shares outstanding used to calculate the diluted EPS, assuming the conversion of all convertible securities and the exercise of warrants or stock options. It is the weighted average diluted share outstanding through the whole accounting period. Note: If Diluted Average Shares are not presented by the firm in the Income Statement and Basic Average Shares are presented, Diluted Average Shares will equal Basic Average Shares. However, if neither value is presented by the firm, Diluted Average Shares will be null.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29011
        /// </remarks>
        [JsonProperty("29011")]
        public DilutedAverageShares DilutedAverageShares => _dilutedAverageShares ??= new(_timeProvider, _securityIdentifier);
        private DilutedAverageShares _dilutedAverageShares;

        /// <summary>
        /// The amount of dividend that a stockholder will receive for each share of stock held. It can be calculated by taking the total amount of dividends paid and dividing it by the total shares outstanding. Dividend per share = total dividend payment/total number of outstanding shares
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29012
        /// </remarks>
        [JsonProperty("29012")]
        public DividendPerShare DividendPerShare => _dividendPerShare ??= new(_timeProvider, _securityIdentifier);
        private DividendPerShare _dividendPerShare;

        /// <summary>
        /// Basic EPS from the Other Gains/Losses is the earnings attributable to the other gains/losses (during the reporting period) divided by the weighted average number of common shares outstanding.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29013
        /// </remarks>
        [JsonProperty("29013")]
        public BasicEPSOtherGainsLosses BasicEPSOtherGainsLosses => _basicEPSOtherGainsLosses ??= new(_timeProvider, _securityIdentifier);
        private BasicEPSOtherGainsLosses _basicEPSOtherGainsLosses;

        /// <summary>
        /// Basic EPS from Continuing Operations plus Basic EPS from Discontinued Operations.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29014
        /// </remarks>
        [JsonProperty("29014")]
        public ContinuingAndDiscontinuedBasicEPS ContinuingAndDiscontinuedBasicEPS => _continuingAndDiscontinuedBasicEPS ??= new(_timeProvider, _securityIdentifier);
        private ContinuingAndDiscontinuedBasicEPS _continuingAndDiscontinuedBasicEPS;

        /// <summary>
        /// The earnings attributable to the tax loss carry forward (during the reporting period).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29015
        /// </remarks>
        [JsonProperty("29015")]
        public TaxLossCarryforwardBasicEPS TaxLossCarryforwardBasicEPS => _taxLossCarryforwardBasicEPS ??= new(_timeProvider, _securityIdentifier);
        private TaxLossCarryforwardBasicEPS _taxLossCarryforwardBasicEPS;

        /// <summary>
        /// The earnings from gains and losses (in the reporting period) divided by the common shares outstanding adjusted for the assumed conversion of all potentially dilutive securities. Securities having a dilutive effect may include convertible debentures, warrants, options, convertible preferred stock, etc.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29016
        /// </remarks>
        [JsonProperty("29016")]
        public DilutedEPSOtherGainsLosses DilutedEPSOtherGainsLosses => _dilutedEPSOtherGainsLosses ??= new(_timeProvider, _securityIdentifier);
        private DilutedEPSOtherGainsLosses _dilutedEPSOtherGainsLosses;

        /// <summary>
        /// Diluted EPS from Continuing Operations plus Diluted EPS from Discontinued Operations.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29017
        /// </remarks>
        [JsonProperty("29017")]
        public ContinuingAndDiscontinuedDilutedEPS ContinuingAndDiscontinuedDilutedEPS => _continuingAndDiscontinuedDilutedEPS ??= new(_timeProvider, _securityIdentifier);
        private ContinuingAndDiscontinuedDilutedEPS _continuingAndDiscontinuedDilutedEPS;

        /// <summary>
        /// The earnings from any tax loss carry forward (in the reporting period).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29018
        /// </remarks>
        [JsonProperty("29018")]
        public TaxLossCarryforwardDilutedEPS TaxLossCarryforwardDilutedEPS => _taxLossCarryforwardDilutedEPS ??= new(_timeProvider, _securityIdentifier);
        private TaxLossCarryforwardDilutedEPS _taxLossCarryforwardDilutedEPS;

        /// <summary>
        /// The basic normalized earnings per share. Normalized EPS removes onetime and unusual items from EPS, to provide investors with a more accurate measure of the company's true earnings. Normalized Earnings / Basic Weighted Average Shares Outstanding.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29019
        /// </remarks>
        [JsonProperty("29019")]
        public NormalizedBasicEPS NormalizedBasicEPS => _normalizedBasicEPS ??= new(_timeProvider, _securityIdentifier);
        private NormalizedBasicEPS _normalizedBasicEPS;

        /// <summary>
        /// The diluted normalized earnings per share. Normalized EPS removes onetime and unusual items from EPS, to provide investors with a more accurate measure of the company's true earnings. Normalized Earnings / Diluted Weighted Average Shares Outstanding.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29020
        /// </remarks>
        [JsonProperty("29020")]
        public NormalizedDilutedEPS NormalizedDilutedEPS => _normalizedDilutedEPS ??= new(_timeProvider, _securityIdentifier);
        private NormalizedDilutedEPS _normalizedDilutedEPS;

        /// <summary>
        /// Total Dividend Per Share is cash dividends and special cash dividends paid per share over a certain period of time.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29021
        /// </remarks>
        [JsonProperty("29021")]
        public TotalDividendPerShare TotalDividendPerShare => _totalDividendPerShare ??= new(_timeProvider, _securityIdentifier);
        private TotalDividendPerShare _totalDividendPerShare;

        /// <summary>
        /// Normalized Basic EPS as reported by the company in the financial statements.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29022
        /// </remarks>
        [JsonProperty("29022")]
        public ReportedNormalizedBasicEPS ReportedNormalizedBasicEPS => _reportedNormalizedBasicEPS ??= new(_timeProvider, _securityIdentifier);
        private ReportedNormalizedBasicEPS _reportedNormalizedBasicEPS;

        /// <summary>
        /// Normalized Diluted EPS as reported by the company in the financial statements.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29023
        /// </remarks>
        [JsonProperty("29023")]
        public ReportedNormalizedDilutedEPS ReportedNormalizedDilutedEPS => _reportedNormalizedDilutedEPS ??= new(_timeProvider, _securityIdentifier);
        private ReportedNormalizedDilutedEPS _reportedNormalizedDilutedEPS;

        /// <summary>
        /// Reflects a firm's capacity to pay a dividend, and is defined as Earnings Per Share / Dividend Per Share
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 29024
        /// </remarks>
        [JsonProperty("29024")]
        public DividendCoverageRatio DividendCoverageRatio => _dividendCoverageRatio ??= new(_timeProvider, _securityIdentifier);
        private DividendCoverageRatio _dividendCoverageRatio;

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public EarningReports(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier)
            : base(timeProvider, securityIdentifier)
        {
        }

        /// <summary>
        /// Clones this instance
        /// </summary>
        public override FundamentalTimeDependentProperty Clone(ITimeProvider timeProvider)
        {
            return new EarningReports(timeProvider, _securityIdentifier);
        }
    }
}
