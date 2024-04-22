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
    /// Definition of the OperationRatios class
    /// </summary>
    public class OperationRatios : FundamentalTimeDependentProperty
    {
        /// <summary>
        /// The growth in the company's revenue on a percentage basis. Morningstar calculates the growth percentage based on the underlying revenue data reported in the Income Statement within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 10001
        /// </remarks>
        [JsonProperty("10001")]
        public RevenueGrowth RevenueGrowth => _revenueGrowth ??= new(_timeProvider, _securityIdentifier);
        private RevenueGrowth _revenueGrowth;

        /// <summary>
        /// The growth in the company's operating income on a percentage basis. Morningstar calculates the growth percentage based on the underlying operating income data reported in the Income Statement within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 10002
        /// </remarks>
        [JsonProperty("10002")]
        public OperationIncomeGrowth OperationIncomeGrowth => _operationIncomeGrowth ??= new(_timeProvider, _securityIdentifier);
        private OperationIncomeGrowth _operationIncomeGrowth;

        /// <summary>
        /// The growth in the company's net income on a percentage basis. Morningstar calculates the growth percentage based on the underlying net income data reported in the Income Statement within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 10003
        /// </remarks>
        [JsonProperty("10003")]
        public NetIncomeGrowth NetIncomeGrowth => _netIncomeGrowth ??= new(_timeProvider, _securityIdentifier);
        private NetIncomeGrowth _netIncomeGrowth;

        /// <summary>
        /// The growth in the company's net income from continuing operations on a percentage basis. Morningstar calculates the growth percentage based on the underlying net income from continuing operations data reported in the Income Statement within the company filings or reports. This figure represents the rate of net income growth for parts of the business that will continue to generate revenue in the future.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 10004
        /// </remarks>
        [JsonProperty("10004")]
        public NetIncomeContOpsGrowth NetIncomeContOpsGrowth => _netIncomeContOpsGrowth ??= new(_timeProvider, _securityIdentifier);
        private NetIncomeContOpsGrowth _netIncomeContOpsGrowth;

        /// <summary>
        /// The growth in the company's cash flow from operations on a percentage basis. Morningstar calculates the growth percentage based on the underlying cash flow from operations data reported in the Cash Flow Statement within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 10005
        /// </remarks>
        [JsonProperty("10005")]
        public CFOGrowth CFOGrowth => _cFOGrowth ??= new(_timeProvider, _securityIdentifier);
        private CFOGrowth _cFOGrowth;

        /// <summary>
        /// The growth in the company's free cash flow on a percentage basis. Morningstar calculates the growth percentage based on the underlying cash flow from operations and capital expenditures data reported in the Cash Flow Statement within the company filings or reports: Free Cash Flow = Cash flow from operations - Capital Expenditures.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 10006
        /// </remarks>
        [JsonProperty("10006")]
        public FCFGrowth FCFGrowth => _fCFGrowth ??= new(_timeProvider, _securityIdentifier);
        private FCFGrowth _fCFGrowth;

        /// <summary>
        /// The growth in the company's operating revenue on a percentage basis. Morningstar calculates the growth percentage based on the underlying operating revenue data reported in the Income Statement within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 10007
        /// </remarks>
        [JsonProperty("10007")]
        public OperationRevenueGrowth3MonthAvg OperationRevenueGrowth3MonthAvg => _operationRevenueGrowth3MonthAvg ??= new(_timeProvider, _securityIdentifier);
        private OperationRevenueGrowth3MonthAvg _operationRevenueGrowth3MonthAvg;

        /// <summary>
        /// Refers to the ratio of gross profit to revenue. Morningstar calculates the ratio by using the underlying data reported in the company filings or reports: (Revenue - Cost of Goods Sold) / Revenue.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11001
        /// </remarks>
        [JsonProperty("11001")]
        public GrossMargin GrossMargin => _grossMargin ??= new(_timeProvider, _securityIdentifier);
        private GrossMargin _grossMargin;

        /// <summary>
        /// Refers to the ratio of operating income to revenue. Morningstar calculates the ratio by using the underlying data reported in the company filings or reports: Operating Income / Revenue.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11002
        /// </remarks>
        [JsonProperty("11002")]
        public OperationMargin OperationMargin => _operationMargin ??= new(_timeProvider, _securityIdentifier);
        private OperationMargin _operationMargin;

        /// <summary>
        /// Refers to the ratio of pretax income to revenue. Morningstar calculates the ratio by using the underlying data reported in the company filings or reports: Pretax Income / Revenue.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11003
        /// </remarks>
        [JsonProperty("11003")]
        public PretaxMargin PretaxMargin => _pretaxMargin ??= new(_timeProvider, _securityIdentifier);
        private PretaxMargin _pretaxMargin;

        /// <summary>
        /// Refers to the ratio of net income to revenue. Morningstar calculates the ratio by using the underlying data reported in the company filings or reports: Net Income / Revenue.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11004
        /// </remarks>
        [JsonProperty("11004")]
        public NetMargin NetMargin => _netMargin ??= new(_timeProvider, _securityIdentifier);
        private NetMargin _netMargin;

        /// <summary>
        /// Refers to the ratio of tax provision to pretax income. Morningstar calculates the ratio by using the underlying data reported in the company filings or reports: Tax Provision / Pretax Income. [Note: Valid only when positive pretax income, and positive tax expense (not tax benefit)]
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11005
        /// </remarks>
        [JsonProperty("11005")]
        public TaxRate TaxRate => _taxRate ??= new(_timeProvider, _securityIdentifier);
        private TaxRate _taxRate;

        /// <summary>
        /// Refers to the ratio of earnings before interest and taxes to revenue. Morningstar calculates the ratio by using the underlying data reported in the company filings or reports: EBIT / Revenue.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11006
        /// </remarks>
        [JsonProperty("11006")]
        public EBITMargin EBITMargin => _eBITMargin ??= new(_timeProvider, _securityIdentifier);
        private EBITMargin _eBITMargin;

        /// <summary>
        /// Refers to the ratio of earnings before interest, taxes and depreciation and amortization to revenue. Morningstar calculates the ratio by using the underlying data reported in the company filings or reports: EBITDA / Revenue.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11007
        /// </remarks>
        [JsonProperty("11007")]
        public EBITDAMargin EBITDAMargin => _eBITDAMargin ??= new(_timeProvider, _securityIdentifier);
        private EBITDAMargin _eBITDAMargin;

        /// <summary>
        /// Refers to the ratio of Revenue to Employees. Morningstar calculates the ratio by using the underlying data reported in the company filings or reports: Revenue / Employee Number.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11008
        /// </remarks>
        [JsonProperty("11008")]
        public SalesPerEmployee SalesPerEmployee => _salesPerEmployee ??= new(_timeProvider, _securityIdentifier);
        private SalesPerEmployee _salesPerEmployee;

        /// <summary>
        /// Refers to the ratio of Current Assets to Current Liabilities. Morningstar calculates the ratio by using the underlying data reported in the Balance Sheet within the company filings or reports: Current Assets / Current Liabilities.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11009
        /// </remarks>
        [JsonProperty("11009")]
        public CurrentRatio CurrentRatio => _currentRatio ??= new(_timeProvider, _securityIdentifier);
        private CurrentRatio _currentRatio;

        /// <summary>
        /// Refers to the ratio of liquid assets to Current Liabilities. Morningstar calculates the ratio by using the underlying data reported in the Balance Sheet within the company filings or reports:(Cash, Cash Equivalents, and Short Term Investments + Receivables ) / Current Liabilities.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11010
        /// </remarks>
        [JsonProperty("11010")]
        public QuickRatio QuickRatio => _quickRatio ??= new(_timeProvider, _securityIdentifier);
        private QuickRatio _quickRatio;

        /// <summary>
        /// Refers to the ratio of Long Term Debt to Total Capital. Morningstar calculates the ratio by using the underlying data reported in the Balance Sheet within the company filings or reports: Long-Term Debt And Capital Lease Obligation / (Long-Term Debt And Capital Lease Obligation + Total Shareholder's Equity)
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11011
        /// </remarks>
        [JsonProperty("11011")]
        public LongTermDebtTotalCapitalRatio LongTermDebtTotalCapitalRatio => _longTermDebtTotalCapitalRatio ??= new(_timeProvider, _securityIdentifier);
        private LongTermDebtTotalCapitalRatio _longTermDebtTotalCapitalRatio;

        /// <summary>
        /// Refers to the ratio of EBIT to Interest Expense. Morningstar calculates the ratio by using the underlying data reported in the Income Statement within the company filings or reports: EBIT / Interest Expense.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11012
        /// </remarks>
        [JsonProperty("11012")]
        public InterestCoverage InterestCoverage => _interestCoverage ??= new(_timeProvider, _securityIdentifier);
        private InterestCoverage _interestCoverage;

        /// <summary>
        /// Refers to the ratio of Long Term Debt to Common Equity. Morningstar calculates the ratio by using the underlying data reported in the Balance Sheet within the company filings or reports: Long-Term Debt And Capital Lease Obligation / Common Equity. [Note: Common Equity = Total Shareholder's Equity - Preferred Stock]
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11013
        /// </remarks>
        [JsonProperty("11013")]
        public LongTermDebtEquityRatio LongTermDebtEquityRatio => _longTermDebtEquityRatio ??= new(_timeProvider, _securityIdentifier);
        private LongTermDebtEquityRatio _longTermDebtEquityRatio;

        /// <summary>
        /// Refers to the ratio of Total Assets to Common Equity. Morningstar calculates the ratio by using the underlying data reported in the Balance Sheet within the company filings or reports: Total Assets / Common Equity. [Note: Common Equity = Total Shareholder's Equity - Preferred Stock]
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11014
        /// </remarks>
        [JsonProperty("11014")]
        public FinancialLeverage FinancialLeverage => _financialLeverage ??= new(_timeProvider, _securityIdentifier);
        private FinancialLeverage _financialLeverage;

        /// <summary>
        /// Refers to the ratio of Total Debt to Common Equity. Morningstar calculates the ratio by using the underlying data reported in the Balance Sheet within the company filings or reports: (Current Debt And Current Capital Lease Obligation + Long-Term Debt And Long-Term Capital Lease Obligation / Common Equity. [Note: Common Equity = Total Shareholder's Equity - Preferred Stock]
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11015
        /// </remarks>
        [JsonProperty("11015")]
        public TotalDebtEquityRatio TotalDebtEquityRatio => _totalDebtEquityRatio ??= new(_timeProvider, _securityIdentifier);
        private TotalDebtEquityRatio _totalDebtEquityRatio;

        /// <summary>
        /// Normalized Income / Total Revenue. A measure of profitability of the company calculated by finding Normalized Net Profit as a percentage of Total Revenues.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 11016
        /// </remarks>
        [JsonProperty("11016")]
        public NormalizedNetProfitMargin NormalizedNetProfitMargin => _normalizedNetProfitMargin ??= new(_timeProvider, _securityIdentifier);
        private NormalizedNetProfitMargin _normalizedNetProfitMargin;

        /// <summary>
        /// 365 / Receivable Turnover
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12001
        /// </remarks>
        [JsonProperty("12001")]
        public DaysInSales DaysInSales => _daysInSales ??= new(_timeProvider, _securityIdentifier);
        private DaysInSales _daysInSales;

        /// <summary>
        /// 365 / Inventory turnover
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12002
        /// </remarks>
        [JsonProperty("12002")]
        public DaysInInventory DaysInInventory => _daysInInventory ??= new(_timeProvider, _securityIdentifier);
        private DaysInInventory _daysInInventory;

        /// <summary>
        /// 365 / Payable turnover
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12003
        /// </remarks>
        [JsonProperty("12003")]
        public DaysInPayment DaysInPayment => _daysInPayment ??= new(_timeProvider, _securityIdentifier);
        private DaysInPayment _daysInPayment;

        /// <summary>
        /// Days In Inventory + Days In Sales - Days In Payment
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12004
        /// </remarks>
        [JsonProperty("12004")]
        public CashConversionCycle CashConversionCycle => _cashConversionCycle ??= new(_timeProvider, _securityIdentifier);
        private CashConversionCycle _cashConversionCycle;

        /// <summary>
        /// Revenue / Average Accounts Receivables
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12005
        /// </remarks>
        [JsonProperty("12005")]
        public ReceivableTurnover ReceivableTurnover => _receivableTurnover ??= new(_timeProvider, _securityIdentifier);
        private ReceivableTurnover _receivableTurnover;

        /// <summary>
        /// Cost Of Goods Sold / Average Inventory
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12006
        /// </remarks>
        [JsonProperty("12006")]
        public InventoryTurnover InventoryTurnover => _inventoryTurnover ??= new(_timeProvider, _securityIdentifier);
        private InventoryTurnover _inventoryTurnover;

        /// <summary>
        /// Cost of Goods Sold / Average Accounts Payables
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12007
        /// </remarks>
        [JsonProperty("12007")]
        public PaymentTurnover PaymentTurnover => _paymentTurnover ??= new(_timeProvider, _securityIdentifier);
        private PaymentTurnover _paymentTurnover;

        /// <summary>
        /// Revenue / Average PP&amp;E
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12008
        /// </remarks>
        [JsonProperty("12008")]
        public FixAssetsTuronver FixAssetsTuronver => _fixAssetsTuronver ??= new(_timeProvider, _securityIdentifier);
        private FixAssetsTuronver _fixAssetsTuronver;

        /// <summary>
        /// Revenue / Average Total Assets
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12009
        /// </remarks>
        [JsonProperty("12009")]
        public AssetsTurnover AssetsTurnover => _assetsTurnover ??= new(_timeProvider, _securityIdentifier);
        private AssetsTurnover _assetsTurnover;

        /// <summary>
        /// Net Income / Average Total Common Equity
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12010
        /// </remarks>
        [JsonProperty("12010")]
        public ROE ROE => _rOE ??= new(_timeProvider, _securityIdentifier);
        private ROE _rOE;

        /// <summary>
        /// Net Income / Average Total Assets
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12011
        /// </remarks>
        [JsonProperty("12011")]
        public ROA ROA => _rOA ??= new(_timeProvider, _securityIdentifier);
        private ROA _rOA;

        /// <summary>
        /// Net Income / (Total Equity + Long-term Debt and Capital Lease Obligation + Short-term Debt and Capital Lease Obligation)
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12012
        /// </remarks>
        [JsonProperty("12012")]
        public ROIC ROIC => _rOIC ??= new(_timeProvider, _securityIdentifier);
        private ROIC _rOIC;

        /// <summary>
        /// Free Cash flow / Revenue
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12013
        /// </remarks>
        [JsonProperty("12013")]
        public FCFSalesRatio FCFSalesRatio => _fCFSalesRatio ??= new(_timeProvider, _securityIdentifier);
        private FCFSalesRatio _fCFSalesRatio;

        /// <summary>
        /// Free Cash Flow / Net Income
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12014
        /// </remarks>
        [JsonProperty("12014")]
        public FCFNetIncomeRatio FCFNetIncomeRatio => _fCFNetIncomeRatio ??= new(_timeProvider, _securityIdentifier);
        private FCFNetIncomeRatio _fCFNetIncomeRatio;

        /// <summary>
        /// Capital Expenditure / Revenue
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12015
        /// </remarks>
        [JsonProperty("12015")]
        public CapExSalesRatio CapExSalesRatio => _capExSalesRatio ??= new(_timeProvider, _securityIdentifier);
        private CapExSalesRatio _capExSalesRatio;

        /// <summary>
        /// This is a leverage ratio used to determine how much debt (a sum of long term and current portion of debt) a company has on its balance sheet relative to total assets. This ratio examines the percent of the company that is financed by debt.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12016
        /// </remarks>
        [JsonProperty("12016")]
        public DebtToAssets DebtToAssets => _debtToAssets ??= new(_timeProvider, _securityIdentifier);
        private DebtToAssets _debtToAssets;

        /// <summary>
        /// This is a financial ratio of common stock equity to total assets that indicates the relative proportion of equity used to finance a company's assets.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12017
        /// </remarks>
        [JsonProperty("12017")]
        public CommonEquityToAssets CommonEquityToAssets => _commonEquityToAssets ??= new(_timeProvider, _securityIdentifier);
        private CommonEquityToAssets _commonEquityToAssets;

        /// <summary>
        /// This is the compound annual growth rate of the company's capital spending over the last 5 years. Capital Spending is the sum of the Capital Expenditure items found in the Statement of Cash Flows.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12018
        /// </remarks>
        [JsonProperty("12018")]
        public CapitalExpenditureAnnual5YrGrowth CapitalExpenditureAnnual5YrGrowth => _capitalExpenditureAnnual5YrGrowth ??= new(_timeProvider, _securityIdentifier);
        private CapitalExpenditureAnnual5YrGrowth _capitalExpenditureAnnual5YrGrowth;

        /// <summary>
        /// This is the compound annual growth rate of the company's Gross Profit over the last 5 years.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12019
        /// </remarks>
        [JsonProperty("12019")]
        public GrossProfitAnnual5YrGrowth GrossProfitAnnual5YrGrowth => _grossProfitAnnual5YrGrowth ??= new(_timeProvider, _securityIdentifier);
        private GrossProfitAnnual5YrGrowth _grossProfitAnnual5YrGrowth;

        /// <summary>
        /// This is the simple average of the company's Annual Gross Margin over the last 5 years. Gross Margin is Total Revenue minus Cost of Goods Sold divided by Total Revenue and is expressed as a percentage.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12020
        /// </remarks>
        [JsonProperty("12020")]
        public GrossMargin5YrAvg GrossMargin5YrAvg => _grossMargin5YrAvg ??= new(_timeProvider, _securityIdentifier);
        private GrossMargin5YrAvg _grossMargin5YrAvg;

        /// <summary>
        /// This is the simple average of the company's Annual Post Tax Margin over the last 5 years. Post tax margin is Post tax divided by total revenue for the same period.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12021
        /// </remarks>
        [JsonProperty("12021")]
        public PostTaxMargin5YrAvg PostTaxMargin5YrAvg => _postTaxMargin5YrAvg ??= new(_timeProvider, _securityIdentifier);
        private PostTaxMargin5YrAvg _postTaxMargin5YrAvg;

        /// <summary>
        /// This is the simple average of the company's Annual Pre Tax Margin over the last 5 years. Pre tax margin is Pre tax divided by total revenue for the same period.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12022
        /// </remarks>
        [JsonProperty("12022")]
        public PreTaxMargin5YrAvg PreTaxMargin5YrAvg => _preTaxMargin5YrAvg ??= new(_timeProvider, _securityIdentifier);
        private PreTaxMargin5YrAvg _preTaxMargin5YrAvg;

        /// <summary>
        /// This is the simple average of the company's Annual Net Profit Margin over the last 5 years. Net profit margin is post tax income divided by total revenue for the same period.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12023
        /// </remarks>
        [JsonProperty("12023")]
        public ProfitMargin5YrAvg ProfitMargin5YrAvg => _profitMargin5YrAvg ??= new(_timeProvider, _securityIdentifier);
        private ProfitMargin5YrAvg _profitMargin5YrAvg;

        /// <summary>
        /// This is the simple average of the company's ROE over the last 5 years. Return on equity reveals how much profit a company has earned in comparison to the total amount of shareholder equity found on the balance sheet.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12024
        /// </remarks>
        [JsonProperty("12024")]
        public ROE5YrAvg ROE5YrAvg => _rOE5YrAvg ??= new(_timeProvider, _securityIdentifier);
        private ROE5YrAvg _rOE5YrAvg;

        /// <summary>
        /// This is the simple average of the company's ROA over the last 5 years. Return on asset is calculated by dividing a company's annual earnings by its average total assets.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12025
        /// </remarks>
        [JsonProperty("12025")]
        public ROA5YrAvg ROA5YrAvg => _rOA5YrAvg ??= new(_timeProvider, _securityIdentifier);
        private ROA5YrAvg _rOA5YrAvg;

        /// <summary>
        /// This is the simple average of the company's ROIC over the last 5 years. Return on invested capital is calculated by taking net operating profit after taxes and dividends and dividing by the total amount of capital invested and expressing the result as a percentage.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12026
        /// </remarks>
        [JsonProperty("12026")]
        public AVG5YrsROIC AVG5YrsROIC => _aVG5YrsROIC ??= new(_timeProvider, _securityIdentifier);
        private AVG5YrsROIC _aVG5YrsROIC;

        /// <summary>
        /// [Normalized Income + (Interest Expense * (1-Tax Rate))] / Invested Capital
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12027
        /// </remarks>
        [JsonProperty("12027")]
        public NormalizedROIC NormalizedROIC => _normalizedROIC ??= new(_timeProvider, _securityIdentifier);
        private NormalizedROIC _normalizedROIC;

        /// <summary>
        /// The five-year growth rate of operating revenue, calculated using regression analysis.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12028
        /// </remarks>
        [JsonProperty("12028")]
        public RegressionGrowthOperatingRevenue5Years RegressionGrowthOperatingRevenue5Years => _regressionGrowthOperatingRevenue5Years ??= new(_timeProvider, _securityIdentifier);
        private RegressionGrowthOperatingRevenue5Years _regressionGrowthOperatingRevenue5Years;

        /// <summary>
        /// Indicates a company's short-term liquidity, defined as short term liquid investments (cash, cash equivalents, short term investments) divided by current liabilities.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12029
        /// </remarks>
        [JsonProperty("12029")]
        public CashRatio CashRatio => _cashRatio ??= new(_timeProvider, _securityIdentifier);
        private CashRatio _cashRatio;

        /// <summary>
        /// Represents the percentage of a company's total assets is in cash.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12030
        /// </remarks>
        [JsonProperty("12030")]
        public CashtoTotalAssets CashtoTotalAssets => _cashtoTotalAssets ??= new(_timeProvider, _securityIdentifier);
        private CashtoTotalAssets _cashtoTotalAssets;

        /// <summary>
        /// Measures the amount a company is investing in its business relative to EBITDA generated in a given period.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12031
        /// </remarks>
        [JsonProperty("12031")]
        public CapitalExpendituretoEBITDA CapitalExpendituretoEBITDA => _capitalExpendituretoEBITDA ??= new(_timeProvider, _securityIdentifier);
        private CapitalExpendituretoEBITDA _capitalExpendituretoEBITDA;

        /// <summary>
        /// Indicates the percentage of a company's operating cash flow is free to be invested in its business after capital expenditures.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12032
        /// </remarks>
        [JsonProperty("12032")]
        public FCFtoCFO FCFtoCFO => _fCFtoCFO ??= new(_timeProvider, _securityIdentifier);
        private FCFtoCFO _fCFtoCFO;

        /// <summary>
        /// The growth in the stockholder's equity on a percentage basis. Morningstar calculates the growth percentage based on the residual interest in the assets of the enterprise that remains after deducting its liabilities reported in the Balance Sheet within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12033
        /// </remarks>
        [JsonProperty("12033")]
        public StockholdersEquityGrowth StockholdersEquityGrowth => _stockholdersEquityGrowth ??= new(_timeProvider, _securityIdentifier);
        private StockholdersEquityGrowth _stockholdersEquityGrowth;

        /// <summary>
        /// The growth in the total assets on a percentage basis. Morningstar calculates the growth percentage based on the total assets reported in the Balance Sheet within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12034
        /// </remarks>
        [JsonProperty("12034")]
        public TotalAssetsGrowth TotalAssetsGrowth => _totalAssetsGrowth ??= new(_timeProvider, _securityIdentifier);
        private TotalAssetsGrowth _totalAssetsGrowth;

        /// <summary>
        /// The growth in the total liabilities on a percentage basis. Morningstar calculates the growth percentage based on the total liabilities reported in the Balance Sheet within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12035
        /// </remarks>
        [JsonProperty("12035")]
        public TotalLiabilitiesGrowth TotalLiabilitiesGrowth => _totalLiabilitiesGrowth ??= new(_timeProvider, _securityIdentifier);
        private TotalLiabilitiesGrowth _totalLiabilitiesGrowth;

        /// <summary>
        /// The growth in the company's total debt to equity ratio on a percentage basis. Morningstar calculates the growth percentage based on the total debt divided by the shareholder's equity reported in the Balance Sheet within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12036
        /// </remarks>
        [JsonProperty("12036")]
        public TotalDebtEquityRatioGrowth TotalDebtEquityRatioGrowth => _totalDebtEquityRatioGrowth ??= new(_timeProvider, _securityIdentifier);
        private TotalDebtEquityRatioGrowth _totalDebtEquityRatioGrowth;

        /// <summary>
        /// The growth in the company's cash ratio on a percentage basis. Morningstar calculates the growth percentage based on the short term liquid investments (cash, cash equivalents, short term investments) divided by current liabilities reported in the Balance Sheet within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12037
        /// </remarks>
        [JsonProperty("12037")]
        public CashRatioGrowth CashRatioGrowth => _cashRatioGrowth ??= new(_timeProvider, _securityIdentifier);
        private CashRatioGrowth _cashRatioGrowth;

        /// <summary>
        /// The growth in the company's EBITDA on a percentage basis. Morningstar calculates the growth percentage based on the earnings minus expenses (excluding interest, tax, depreciation, and amortization expenses) reported in the Financial Statements within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12038
        /// </remarks>
        [JsonProperty("12038")]
        public EBITDAGrowth EBITDAGrowth => _eBITDAGrowth ??= new(_timeProvider, _securityIdentifier);
        private EBITDAGrowth _eBITDAGrowth;

        /// <summary>
        /// The growth in the company's cash flows from financing on a percentage basis. Morningstar calculates the growth percentage based on the financing cash flows reported in the Cash Flow Statement within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12039
        /// </remarks>
        [JsonProperty("12039")]
        public CashFlowFromFinancingGrowth CashFlowFromFinancingGrowth => _cashFlowFromFinancingGrowth ??= new(_timeProvider, _securityIdentifier);
        private CashFlowFromFinancingGrowth _cashFlowFromFinancingGrowth;

        /// <summary>
        /// The growth in the company's cash flows from investing on a percentage basis. Morningstar calculates the growth percentage based on the cash flows from investing reported in the Cash Flow Statement within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12040
        /// </remarks>
        [JsonProperty("12040")]
        public CashFlowFromInvestingGrowth CashFlowFromInvestingGrowth => _cashFlowFromInvestingGrowth ??= new(_timeProvider, _securityIdentifier);
        private CashFlowFromInvestingGrowth _cashFlowFromInvestingGrowth;

        /// <summary>
        /// The growth in the company's capital expenditures on a percentage basis. Morningstar calculates the growth percentage based on the capital expenditures reported in the Cash Flow Statement within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12041
        /// </remarks>
        [JsonProperty("12041")]
        public CapExGrowth CapExGrowth => _capExGrowth ??= new(_timeProvider, _securityIdentifier);
        private CapExGrowth _capExGrowth;

        /// <summary>
        /// The growth in the company's current ratio on a percentage basis. Morningstar calculates the growth percentage based on the current assets divided by current liabilities reported in the Balance Sheet within the company filings or reports.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12042
        /// </remarks>
        [JsonProperty("12042")]
        public CurrentRatioGrowth CurrentRatioGrowth => _currentRatioGrowth ??= new(_timeProvider, _securityIdentifier);
        private CurrentRatioGrowth _currentRatioGrowth;

        /// <summary>
        /// Total revenue / working capital (current assets minus current liabilities)
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12043
        /// </remarks>
        [JsonProperty("12043")]
        public WorkingCapitalTurnoverRatio WorkingCapitalTurnoverRatio => _workingCapitalTurnoverRatio ??= new(_timeProvider, _securityIdentifier);
        private WorkingCapitalTurnoverRatio _workingCapitalTurnoverRatio;

        /// <summary>
        /// Refers to the ratio of Net Income to Employees. Morningstar calculates the ratio by using the underlying data reported in the company filings or reports: Net Income / Employee Number.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12044
        /// </remarks>
        [JsonProperty("12044")]
        public NetIncomePerEmployee NetIncomePerEmployee => _netIncomePerEmployee ??= new(_timeProvider, _securityIdentifier);
        private NetIncomePerEmployee _netIncomePerEmployee;

        /// <summary>
        /// Measure of whether a company's cash flow is sufficient to meet its short-term and long-term debt requirements. The lower this ratio is, the greater the probability that the company will be in financial distress. Net Income + Depreciation, Depletion and Amortization/ average of annual Total Liabilities over the most recent two periods.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12045
        /// </remarks>
        [JsonProperty("12045")]
        public SolvencyRatio SolvencyRatio => _solvencyRatio ??= new(_timeProvider, _securityIdentifier);
        private SolvencyRatio _solvencyRatio;

        /// <summary>
        /// A measure of operating performance for Insurance companies, as it shows the relationship between the premiums earned and administrative expenses related to claims such as fees and commissions. A number of 1 or lower is preferred, as this means the premiums exceed the expenses. Calculated as: (Deferred Policy Acquisition Amortization Expense+Fees and Commission Expense+Other Underwriting Expenses+Selling, General and Administrative) / Net Premiums Earned
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12046
        /// </remarks>
        [JsonProperty("12046")]
        public ExpenseRatio ExpenseRatio => _expenseRatio ??= new(_timeProvider, _securityIdentifier);
        private ExpenseRatio _expenseRatio;

        /// <summary>
        /// A measure of operating performance for Insurance companies, as it shows the relationship between the premiums earned and the expenses related to claims. A number of 1 or lower is preferred, as this means the premiums exceed the expenses. Calculated as: Benefits, Claims and Loss Adjustment Expense, Net / Net Premiums Earned
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 12047
        /// </remarks>
        [JsonProperty("12047")]
        public LossRatio LossRatio => _lossRatio ??= new(_timeProvider, _securityIdentifier);
        private LossRatio _lossRatio;

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public OperationRatios(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier)
            : base(timeProvider, securityIdentifier)
        {
        }

        /// <summary>
        /// Clones this instance
        /// </summary>
        public override FundamentalTimeDependentProperty Clone(ITimeProvider timeProvider)
        {
            return new OperationRatios(timeProvider, _securityIdentifier);
        }
    }
}
