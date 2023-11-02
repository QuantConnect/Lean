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
    /// Definition of the IncomeStatement class
    /// </summary>
    public class IncomeStatement : ReusuableCLRObject
    {
        /// <summary>
        /// Filing date of the Income Statement.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20423
        /// </remarks>
        [JsonProperty("20423")]
        public IncomeStatementFileDate ISFileDate => _iSFileDate ??= new(_timeProvider, _securityIdentifier);
        private IncomeStatementFileDate _iSFileDate;

        /// <summary>
        /// The non-cash expense recognized on intangible assets over the benefit period of the asset.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20007
        /// </remarks>
        [JsonProperty("20007")]
        public AmortizationIncomeStatement Amortization => _amortization ??= new(_timeProvider, _securityIdentifier);
        private AmortizationIncomeStatement _amortization;

        /// <summary>
        /// The gradual elimination of a liability, such as a mortgage, in regular payments over a specified period of time. Such payments must be sufficient to cover both principal and interest.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20008
        /// </remarks>
        [JsonProperty("20008")]
        public SecuritiesAmortizationIncomeStatement SecuritiesAmortization => _securitiesAmortization ??= new(_timeProvider, _securityIdentifier);
        private SecuritiesAmortizationIncomeStatement _securitiesAmortization;

        /// <summary>
        /// The aggregate cost of goods produced and sold and services rendered during the reporting period. It excludes all operating expenses such as depreciation, depletion, amortization, and SG&amp;A. For the must have cost industry, if the number is not reported by the company, it will be calculated based on accounting equation. Cost of Revenue = Revenue - Operating Expenses - Operating Profit.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20013
        /// </remarks>
        [JsonProperty("20013")]
        public CostOfRevenueIncomeStatement CostOfRevenue => _costOfRevenue ??= new(_timeProvider, _securityIdentifier);
        private CostOfRevenueIncomeStatement _costOfRevenue;

        /// <summary>
        /// The non-cash expense recognized on natural resources (eg. Oil and mineral deposits) over the benefit period of the asset.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20017
        /// </remarks>
        [JsonProperty("20017")]
        public DepletionIncomeStatement Depletion => _depletion ??= new(_timeProvider, _securityIdentifier);
        private DepletionIncomeStatement _depletion;

        /// <summary>
        /// The current period non-cash expense recognized on tangible assets used in the normal course of business, by allocating the cost of assets over their useful lives, in the Income Statement. Examples of tangible asset include buildings, production and equipment.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20018
        /// </remarks>
        [JsonProperty("20018")]
        public DepreciationIncomeStatement Depreciation => _depreciation ??= new(_timeProvider, _securityIdentifier);
        private DepreciationIncomeStatement _depreciation;

        /// <summary>
        /// The sum of depreciation and amortization expense in the Income Statement. Depreciation is the non-cash expense recognized on tangible assets used in the normal course of business, by allocating the cost of assets over their useful lives Amortization is the non-cash expense recognized on intangible assets over the benefit period of the asset.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20019
        /// </remarks>
        [JsonProperty("20019")]
        public DepreciationAndAmortizationIncomeStatement DepreciationAndAmortization => _depreciationAndAmortization ??= new(_timeProvider, _securityIdentifier);
        private DepreciationAndAmortizationIncomeStatement _depreciationAndAmortization;

        /// <summary>
        /// The sum of depreciation, amortization and depletion expense in the Income Statement. Depreciation is the non-cash expense recognized on tangible assets used in the normal course of business, by allocating the cost of assets over their useful lives Amortization is the non-cash expense recognized on intangible assets over the benefit period of the asset. Depletion is the non-cash expense recognized on natural resources (eg. Oil and mineral deposits) over the benefit period of the asset.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20020
        /// </remarks>
        [JsonProperty("20020")]
        public DepreciationAmortizationDepletionIncomeStatement DepreciationAmortizationDepletion => _depreciationAmortizationDepletion ??= new(_timeProvider, _securityIdentifier);
        private DepreciationAmortizationDepletionIncomeStatement _depreciationAmortizationDepletion;

        /// <summary>
        /// To be classified as discontinued operations, if both of the following conditions are met: 1: The operations and cash flow of the component have been or will be removed from the ongoing operations of the entity as a result of the disposal transaction, and 2: The entity will have no significant continuing involvement in the operations of the component after the disposal transaction. The discontinued operation is reported net of tax. Gains/Loss on Disposal of Discontinued Operations: Any gains or loss recognized on disposal of discontinued operations, which is the difference between the carrying value of the division and its fair value less costs to sell. Provision for Gain/Loss on Disposal: The amount of current expense charged in order to prepare for the disposal of discontinued operations.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20022
        /// </remarks>
        [JsonProperty("20022")]
        public NetIncomeDiscontinuousOperationsIncomeStatement NetIncomeDiscontinuousOperations => _netIncomeDiscontinuousOperations ??= new(_timeProvider, _securityIdentifier);
        private NetIncomeDiscontinuousOperationsIncomeStatement _netIncomeDiscontinuousOperations;

        /// <summary>
        /// Excise taxes are taxes paid when purchases are made on a specific good, such as gasoline. Excise taxes are often included in the price of the product. There are also excise taxes on activities, such as on wagering or on highway usage by trucks.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20028
        /// </remarks>
        [JsonProperty("20028")]
        public ExciseTaxesIncomeStatement ExciseTaxes => _exciseTaxes ??= new(_timeProvider, _securityIdentifier);
        private ExciseTaxesIncomeStatement _exciseTaxes;

        /// <summary>
        /// Gains (losses), whether arising from extinguishment of debt, prior period adjustments, or from other events or transactions, that are both unusual in nature and infrequent in occurrence thereby meeting the criteria for an event or transaction to be classified as an extraordinary item.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20030
        /// </remarks>
        [JsonProperty("20030")]
        public NetIncomeExtraordinaryIncomeStatement NetIncomeExtraordinary => _netIncomeExtraordinary ??= new(_timeProvider, _securityIdentifier);
        private NetIncomeExtraordinaryIncomeStatement _netIncomeExtraordinary;

        /// <summary>
        /// The aggregate amount of fees, commissions, and other income.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20031
        /// </remarks>
        [JsonProperty("20031")]
        public FeeRevenueAndOtherIncomeIncomeStatement FeeRevenueAndOtherIncome => _feeRevenueAndOtherIncome ??= new(_timeProvider, _securityIdentifier);
        private FeeRevenueAndOtherIncomeIncomeStatement _feeRevenueAndOtherIncome;

        /// <summary>
        /// The aggregate total of general managing and administering expenses for the company.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20045
        /// </remarks>
        [JsonProperty("20045")]
        public GeneralAndAdministrativeExpenseIncomeStatement GeneralAndAdministrativeExpense => _generalAndAdministrativeExpense ??= new(_timeProvider, _securityIdentifier);
        private GeneralAndAdministrativeExpenseIncomeStatement _generalAndAdministrativeExpense;

        /// <summary>
        /// Total revenue less cost of revenue. The number is as reported by the company on the income statement; however, the number will be calculated if it is not reported. This field is null if the cost of revenue is not given. Gross Profit = Total Revenue - Cost of Revenue.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20046
        /// </remarks>
        [JsonProperty("20046")]
        public GrossProfitIncomeStatement GrossProfit => _grossProfit ??= new(_timeProvider, _securityIdentifier);
        private GrossProfitIncomeStatement _grossProfit;

        /// <summary>
        /// Relates to the general cost of borrowing money. It is the price that a lender charges a borrower for the use of the lender's money.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20057
        /// </remarks>
        [JsonProperty("20057")]
        public InterestExpenseIncomeStatement InterestExpense => _interestExpense ??= new(_timeProvider, _securityIdentifier);
        private InterestExpenseIncomeStatement _interestExpense;

        /// <summary>
        /// Interest expense caused by long term financing activities; such as interest expense incurred on trading liabilities, commercial paper, long-term debt, capital leases, deposits, and all other borrowings.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20064
        /// </remarks>
        [JsonProperty("20064")]
        public InterestExpenseNonOperatingIncomeStatement InterestExpenseNonOperating => _interestExpenseNonOperating ??= new(_timeProvider, _securityIdentifier);
        private InterestExpenseNonOperatingIncomeStatement _interestExpenseNonOperating;

        /// <summary>
        /// Net interest and dividend income or expense, including any amortization and accretion (as applicable) of discounts and premiums, including consideration of the provisions for loan, lease, credit, and other related losses, if any.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20066
        /// </remarks>
        [JsonProperty("20066")]
        public InterestIncomeAfterProvisionForLoanLossIncomeStatement InterestIncomeAfterProvisionForLoanLoss => _interestIncomeAfterProvisionForLoanLoss ??= new(_timeProvider, _securityIdentifier);
        private InterestIncomeAfterProvisionForLoanLossIncomeStatement _interestIncomeAfterProvisionForLoanLoss;

        /// <summary>
        /// Interest income earned from long term financing activities.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20075
        /// </remarks>
        [JsonProperty("20075")]
        public InterestIncomeNonOperatingIncomeStatement InterestIncomeNonOperating => _interestIncomeNonOperating ??= new(_timeProvider, _securityIdentifier);
        private InterestIncomeNonOperatingIncomeStatement _interestIncomeNonOperating;

        /// <summary>
        /// Net-Non Operating interest income or expenses caused by financing activities.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20077
        /// </remarks>
        [JsonProperty("20077")]
        public NetNonOperatingInterestIncomeExpenseIncomeStatement NetNonOperatingInterestIncomeExpense => _netNonOperatingInterestIncomeExpense ??= new(_timeProvider, _securityIdentifier);
        private NetNonOperatingInterestIncomeExpenseIncomeStatement _netNonOperatingInterestIncomeExpense;

        /// <summary>
        /// Losses generally refer to (1) the amount of reduction in the value of an insured's property caused by an insured peril, (2) the amount sought through an insured's claim, or (3) the amount paid on behalf of an insured under an insurance contract. Loss Adjustment Expenses is expenses incurred in the course of investigating and settling claims that includes any legal and adjusters' fees and the costs of paying claims and all related expenses.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20084
        /// </remarks>
        [JsonProperty("20084")]
        public LossAdjustmentExpenseIncomeStatement LossAdjustmentExpense => _lossAdjustmentExpense ??= new(_timeProvider, _securityIdentifier);
        private LossAdjustmentExpenseIncomeStatement _lossAdjustmentExpense;

        /// <summary>
        /// Represents par or stated value of the subsidiary stock not owned by the parent company plus the minority interest's equity in the surplus of the subsidiary. This item includes preferred dividend averages on the minority preferred stock (preferred shares not owned by the reporting parent company). Minority interest also refers to stockholders who own less than 50% of a subsidiary's outstanding voting common stock. The minority stockholders hold an interest in the subsidiary's net assets and share earnings with the parent company.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20087
        /// </remarks>
        [JsonProperty("20087")]
        public MinorityInterestsIncomeStatement MinorityInterests => _minorityInterests ??= new(_timeProvider, _securityIdentifier);
        private MinorityInterestsIncomeStatement _minorityInterests;

        /// <summary>
        /// Includes all the operations (continuing and discontinued) and all the other income or charges (extraordinary, accounting changes, tax loss carry forward, and other gains and losses).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20091
        /// </remarks>
        [JsonProperty("20091")]
        public NetIncomeIncomeStatement NetIncome => _netIncome ??= new(_timeProvider, _securityIdentifier);
        private NetIncomeIncomeStatement _netIncome;

        /// <summary>
        /// Net income minus the preferred dividends paid as presented in the Income Statement.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20093
        /// </remarks>
        [JsonProperty("20093")]
        public NetIncomeCommonStockholdersIncomeStatement NetIncomeCommonStockholders => _netIncomeCommonStockholders ??= new(_timeProvider, _securityIdentifier);
        private NetIncomeCommonStockholdersIncomeStatement _netIncomeCommonStockholders;

        /// <summary>
        /// Revenue less expenses and taxes from the entity's ongoing operations and before income (loss) from: Preferred Dividends; Extraordinary Gains and Losses; Income from Cumulative Effects of Accounting Change; Discontinuing Operation; Income from Tax Loss Carry forward; Other Gains/Losses.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20094
        /// </remarks>
        [JsonProperty("20094")]
        public NetIncomeContinuousOperationsIncomeStatement NetIncomeContinuousOperations => _netIncomeContinuousOperations ??= new(_timeProvider, _securityIdentifier);
        private NetIncomeContinuousOperationsIncomeStatement _netIncomeContinuousOperations;

        /// <summary>
        /// Total interest income minus total interest expense. It represents the difference between interest and dividends earned on interest- bearing assets and interest paid to depositors and other creditors.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20095
        /// </remarks>
        [JsonProperty("20095")]
        public NetInterestIncomeIncomeStatement NetInterestIncome => _netInterestIncome ??= new(_timeProvider, _securityIdentifier);
        private NetInterestIncomeIncomeStatement _netInterestIncome;

        /// <summary>
        /// Total of interest, dividends, and other earnings derived from the insurance company's invested assets minus the expenses associated with these investments. Excluded from this income are capital gains or losses as the result of the sale of assets, as well as any unrealized capital gains or losses.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20096
        /// </remarks>
        [JsonProperty("20096")]
        public NetInvestmentIncomeIncomeStatement NetInvestmentIncome => _netInvestmentIncome ??= new(_timeProvider, _securityIdentifier);
        private NetInvestmentIncomeIncomeStatement _netInvestmentIncome;

        /// <summary>
        /// All sales, business revenues and income that the company makes from its business operations, net of excise taxes. This applies for all companies and can be used as comparison for all industries. For Normal, Mining, Transportation and Utility templates companies, this is the sum of Operating Revenues, Excise Taxes and Fees. For Bank template companies, this is the sum of Net Interest Income and Non-Interest Income. For Insurance template companies, this is the sum of Premiums, Interest Income, Fees, Investment and Other Income.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20100
        /// </remarks>
        [JsonProperty("20100")]
        public TotalRevenueIncomeStatement TotalRevenue => _totalRevenue ??= new(_timeProvider, _securityIdentifier);
        private TotalRevenueIncomeStatement _totalRevenue;

        /// <summary>
        /// Any expenses that not related to interest. It includes labor and related expense, occupancy and equipment, commission, professional expense and contract services expenses, selling, general and administrative, research and development depreciation, amortization and depletion, and any other special income/charges.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20105
        /// </remarks>
        [JsonProperty("20105")]
        public NonInterestExpenseIncomeStatement NonInterestExpense => _nonInterestExpense ??= new(_timeProvider, _securityIdentifier);
        private NonInterestExpenseIncomeStatement _nonInterestExpense;

        /// <summary>
        /// The total amount of non-interest income which may be derived from: (1) fees and commissions; (2) premiums earned; (3) equity investment; (4) the sale or disposal of assets; and (5) other sources not otherwise specified.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20106
        /// </remarks>
        [JsonProperty("20106")]
        public NonInterestIncomeIncomeStatement NonInterestIncome => _nonInterestIncome ??= new(_timeProvider, _securityIdentifier);
        private NonInterestIncomeIncomeStatement _nonInterestIncome;

        /// <summary>
        /// Operating expenses are primary recurring costs associated with central operations (other than cost of goods sold) that are incurred in order to generate sales.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20108
        /// </remarks>
        [JsonProperty("20108")]
        public OperatingExpenseIncomeStatement OperatingExpense => _operatingExpense ??= new(_timeProvider, _securityIdentifier);
        private OperatingExpenseIncomeStatement _operatingExpense;

        /// <summary>
        /// Income from normal business operations after deducting cost of revenue and operating expenses. It does not include income from any investing activities.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20109
        /// </remarks>
        [JsonProperty("20109")]
        public OperatingIncomeIncomeStatement OperatingIncome => _operatingIncome ??= new(_timeProvider, _securityIdentifier);
        private OperatingIncomeIncomeStatement _operatingIncome;

        /// <summary>
        /// Sales and income that the company makes from its business operations. This applies only to non-bank and insurance companies. For Utility template companies, this is the sum of revenue from electric, gas, transportation and other operating revenue. For Transportation template companies, this is the sum of revenue-passenger, revenue-cargo, and other operating revenue.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20112
        /// </remarks>
        [JsonProperty("20112")]
        public OperatingRevenueIncomeStatement OperatingRevenue => _operatingRevenue ??= new(_timeProvider, _securityIdentifier);
        private OperatingRevenueIncomeStatement _operatingRevenue;

        /// <summary>
        /// Income or expense that comes from miscellaneous sources.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20117
        /// </remarks>
        [JsonProperty("20117")]
        public OtherIncomeExpenseIncomeStatement OtherIncomeExpense => _otherIncomeExpense ??= new(_timeProvider, _securityIdentifier);
        private OtherIncomeExpenseIncomeStatement _otherIncomeExpense;

        /// <summary>
        /// Costs that vary with and are primarily related to the acquisition of new and renewal insurance contracts. Also referred to as underwriting expenses.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20125
        /// </remarks>
        [JsonProperty("20125")]
        public PolicyAcquisitionExpenseIncomeStatement PolicyAcquisitionExpense => _policyAcquisitionExpense ??= new(_timeProvider, _securityIdentifier);
        private PolicyAcquisitionExpenseIncomeStatement _policyAcquisitionExpense;

        /// <summary>
        /// The net provision in current period for future policy benefits, claims, and claims settlement expenses incurred in the claims settlement process before the effects of reinsurance arrangements. The value is net of the effects of contracts assumed and ceded.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20129
        /// </remarks>
        [JsonProperty("20129")]
        public NetPolicyholderBenefitsAndClaimsIncomeStatement NetPolicyholderBenefitsAndClaims => _netPolicyholderBenefitsAndClaims ??= new(_timeProvider, _securityIdentifier);
        private NetPolicyholderBenefitsAndClaimsIncomeStatement _netPolicyholderBenefitsAndClaims;

        /// <summary>
        /// The amount of dividends declared or paid in the period to preferred shareholders, or the amount for which the obligation to pay them dividends arose in the period. Preferred dividends are the amount required for the current year only, and not for any amount required in past years.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20134
        /// </remarks>
        [JsonProperty("20134")]
        public PreferredStockDividendsIncomeStatement PreferredStockDividends => _preferredStockDividends ??= new(_timeProvider, _securityIdentifier);
        private PreferredStockDividendsIncomeStatement _preferredStockDividends;

        /// <summary>
        /// Premiums earned is the portion of an insurance written premium which is considered "earned" by the insurer, based on the part of the policy period that the insurance has been in effect, and during which the insurer has been exposed to loss.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20135
        /// </remarks>
        [JsonProperty("20135")]
        public TotalPremiumsEarnedIncomeStatement TotalPremiumsEarned => _totalPremiumsEarned ??= new(_timeProvider, _securityIdentifier);
        private TotalPremiumsEarnedIncomeStatement _totalPremiumsEarned;

        /// <summary>
        /// Reported income before the deduction or benefit of income taxes.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20136
        /// </remarks>
        [JsonProperty("20136")]
        public PretaxIncomeIncomeStatement PretaxIncome => _pretaxIncome ??= new(_timeProvider, _securityIdentifier);
        private PretaxIncomeIncomeStatement _pretaxIncome;

        /// <summary>
        /// Include any taxes on income, net of any investment tax credits for the current accounting period.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20145
        /// </remarks>
        [JsonProperty("20145")]
        public TaxProvisionIncomeStatement TaxProvision => _taxProvision ??= new(_timeProvider, _securityIdentifier);
        private TaxProvisionIncomeStatement _taxProvision;

        /// <summary>
        /// A charge to income which represents an expense deemed adequate by management given the composition of a bank's credit portfolios, their probability of default, the economic environment and the allowance for credit losses already established. Specific provisions are established to reduce the book value of specific assets (primarily loans) to establish the amount expected to be recovered on the loans.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20146
        /// </remarks>
        [JsonProperty("20146")]
        public CreditLossesProvisionIncomeStatement CreditLossesProvision => _creditLossesProvision ??= new(_timeProvider, _securityIdentifier);
        private CreditLossesProvisionIncomeStatement _creditLossesProvision;

        /// <summary>
        /// The aggregate amount of research and development expenses during the year.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20151
        /// </remarks>
        [JsonProperty("20151")]
        public ResearchAndDevelopmentIncomeStatement ResearchAndDevelopment => _researchAndDevelopment ??= new(_timeProvider, _securityIdentifier);
        private ResearchAndDevelopmentIncomeStatement _researchAndDevelopment;

        /// <summary>
        /// The aggregate total amount of expenses directly related to the marketing or selling of products or services.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20158
        /// </remarks>
        [JsonProperty("20158")]
        public SellingAndMarketingExpenseIncomeStatement SellingAndMarketingExpense => _sellingAndMarketingExpense ??= new(_timeProvider, _securityIdentifier);
        private SellingAndMarketingExpenseIncomeStatement _sellingAndMarketingExpense;

        /// <summary>
        /// The aggregate total costs related to selling a firm's product and services, as well as all other general and administrative expenses. Selling expenses are those directly related to the company's efforts to generate sales (e.g., sales salaries, commissions, advertising, delivery expenses). General and administrative expenses are expenses related to general administration of the company's operation (e.g., officers and office salaries, office supplies, telephone, accounting and legal services, and business licenses and fees).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20159
        /// </remarks>
        [JsonProperty("20159")]
        public SellingGeneralAndAdministrationIncomeStatement SellingGeneralAndAdministration => _sellingGeneralAndAdministration ??= new(_timeProvider, _securityIdentifier);
        private SellingGeneralAndAdministrationIncomeStatement _sellingGeneralAndAdministration;

        /// <summary>
        /// Earnings or losses attributable to occurrences or actions by the firm that is either infrequent or unusual.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20162
        /// </remarks>
        [JsonProperty("20162")]
        public SpecialIncomeChargesIncomeStatement SpecialIncomeCharges => _specialIncomeCharges ??= new(_timeProvider, _securityIdentifier);
        private SpecialIncomeChargesIncomeStatement _specialIncomeCharges;

        /// <summary>
        /// The sum of operating expense and cost of revenue. If the company does not give the reported number, it will be calculated by adding operating expense and cost of revenue.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20164
        /// </remarks>
        [JsonProperty("20164")]
        public TotalExpensesIncomeStatement TotalExpenses => _totalExpenses ??= new(_timeProvider, _securityIdentifier);
        private TotalExpensesIncomeStatement _totalExpenses;

        /// <summary>
        /// Income generated from interest-bearing deposits or accounts.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20177
        /// </remarks>
        [JsonProperty("20177")]
        public InterestIncomeIncomeStatement InterestIncome => _interestIncome ??= new(_timeProvider, _securityIdentifier);
        private InterestIncomeIncomeStatement _interestIncome;

        /// <summary>
        /// Earnings minus expenses (excluding interest and tax expenses).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20189
        /// </remarks>
        [JsonProperty("20189")]
        public EBITIncomeStatement EBIT => _eBIT ??= new(_timeProvider, _securityIdentifier);
        private EBITIncomeStatement _eBIT;

        /// <summary>
        /// Earnings minus expenses (excluding interest, tax, depreciation, and amortization expenses).
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20190
        /// </remarks>
        [JsonProperty("20190")]
        public EBITDAIncomeStatement EBITDA => _eBITDA ??= new(_timeProvider, _securityIdentifier);
        private EBITDAIncomeStatement _eBITDA;

        /// <summary>
        /// Revenue less expenses and taxes from the entity's ongoing operations net of minority interest and before income (loss) from: Preferred Dividends; Extraordinary Gains and Losses; Income from Cumulative Effects of Accounting Change; Discontinuing Operation; Income from Tax Loss Carry forward; Other Gains/Losses.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20191
        /// </remarks>
        [JsonProperty("20191")]
        public NetIncomeContinuousOperationsNetMinorityInterestIncomeStatement NetIncomeContinuousOperationsNetMinorityInterest => _netIncomeContinuousOperationsNetMinorityInterest ??= new(_timeProvider, _securityIdentifier);
        private NetIncomeContinuousOperationsNetMinorityInterestIncomeStatement _netIncomeContinuousOperationsNetMinorityInterest;

        /// <summary>
        /// The amount of premiums paid and payable to another insurer as a result of reinsurance arrangements in order to exchange for that company accepting all or part of insurance on a risk or exposure. This item is usually only available for insurance industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20201
        /// </remarks>
        [JsonProperty("20201")]
        public CededPremiumsIncomeStatement CededPremiums => _cededPremiums ??= new(_timeProvider, _securityIdentifier);
        private CededPremiumsIncomeStatement _cededPremiums;

        /// <summary>
        /// <remarks> Morningstar DataId: 20202 </remarks>
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20202
        /// </remarks>
        [JsonProperty("20202")]
        public CommissionExpensesIncomeStatement CommissionExpenses => _commissionExpenses ??= new(_timeProvider, _securityIdentifier);
        private CommissionExpensesIncomeStatement _commissionExpenses;

        /// <summary>
        /// Income earned from credit card services including late, over limit, and annual fees. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20204
        /// </remarks>
        [JsonProperty("20204")]
        public CreditCardIncomeStatement CreditCard => _creditCard ??= new(_timeProvider, _securityIdentifier);
        private CreditCardIncomeStatement _creditCard;

        /// <summary>
        /// Dividends earned from equity investment securities. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20206
        /// </remarks>
        [JsonProperty("20206")]
        public DividendIncomeIncomeStatement DividendIncome => _dividendIncome ??= new(_timeProvider, _securityIdentifier);
        private DividendIncomeIncomeStatement _dividendIncome;

        /// <summary>
        /// The earnings from equity interest can be a result of any of the following: Income from earnings distribution of the business, either as dividends paid to corporate shareholders or as drawings in a partnership; Capital gain realized upon sale of the business; Capital gain realized from selling his or her interest to other partners. This item is usually not available for bank and insurance industries.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20208
        /// </remarks>
        [JsonProperty("20208")]
        public EarningsFromEquityInterestIncomeStatement EarningsFromEquityInterest => _earningsFromEquityInterest ??= new(_timeProvider, _securityIdentifier);
        private EarningsFromEquityInterestIncomeStatement _earningsFromEquityInterest;

        /// <summary>
        /// Equipment expenses include depreciation, repairs, rentals, and service contract costs. This also includes equipment purchases which do not qualify for capitalization in accordance with the entity's accounting policy. This item may also include furniture expenses. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20210
        /// </remarks>
        [JsonProperty("20210")]
        public EquipmentIncomeStatement Equipment => _equipment ??= new(_timeProvider, _securityIdentifier);
        private EquipmentIncomeStatement _equipment;

        /// <summary>
        /// Costs incurred in identifying areas that may warrant examination and in examining specific areas that are considered to have prospects of containing energy or metal reserves, including costs of drilling exploratory wells. Development expense is the capitalized costs incurred to obtain access to proved reserves and to provide facilities for extracting, treating, gathering and storing the energy and metal. Mineral property includes oil and gas wells, mines, and other natural deposits (including geothermal deposits). The payment for leasing those properties is called mineral property lease expense. Exploration expense is included in operation expenses for mining industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20211
        /// </remarks>
        [JsonProperty("20211")]
        public ExplorationDevelopmentAndMineralPropertyLeaseExpensesIncomeStatement ExplorationDevelopmentAndMineralPropertyLeaseExpenses => _explorationDevelopmentAndMineralPropertyLeaseExpenses ??= new(_timeProvider, _securityIdentifier);
        private ExplorationDevelopmentAndMineralPropertyLeaseExpensesIncomeStatement _explorationDevelopmentAndMineralPropertyLeaseExpenses;

        /// <summary>
        /// Total fees and commissions earned from providing services such as leasing of space or maintaining: (1) depositor accounts; (2) transfer agent; (3) fiduciary and trust; (4) brokerage and underwriting; (5) mortgage; (6) credit cards; (7) correspondent clearing; and (8) other such services and activities performed for others. This item is usually available for bank and insurance industries.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20213
        /// </remarks>
        [JsonProperty("20213")]
        public FeesAndCommissionsIncomeStatement FeesAndCommissions => _feesAndCommissions ??= new(_timeProvider, _securityIdentifier);
        private FeesAndCommissionsIncomeStatement _feesAndCommissions;

        /// <summary>
        /// Trading revenues that result from foreign exchange exposures such as cash instruments and off-balance sheet derivative instruments. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20214
        /// </remarks>
        [JsonProperty("20214")]
        public ForeignExchangeTradingGainsIncomeStatement ForeignExchangeTradingGains => _foreignExchangeTradingGains ??= new(_timeProvider, _securityIdentifier);
        private ForeignExchangeTradingGainsIncomeStatement _foreignExchangeTradingGains;

        /// <summary>
        /// The aggregate amount of fuel cost for current period associated with the revenue generation. This item is usually only available for transportation industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20215
        /// </remarks>
        [JsonProperty("20215")]
        public FuelIncomeStatement Fuel => _fuel ??= new(_timeProvider, _securityIdentifier);
        private FuelIncomeStatement _fuel;

        /// <summary>
        /// Cost of fuel, purchase power and gas associated with revenue generation. This item is usually only available for utility industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20216
        /// </remarks>
        [JsonProperty("20216")]
        public FuelAndPurchasePowerIncomeStatement FuelAndPurchasePower => _fuelAndPurchasePower ??= new(_timeProvider, _securityIdentifier);
        private FuelAndPurchasePowerIncomeStatement _fuelAndPurchasePower;

        /// <summary>
        /// The amount of excess earned in comparison to fair value when selling a business. This item is usually not available for insurance industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20217
        /// </remarks>
        [JsonProperty("20217")]
        public GainOnSaleOfBusinessIncomeStatement GainOnSaleOfBusiness => _gainOnSaleOfBusiness ??= new(_timeProvider, _securityIdentifier);
        private GainOnSaleOfBusinessIncomeStatement _gainOnSaleOfBusiness;

        /// <summary>
        /// The amount of excess earned in comparison to the net book value for sale of property, plant, equipment. This item is usually not available for bank and insurance industries.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20218
        /// </remarks>
        [JsonProperty("20218")]
        public GainOnSaleOfPPEIncomeStatement GainOnSaleOfPPE => _gainOnSaleOfPPE ??= new(_timeProvider, _securityIdentifier);
        private GainOnSaleOfPPEIncomeStatement _gainOnSaleOfPPE;

        /// <summary>
        /// The amount of excess earned in comparison to the original purchase value of the security.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20219
        /// </remarks>
        [JsonProperty("20219")]
        public GainOnSaleOfSecurityIncomeStatement GainOnSaleOfSecurity => _gainOnSaleOfSecurity ??= new(_timeProvider, _securityIdentifier);
        private GainOnSaleOfSecurityIncomeStatement _gainOnSaleOfSecurity;

        /// <summary>
        /// Total premiums generated from all policies written by an insurance company within a given period of time. This item is usually only available for insurance industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20224
        /// </remarks>
        [JsonProperty("20224")]
        public GrossPremiumsWrittenIncomeStatement GrossPremiumsWritten => _grossPremiumsWritten ??= new(_timeProvider, _securityIdentifier);
        private GrossPremiumsWrittenIncomeStatement _grossPremiumsWritten;

        /// <summary>
        /// Impairments are considered to be permanent, which is a downward revaluation of fixed assets. If the sum of all estimated future cash flows is less than the carrying value of the asset, then the asset would be considered impaired and would have to be written down to its fair value. Once an asset is written down, it may only be written back up under very few circumstances. Usually the company uses the sum of undiscounted future cash flows to determine if the impairment should occur, and uses the sum of discounted future cash flows to make the impairment judgment. The impairment decision emphasizes on capital assets' future profit collection ability.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20225
        /// </remarks>
        [JsonProperty("20225")]
        public ImpairmentOfCapitalAssetsIncomeStatement ImpairmentOfCapitalAssets => _impairmentOfCapitalAssets ??= new(_timeProvider, _securityIdentifier);
        private ImpairmentOfCapitalAssetsIncomeStatement _impairmentOfCapitalAssets;

        /// <summary>
        /// Premium might contain a portion of the amount that has been paid in advance for insurance that has not yet been provided, which is called unearned premium. If either party cancels the contract, the insurer must have the unearned premium ready to refund. Hence, the amount of premium reserve maintained by insurers is called unearned premium reserves, which is prepared for liquidation. This item is usually only available for insurance industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20230
        /// </remarks>
        [JsonProperty("20230")]
        public IncreaseDecreaseInNetUnearnedPremiumReservesIncomeStatement IncreaseDecreaseInNetUnearnedPremiumReserves => _increaseDecreaseInNetUnearnedPremiumReserves ??= new(_timeProvider, _securityIdentifier);
        private IncreaseDecreaseInNetUnearnedPremiumReservesIncomeStatement _increaseDecreaseInNetUnearnedPremiumReserves;

        /// <summary>
        /// Insurance and claims are the expenses in the period incurred with respect to protection provided by insurance entities against risks other than risks associated with production (which is allocated to cost of sales). This item is usually not available for insurance industries.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20231
        /// </remarks>
        [JsonProperty("20231")]
        public InsuranceAndClaimsIncomeStatement InsuranceAndClaims => _insuranceAndClaims ??= new(_timeProvider, _securityIdentifier);
        private InsuranceAndClaimsIncomeStatement _insuranceAndClaims;

        /// <summary>
        /// Includes interest expense on the following deposit accounts: Interest-bearing Demand deposit; Checking account; Savings account; Deposit in foreign offices; Money Market Certificates &amp; Deposit Accounts. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20235
        /// </remarks>
        [JsonProperty("20235")]
        public InterestExpenseForDepositIncomeStatement InterestExpenseForDeposit => _interestExpenseForDeposit ??= new(_timeProvider, _securityIdentifier);
        private InterestExpenseForDepositIncomeStatement _interestExpenseForDeposit;

        /// <summary>
        /// Gross expenses on the purchase of Federal funds at a specified price with a simultaneous agreement to sell the same to the same counterparty at a fixed or determinable price at a future date. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20236
        /// </remarks>
        [JsonProperty("20236")]
        public InterestExpenseForFederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResellIncomeStatement InterestExpenseForFederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResell => _interestExpenseForFederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResell ??= new(_timeProvider, _securityIdentifier);
        private InterestExpenseForFederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResellIncomeStatement _interestExpenseForFederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResell;

        /// <summary>
        /// The aggregate interest expenses incurred on long-term borrowings and any interest expenses on fixed assets (property, plant, equipment) that are leased due longer than one year. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20238
        /// </remarks>
        [JsonProperty("20238")]
        public InterestExpenseForLongTermDebtAndCapitalSecuritiesIncomeStatement InterestExpenseForLongTermDebtAndCapitalSecurities => _interestExpenseForLongTermDebtAndCapitalSecurities ??= new(_timeProvider, _securityIdentifier);
        private InterestExpenseForLongTermDebtAndCapitalSecuritiesIncomeStatement _interestExpenseForLongTermDebtAndCapitalSecurities;

        /// <summary>
        /// The aggregate interest expenses incurred on short-term borrowings and any interest expenses on fixed assets (property, plant, equipment) that are leased within one year. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20239
        /// </remarks>
        [JsonProperty("20239")]
        public InterestExpenseForShortTermDebtIncomeStatement InterestExpenseForShortTermDebt => _interestExpenseForShortTermDebt ??= new(_timeProvider, _securityIdentifier);
        private InterestExpenseForShortTermDebtIncomeStatement _interestExpenseForShortTermDebt;

        /// <summary>
        /// Interest income generated from all deposit accounts. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20240
        /// </remarks>
        [JsonProperty("20240")]
        public InterestIncomeFromDepositsIncomeStatement InterestIncomeFromDeposits => _interestIncomeFromDeposits ??= new(_timeProvider, _securityIdentifier);
        private InterestIncomeFromDepositsIncomeStatement _interestIncomeFromDeposits;

        /// <summary>
        /// The carrying value of funds outstanding loaned in the form of security resale agreements if the agreement requires the purchaser to resell the identical security purchased or a security that meets the definition of ""substantially the same"" in the case of a dollar roll. Also includes purchases of participations in pools of securities that are subject to a resale agreement; This category includes all interest income generated from federal funds sold and securities purchases under agreements to resell; This category includes all interest income generated from federal funds sold and securities purchases under agreements to resell.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20241
        /// </remarks>
        [JsonProperty("20241")]
        public InterestIncomeFromFederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResellIncomeStatement InterestIncomeFromFederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResell => _interestIncomeFromFederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResell ??= new(_timeProvider, _securityIdentifier);
        private InterestIncomeFromFederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResellIncomeStatement _interestIncomeFromFederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResell;

        /// <summary>
        /// Includes interest and fee income generated by direct lease financing. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20243
        /// </remarks>
        [JsonProperty("20243")]
        public InterestIncomeFromLeasesIncomeStatement InterestIncomeFromLeases => _interestIncomeFromLeases ??= new(_timeProvider, _securityIdentifier);
        private InterestIncomeFromLeasesIncomeStatement _interestIncomeFromLeases;

        /// <summary>
        /// Loan is a common field to banks. Interest Income from Loans is interest and fee income generated from all loans, which includes Commercial loans; Credit loans; Other consumer loans; Real Estate - Construction; Real Estate - Mortgage; Foreign loans. Banks earn interest from loans. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20244
        /// </remarks>
        [JsonProperty("20244")]
        public InterestIncomeFromLoansIncomeStatement InterestIncomeFromLoans => _interestIncomeFromLoans ??= new(_timeProvider, _securityIdentifier);
        private InterestIncomeFromLoansIncomeStatement _interestIncomeFromLoans;

        /// <summary>
        /// Total interest and fee income generated by loans and lease. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20245
        /// </remarks>
        [JsonProperty("20245")]
        public InterestIncomeFromLoansAndLeaseIncomeStatement InterestIncomeFromLoansAndLease => _interestIncomeFromLoansAndLease ??= new(_timeProvider, _securityIdentifier);
        private InterestIncomeFromLoansAndLeaseIncomeStatement _interestIncomeFromLoansAndLease;

        /// <summary>
        /// Represents total interest and dividend income from U.S. Treasury securities, U.S. government agency and corporation obligations, securities issued by states and political subdivisions, other domestic debt securities, foreign debt securities, and equity securities (including investments in mutual funds). Excludes interest income from securities held in trading accounts. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20246
        /// </remarks>
        [JsonProperty("20246")]
        public InterestIncomeFromSecuritiesIncomeStatement InterestIncomeFromSecurities => _interestIncomeFromSecurities ??= new(_timeProvider, _securityIdentifier);
        private InterestIncomeFromSecuritiesIncomeStatement _interestIncomeFromSecurities;

        /// <summary>
        /// Includes (1) underwriting revenue (the spread between the resale price received and the cost of the securities and related expenses) generated through the purchasing, distributing and reselling of new issues of securities (alternatively, could be a secondary offering of a large block of previously issued securities); and (2) fees earned for mergers, acquisitions, divestitures, restructurings, and other types of financial advisory services. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20248
        /// </remarks>
        [JsonProperty("20248")]
        public InvestmentBankingProfitIncomeStatement InvestmentBankingProfit => _investmentBankingProfit ??= new(_timeProvider, _securityIdentifier);
        private InvestmentBankingProfitIncomeStatement _investmentBankingProfit;

        /// <summary>
        /// The aggregate amount of maintenance and repair expenses in the current period associated with the revenue generation. Mainly for fixed assets. This item is usually only available for transportation industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20252
        /// </remarks>
        [JsonProperty("20252")]
        public MaintenanceAndRepairsIncomeStatement MaintenanceAndRepairs => _maintenanceAndRepairs ??= new(_timeProvider, _securityIdentifier);
        private MaintenanceAndRepairsIncomeStatement _maintenanceAndRepairs;

        /// <summary>
        /// The aggregate foreign currency translation gain or loss (both realized and unrealized) included as part of revenue. This item is usually only available for insurance industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20255
        /// </remarks>
        [JsonProperty("20255")]
        public NetForeignExchangeGainLossIncomeStatement NetForeignExchangeGainLoss => _netForeignExchangeGainLoss ??= new(_timeProvider, _securityIdentifier);
        private NetForeignExchangeGainLossIncomeStatement _netForeignExchangeGainLoss;

        /// <summary>
        /// Occupancy expense may include items, such as depreciation of facilities and equipment, lease expenses, property taxes and property and casualty insurance expense. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20256
        /// </remarks>
        [JsonProperty("20256")]
        public NetOccupancyExpenseIncomeStatement NetOccupancyExpense => _netOccupancyExpense ??= new(_timeProvider, _securityIdentifier);
        private NetOccupancyExpenseIncomeStatement _netOccupancyExpense;

        /// <summary>
        /// Net premiums written are gross premiums written less ceded premiums. This item is usually only available for insurance industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20257
        /// </remarks>
        [JsonProperty("20257")]
        public NetPremiumsWrittenIncomeStatement NetPremiumsWritten => _netPremiumsWritten ??= new(_timeProvider, _securityIdentifier);
        private NetPremiumsWrittenIncomeStatement _netPremiumsWritten;

        /// <summary>
        /// Gain or loss realized during the period of time for all kinds of investment securities. In might include trading, available-for-sale, or held-to-maturity securities. This item is usually only available for insurance industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20258
        /// </remarks>
        [JsonProperty("20258")]
        public NetRealizedGainLossOnInvestmentsIncomeStatement NetRealizedGainLossOnInvestments => _netRealizedGainLossOnInvestments ??= new(_timeProvider, _securityIdentifier);
        private NetRealizedGainLossOnInvestmentsIncomeStatement _netRealizedGainLossOnInvestments;

        /// <summary>
        /// Includes total expenses of occupancy and equipment. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20260
        /// </remarks>
        [JsonProperty("20260")]
        public OccupancyAndEquipmentIncomeStatement OccupancyAndEquipment => _occupancyAndEquipment ??= new(_timeProvider, _securityIdentifier);
        private OccupancyAndEquipmentIncomeStatement _occupancyAndEquipment;

        /// <summary>
        /// The aggregate amount of operation and maintenance expenses, which is the one important operating expense for the utility industry. It includes any costs related to production and maintenance cost of the property during the revenue generation process. This item is usually only available for mining and utility industries.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20262
        /// </remarks>
        [JsonProperty("20262")]
        public OperationAndMaintenanceIncomeStatement OperationAndMaintenance => _operationAndMaintenance ??= new(_timeProvider, _securityIdentifier);
        private OperationAndMaintenanceIncomeStatement _operationAndMaintenance;

        /// <summary>
        /// Represents fees and commissions earned from provide other services. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20263
        /// </remarks>
        [JsonProperty("20263")]
        public OtherCustomerServicesIncomeStatement OtherCustomerServices => _otherCustomerServices ??= new(_timeProvider, _securityIdentifier);
        private OtherCustomerServicesIncomeStatement _otherCustomerServices;

        /// <summary>
        /// All other interest expense that is not otherwise classified
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20265
        /// </remarks>
        [JsonProperty("20265")]
        public OtherInterestExpenseIncomeStatement OtherInterestExpense => _otherInterestExpense ??= new(_timeProvider, _securityIdentifier);
        private OtherInterestExpenseIncomeStatement _otherInterestExpense;

        /// <summary>
        /// All other interest income that is not otherwise classified
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20266
        /// </remarks>
        [JsonProperty("20266")]
        public OtherInterestIncomeIncomeStatement OtherInterestIncome => _otherInterestIncome ??= new(_timeProvider, _securityIdentifier);
        private OtherInterestIncomeIncomeStatement _otherInterestIncome;

        /// <summary>
        /// All other non interest expense that is not otherwise classified
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20267
        /// </remarks>
        [JsonProperty("20267")]
        public OtherNonInterestExpenseIncomeStatement OtherNonInterestExpense => _otherNonInterestExpense ??= new(_timeProvider, _securityIdentifier);
        private OtherNonInterestExpenseIncomeStatement _otherNonInterestExpense;

        /// <summary>
        /// All other special charges that are not otherwise classified
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20269
        /// </remarks>
        [JsonProperty("20269")]
        public OtherSpecialChargesIncomeStatement OtherSpecialCharges => _otherSpecialCharges ??= new(_timeProvider, _securityIdentifier);
        private OtherSpecialChargesIncomeStatement _otherSpecialCharges;

        /// <summary>
        /// Any taxes that are not part of income taxes. This item is usually not available for bank and insurance industries.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20271
        /// </remarks>
        [JsonProperty("20271")]
        public OtherTaxesIncomeStatement OtherTaxes => _otherTaxes ??= new(_timeProvider, _securityIdentifier);
        private OtherTaxesIncomeStatement _otherTaxes;

        /// <summary>
        /// The provision in current period for future policy benefits, claims, and claims settlement, which is under reinsurance arrangements. This item is usually only available for insurance industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20273
        /// </remarks>
        [JsonProperty("20273")]
        public PolicyholderBenefitsCededIncomeStatement PolicyholderBenefitsCeded => _policyholderBenefitsCeded ??= new(_timeProvider, _securityIdentifier);
        private PolicyholderBenefitsCededIncomeStatement _policyholderBenefitsCeded;

        /// <summary>
        /// The gross amount of provision in current period for future policyholder benefits, claims, and claims settlement, incurred in the claims settlement process before the effects of reinsurance arrangements. This item is usually only available for insurance industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20274
        /// </remarks>
        [JsonProperty("20274")]
        public PolicyholderBenefitsGrossIncomeStatement PolicyholderBenefitsGross => _policyholderBenefitsGross ??= new(_timeProvider, _securityIdentifier);
        private PolicyholderBenefitsGrossIncomeStatement _policyholderBenefitsGross;

        /// <summary>
        /// Payments made or credits extended to the insured by the company, usually at the end of a policy year results in reducing the net insurance cost to the policyholder. Such dividends may be paid in cash to the insured or applied by the insured as reductions of the premiums due for the next policy year. This item is usually only available for insurance industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20275
        /// </remarks>
        [JsonProperty("20275")]
        public PolicyholderDividendsIncomeStatement PolicyholderDividends => _policyholderDividends ??= new(_timeProvider, _securityIdentifier);
        private PolicyholderDividendsIncomeStatement _policyholderDividends;

        /// <summary>
        /// The periodic income payment provided to the annuitant by the insurance company, which is determined by the assumed interest rate (AIR) and other factors. This item is usually only available for insurance industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20276
        /// </remarks>
        [JsonProperty("20276")]
        public PolicyholderInterestIncomeStatement PolicyholderInterest => _policyholderInterest ??= new(_timeProvider, _securityIdentifier);
        private PolicyholderInterestIncomeStatement _policyholderInterest;

        /// <summary>
        /// Professional and contract service expense includes cost reimbursements for support services related to contracted projects, outsourced management, technical and staff support. This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20280
        /// </remarks>
        [JsonProperty("20280")]
        public ProfessionalExpenseAndContractServicesExpenseIncomeStatement ProfessionalExpenseAndContractServicesExpense => _professionalExpenseAndContractServicesExpense ??= new(_timeProvider, _securityIdentifier);
        private ProfessionalExpenseAndContractServicesExpenseIncomeStatement _professionalExpenseAndContractServicesExpense;

        /// <summary>
        /// Amount of the current period expense charged against operations, the offset which is generally to the allowance for doubtful accounts for the purpose of reducing receivables, including notes receivable, to an amount that approximates their net realizable value (the amount expected to be collected). The category includes provision for loan losses, provision for any doubtful account receivable, and bad debt expenses. This item is usually not available for bank and insurance industries.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20283
        /// </remarks>
        [JsonProperty("20283")]
        public ProvisionForDoubtfulAccountsIncomeStatement ProvisionForDoubtfulAccounts => _provisionForDoubtfulAccounts ??= new(_timeProvider, _securityIdentifier);
        private ProvisionForDoubtfulAccountsIncomeStatement _provisionForDoubtfulAccounts;

        /// <summary>
        /// Rent fees are the cost of occupying space during the accounting period. Landing fees are a change paid to an airport company for landing at a particular airport. This item is not available for insurance industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20287
        /// </remarks>
        [JsonProperty("20287")]
        public RentAndLandingFeesIncomeStatement RentAndLandingFees => _rentAndLandingFees ??= new(_timeProvider, _securityIdentifier);
        private RentAndLandingFeesIncomeStatement _rentAndLandingFees;

        /// <summary>
        /// Expenses are related to restructuring, merger, or acquisitions. Restructuring expenses are charges associated with the consolidation and relocation of operations, disposition or abandonment of operations or productive assets. Merger and acquisition expenses are the amount of costs of a business combination including legal, accounting, and other costs that were charged to expense during the period.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20289
        /// </remarks>
        [JsonProperty("20289")]
        public RestructuringAndMergernAcquisitionIncomeStatement RestructuringAndMergernAcquisition => _restructuringAndMergernAcquisition ??= new(_timeProvider, _securityIdentifier);
        private RestructuringAndMergernAcquisitionIncomeStatement _restructuringAndMergernAcquisition;

        /// <summary>
        /// All salary, wages, compensation, management fees, and employee benefit expenses.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20292
        /// </remarks>
        [JsonProperty("20292")]
        public SalariesAndWagesIncomeStatement SalariesAndWages => _salariesAndWages ??= new(_timeProvider, _securityIdentifier);
        private SalariesAndWagesIncomeStatement _salariesAndWages;

        /// <summary>
        /// Income/Loss from Securities and Activities
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20293
        /// </remarks>
        [JsonProperty("20293")]
        public SecuritiesActivitiesIncomeStatement SecuritiesActivities => _securitiesActivities ??= new(_timeProvider, _securityIdentifier);
        private SecuritiesActivitiesIncomeStatement _securitiesActivities;

        /// <summary>
        /// Includes any service charges on following accounts: Demand Deposit; Checking account; Savings account; Deposit in foreign offices; ESCROW accounts; Money Market Certificates &amp; Deposit accounts, CDs (Negotiable Certificates of Deposits); NOW Accounts (Negotiable Order of Withdrawal); IRAs (Individual Retirement Accounts). This item is usually only available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20295
        /// </remarks>
        [JsonProperty("20295")]
        public ServiceChargeOnDepositorAccountsIncomeStatement ServiceChargeOnDepositorAccounts => _serviceChargeOnDepositorAccounts ??= new(_timeProvider, _securityIdentifier);
        private ServiceChargeOnDepositorAccountsIncomeStatement _serviceChargeOnDepositorAccounts;

        /// <summary>
        /// A broker-dealer or other financial entity may buy and sell securities exclusively for its own account, sometimes referred to as proprietary trading. The profit or loss is measured by the difference between the acquisition cost and the selling price or current market or fair value. The net gain or loss, includes both realized and unrealized, from trading cash instruments, equities and derivative contracts (including commodity contracts) that has been recognized during the accounting period for the broker dealer or other financial entity's own account. This item is typically available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20298
        /// </remarks>
        [JsonProperty("20298")]
        public TradingGainLossIncomeStatement TradingGainLoss => _tradingGainLoss ??= new(_timeProvider, _securityIdentifier);
        private TradingGainLossIncomeStatement _tradingGainLoss;

        /// <summary>
        /// Bank manages funds on behalf of its customers through the operation of various trust accounts. Any fees earned through managing those funds are called trust fees, which are recognized when earned. This item is typically available for bank industry.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20300
        /// </remarks>
        [JsonProperty("20300")]
        public TrustFeesbyCommissionsIncomeStatement TrustFeesbyCommissions => _trustFeesbyCommissions ??= new(_timeProvider, _securityIdentifier);
        private TrustFeesbyCommissionsIncomeStatement _trustFeesbyCommissions;

        /// <summary>
        /// Also known as Policy Acquisition Costs; and reported by insurance companies. The cost incurred by an insurer when deciding whether to accept or decline a risk; may include meetings with the insureds or brokers, actuarial review of loss history, or physical inspections of exposures. Also, expenses deducted from insurance company revenues (including incurred losses and acquisition costs) to determine underwriting profit.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20301
        /// </remarks>
        [JsonProperty("20301")]
        public UnderwritingExpensesIncomeStatement UnderwritingExpenses => _underwritingExpenses ??= new(_timeProvider, _securityIdentifier);
        private UnderwritingExpensesIncomeStatement _underwritingExpenses;

        /// <summary>
        /// A reduction in the value of an asset or earnings by the amount of an expense or loss.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20304
        /// </remarks>
        [JsonProperty("20304")]
        public WriteOffIncomeStatement WriteOff => _writeOff ??= new(_timeProvider, _securityIdentifier);
        private WriteOffIncomeStatement _writeOff;

        /// <summary>
        /// Usually available for the banking industry. This is Non-Interest Income that is not otherwise classified.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20306
        /// </remarks>
        [JsonProperty("20306")]
        public OtherNonInterestIncomeIncomeStatement OtherNonInterestIncome => _otherNonInterestIncome ??= new(_timeProvider, _securityIdentifier);
        private OtherNonInterestIncomeIncomeStatement _otherNonInterestIncome;

        /// <summary>
        /// The aggregate expense charged against earnings to allocate the cost of intangible assets (nonphysical assets not used in production) in a systematic and rational manner to the periods expected to benefit from such assets.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20308
        /// </remarks>
        [JsonProperty("20308")]
        public AmortizationOfIntangiblesIncomeStatement AmortizationOfIntangibles => _amortizationOfIntangibles ??= new(_timeProvider, _securityIdentifier);
        private AmortizationOfIntangiblesIncomeStatement _amortizationOfIntangibles;

        /// <summary>
        /// Net Income from Continuing Operations and Discontinued Operations, added together.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20309
        /// </remarks>
        [JsonProperty("20309")]
        public NetIncomeFromContinuingAndDiscontinuedOperationIncomeStatement NetIncomeFromContinuingAndDiscontinuedOperation => _netIncomeFromContinuingAndDiscontinuedOperation ??= new(_timeProvider, _securityIdentifier);
        private NetIncomeFromContinuingAndDiscontinuedOperationIncomeStatement _netIncomeFromContinuingAndDiscontinuedOperation;

        /// <summary>
        /// Occurs if a company has had a net loss from operations on a previous year that can be carried forward to reduce net income for tax purposes.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20311
        /// </remarks>
        [JsonProperty("20311")]
        public NetIncomeFromTaxLossCarryforwardIncomeStatement NetIncomeFromTaxLossCarryforward => _netIncomeFromTaxLossCarryforward ??= new(_timeProvider, _securityIdentifier);
        private NetIncomeFromTaxLossCarryforwardIncomeStatement _netIncomeFromTaxLossCarryforward;

        /// <summary>
        /// The aggregate amount of operating expenses associated with normal operations. Will not include any gain, loss, benefit, or income; and its value reported by the company should be &lt;0.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20312
        /// </remarks>
        [JsonProperty("20312")]
        public OtherOperatingExpensesIncomeStatement OtherOperatingExpenses => _otherOperatingExpenses ??= new(_timeProvider, _securityIdentifier);
        private OtherOperatingExpensesIncomeStatement _otherOperatingExpenses;

        /// <summary>
        /// The sum of the money market investments held by a bank's depositors, which are FDIC insured.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20313
        /// </remarks>
        [JsonProperty("20313")]
        public TotalMoneyMarketInvestmentsIncomeStatement TotalMoneyMarketInvestments => _totalMoneyMarketInvestments ??= new(_timeProvider, _securityIdentifier);
        private TotalMoneyMarketInvestmentsIncomeStatement _totalMoneyMarketInvestments;

        /// <summary>
        /// The Cost Of Revenue plus Depreciation, Depletion &amp; Amortization from the IncomeStatement; minus Depreciation, Depletion &amp; Amortization from the Cash Flow Statement
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20314
        /// </remarks>
        [JsonProperty("20314")]
        public ReconciledCostOfRevenueIncomeStatement ReconciledCostOfRevenue => _reconciledCostOfRevenue ??= new(_timeProvider, _securityIdentifier);
        private ReconciledCostOfRevenueIncomeStatement _reconciledCostOfRevenue;

        /// <summary>
        /// Is Depreciation, Depletion &amp; Amortization from the Cash Flow Statement
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20315
        /// </remarks>
        [JsonProperty("20315")]
        public ReconciledDepreciationIncomeStatement ReconciledDepreciation => _reconciledDepreciation ??= new(_timeProvider, _securityIdentifier);
        private ReconciledDepreciationIncomeStatement _reconciledDepreciation;

        /// <summary>
        /// This calculation represents earnings adjusted for items that are irregular or unusual in nature, and/or are non-recurring. This can be used to fairly measure a company's profitability. This is calculated using Net Income from Continuing Operations plus/minus any tax affected unusual Items and Goodwill Impairments/Write Offs.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20316
        /// </remarks>
        [JsonProperty("20316")]
        public NormalizedIncomeIncomeStatement NormalizedIncome => _normalizedIncome ??= new(_timeProvider, _securityIdentifier);
        private NormalizedIncomeIncomeStatement _normalizedIncome;

        /// <summary>
        /// Revenue less expenses and taxes from the entity's ongoing operations net of minority interest and before income (loss) from: Preferred Dividends; Extraordinary Gains and Losses; Income from Cumulative Effects of Accounting Change; Discontinuing Operation; Income from Tax Loss Carry forward; Other Gains/Losses.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20331
        /// </remarks>
        [JsonProperty("20331")]
        public NetIncomeFromContinuingOperationNetMinorityInterestIncomeStatement NetIncomeFromContinuingOperationNetMinorityInterest => _netIncomeFromContinuingOperationNetMinorityInterest ??= new(_timeProvider, _securityIdentifier);
        private NetIncomeFromContinuingOperationNetMinorityInterestIncomeStatement _netIncomeFromContinuingOperationNetMinorityInterest;

        /// <summary>
        /// Any gain (loss) recognized on the sale of assets or a sale which generates profit or loss, which is a difference between sales price and net book value at the disposal time.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20333
        /// </remarks>
        [JsonProperty("20333")]
        public GainLossonSaleofAssetsIncomeStatement GainLossonSaleofAssets => _gainLossonSaleofAssets ??= new(_timeProvider, _securityIdentifier);
        private GainLossonSaleofAssetsIncomeStatement _gainLossonSaleofAssets;

        /// <summary>
        /// Gain on sale of any loans investment.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20334
        /// </remarks>
        [JsonProperty("20334")]
        public GainonSaleofLoansIncomeStatement GainonSaleofLoans => _gainonSaleofLoans ??= new(_timeProvider, _securityIdentifier);
        private GainonSaleofLoansIncomeStatement _gainonSaleofLoans;

        /// <summary>
        /// Gain on the disposal of investment property.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20335
        /// </remarks>
        [JsonProperty("20335")]
        public GainonSaleofInvestmentPropertyIncomeStatement GainonSaleofInvestmentProperty => _gainonSaleofInvestmentProperty ??= new(_timeProvider, _securityIdentifier);
        private GainonSaleofInvestmentPropertyIncomeStatement _gainonSaleofInvestmentProperty;

        /// <summary>
        /// Loss on extinguishment of debt is the accounting loss that results from a debt extinguishment. A debt shall be accounted for as having been extinguished in a number of circumstances, including when it has been settled through repayment or replacement by another liability. It generally results in an accounting gain or loss. Amount represents the difference between the fair value of the payments made and the carrying amount of the debt at the time of its extinguishment.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20343
        /// </remarks>
        [JsonProperty("20343")]
        public LossonExtinguishmentofDebtIncomeStatement LossonExtinguishmentofDebt => _lossonExtinguishmentofDebt ??= new(_timeProvider, _securityIdentifier);
        private LossonExtinguishmentofDebtIncomeStatement _lossonExtinguishmentofDebt;

        /// <summary>
        /// Income from other equity interest reported after Provision of Tax. This applies to all industries.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20345
        /// </remarks>
        [JsonProperty("20345")]
        public EarningsfromEquityInterestNetOfTaxIncomeStatement EarningsfromEquityInterestNetOfTax => _earningsfromEquityInterestNetOfTax ??= new(_timeProvider, _securityIdentifier);
        private EarningsfromEquityInterestNetOfTaxIncomeStatement _earningsfromEquityInterestNetOfTax;

        /// <summary>
        /// Net income of the group after the adjustment of all expenses and benefit.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20346
        /// </remarks>
        [JsonProperty("20346")]
        public NetIncomeIncludingNoncontrollingInterestsIncomeStatement NetIncomeIncludingNoncontrollingInterests => _netIncomeIncludingNoncontrollingInterests ??= new(_timeProvider, _securityIdentifier);
        private NetIncomeIncludingNoncontrollingInterestsIncomeStatement _netIncomeIncludingNoncontrollingInterests;

        /// <summary>
        /// Dividend paid to the preferred shareholders before the common stock shareholders.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20347
        /// </remarks>
        [JsonProperty("20347")]
        public OtherunderPreferredStockDividendIncomeStatement OtherunderPreferredStockDividend => _otherunderPreferredStockDividend ??= new(_timeProvider, _securityIdentifier);
        private OtherunderPreferredStockDividendIncomeStatement _otherunderPreferredStockDividend;

        /// <summary>
        /// Total staff cost which is paid to the employees that is not part of Selling, General, and Administration expense.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20359
        /// </remarks>
        [JsonProperty("20359")]
        public StaffCostsIncomeStatement StaffCosts => _staffCosts ??= new(_timeProvider, _securityIdentifier);
        private StaffCostsIncomeStatement _staffCosts;

        /// <summary>
        /// Benefits paid to the employees in respect of their work.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20360
        /// </remarks>
        [JsonProperty("20360")]
        public SocialSecurityCostsIncomeStatement SocialSecurityCosts => _socialSecurityCosts ??= new(_timeProvider, _securityIdentifier);
        private SocialSecurityCostsIncomeStatement _socialSecurityCosts;

        /// <summary>
        /// The expense that a company incurs each year by providing a pension plan for its employees. Major expenses in the pension cost include employer matching contributions and management fees.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20361
        /// </remarks>
        [JsonProperty("20361")]
        public PensionCostsIncomeStatement PensionCosts => _pensionCosts ??= new(_timeProvider, _securityIdentifier);
        private PensionCostsIncomeStatement _pensionCosts;

        /// <summary>
        /// Total Other Operating Income- including interest income, dividend income and other types of operating income.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20363
        /// </remarks>
        [JsonProperty("20363")]
        public OtherOperatingIncomeTotalIncomeStatement OtherOperatingIncomeTotal => _otherOperatingIncomeTotal ??= new(_timeProvider, _securityIdentifier);
        private OtherOperatingIncomeTotalIncomeStatement _otherOperatingIncomeTotal;

        /// <summary>
        /// Total income from the associates and joint venture via investment, accounted for in the Non-Operating section.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20367
        /// </remarks>
        [JsonProperty("20367")]
        public IncomefromAssociatesandOtherParticipatingInterestsIncomeStatement IncomefromAssociatesandOtherParticipatingInterests => _incomefromAssociatesandOtherParticipatingInterests ??= new(_timeProvider, _securityIdentifier);
        private IncomefromAssociatesandOtherParticipatingInterestsIncomeStatement _incomefromAssociatesandOtherParticipatingInterests;

        /// <summary>
        /// Any other finance cost which is not clearly defined in the Non-Operating section.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20368
        /// </remarks>
        [JsonProperty("20368")]
        public TotalOtherFinanceCostIncomeStatement TotalOtherFinanceCost => _totalOtherFinanceCost ??= new(_timeProvider, _securityIdentifier);
        private TotalOtherFinanceCostIncomeStatement _totalOtherFinanceCost;

        /// <summary>
        /// Total amount paid in dividends to investors- this includes dividends paid on equity and non-equity shares.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20371
        /// </remarks>
        [JsonProperty("20371")]
        public GrossDividendPaymentIncomeStatement GrossDividendPayment => _grossDividendPayment ??= new(_timeProvider, _securityIdentifier);
        private GrossDividendPaymentIncomeStatement _grossDividendPayment;

        /// <summary>
        /// Fees and commission income earned by bank and insurance companies on the rendering services.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20377
        /// </remarks>
        [JsonProperty("20377")]
        public FeesandCommissionIncomeIncomeStatement FeesandCommissionIncome => _feesandCommissionIncome ??= new(_timeProvider, _securityIdentifier);
        private FeesandCommissionIncomeIncomeStatement _feesandCommissionIncome;

        /// <summary>
        /// Cost incurred by bank and insurance companies for fees and commission income.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20378
        /// </remarks>
        [JsonProperty("20378")]
        public FeesandCommissionExpenseIncomeStatement FeesandCommissionExpense => _feesandCommissionExpense ??= new(_timeProvider, _securityIdentifier);
        private FeesandCommissionExpenseIncomeStatement _feesandCommissionExpense;

        /// <summary>
        /// Any trading income on the securities.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20379
        /// </remarks>
        [JsonProperty("20379")]
        public NetTradingIncomeIncomeStatement NetTradingIncome => _netTradingIncome ??= new(_timeProvider, _securityIdentifier);
        private NetTradingIncomeIncomeStatement _netTradingIncome;

        /// <summary>
        /// Other costs in incurred in lieu of the employees that cannot be identified by other specific items in the Staff Costs section.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20381
        /// </remarks>
        [JsonProperty("20381")]
        public OtherStaffCostsIncomeStatement OtherStaffCosts => _otherStaffCosts ??= new(_timeProvider, _securityIdentifier);
        private OtherStaffCostsIncomeStatement _otherStaffCosts;

        /// <summary>
        /// Gain on disposal and change in fair value of investment properties.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20383
        /// </remarks>
        [JsonProperty("20383")]
        public GainonInvestmentPropertiesIncomeStatement GainonInvestmentProperties => _gainonInvestmentProperties ??= new(_timeProvider, _securityIdentifier);
        private GainonInvestmentPropertiesIncomeStatement _gainonInvestmentProperties;

        /// <summary>
        /// Adjustments to reported net income to calculate Diluted EPS, by assuming that all convertible instruments are converted to Common Equity. The adjustments usually include the interest expense of debentures when assumed converted and preferred dividends of convertible preferred stock when assumed converted.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20385
        /// </remarks>
        [JsonProperty("20385")]
        public AverageDilutionEarningsIncomeStatement AverageDilutionEarnings => _averageDilutionEarnings ??= new(_timeProvider, _securityIdentifier);
        private AverageDilutionEarningsIncomeStatement _averageDilutionEarnings;

        /// <summary>
        /// Gain/Loss through hedging activities.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20391
        /// </remarks>
        [JsonProperty("20391")]
        public GainLossonFinancialInstrumentsDesignatedasCashFlowHedgesIncomeStatement GainLossonFinancialInstrumentsDesignatedasCashFlowHedges => _gainLossonFinancialInstrumentsDesignatedasCashFlowHedges ??= new(_timeProvider, _securityIdentifier);
        private GainLossonFinancialInstrumentsDesignatedasCashFlowHedgesIncomeStatement _gainLossonFinancialInstrumentsDesignatedasCashFlowHedges;

        /// <summary>
        /// Gain/loss on the write-off of financial assets available-for-sale.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20392
        /// </remarks>
        [JsonProperty("20392")]
        public GainLossonDerecognitionofAvailableForSaleFinancialAssetsIncomeStatement GainLossonDerecognitionofAvailableForSaleFinancialAssets => _gainLossonDerecognitionofAvailableForSaleFinancialAssets ??= new(_timeProvider, _securityIdentifier);
        private GainLossonDerecognitionofAvailableForSaleFinancialAssetsIncomeStatement _gainLossonDerecognitionofAvailableForSaleFinancialAssets;

        /// <summary>
        /// Negative Goodwill recognized in the Income Statement. Negative Goodwill arises where the net assets at the date of acquisition, fairly valued, falls below the cost of acquisition.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20394
        /// </remarks>
        [JsonProperty("20394")]
        public NegativeGoodwillImmediatelyRecognizedIncomeStatement NegativeGoodwillImmediatelyRecognized => _negativeGoodwillImmediatelyRecognized ??= new(_timeProvider, _securityIdentifier);
        private NegativeGoodwillImmediatelyRecognizedIncomeStatement _negativeGoodwillImmediatelyRecognized;

        /// <summary>
        /// Gain or loss on derivatives investment due to the fair value adjustment.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20395
        /// </remarks>
        [JsonProperty("20395")]
        public GainsLossesonFinancialInstrumentsDuetoFairValueAdjustmentsinHedgeAccountingTotalIncomeStatement GainsLossesonFinancialInstrumentsDuetoFairValueAdjustmentsinHedgeAccountingTotal => _gainsLossesonFinancialInstrumentsDuetoFairValueAdjustmentsinHedgeAccountingTotal ??= new(_timeProvider, _securityIdentifier);
        private GainsLossesonFinancialInstrumentsDuetoFairValueAdjustmentsinHedgeAccountingTotalIncomeStatement _gainsLossesonFinancialInstrumentsDuetoFairValueAdjustmentsinHedgeAccountingTotal;

        /// <summary>
        /// Impairment or reversal of impairment on financial instrument such as derivative. This is a contra account under Total Revenue in banks.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20396
        /// </remarks>
        [JsonProperty("20396")]
        public ImpairmentLossesReversalsFinancialInstrumentsNetIncomeStatement ImpairmentLossesReversalsFinancialInstrumentsNet => _impairmentLossesReversalsFinancialInstrumentsNet ??= new(_timeProvider, _securityIdentifier);
        private ImpairmentLossesReversalsFinancialInstrumentsNetIncomeStatement _impairmentLossesReversalsFinancialInstrumentsNet;

        /// <summary>
        /// All reported claims arising out of incidents in that year are considered incurred grouped with claims paid out.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20400
        /// </remarks>
        [JsonProperty("20400")]
        public ClaimsandPaidIncurredIncomeStatement ClaimsandPaidIncurred => _claimsandPaidIncurred ??= new(_timeProvider, _securityIdentifier);
        private ClaimsandPaidIncurredIncomeStatement _claimsandPaidIncurred;

        /// <summary>
        /// Claim on the reinsurance company and take the benefits.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20401
        /// </remarks>
        [JsonProperty("20401")]
        public ReinsuranceRecoveriesClaimsandBenefitsIncomeStatement ReinsuranceRecoveriesClaimsandBenefits => _reinsuranceRecoveriesClaimsandBenefits ??= new(_timeProvider, _securityIdentifier);
        private ReinsuranceRecoveriesClaimsandBenefitsIncomeStatement _reinsuranceRecoveriesClaimsandBenefits;

        /// <summary>
        /// Income/Expense due to changes between periods in insurance liabilities.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20402
        /// </remarks>
        [JsonProperty("20402")]
        public ChangeinInsuranceLiabilitiesNetofReinsuranceIncomeStatement ChangeinInsuranceLiabilitiesNetofReinsurance => _changeinInsuranceLiabilitiesNetofReinsurance ??= new(_timeProvider, _securityIdentifier);
        private ChangeinInsuranceLiabilitiesNetofReinsuranceIncomeStatement _changeinInsuranceLiabilitiesNetofReinsurance;

        /// <summary>
        /// Income/Expense due to changes between periods in Investment Contracts.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20405
        /// </remarks>
        [JsonProperty("20405")]
        public ChangeinInvestmentContractIncomeStatement ChangeinInvestmentContract => _changeinInvestmentContract ??= new(_timeProvider, _securityIdentifier);
        private ChangeinInvestmentContractIncomeStatement _changeinInvestmentContract;

        /// <summary>
        /// Provision for the risk of loss of principal or loss of a financial reward stemming from a borrower's failure to repay a loan or otherwise meet a contractual obligation. Credit risk arises whenever a borrower is expecting to use future cash flows to pay a current debt. Investors are compensated for assuming credit risk by way of interest payments from the borrower or issuer of a debt obligation. This is a contra account under Total Revenue in banks.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20409
        /// </remarks>
        [JsonProperty("20409")]
        public CreditRiskProvisionsIncomeStatement CreditRiskProvisions => _creditRiskProvisions ??= new(_timeProvider, _securityIdentifier);
        private CreditRiskProvisionsIncomeStatement _creditRiskProvisions;

        /// <summary>
        /// This is the portion under Staff Costs that represents salary paid to the employees in respect of their work.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20411
        /// </remarks>
        [JsonProperty("20411")]
        public WagesandSalariesIncomeStatement WagesandSalaries => _wagesandSalaries ??= new(_timeProvider, _securityIdentifier);
        private WagesandSalariesIncomeStatement _wagesandSalaries;

        /// <summary>
        /// Total other income and expense of the company that cannot be identified by other specific items in the Non-Operating section.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20412
        /// </remarks>
        [JsonProperty("20412")]
        public OtherNonOperatingIncomeExpensesIncomeStatement OtherNonOperatingIncomeExpenses => _otherNonOperatingIncomeExpenses ??= new(_timeProvider, _securityIdentifier);
        private OtherNonOperatingIncomeExpensesIncomeStatement _otherNonOperatingIncomeExpenses;

        /// <summary>
        /// Other income of the company that cannot be identified by other specific items in the Non-Operating section.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20414
        /// </remarks>
        [JsonProperty("20414")]
        public OtherNonOperatingIncomeIncomeStatement OtherNonOperatingIncome => _otherNonOperatingIncome ??= new(_timeProvider, _securityIdentifier);
        private OtherNonOperatingIncomeIncomeStatement _otherNonOperatingIncome;

        /// <summary>
        /// Other expenses of the company that cannot be identified by other specific items in the Non-Operating section.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20415
        /// </remarks>
        [JsonProperty("20415")]
        public OtherNonOperatingExpensesIncomeStatement OtherNonOperatingExpenses => _otherNonOperatingExpenses ??= new(_timeProvider, _securityIdentifier);
        private OtherNonOperatingExpensesIncomeStatement _otherNonOperatingExpenses;

        /// <summary>
        /// Total unusual items including Negative Goodwill.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20416
        /// </remarks>
        [JsonProperty("20416")]
        public TotalUnusualItemsIncomeStatement TotalUnusualItems => _totalUnusualItems ??= new(_timeProvider, _securityIdentifier);
        private TotalUnusualItemsIncomeStatement _totalUnusualItems;

        /// <summary>
        /// The sum of all the identifiable operating and non-operating unusual items.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20417
        /// </remarks>
        [JsonProperty("20417")]
        public TotalUnusualItemsExcludingGoodwillIncomeStatement TotalUnusualItemsExcludingGoodwill => _totalUnusualItemsExcludingGoodwill ??= new(_timeProvider, _securityIdentifier);
        private TotalUnusualItemsExcludingGoodwillIncomeStatement _totalUnusualItemsExcludingGoodwill;

        /// <summary>
        /// Tax rate used for Morningstar calculations.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20418
        /// </remarks>
        [JsonProperty("20418")]
        public TaxRateForCalcsIncomeStatement TaxRateForCalcs => _taxRateForCalcs ??= new(_timeProvider, _securityIdentifier);
        private TaxRateForCalcsIncomeStatement _taxRateForCalcs;

        /// <summary>
        /// Tax effect of the usual items
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20419
        /// </remarks>
        [JsonProperty("20419")]
        public TaxEffectOfUnusualItemsIncomeStatement TaxEffectOfUnusualItems => _taxEffectOfUnusualItems ??= new(_timeProvider, _securityIdentifier);
        private TaxEffectOfUnusualItemsIncomeStatement _taxEffectOfUnusualItems;

        /// <summary>
        /// EBITDA less Total Unusual Items
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20420
        /// </remarks>
        [JsonProperty("20420")]
        public NormalizedEBITDAIncomeStatement NormalizedEBITDA => _normalizedEBITDA ??= new(_timeProvider, _securityIdentifier);
        private NormalizedEBITDAIncomeStatement _normalizedEBITDA;

        /// <summary>
        /// The cost to the company for granting stock options to reward employees.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20422
        /// </remarks>
        [JsonProperty("20422")]
        public StockBasedCompensationIncomeStatement StockBasedCompensation => _stockBasedCompensation ??= new(_timeProvider, _securityIdentifier);
        private StockBasedCompensationIncomeStatement _stockBasedCompensation;

        /// <summary>
        /// Net income to calculate Diluted EPS, accounting for adjustments assuming that all the convertible instruments are being converted to Common Equity.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20424
        /// </remarks>
        [JsonProperty("20424")]
        public DilutedNIAvailtoComStockholdersIncomeStatement DilutedNIAvailtoComStockholders => _dilutedNIAvailtoComStockholders ??= new(_timeProvider, _securityIdentifier);
        private DilutedNIAvailtoComStockholdersIncomeStatement _dilutedNIAvailtoComStockholders;

        /// <summary>
        /// Income/Expenses due to the insurer's liabilities incurred in Investment Contracts.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20425
        /// </remarks>
        [JsonProperty("20425")]
        public InvestmentContractLiabilitiesIncurredIncomeStatement InvestmentContractLiabilitiesIncurred => _investmentContractLiabilitiesIncurred ??= new(_timeProvider, _securityIdentifier);
        private InvestmentContractLiabilitiesIncurredIncomeStatement _investmentContractLiabilitiesIncurred;

        /// <summary>
        /// Income/Expense due to recoveries from reinsurers for Investment Contracts.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20426
        /// </remarks>
        [JsonProperty("20426")]
        public ReinsuranceRecoveriesofInvestmentContractIncomeStatement ReinsuranceRecoveriesofInvestmentContract => _reinsuranceRecoveriesofInvestmentContract ??= new(_timeProvider, _securityIdentifier);
        private ReinsuranceRecoveriesofInvestmentContractIncomeStatement _reinsuranceRecoveriesofInvestmentContract;

        /// <summary>
        /// Total amount paid in dividends to equity securities investors.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20429
        /// </remarks>
        [JsonProperty("20429")]
        public TotalDividendPaymentofEquitySharesIncomeStatement TotalDividendPaymentofEquityShares => _totalDividendPaymentofEquityShares ??= new(_timeProvider, _securityIdentifier);
        private TotalDividendPaymentofEquitySharesIncomeStatement _totalDividendPaymentofEquityShares;

        /// <summary>
        /// Total amount paid in dividends to Non-Equity securities investors.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20430
        /// </remarks>
        [JsonProperty("20430")]
        public TotalDividendPaymentofNonEquitySharesIncomeStatement TotalDividendPaymentofNonEquityShares => _totalDividendPaymentofNonEquityShares ??= new(_timeProvider, _securityIdentifier);
        private TotalDividendPaymentofNonEquitySharesIncomeStatement _totalDividendPaymentofNonEquityShares;

        /// <summary>
        /// The change in the amount of the unearned premium reserves maintained by insurers.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20431
        /// </remarks>
        [JsonProperty("20431")]
        public ChangeinTheGrossProvisionforUnearnedPremiumsIncomeStatement ChangeinTheGrossProvisionforUnearnedPremiums => _changeinTheGrossProvisionforUnearnedPremiums ??= new(_timeProvider, _securityIdentifier);
        private ChangeinTheGrossProvisionforUnearnedPremiumsIncomeStatement _changeinTheGrossProvisionforUnearnedPremiums;

        /// <summary>
        /// The change in the amount of unearned premium reserve to be covered by reinsurers.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20432
        /// </remarks>
        [JsonProperty("20432")]
        public ChangeinTheGrossProvisionforUnearnedPremiumsReinsurersShareIncomeStatement ChangeinTheGrossProvisionforUnearnedPremiumsReinsurersShare => _changeinTheGrossProvisionforUnearnedPremiumsReinsurersShare ??= new(_timeProvider, _securityIdentifier);
        private ChangeinTheGrossProvisionforUnearnedPremiumsReinsurersShareIncomeStatement _changeinTheGrossProvisionforUnearnedPremiumsReinsurersShare;

        /// <summary>
        /// Income/Expense due to the insurer's changes in insurance liabilities.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20433
        /// </remarks>
        [JsonProperty("20433")]
        public ClaimsandChangeinInsuranceLiabilitiesIncomeStatement ClaimsandChangeinInsuranceLiabilities => _claimsandChangeinInsuranceLiabilities ??= new(_timeProvider, _securityIdentifier);
        private ClaimsandChangeinInsuranceLiabilitiesIncomeStatement _claimsandChangeinInsuranceLiabilities;

        /// <summary>
        /// Income/Expense due to recoveries from reinsurers for insurance liabilities.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20434
        /// </remarks>
        [JsonProperty("20434")]
        public ReinsuranceRecoveriesofInsuranceLiabilitiesIncomeStatement ReinsuranceRecoveriesofInsuranceLiabilities => _reinsuranceRecoveriesofInsuranceLiabilities ??= new(_timeProvider, _securityIdentifier);
        private ReinsuranceRecoveriesofInsuranceLiabilitiesIncomeStatement _reinsuranceRecoveriesofInsuranceLiabilities;

        /// <summary>
        /// Operating profit/loss as reported by the company, may be the same or not the same as Morningstar's standardized definition.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20435
        /// </remarks>
        [JsonProperty("20435")]
        public TotalOperatingIncomeAsReportedIncomeStatement TotalOperatingIncomeAsReported => _totalOperatingIncomeAsReported ??= new(_timeProvider, _securityIdentifier);
        private TotalOperatingIncomeAsReportedIncomeStatement _totalOperatingIncomeAsReported;

        /// <summary>
        /// Other General and Administrative Expenses not categorized that the company incurs that are not directly tied to a specific function such as manufacturing, production, or sales.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20436
        /// </remarks>
        [JsonProperty("20436")]
        public OtherGAIncomeStatement OtherGA => _otherGA ??= new(_timeProvider, _securityIdentifier);
        private OtherGAIncomeStatement _otherGA;

        /// <summary>
        /// Other costs associated with the revenue-generating activities of the company not categorized above.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20437
        /// </remarks>
        [JsonProperty("20437")]
        public OtherCostofRevenueIncomeStatement OtherCostofRevenue => _otherCostofRevenue ??= new(_timeProvider, _securityIdentifier);
        private OtherCostofRevenueIncomeStatement _otherCostofRevenue;

        /// <summary>
        /// Costs paid to use the facilities necessary to generate revenue during the accounting period.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20438
        /// </remarks>
        [JsonProperty("20438")]
        public RentandLandingFeesCostofRevenueIncomeStatement RentandLandingFeesCostofRevenue => _rentandLandingFeesCostofRevenue ??= new(_timeProvider, _securityIdentifier);
        private RentandLandingFeesCostofRevenueIncomeStatement _rentandLandingFeesCostofRevenue;

        /// <summary>
        /// Costs of depreciation and amortization on assets used for the revenue-generating activities during the accounting period
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20439
        /// </remarks>
        [JsonProperty("20439")]
        public DDACostofRevenueIncomeStatement DDACostofRevenue => _dDACostofRevenue ??= new(_timeProvider, _securityIdentifier);
        private DDACostofRevenueIncomeStatement _dDACostofRevenue;

        /// <summary>
        /// The sum of all rent expenses incurred by the company for operating leases during the year, it is a supplemental value which would be reported outside consolidated statements or consolidated statement's footnotes.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20440
        /// </remarks>
        [JsonProperty("20440")]
        public RentExpenseSupplementalIncomeStatement RentExpenseSupplemental => _rentExpenseSupplemental ??= new(_timeProvider, _securityIdentifier);
        private RentExpenseSupplementalIncomeStatement _rentExpenseSupplemental;

        /// <summary>
        /// This calculation represents pre-tax earnings adjusted for items that are irregular or unusual in nature, and/or are non-recurring. This can be used to fairly measure a company's profitability. This is calculated using Pre-Tax Income plus/minus any unusual Items and Goodwill Impairments/Write Offs.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20441
        /// </remarks>
        [JsonProperty("20441")]
        public NormalizedPreTaxIncomeIncomeStatement NormalizedPreTaxIncome => _normalizedPreTaxIncome ??= new(_timeProvider, _securityIdentifier);
        private NormalizedPreTaxIncomeIncomeStatement _normalizedPreTaxIncome;

        /// <summary>
        /// The aggregate amount of research and development expenses during the year. It is a supplemental value which would be reported outside consolidated statements.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20442
        /// </remarks>
        [JsonProperty("20442")]
        public ResearchAndDevelopmentExpensesSupplementalIncomeStatement ResearchAndDevelopmentExpensesSupplemental => _researchAndDevelopmentExpensesSupplemental ??= new(_timeProvider, _securityIdentifier);
        private ResearchAndDevelopmentExpensesSupplementalIncomeStatement _researchAndDevelopmentExpensesSupplemental;

        /// <summary>
        /// The current period expense charged against earnings on tangible asset over its useful life. It is a supplemental value which would be reported outside consolidated statements.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20443
        /// </remarks>
        [JsonProperty("20443")]
        public DepreciationSupplementalIncomeStatement DepreciationSupplemental => _depreciationSupplemental ??= new(_timeProvider, _securityIdentifier);
        private DepreciationSupplementalIncomeStatement _depreciationSupplemental;

        /// <summary>
        /// The current period expense charged against earnings on intangible asset over its useful life. It is a supplemental value which would be reported outside consolidated statements.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20444
        /// </remarks>
        [JsonProperty("20444")]
        public AmortizationSupplementalIncomeStatement AmortizationSupplemental => _amortizationSupplemental ??= new(_timeProvider, _securityIdentifier);
        private AmortizationSupplementalIncomeStatement _amortizationSupplemental;

        /// <summary>
        /// Total revenue as reported by the company, may be the same or not the same as Morningstar's standardized definition.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20445
        /// </remarks>
        [JsonProperty("20445")]
        public TotalRevenueAsReportedIncomeStatement TotalRevenueAsReported => _totalRevenueAsReported ??= new(_timeProvider, _securityIdentifier);
        private TotalRevenueAsReportedIncomeStatement _totalRevenueAsReported;

        /// <summary>
        /// Operating expense as reported by the company, may be the same or not the same as Morningstar's standardized definition.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20446
        /// </remarks>
        [JsonProperty("20446")]
        public OperatingExpenseAsReportedIncomeStatement OperatingExpenseAsReported => _operatingExpenseAsReported ??= new(_timeProvider, _securityIdentifier);
        private OperatingExpenseAsReportedIncomeStatement _operatingExpenseAsReported;

        /// <summary>
        /// Earnings adjusted for items that are irregular or unusual in nature, and/or are non-recurring. This can be used to fairly measure a company's profitability. This is as reported by the company, may be the same or not the same as Morningstar's standardized definition.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20447
        /// </remarks>
        [JsonProperty("20447")]
        public NormalizedIncomeAsReportedIncomeStatement NormalizedIncomeAsReported => _normalizedIncomeAsReported ??= new(_timeProvider, _securityIdentifier);
        private NormalizedIncomeAsReportedIncomeStatement _normalizedIncomeAsReported;

        /// <summary>
        /// EBITDA less Total Unusual Items. This is as reported by the company, may be the same or not the same as Morningstar's standardized definition.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20448
        /// </remarks>
        [JsonProperty("20448")]
        public NormalizedEBITDAAsReportedIncomeStatement NormalizedEBITDAAsReported => _normalizedEBITDAAsReported ??= new(_timeProvider, _securityIdentifier);
        private NormalizedEBITDAAsReportedIncomeStatement _normalizedEBITDAAsReported;

        /// <summary>
        /// EBIT less Total Unusual Items. This is as reported by the company, may be the same or not the same as Morningstar's standardized definition.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20449
        /// </remarks>
        [JsonProperty("20449")]
        public NormalizedEBITAsReportedIncomeStatement NormalizedEBITAsReported => _normalizedEBITAsReported ??= new(_timeProvider, _securityIdentifier);
        private NormalizedEBITAsReportedIncomeStatement _normalizedEBITAsReported;

        /// <summary>
        /// Operating profit adjusted for items that are irregular or unusual in nature, and/or are non-recurring. This can be used to fairly measure a company's profitability. This is as reported by the company, may be the same or not the same as Morningstar's standardized definition.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20450
        /// </remarks>
        [JsonProperty("20450")]
        public NormalizedOperatingProfitAsReportedIncomeStatement NormalizedOperatingProfitAsReported => _normalizedOperatingProfitAsReported ??= new(_timeProvider, _securityIdentifier);
        private NormalizedOperatingProfitAsReportedIncomeStatement _normalizedOperatingProfitAsReported;

        /// <summary>
        /// The average tax rate for the period as reported by the company, may be the same or not the same as Morningstar's standardized definition.
        /// </summary>
        /// <remarks>
        /// Morningstar DataId: 20451
        /// </remarks>
        [JsonProperty("20451")]
        public EffectiveTaxRateAsReportedIncomeStatement EffectiveTaxRateAsReported => _effectiveTaxRateAsReported ??= new(_timeProvider, _securityIdentifier);
        private EffectiveTaxRateAsReportedIncomeStatement _effectiveTaxRateAsReported;

        private readonly ITimeProvider _timeProvider;
        private readonly SecurityIdentifier _securityIdentifier;

        /// <summary>
        /// Creates a new instance for the given time and security
        /// </summary>
        public IncomeStatement(ITimeProvider timeProvider, SecurityIdentifier securityIdentifier)
        {
            _timeProvider = timeProvider;
            _securityIdentifier = securityIdentifier;
        }
    }
}
