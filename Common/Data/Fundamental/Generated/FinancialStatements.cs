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
    /// Definition of the FinancialStatements class
    /// </summary>
    public class FinancialStatements : FundamentalTimeDependentProperty
    {
        /// <summary>
        /// The exact date that is given in the financial statements for each quarter's end.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20001
        /// </remarks>
        [JsonProperty("20001")]
        public FinancialStatementsPeriodEndingDate PeriodEndingDate => _periodEndingDate ??= new(_timeProvider, _securityIdentifier);
        private FinancialStatementsPeriodEndingDate _periodEndingDate;

        /// <summary>
        /// Specific date on which a company released its filing to the public.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20002
        /// </remarks>
        [JsonProperty("20002")]
        public FinancialStatementsFileDate FileDate => _fileDate ??= new(_timeProvider, _securityIdentifier);
        private FinancialStatementsFileDate _fileDate;

        /// <summary>
        /// The accession number is a unique number that EDGAR assigns to each submission as the submission is received.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20003
        /// </remarks>
        [JsonProperty("20003")]
        public FinancialStatementsAccessionNumber AccessionNumber => _accessionNumber ??= new(_timeProvider, _securityIdentifier);
        private FinancialStatementsAccessionNumber _accessionNumber;

        /// <summary>
        /// The type of filing of the report: for instance, 10-K (annual report) or 10-Q (quarterly report).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20004
        /// </remarks>
        [JsonProperty("20004")]
        public FinancialStatementsFormType FormType => _formType ??= new(_timeProvider, _securityIdentifier);
        private FinancialStatementsFormType _formType;

        /// <summary>
        /// The name of the auditor that performed the financial statement audit for the given period.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 28000
        /// </remarks>
        [JsonProperty("28000")]
        public PeriodAuditor PeriodAuditor => _periodAuditor ??= new(_timeProvider, _securityIdentifier);
        private PeriodAuditor _periodAuditor;

        /// <summary>
        /// Auditor opinion code will be one of the following for each annual period: Code Meaning UQ Unqualified Opinion UE Unqualified Opinion with Explanation QM Qualified - Due to change in accounting method QL Qualified - Due to litigation OT Qualified Opinion - Other AO Adverse Opinion DS Disclaim an opinion UA Unaudited
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 28001
        /// </remarks>
        [JsonProperty("28001")]
        public AuditorReportStatus AuditorReportStatus => _auditorReportStatus ??= new(_timeProvider, _securityIdentifier);
        private AuditorReportStatus _auditorReportStatus;

        /// <summary>
        /// Which method of inventory valuation was used - LIFO, FIFO, Average, Standard costs, Net realizable value, Others, LIFO and FIFO, FIFO and Average, FIFO and other, LIFO and Average, LIFO and other, Average and other, 3 or more methods, None
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 28002
        /// </remarks>
        [JsonProperty("28002")]
        public InventoryValuationMethod InventoryValuationMethod => _inventoryValuationMethod ??= new(_timeProvider, _securityIdentifier);
        private InventoryValuationMethod _inventoryValuationMethod;

        /// <summary>
        /// The number of shareholders on record
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 28003
        /// </remarks>
        [JsonProperty("28003")]
        public NumberOfShareHolders NumberOfShareHolders => _numberOfShareHolders ??= new(_timeProvider, _securityIdentifier);
        private NumberOfShareHolders _numberOfShareHolders;

        /// <summary>
        /// The nature of the period covered by an individual set of financial results. The output can be: Quarter, Semi-annual or Annual. Assuming a 12-month fiscal year, quarter typically covers a three-month period, semi-annual a six-month period, and annual a twelve-month period. Annual could cover results collected either from preliminary results or an annual report
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 28006
        /// </remarks>
        [JsonProperty("28006")]
        public FinancialStatementsPeriodType PeriodType => _periodType ??= new(_timeProvider, _securityIdentifier);
        private FinancialStatementsPeriodType _periodType;

        /// <summary>
        /// The sum of Tier 1 and Tier 2 Capital. Tier 1 capital consists of common shareholders equity, perpetual preferred shareholders equity with non-cumulative dividends, retained earnings, and minority interests in the equity accounts of consolidated subsidiaries. Tier 2 capital consists of subordinated debt, intermediate-term preferred stock, cumulative and long-term preferred stock, and a portion of a bank's allowance for loan and lease losses.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 28004
        /// </remarks>
        [JsonProperty("28004")]
        public TotalRiskBasedCapital TotalRiskBasedCapital => _totalRiskBasedCapital ??= new(_timeProvider, _securityIdentifier);
        private TotalRiskBasedCapital _totalRiskBasedCapital;

        /// <summary>
        /// The instance of the IncomeStatement class
        /// </summary>

        public IncomeStatement IncomeStatement => _incomeStatement ??= new(_timeProvider, _securityIdentifier);
        private IncomeStatement _incomeStatement;

        /// <summary>
        /// The instance of the BalanceSheet class
        /// </summary>

        public BalanceSheet BalanceSheet => _balanceSheet ??= new(_timeProvider, _securityIdentifier);
        private BalanceSheet _balanceSheet;

        /// <summary>
        /// The instance of the CashFlowStatement class
        /// </summary>

        public CashFlowStatement CashFlowStatement => _cashFlowStatement ??= new(_timeProvider, _securityIdentifier);
        private CashFlowStatement _cashFlowStatement;

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public FinancialStatements(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier)
            : base(timeProvider, securityIdentifier)
        {
        }

        /// <summary>
        /// Clones this instance
        /// </summary>
        public override FundamentalTimeDependentProperty Clone(ITimeProvider timeProvider)
        {
            return new FinancialStatements(timeProvider, _securityIdentifier);
        }
    }
}
