from .__Fundamental_4 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class AVG5YrsROIC(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This is the simple average of the company's ROIC over the last 5 years. Return on invested capital is calculated by taking net
                operating profit after taxes and dividends and dividing by the total amount of capital invested and expressing the result as a
                percentage.
    
    AVG5YrsROIC(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.AVG5YrsROIC:
        pass

    FiveYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BalanceSheet(System.object):
    """
    Definition of the BalanceSheet class
    
    BalanceSheet()
    """
    def UpdateValues(self, update: QuantConnect.Data.Fundamental.BalanceSheet) -> None:
        pass

    AccountsPayable: QuantConnect.Data.Fundamental.AccountsPayableBalanceSheet

    AccountsReceivable: QuantConnect.Data.Fundamental.AccountsReceivableBalanceSheet

    AccruedandDeferredIncome: QuantConnect.Data.Fundamental.AccruedandDeferredIncomeBalanceSheet

    AccruedandDeferredIncomeCurrent: QuantConnect.Data.Fundamental.AccruedandDeferredIncomeCurrentBalanceSheet

    AccruedandDeferredIncomeNonCurrent: QuantConnect.Data.Fundamental.AccruedandDeferredIncomeNonCurrentBalanceSheet

    AccruedInterestReceivable: QuantConnect.Data.Fundamental.AccruedInterestReceivableBalanceSheet

    AccruedInvestmentIncome: QuantConnect.Data.Fundamental.AccruedInvestmentIncomeBalanceSheet

    AccruedLiabilitiesTotal: QuantConnect.Data.Fundamental.AccruedLiabilitiesTotalBalanceSheet

    AccumulatedDepreciation: QuantConnect.Data.Fundamental.AccumulatedDepreciationBalanceSheet

    AdditionalPaidInCapital: QuantConnect.Data.Fundamental.AdditionalPaidInCapitalBalanceSheet

    AdvanceFromFederalHomeLoanBanks: QuantConnect.Data.Fundamental.AdvanceFromFederalHomeLoanBanksBalanceSheet

    AdvancesfromCentralBanks: QuantConnect.Data.Fundamental.AdvancesfromCentralBanksBalanceSheet

    AllowanceForDoubtfulAccountsReceivable: QuantConnect.Data.Fundamental.AllowanceForDoubtfulAccountsReceivableBalanceSheet

    AllowanceForLoansAndLeaseLosses: QuantConnect.Data.Fundamental.AllowanceForLoansAndLeaseLossesBalanceSheet

    AllowanceForNotesReceivable: QuantConnect.Data.Fundamental.AllowanceForNotesReceivableBalanceSheet

    AssetsHeldForSale: QuantConnect.Data.Fundamental.AssetsHeldForSaleBalanceSheet

    AssetsHeldForSaleCurrent: QuantConnect.Data.Fundamental.AssetsHeldForSaleCurrentBalanceSheet

    AssetsHeldForSaleNonCurrent: QuantConnect.Data.Fundamental.AssetsHeldForSaleNonCurrentBalanceSheet

    AssetsOfDiscontinuedOperations: QuantConnect.Data.Fundamental.AssetsOfDiscontinuedOperationsBalanceSheet

    AssetsPledgedasCollateralSubjecttoSaleorRepledgingTotal: QuantConnect.Data.Fundamental.AssetsPledgedasCollateralSubjecttoSaleorRepledgingTotalBalanceSheet

    AvailableForSaleSecurities: QuantConnect.Data.Fundamental.AvailableForSaleSecuritiesBalanceSheet

    BankIndebtedness: QuantConnect.Data.Fundamental.BankIndebtednessBalanceSheet

    BankLoansCurrent: QuantConnect.Data.Fundamental.BankLoansCurrentBalanceSheet

    BankLoansNonCurrent: QuantConnect.Data.Fundamental.BankLoansNonCurrentBalanceSheet

    BankLoansTotal: QuantConnect.Data.Fundamental.BankLoansTotalBalanceSheet

    BankOwnedLifeInsurance: QuantConnect.Data.Fundamental.BankOwnedLifeInsuranceBalanceSheet

    BiologicalAssets: QuantConnect.Data.Fundamental.BiologicalAssetsBalanceSheet

    BSFileDate: datetime.datetime

    BuildingsAndImprovements: QuantConnect.Data.Fundamental.BuildingsAndImprovementsBalanceSheet

    CapitalLeaseObligations: QuantConnect.Data.Fundamental.CapitalLeaseObligationsBalanceSheet

    CapitalStock: QuantConnect.Data.Fundamental.CapitalStockBalanceSheet

    Cash: QuantConnect.Data.Fundamental.CashBalanceSheet

    CashAndCashEquivalents: QuantConnect.Data.Fundamental.CashAndCashEquivalentsBalanceSheet

    CashAndDueFromBanks: QuantConnect.Data.Fundamental.CashAndDueFromBanksBalanceSheet

    CashCashEquivalentsAndFederalFundsSold: QuantConnect.Data.Fundamental.CashCashEquivalentsAndFederalFundsSoldBalanceSheet

    CashCashEquivalentsAndMarketableSecurities: QuantConnect.Data.Fundamental.CashCashEquivalentsAndMarketableSecuritiesBalanceSheet

    CashEquivalents: QuantConnect.Data.Fundamental.CashEquivalentsBalanceSheet

    CashRestrictedOrPledged: QuantConnect.Data.Fundamental.CashRestrictedOrPledgedBalanceSheet

    ClaimsOutstanding: QuantConnect.Data.Fundamental.ClaimsOutstandingBalanceSheet

    CommercialLoan: QuantConnect.Data.Fundamental.CommercialLoanBalanceSheet

    CommercialPaper: QuantConnect.Data.Fundamental.CommercialPaperBalanceSheet

    CommonStock: QuantConnect.Data.Fundamental.CommonStockBalanceSheet

    CommonStockEquity: QuantConnect.Data.Fundamental.CommonStockEquityBalanceSheet

    CommonUtilityPlant: QuantConnect.Data.Fundamental.CommonUtilityPlantBalanceSheet

    ComTreShaNum: QuantConnect.Data.Fundamental.ComTreShaNumBalanceSheet

    ConstructionInProgress: QuantConnect.Data.Fundamental.ConstructionInProgressBalanceSheet

    ConsumerLoan: QuantConnect.Data.Fundamental.ConsumerLoanBalanceSheet

    ConvertibleLoansCurrent: QuantConnect.Data.Fundamental.ConvertibleLoansCurrentBalanceSheet

    ConvertibleLoansNonCurrent: QuantConnect.Data.Fundamental.ConvertibleLoansNonCurrentBalanceSheet

    ConvertibleLoansTotal: QuantConnect.Data.Fundamental.ConvertibleLoansTotalBalanceSheet

    CurrentAccruedExpenses: QuantConnect.Data.Fundamental.CurrentAccruedExpensesBalanceSheet

    CurrentAssets: QuantConnect.Data.Fundamental.CurrentAssetsBalanceSheet

    CurrentCapitalLeaseObligation: QuantConnect.Data.Fundamental.CurrentCapitalLeaseObligationBalanceSheet

    CurrentDebt: QuantConnect.Data.Fundamental.CurrentDebtBalanceSheet

    CurrentDebtAndCapitalLeaseObligation: QuantConnect.Data.Fundamental.CurrentDebtAndCapitalLeaseObligationBalanceSheet

    CurrentDeferredAssets: QuantConnect.Data.Fundamental.CurrentDeferredAssetsBalanceSheet

    CurrentDeferredLiabilities: QuantConnect.Data.Fundamental.CurrentDeferredLiabilitiesBalanceSheet

    CurrentDeferredRevenue: QuantConnect.Data.Fundamental.CurrentDeferredRevenueBalanceSheet

    CurrentDeferredTaxesAssets: QuantConnect.Data.Fundamental.CurrentDeferredTaxesAssetsBalanceSheet

    CurrentDeferredTaxesLiabilities: QuantConnect.Data.Fundamental.CurrentDeferredTaxesLiabilitiesBalanceSheet

    CurrentLiabilities: QuantConnect.Data.Fundamental.CurrentLiabilitiesBalanceSheet

    CurrentNotesPayable: QuantConnect.Data.Fundamental.CurrentNotesPayableBalanceSheet

    CurrentOtherFinancialLiabilities: QuantConnect.Data.Fundamental.CurrentOtherFinancialLiabilitiesBalanceSheet

    CurrentProvisions: QuantConnect.Data.Fundamental.CurrentProvisionsBalanceSheet

    CustomerAcceptances: QuantConnect.Data.Fundamental.CustomerAcceptancesBalanceSheet

    CustomerAccounts: QuantConnect.Data.Fundamental.CustomerAccountsBalanceSheet

    DebtDueBeyond: QuantConnect.Data.Fundamental.DebtDueBeyondBalanceSheet

    DebtDueInYear1: QuantConnect.Data.Fundamental.DebtDueInYear1BalanceSheet

    DebtDueInYear2: QuantConnect.Data.Fundamental.DebtDueInYear2BalanceSheet

    DebtDueInYear5: QuantConnect.Data.Fundamental.DebtDueInYear5BalanceSheet

    DebtSecurities: QuantConnect.Data.Fundamental.DebtSecuritiesBalanceSheet

    DebtSecuritiesinIssue: QuantConnect.Data.Fundamental.DebtSecuritiesinIssueBalanceSheet

    DebtTotal: QuantConnect.Data.Fundamental.DebtTotalBalanceSheet

    DeferredAssets: QuantConnect.Data.Fundamental.DeferredAssetsBalanceSheet

    DeferredCosts: QuantConnect.Data.Fundamental.DeferredCostsBalanceSheet

    DeferredIncomeTotal: QuantConnect.Data.Fundamental.DeferredIncomeTotalBalanceSheet

    DeferredPolicyAcquisitionCosts: QuantConnect.Data.Fundamental.DeferredPolicyAcquisitionCostsBalanceSheet

    DeferredTaxAssets: QuantConnect.Data.Fundamental.DeferredTaxAssetsBalanceSheet

    DeferredTaxLiabilitiesTotal: QuantConnect.Data.Fundamental.DeferredTaxLiabilitiesTotalBalanceSheet

    DefinedPensionBenefit: QuantConnect.Data.Fundamental.DefinedPensionBenefitBalanceSheet

    DepositCertificates: QuantConnect.Data.Fundamental.DepositCertificatesBalanceSheet

    DepositsbyBank: QuantConnect.Data.Fundamental.DepositsbyBankBalanceSheet

    DepositsMadeunderAssumedReinsuranceContract: QuantConnect.Data.Fundamental.DepositsMadeunderAssumedReinsuranceContractBalanceSheet

    DepositsReceivedunderCededInsuranceContract: QuantConnect.Data.Fundamental.DepositsReceivedunderCededInsuranceContractBalanceSheet

    DerivativeAssets: QuantConnect.Data.Fundamental.DerivativeAssetsBalanceSheet

    DerivativeProductLiabilities: QuantConnect.Data.Fundamental.DerivativeProductLiabilitiesBalanceSheet

    DividendsPayable: QuantConnect.Data.Fundamental.DividendsPayableBalanceSheet

    DueFromRelatedParties: QuantConnect.Data.Fundamental.DueFromRelatedPartiesBalanceSheet

    DuefromRelatedPartiesCurrent: QuantConnect.Data.Fundamental.DuefromRelatedPartiesCurrentBalanceSheet

    DuefromRelatedPartiesNonCurrent: QuantConnect.Data.Fundamental.DuefromRelatedPartiesNonCurrentBalanceSheet

    DuetoRelatedParties: QuantConnect.Data.Fundamental.DuetoRelatedPartiesBalanceSheet

    DuetoRelatedPartiesCurrent: QuantConnect.Data.Fundamental.DuetoRelatedPartiesCurrentBalanceSheet

    DuetoRelatedPartiesNonCurrent: QuantConnect.Data.Fundamental.DuetoRelatedPartiesNonCurrentBalanceSheet

    ElectricUtilityPlant: QuantConnect.Data.Fundamental.ElectricUtilityPlantBalanceSheet

    EmployeeBenefits: QuantConnect.Data.Fundamental.EmployeeBenefitsBalanceSheet

    EquityAttributableToOwnersOfParent: QuantConnect.Data.Fundamental.EquityAttributableToOwnersOfParentBalanceSheet

    EquityInvestments: QuantConnect.Data.Fundamental.EquityInvestmentsBalanceSheet

    EquitySharesInvestments: QuantConnect.Data.Fundamental.EquitySharesInvestmentsBalanceSheet

    FederalFundsPurchased: QuantConnect.Data.Fundamental.FederalFundsPurchasedBalanceSheet

    FederalFundsPurchasedAndSecuritiesSoldUnderAgreementToRepurchase: QuantConnect.Data.Fundamental.FederalFundsPurchasedAndSecuritiesSoldUnderAgreementToRepurchaseBalanceSheet

    FederalFundsSold: QuantConnect.Data.Fundamental.FederalFundsSoldBalanceSheet

    FederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResell: QuantConnect.Data.Fundamental.FederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResellBalanceSheet

    FederalHomeLoanBankStock: QuantConnect.Data.Fundamental.FederalHomeLoanBankStockBalanceSheet

    FinanceLeaseReceivables: QuantConnect.Data.Fundamental.FinanceLeaseReceivablesBalanceSheet

    FinanceLeaseReceivablesCurrent: QuantConnect.Data.Fundamental.FinanceLeaseReceivablesCurrentBalanceSheet

    FinanceLeaseReceivablesNonCurrent: QuantConnect.Data.Fundamental.FinanceLeaseReceivablesNonCurrentBalanceSheet

    FinancialAssets: QuantConnect.Data.Fundamental.FinancialAssetsBalanceSheet

    FinancialAssetsDesignatedasFairValueThroughProfitorLossTotal: QuantConnect.Data.Fundamental.FinancialAssetsDesignatedasFairValueThroughProfitorLossTotalBalanceSheet

    FinancialInstrumentsSoldUnderAgreementsToRepurchase: QuantConnect.Data.Fundamental.FinancialInstrumentsSoldUnderAgreementsToRepurchaseBalanceSheet

    FinancialLiabilitiesCurrent: QuantConnect.Data.Fundamental.FinancialLiabilitiesCurrentBalanceSheet

    FinancialLiabilitiesDesignatedasFairValueThroughProfitorLossTotal: QuantConnect.Data.Fundamental.FinancialLiabilitiesDesignatedasFairValueThroughProfitorLossTotalBalanceSheet

    FinancialLiabilitiesMeasuredatAmortizedCostTotal: QuantConnect.Data.Fundamental.FinancialLiabilitiesMeasuredatAmortizedCostTotalBalanceSheet

    FinancialLiabilitiesNonCurrent: QuantConnect.Data.Fundamental.FinancialLiabilitiesNonCurrentBalanceSheet

    FinancialOrDerivativeInvestmentCurrentLiabilities: QuantConnect.Data.Fundamental.FinancialOrDerivativeInvestmentCurrentLiabilitiesBalanceSheet

    FinishedGoods: QuantConnect.Data.Fundamental.FinishedGoodsBalanceSheet

    FixedAssetsRevaluationReserve: QuantConnect.Data.Fundamental.FixedAssetsRevaluationReserveBalanceSheet

    FixedMaturityInvestments: QuantConnect.Data.Fundamental.FixedMaturityInvestmentsBalanceSheet

    FlightFleetVehicleAndRelatedEquipments: QuantConnect.Data.Fundamental.FlightFleetVehicleAndRelatedEquipmentsBalanceSheet

    ForeclosedAssets: QuantConnect.Data.Fundamental.ForeclosedAssetsBalanceSheet

    ForeignCurrencyTranslationAdjustments: QuantConnect.Data.Fundamental.ForeignCurrencyTranslationAdjustmentsBalanceSheet

    FuturePolicyBenefits: QuantConnect.Data.Fundamental.FuturePolicyBenefitsBalanceSheet

    GainsLossesNotAffectingRetainedEarnings: QuantConnect.Data.Fundamental.GainsLossesNotAffectingRetainedEarningsBalanceSheet

    GeneralPartnershipCapital: QuantConnect.Data.Fundamental.GeneralPartnershipCapitalBalanceSheet

    Goodwill: QuantConnect.Data.Fundamental.GoodwillBalanceSheet

    GoodwillAndOtherIntangibleAssets: QuantConnect.Data.Fundamental.GoodwillAndOtherIntangibleAssetsBalanceSheet

    GrossAccountsReceivable: QuantConnect.Data.Fundamental.GrossAccountsReceivableBalanceSheet

    GrossLoan: QuantConnect.Data.Fundamental.GrossLoanBalanceSheet

    GrossNotesReceivable: QuantConnect.Data.Fundamental.GrossNotesReceivableBalanceSheet

    GrossPPE: QuantConnect.Data.Fundamental.GrossPPEBalanceSheet

    HedgingAssetsCurrent: QuantConnect.Data.Fundamental.HedgingAssetsCurrentBalanceSheet

    HeldToMaturitySecurities: QuantConnect.Data.Fundamental.HeldToMaturitySecuritiesBalanceSheet

    IncomeTaxPayable: QuantConnect.Data.Fundamental.IncomeTaxPayableBalanceSheet

    InsuranceContractAssets: QuantConnect.Data.Fundamental.InsuranceContractAssetsBalanceSheet

    InsuranceContractLiabilities: QuantConnect.Data.Fundamental.InsuranceContractLiabilitiesBalanceSheet

    InsuranceFundsNonCurrent: QuantConnect.Data.Fundamental.InsuranceFundsNonCurrentBalanceSheet

    InterestBearingBorrowingsNonCurrent: QuantConnect.Data.Fundamental.InterestBearingBorrowingsNonCurrentBalanceSheet

    InterestBearingDepositsAssets: QuantConnect.Data.Fundamental.InterestBearingDepositsAssetsBalanceSheet

    InterestBearingDepositsLiabilities: QuantConnect.Data.Fundamental.InterestBearingDepositsLiabilitiesBalanceSheet

    InterestPayable: QuantConnect.Data.Fundamental.InterestPayableBalanceSheet

    InventoriesAdjustmentsAllowances: QuantConnect.Data.Fundamental.InventoriesAdjustmentsAllowancesBalanceSheet

    Inventory: QuantConnect.Data.Fundamental.InventoryBalanceSheet

    InvestedCapital: QuantConnect.Data.Fundamental.InvestedCapitalBalanceSheet

    InvestmentContractLiabilities: QuantConnect.Data.Fundamental.InvestmentContractLiabilitiesBalanceSheet

    InvestmentinFinancialAssets: QuantConnect.Data.Fundamental.InvestmentinFinancialAssetsBalanceSheet

    InvestmentProperties: QuantConnect.Data.Fundamental.InvestmentPropertiesBalanceSheet

    InvestmentsAndAdvances: QuantConnect.Data.Fundamental.InvestmentsAndAdvancesBalanceSheet

    InvestmentsinAssociatesatCost: QuantConnect.Data.Fundamental.InvestmentsinAssociatesatCostBalanceSheet

    InvestmentsinJointVenturesatCost: QuantConnect.Data.Fundamental.InvestmentsinJointVenturesatCostBalanceSheet

    InvestmentsInOtherVenturesUnderEquityMethod: QuantConnect.Data.Fundamental.InvestmentsInOtherVenturesUnderEquityMethodBalanceSheet

    InvestmentsinSubsidiariesatCost: QuantConnect.Data.Fundamental.InvestmentsinSubsidiariesatCostBalanceSheet

    ItemsinTheCourseofTransmissiontoOtherBanks: QuantConnect.Data.Fundamental.ItemsinTheCourseofTransmissiontoOtherBanksBalanceSheet

    LandAndImprovements: QuantConnect.Data.Fundamental.LandAndImprovementsBalanceSheet

    Leases: QuantConnect.Data.Fundamental.LeasesBalanceSheet

    LiabilitiesHeldforSaleCurrent: QuantConnect.Data.Fundamental.LiabilitiesHeldforSaleCurrentBalanceSheet

    LiabilitiesHeldforSaleNonCurrent: QuantConnect.Data.Fundamental.LiabilitiesHeldforSaleNonCurrentBalanceSheet

    LiabilitiesHeldforSaleTotal: QuantConnect.Data.Fundamental.LiabilitiesHeldforSaleTotalBalanceSheet

    LiabilitiesOfDiscontinuedOperations: QuantConnect.Data.Fundamental.LiabilitiesOfDiscontinuedOperationsBalanceSheet

    LimitedPartnershipCapital: QuantConnect.Data.Fundamental.LimitedPartnershipCapitalBalanceSheet

    LineOfCredit: QuantConnect.Data.Fundamental.LineOfCreditBalanceSheet

    LoansandAdvancestoBank: QuantConnect.Data.Fundamental.LoansandAdvancestoBankBalanceSheet

    LoansandAdvancestoCustomer: QuantConnect.Data.Fundamental.LoansandAdvancestoCustomerBalanceSheet

    LoansHeldForSale: QuantConnect.Data.Fundamental.LoansHeldForSaleBalanceSheet

    LoansReceivable: QuantConnect.Data.Fundamental.LoansReceivableBalanceSheet

    LongTermCapitalLeaseObligation: QuantConnect.Data.Fundamental.LongTermCapitalLeaseObligationBalanceSheet

    LongTermDebt: QuantConnect.Data.Fundamental.LongTermDebtBalanceSheet

    LongTermDebtAndCapitalLeaseObligation: QuantConnect.Data.Fundamental.LongTermDebtAndCapitalLeaseObligationBalanceSheet

    LongTermInvestments: QuantConnect.Data.Fundamental.LongTermInvestmentsBalanceSheet

    LongTermProvisions: QuantConnect.Data.Fundamental.LongTermProvisionsBalanceSheet

    MachineryFurnitureEquipment: QuantConnect.Data.Fundamental.MachineryFurnitureEquipmentBalanceSheet

    MaterialsAndSupplies: QuantConnect.Data.Fundamental.MaterialsAndSuppliesBalanceSheet

    MineralProperties: QuantConnect.Data.Fundamental.MineralPropertiesBalanceSheet

    MinimumPensionLiabilities: QuantConnect.Data.Fundamental.MinimumPensionLiabilitiesBalanceSheet

    MinorityInterest: QuantConnect.Data.Fundamental.MinorityInterestBalanceSheet

    MoneyMarketInvestments: QuantConnect.Data.Fundamental.MoneyMarketInvestmentsBalanceSheet

    MortgageAndConsumerloans: QuantConnect.Data.Fundamental.MortgageAndConsumerloansBalanceSheet

    MortgageLoan: QuantConnect.Data.Fundamental.MortgageLoanBalanceSheet

    NaturalGasFuelAndOther: QuantConnect.Data.Fundamental.NaturalGasFuelAndOtherBalanceSheet

    NetDebt: QuantConnect.Data.Fundamental.NetDebtBalanceSheet

    NetLoan: QuantConnect.Data.Fundamental.NetLoanBalanceSheet

    NetPPE: QuantConnect.Data.Fundamental.NetPPEBalanceSheet

    NetTangibleAssets: QuantConnect.Data.Fundamental.NetTangibleAssetsBalanceSheet

    NetUtilityPlant: QuantConnect.Data.Fundamental.NetUtilityPlantBalanceSheet

    NonCurrentAccountsReceivable: QuantConnect.Data.Fundamental.NonCurrentAccountsReceivableBalanceSheet

    NonCurrentAccruedExpenses: QuantConnect.Data.Fundamental.NonCurrentAccruedExpensesBalanceSheet

    NonCurrentDeferredAssets: QuantConnect.Data.Fundamental.NonCurrentDeferredAssetsBalanceSheet

    NonCurrentDeferredLiabilities: QuantConnect.Data.Fundamental.NonCurrentDeferredLiabilitiesBalanceSheet

    NonCurrentDeferredRevenue: QuantConnect.Data.Fundamental.NonCurrentDeferredRevenueBalanceSheet

    NonCurrentDeferredTaxesAssets: QuantConnect.Data.Fundamental.NonCurrentDeferredTaxesAssetsBalanceSheet

    NonCurrentDeferredTaxesLiabilities: QuantConnect.Data.Fundamental.NonCurrentDeferredTaxesLiabilitiesBalanceSheet

    NonCurrentNoteReceivables: QuantConnect.Data.Fundamental.NonCurrentNoteReceivablesBalanceSheet

    NonCurrentOtherFinancialLiabilities: QuantConnect.Data.Fundamental.NonCurrentOtherFinancialLiabilitiesBalanceSheet

    NonCurrentPensionAndOtherPostretirementBenefitPlans: QuantConnect.Data.Fundamental.NonCurrentPensionAndOtherPostretirementBenefitPlansBalanceSheet

    NonCurrentPrepaidAssets: QuantConnect.Data.Fundamental.NonCurrentPrepaidAssetsBalanceSheet

    NonInterestBearingBorrowingsCurrent: QuantConnect.Data.Fundamental.NonInterestBearingBorrowingsCurrentBalanceSheet

    NonInterestBearingBorrowingsNonCurrent: QuantConnect.Data.Fundamental.NonInterestBearingBorrowingsNonCurrentBalanceSheet

    NonInterestBearingBorrowingsTotal: QuantConnect.Data.Fundamental.NonInterestBearingBorrowingsTotalBalanceSheet

    NonInterestBearingDeposits: QuantConnect.Data.Fundamental.NonInterestBearingDepositsBalanceSheet

    NotesReceivable: QuantConnect.Data.Fundamental.NotesReceivableBalanceSheet

    OperatingLeaseAssets: QuantConnect.Data.Fundamental.OperatingLeaseAssetsBalanceSheet

    OrdinarySharesNumber: QuantConnect.Data.Fundamental.OrdinarySharesNumberBalanceSheet

    OtherAssets: QuantConnect.Data.Fundamental.OtherAssetsBalanceSheet

    OtherBorrowedFunds: QuantConnect.Data.Fundamental.OtherBorrowedFundsBalanceSheet

    OtherCapitalStock: QuantConnect.Data.Fundamental.OtherCapitalStockBalanceSheet

    OtherCurrentAssets: QuantConnect.Data.Fundamental.OtherCurrentAssetsBalanceSheet

    OtherCurrentBorrowings: QuantConnect.Data.Fundamental.OtherCurrentBorrowingsBalanceSheet

    OtherCurrentLiabilities: QuantConnect.Data.Fundamental.OtherCurrentLiabilitiesBalanceSheet

    OtherEquityAdjustments: QuantConnect.Data.Fundamental.OtherEquityAdjustmentsBalanceSheet

    OtherEquityInterest: QuantConnect.Data.Fundamental.OtherEquityInterestBalanceSheet

    OtherFinancialLiabilities: QuantConnect.Data.Fundamental.OtherFinancialLiabilitiesBalanceSheet

    OtherIntangibleAssets: QuantConnect.Data.Fundamental.OtherIntangibleAssetsBalanceSheet

    OtherInventories: QuantConnect.Data.Fundamental.OtherInventoriesBalanceSheet

    OtherInvestedAssets: QuantConnect.Data.Fundamental.OtherInvestedAssetsBalanceSheet

    OtherInvestments: QuantConnect.Data.Fundamental.OtherInvestmentsBalanceSheet

    OtherLiabilities: QuantConnect.Data.Fundamental.OtherLiabilitiesBalanceSheet

    OtherLoanAssets: QuantConnect.Data.Fundamental.OtherLoanAssetsBalanceSheet

    OtherLoansCurrent: QuantConnect.Data.Fundamental.OtherLoansCurrentBalanceSheet

    OtherLoansNonCurrent: QuantConnect.Data.Fundamental.OtherLoansNonCurrentBalanceSheet

    OtherLoansTotal: QuantConnect.Data.Fundamental.OtherLoansTotalBalanceSheet

    OtherNonCurrentAssets: QuantConnect.Data.Fundamental.OtherNonCurrentAssetsBalanceSheet

    OtherNonCurrentLiabilities: QuantConnect.Data.Fundamental.OtherNonCurrentLiabilitiesBalanceSheet

    OtherPayable: QuantConnect.Data.Fundamental.OtherPayableBalanceSheet

    OtherProperties: QuantConnect.Data.Fundamental.OtherPropertiesBalanceSheet

    OtherRealEstateOwned: QuantConnect.Data.Fundamental.OtherRealEstateOwnedBalanceSheet

    OtherReceivables: QuantConnect.Data.Fundamental.OtherReceivablesBalanceSheet

    OtherReserves: QuantConnect.Data.Fundamental.OtherReservesBalanceSheet

    OtherShortTermInvestments: QuantConnect.Data.Fundamental.OtherShortTermInvestmentsBalanceSheet

    Payables: QuantConnect.Data.Fundamental.PayablesBalanceSheet

    PayablesAndAccruedExpenses: QuantConnect.Data.Fundamental.PayablesAndAccruedExpensesBalanceSheet

    PensionandOtherPostRetirementBenefitPlansCurrent: QuantConnect.Data.Fundamental.PensionandOtherPostRetirementBenefitPlansCurrentBalanceSheet

    PensionAndOtherPostretirementBenefitPlansTotal: QuantConnect.Data.Fundamental.PensionAndOtherPostretirementBenefitPlansTotalBalanceSheet

    PolicyholderFunds: QuantConnect.Data.Fundamental.PolicyholderFundsBalanceSheet

    PolicyLoans: QuantConnect.Data.Fundamental.PolicyLoansBalanceSheet

    PolicyReservesBenefits: QuantConnect.Data.Fundamental.PolicyReservesBenefitsBalanceSheet

    PreferredSecuritiesOutsideStockEquity: QuantConnect.Data.Fundamental.PreferredSecuritiesOutsideStockEquityBalanceSheet

    PreferredSharesNumber: QuantConnect.Data.Fundamental.PreferredSharesNumberBalanceSheet

    PreferredStock: QuantConnect.Data.Fundamental.PreferredStockBalanceSheet

    PreferredStockEquity: QuantConnect.Data.Fundamental.PreferredStockEquityBalanceSheet

    PrepaidAssets: QuantConnect.Data.Fundamental.PrepaidAssetsBalanceSheet

    PreTreShaNum: QuantConnect.Data.Fundamental.PreTreShaNumBalanceSheet

    Properties: QuantConnect.Data.Fundamental.PropertiesBalanceSheet

    ProvisionsTotal: QuantConnect.Data.Fundamental.ProvisionsTotalBalanceSheet

    RawMaterials: QuantConnect.Data.Fundamental.RawMaterialsBalanceSheet

    Receivables: QuantConnect.Data.Fundamental.ReceivablesBalanceSheet

    ReceivablesAdjustmentsAllowances: QuantConnect.Data.Fundamental.ReceivablesAdjustmentsAllowancesBalanceSheet

    RegulatoryAssets: QuantConnect.Data.Fundamental.RegulatoryAssetsBalanceSheet

    RegulatoryLiabilities: QuantConnect.Data.Fundamental.RegulatoryLiabilitiesBalanceSheet

    ReinsuranceAssets: QuantConnect.Data.Fundamental.ReinsuranceAssetsBalanceSheet

    ReinsuranceBalancesPayable: QuantConnect.Data.Fundamental.ReinsuranceBalancesPayableBalanceSheet

    ReinsuranceRecoverable: QuantConnect.Data.Fundamental.ReinsuranceRecoverableBalanceSheet

    RestrictedCash: QuantConnect.Data.Fundamental.RestrictedCashBalanceSheet

    RestrictedCashAndCashEquivalents: QuantConnect.Data.Fundamental.RestrictedCashAndCashEquivalentsBalanceSheet

    RestrictedCashAndInvestments: QuantConnect.Data.Fundamental.RestrictedCashAndInvestmentsBalanceSheet

    RestrictedCommonStock: QuantConnect.Data.Fundamental.RestrictedCommonStockBalanceSheet

    RestrictedInvestments: QuantConnect.Data.Fundamental.RestrictedInvestmentsBalanceSheet

    RetainedEarnings: QuantConnect.Data.Fundamental.RetainedEarningsBalanceSheet

    SecuritiesAndInvestments: QuantConnect.Data.Fundamental.SecuritiesAndInvestmentsBalanceSheet

    SecuritiesLendingCollateral: QuantConnect.Data.Fundamental.SecuritiesLendingCollateralBalanceSheet

    SecuritiesLoaned: QuantConnect.Data.Fundamental.SecuritiesLoanedBalanceSheet

    SecurityAgreeToBeResell: QuantConnect.Data.Fundamental.SecurityAgreeToBeResellBalanceSheet

    SecurityBorrowed: QuantConnect.Data.Fundamental.SecurityBorrowedBalanceSheet

    SecuritySoldNotYetRepurchased: QuantConnect.Data.Fundamental.SecuritySoldNotYetRepurchasedBalanceSheet

    SeparateAccountAssets: QuantConnect.Data.Fundamental.SeparateAccountAssetsBalanceSheet

    SeparateAccountBusiness: QuantConnect.Data.Fundamental.SeparateAccountBusinessBalanceSheet

    ShareIssued: QuantConnect.Data.Fundamental.ShareIssuedBalanceSheet

    ShortTermInvestmentsAvailableForSale: QuantConnect.Data.Fundamental.ShortTermInvestmentsAvailableForSaleBalanceSheet

    ShortTermInvestmentsHeldToMaturity: QuantConnect.Data.Fundamental.ShortTermInvestmentsHeldToMaturityBalanceSheet

    ShortTermInvestmentsTrading: QuantConnect.Data.Fundamental.ShortTermInvestmentsTradingBalanceSheet

    StockholdersEquity: QuantConnect.Data.Fundamental.StockholdersEquityBalanceSheet

    SubordinatedLiabilities: QuantConnect.Data.Fundamental.SubordinatedLiabilitiesBalanceSheet

    TangibleBookValue: QuantConnect.Data.Fundamental.TangibleBookValueBalanceSheet

    TaxAssetsTotal: QuantConnect.Data.Fundamental.TaxAssetsTotalBalanceSheet

    TaxesAssetsCurrent: QuantConnect.Data.Fundamental.TaxesAssetsCurrentBalanceSheet

    TaxesReceivable: QuantConnect.Data.Fundamental.TaxesReceivableBalanceSheet

    TotalAssets: QuantConnect.Data.Fundamental.TotalAssetsBalanceSheet

    TotalCapitalization: QuantConnect.Data.Fundamental.TotalCapitalizationBalanceSheet

    TotalDebt: QuantConnect.Data.Fundamental.TotalDebtBalanceSheet

    TotalDebtInMaturitySchedule: QuantConnect.Data.Fundamental.TotalDebtInMaturityScheduleBalanceSheet

    TotalDeferredCreditsAndOtherNonCurrentLiabilities: QuantConnect.Data.Fundamental.TotalDeferredCreditsAndOtherNonCurrentLiabilitiesBalanceSheet

    TotalDeposits: QuantConnect.Data.Fundamental.TotalDepositsBalanceSheet

    TotalEquity: QuantConnect.Data.Fundamental.TotalEquityBalanceSheet

    TotalEquityAsReported: QuantConnect.Data.Fundamental.TotalEquityAsReportedBalanceSheet

    TotalEquityGrossMinorityInterest: QuantConnect.Data.Fundamental.TotalEquityGrossMinorityInterestBalanceSheet

    TotalFinancialLeaseObligations: QuantConnect.Data.Fundamental.TotalFinancialLeaseObligationsBalanceSheet

    TotalInvestments: QuantConnect.Data.Fundamental.TotalInvestmentsBalanceSheet

    TotalLiabilitiesAsReported: QuantConnect.Data.Fundamental.TotalLiabilitiesAsReportedBalanceSheet

    TotalLiabilitiesNetMinorityInterest: QuantConnect.Data.Fundamental.TotalLiabilitiesNetMinorityInterestBalanceSheet

    TotalNonCurrentAssets: QuantConnect.Data.Fundamental.TotalNonCurrentAssetsBalanceSheet

    TotalNonCurrentLiabilitiesNetMinorityInterest: QuantConnect.Data.Fundamental.TotalNonCurrentLiabilitiesNetMinorityInterestBalanceSheet

    TotalPartnershipCapital: QuantConnect.Data.Fundamental.TotalPartnershipCapitalBalanceSheet

    TotalTaxPayable: QuantConnect.Data.Fundamental.TotalTaxPayableBalanceSheet

    TradeandOtherPayablesNonCurrent: QuantConnect.Data.Fundamental.TradeandOtherPayablesNonCurrentBalanceSheet

    TradeAndOtherReceivablesNonCurrent: QuantConnect.Data.Fundamental.TradeAndOtherReceivablesNonCurrentBalanceSheet

    TradingandFinancialLiabilities: QuantConnect.Data.Fundamental.TradingandFinancialLiabilitiesBalanceSheet

    TradingAndOtherReceivable: QuantConnect.Data.Fundamental.TradingAndOtherReceivableBalanceSheet

    TradingAssets: QuantConnect.Data.Fundamental.TradingAssetsBalanceSheet

    TradingLiabilities: QuantConnect.Data.Fundamental.TradingLiabilitiesBalanceSheet

    TradingSecurities: QuantConnect.Data.Fundamental.TradingSecuritiesBalanceSheet

    TreasuryBillsandOtherEligibleBills: QuantConnect.Data.Fundamental.TreasuryBillsandOtherEligibleBillsBalanceSheet

    TreasurySharesNumber: QuantConnect.Data.Fundamental.TreasurySharesNumberBalanceSheet

    TreasuryStock: QuantConnect.Data.Fundamental.TreasuryStockBalanceSheet

    UnallocatedSurplus: QuantConnect.Data.Fundamental.UnallocatedSurplusBalanceSheet

    UnbilledReceivables: QuantConnect.Data.Fundamental.UnbilledReceivablesBalanceSheet

    UnearnedIncome: QuantConnect.Data.Fundamental.UnearnedIncomeBalanceSheet

    UnearnedPremiums: QuantConnect.Data.Fundamental.UnearnedPremiumsBalanceSheet

    UnpaidLossAndLossReserve: QuantConnect.Data.Fundamental.UnpaidLossAndLossReserveBalanceSheet

    UnrealizedGainLoss: QuantConnect.Data.Fundamental.UnrealizedGainLossBalanceSheet

    WaterProduction: QuantConnect.Data.Fundamental.WaterProductionBalanceSheet

    WorkingCapital: QuantConnect.Data.Fundamental.WorkingCapitalBalanceSheet

    WorkInProcess: QuantConnect.Data.Fundamental.WorkInProcessBalanceSheet
