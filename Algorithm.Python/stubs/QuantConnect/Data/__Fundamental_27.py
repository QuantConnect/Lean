from .__Fundamental_28 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class FinancialLiabilitiesDesignatedasFairValueThroughProfitorLossTotalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Financial liabilities that are held at fair value through profit or loss.
    
    FinancialLiabilitiesDesignatedasFairValueThroughProfitorLossTotalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinancialLiabilitiesDesignatedasFairValueThroughProfitorLossTotalBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FinancialLiabilitiesMeasuredatAmortizedCostTotalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Financial liabilities carried at amortized cost.
    
    FinancialLiabilitiesMeasuredatAmortizedCostTotalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinancialLiabilitiesMeasuredatAmortizedCostTotalBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FinancialLiabilitiesNonCurrentBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Financial related liabilities due beyond one year, including long term debt, capital leases and derivative liabilities.
    
    FinancialLiabilitiesNonCurrentBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinancialLiabilitiesNonCurrentBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FinancialOrDerivativeInvestmentCurrentLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Financial instruments that are linked to a specific financial instrument or indicator or commodity, and through which specific
                financial risks can be traded in financial markets in their own right, such as financial options, futures, forwards, etc.
    
    FinancialOrDerivativeInvestmentCurrentLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinancialOrDerivativeInvestmentCurrentLiabilitiesBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FinancialStatements(System.object):
    """
    Definition of the FinancialStatements class
    
    FinancialStatements()
    """
    def UpdateValues(self, update: QuantConnect.Data.Fundamental.FinancialStatements) -> None:
        pass

    AccessionNumber: str

    AuditorReportStatus: str

    BalanceSheet: QuantConnect.Data.Fundamental.BalanceSheet

    CashFlowStatement: QuantConnect.Data.Fundamental.CashFlowStatement

    FileDate: datetime.datetime

    FormType: str

    IncomeStatement: QuantConnect.Data.Fundamental.IncomeStatement

    InventoryValuationMethod: str

    NumberOfShareHolders: int

    PeriodAuditor: str

    PeriodEndingDate: datetime.datetime

    PeriodType: str

    TotalRiskBasedCapital: QuantConnect.Data.Fundamental.TotalRiskBasedCapital



class FinancingCashFlowCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The net cash inflow (outflow) from financing activity for the period, which involve changes to the long-term liabilities and
                stockholders' equity.
    
    FinancingCashFlowCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinancingCashFlowCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FineFundamental(QuantConnect.Data.BaseData, QuantConnect.Data.IBaseData):
    """
    Definition of the FineFundamental class
    
    FineFundamental()
    """
    @staticmethod
    def CreateUniverseSymbol(market: str, addGuid: bool) -> QuantConnect.Symbol:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.SubscriptionDataSource:
        pass

    @typing.overload
    def GetSource(self, config: QuantConnect.Data.SubscriptionDataConfig, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> str:
        pass

    def GetSource(self, *args) -> str:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, stream: System.IO.StreamReader, date: datetime.datetime, isLiveMode: bool) -> QuantConnect.Data.BaseData:
        pass

    @typing.overload
    def Reader(self, config: QuantConnect.Data.SubscriptionDataConfig, line: str, date: datetime.datetime, datafeed: QuantConnect.DataFeedEndpoint) -> QuantConnect.Data.BaseData:
        pass

    def Reader(self, *args) -> QuantConnect.Data.BaseData:
        pass

    def UpdateValues(self, update: QuantConnect.Data.Fundamental.FineFundamental) -> None:
        pass

    AssetClassification: QuantConnect.Data.Fundamental.AssetClassification

    CompanyProfile: QuantConnect.Data.Fundamental.CompanyProfile

    CompanyReference: QuantConnect.Data.Fundamental.CompanyReference

    EarningRatios: QuantConnect.Data.Fundamental.EarningRatios

    EarningReports: QuantConnect.Data.Fundamental.EarningReports

    EndTime: datetime.datetime

    FinancialStatements: QuantConnect.Data.Fundamental.FinancialStatements

    MarketCap: int

    OperationRatios: QuantConnect.Data.Fundamental.OperationRatios

    SecurityReference: QuantConnect.Data.Fundamental.SecurityReference

    ValuationRatios: QuantConnect.Data.Fundamental.ValuationRatios



class FinishedGoodsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The carrying amount as of the balance sheet date of merchandise or goods held by the company that are readily available for sale.
                This item is typically available for mining and manufacturing industries.
    
    FinishedGoodsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FinishedGoodsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FixAssetsTuronver(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Revenue / Average PP&E
    
    FixAssetsTuronver(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FixAssetsTuronver:
        pass

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FixedAssetsRevaluationReserveBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Reserves created by revaluation of assets.
    
    FixedAssetsRevaluationReserveBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FixedAssetsRevaluationReserveBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FixedMaturityInvestmentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This asset refers to types of investments that may be contained within the fixed maturity category which securities are having a
                stated final repayment date. Examples of items within this category may include bonds, including convertibles and bonds with
                warrants, and redeemable preferred stocks.
    
    FixedMaturityInvestmentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FixedMaturityInvestmentsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
