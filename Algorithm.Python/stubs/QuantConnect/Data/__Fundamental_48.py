from .__Fundamental_49 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class OperationMargin(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Refers to the ratio of operating income to revenue. Morningstar calculates the ratio by using the underlying data reported in the
                company filings or reports:   Operating Income / Revenue.
    
    OperationMargin(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OperationMargin:
        pass

    NineMonths: float

    OneMonth: float

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OperationRatios(System.object):
    """
    Definition of the OperationRatios class
    
    OperationRatios()
    """
    def UpdateValues(self, update: QuantConnect.Data.Fundamental.OperationRatios) -> None:
        pass

    AssetsTurnover: QuantConnect.Data.Fundamental.AssetsTurnover

    AVG5YrsROIC: QuantConnect.Data.Fundamental.AVG5YrsROIC

    CapExGrowth: QuantConnect.Data.Fundamental.CapExGrowth

    CapExSalesRatio: QuantConnect.Data.Fundamental.CapExSalesRatio

    CapitalExpenditureAnnual5YrGrowth: QuantConnect.Data.Fundamental.CapitalExpenditureAnnual5YrGrowth

    CapitalExpendituretoEBITDA: QuantConnect.Data.Fundamental.CapitalExpendituretoEBITDA

    CashConversionCycle: QuantConnect.Data.Fundamental.CashConversionCycle

    CashFlowfromFinancingGrowth: QuantConnect.Data.Fundamental.CashFlowfromFinancingGrowth

    CashFlowfromInvestingGrowth: QuantConnect.Data.Fundamental.CashFlowfromInvestingGrowth

    CashRatio: QuantConnect.Data.Fundamental.CashRatio

    CashRatioGrowth: QuantConnect.Data.Fundamental.CashRatioGrowth

    CashtoTotalAssets: QuantConnect.Data.Fundamental.CashtoTotalAssets

    CFOGrowth: QuantConnect.Data.Fundamental.CFOGrowth

    CommonEquityToAssets: QuantConnect.Data.Fundamental.CommonEquityToAssets

    CurrentRatio: QuantConnect.Data.Fundamental.CurrentRatio

    CurrentRatioGrowth: QuantConnect.Data.Fundamental.CurrentRatioGrowth

    DaysInInventory: QuantConnect.Data.Fundamental.DaysInInventory

    DaysInPayment: QuantConnect.Data.Fundamental.DaysInPayment

    DaysInSales: QuantConnect.Data.Fundamental.DaysInSales

    DebttoAssets: QuantConnect.Data.Fundamental.DebttoAssets

    EBITDAGrowth: QuantConnect.Data.Fundamental.EBITDAGrowth

    EBITDAMargin: QuantConnect.Data.Fundamental.EBITDAMargin

    EBITMargin: QuantConnect.Data.Fundamental.EBITMargin

    ExpenseRatio: QuantConnect.Data.Fundamental.ExpenseRatio

    FCFGrowth: QuantConnect.Data.Fundamental.FCFGrowth

    FCFNetIncomeRatio: QuantConnect.Data.Fundamental.FCFNetIncomeRatio

    FCFSalesRatio: QuantConnect.Data.Fundamental.FCFSalesRatio

    FCFtoCFO: QuantConnect.Data.Fundamental.FCFtoCFO

    FinancialLeverage: QuantConnect.Data.Fundamental.FinancialLeverage

    FixAssetsTuronver: QuantConnect.Data.Fundamental.FixAssetsTuronver

    GrossMargin: QuantConnect.Data.Fundamental.GrossMargin

    GrossMargin5YrAvg: QuantConnect.Data.Fundamental.GrossMargin5YrAvg

    GrossProfitAnnual5YrGrowth: QuantConnect.Data.Fundamental.GrossProfitAnnual5YrGrowth

    InterestCoverage: QuantConnect.Data.Fundamental.InterestCoverage

    InventoryTurnover: QuantConnect.Data.Fundamental.InventoryTurnover

    LongTermDebtEquityRatio: QuantConnect.Data.Fundamental.LongTermDebtEquityRatio

    LongTermDebtTotalCapitalRatio: QuantConnect.Data.Fundamental.LongTermDebtTotalCapitalRatio

    LossRatio: QuantConnect.Data.Fundamental.LossRatio

    NetIncomeContOpsGrowth: QuantConnect.Data.Fundamental.NetIncomeContOpsGrowth

    NetIncomeGrowth: QuantConnect.Data.Fundamental.NetIncomeGrowth

    NetIncomePerEmployee: QuantConnect.Data.Fundamental.NetIncomePerEmployee

    NetMargin: QuantConnect.Data.Fundamental.NetMargin

    NormalizedNetProfitMargin: QuantConnect.Data.Fundamental.NormalizedNetProfitMargin

    NormalizedROIC: QuantConnect.Data.Fundamental.NormalizedROIC

    OperationIncomeGrowth: QuantConnect.Data.Fundamental.OperationIncomeGrowth

    OperationMargin: QuantConnect.Data.Fundamental.OperationMargin

    OperationRevenueGrowth3MonthAvg: QuantConnect.Data.Fundamental.OperationRevenueGrowth3MonthAvg

    PaymentTurnover: QuantConnect.Data.Fundamental.PaymentTurnover

    PostTaxMargin5YrAvg: QuantConnect.Data.Fundamental.PostTaxMargin5YrAvg

    PretaxMargin: QuantConnect.Data.Fundamental.PretaxMargin

    PreTaxMargin5YrAvg: QuantConnect.Data.Fundamental.PreTaxMargin5YrAvg

    ProfitMargin5YrAvg: QuantConnect.Data.Fundamental.ProfitMargin5YrAvg

    QuickRatio: QuantConnect.Data.Fundamental.QuickRatio

    ReceivableTurnover: QuantConnect.Data.Fundamental.ReceivableTurnover

    RegressionGrowthOperatingRevenue5Years: QuantConnect.Data.Fundamental.RegressionGrowthOperatingRevenue5Years

    RevenueGrowth: QuantConnect.Data.Fundamental.RevenueGrowth

    ROA: QuantConnect.Data.Fundamental.ROA

    ROA5YrAvg: QuantConnect.Data.Fundamental.ROA5YrAvg

    ROE: QuantConnect.Data.Fundamental.ROE

    ROE5YrAvg: QuantConnect.Data.Fundamental.ROE5YrAvg

    ROIC: QuantConnect.Data.Fundamental.ROIC

    SalesPerEmployee: QuantConnect.Data.Fundamental.SalesPerEmployee

    SolvencyRatio: QuantConnect.Data.Fundamental.SolvencyRatio

    StockholdersEquityGrowth: QuantConnect.Data.Fundamental.StockholdersEquityGrowth

    TaxRate: QuantConnect.Data.Fundamental.TaxRate

    TotalAssetsGrowth: QuantConnect.Data.Fundamental.TotalAssetsGrowth

    TotalDebtEquityRatio: QuantConnect.Data.Fundamental.TotalDebtEquityRatio

    TotalDebtEquityRatioGrowth: QuantConnect.Data.Fundamental.TotalDebtEquityRatioGrowth

    TotalLiabilitiesGrowth: QuantConnect.Data.Fundamental.TotalLiabilitiesGrowth

    WorkingCapitalTurnoverRatio: QuantConnect.Data.Fundamental.WorkingCapitalTurnoverRatio



class OperationRevenueGrowth3MonthAvg(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's operating revenue on a percentage basis. Morningstar calculates the growth percentage based on
                the underlying operating revenue data reported in the Income Statement within the company filings or reports.
    
    OperationRevenueGrowth3MonthAvg(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OperationRevenueGrowth3MonthAvg:
        pass

    FiveYears: float

    OneYear: float

    ThreeMonths: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OrdinarySharesNumberBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Number of Common or Ordinary Shares.
    
    OrdinarySharesNumberBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OrdinarySharesNumberBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Other non-current assets that are not otherwise classified.
    
    OtherAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherAssetsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherBorrowedFundsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Other borrowings by the bank to fund its activities that cannot be identified by other specific items in the Liabilities section.
    
    OtherBorrowedFundsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherBorrowedFundsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherCapitalStockBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Other Capital Stock that is not otherwise classified.
    
    OtherCapitalStockBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherCapitalStockBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherCashAdjustExcludeFromChangeinCashCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Other changes to cash and cash equivalents during the accounting PeriodAsByte.
    
    OtherCashAdjustExcludeFromChangeinCashCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherCashAdjustExcludeFromChangeinCashCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
