from .__Fundamental_61 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class RestrictedCommonStockBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Shares of stock for which sale is contractually or governmentally restricted for a given period of time. Stock that is acquired through
                an employee stock option plan or other private means may not be transferred. Restricted stock must be traded in compliance with
                special SEC regulations.
    
    RestrictedCommonStockBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.RestrictedCommonStockBalanceSheet:
        pass

    NineMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class RestrictedInvestmentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Investments whose use is restricted in whole or in part, generally by contractual agreements or regulatory requirements. This item
                is usually only available for bank industry.
    
    RestrictedInvestmentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.RestrictedInvestmentsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class RestructuringAndMergernAcquisitionIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Expenses are related to restructuring, merger, or acquisitions. Restructuring expenses are charges associated with the
                consolidation and relocation of operations, disposition or abandonment of operations or productive assets. Merger and acquisition
                expenses are the amount of costs of a business combination including legal, accounting, and other costs that were charged to
                expense during the PeriodAsByte.
    
    RestructuringAndMergernAcquisitionIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.RestructuringAndMergernAcquisitionIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class RetainedEarningsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The cumulative net income of the company from the date of its inception (or reorganization) to the date of the financial statement
                less the cumulative distributions to shareholders either directly (dividends) or indirectly (treasury stock).
    
    RetainedEarningsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.RetainedEarningsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class RevenueGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's revenue on a percentage basis. Morningstar calculates the growth percentage based on the
                underlying revenue data reported in the Income Statement within the company filings or reports.
    
    RevenueGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.RevenueGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeMonths: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ROA(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Net Income / Average Total Assets
    
    ROA(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ROA:
        pass

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ROA5YrAvg(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This is the simple average of the company's ROA over the last 5 years. Return on asset is calculated by dividing a company's annual
                earnings by its average total assets.
    
    ROA5YrAvg(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ROA5YrAvg:
        pass

    FiveYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ROE(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Net Income / Average Total Common Equity
    
    ROE(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ROE:
        pass

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ROE5YrAvg(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This is the simple average of the company's ROE over the last 5 years. Return on equity reveals how much profit a company has
                earned in comparison to the total amount of shareholder equity found on the balance sheet.
    
    ROE5YrAvg(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ROE5YrAvg:
        pass

    FiveYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class ROIC(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Net Income / (Total Equity + Long-term Debt and Capital Lease Obligation + Short-term Debt and Capital Lease Obligation)
    
    ROIC(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ROIC:
        pass

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SalariesAndWagesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    All salary, wages, compensation, management fees, and employee benefit expenses.
    
    SalariesAndWagesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SalariesAndWagesIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SaleOfBusinessCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Proceeds received from selling a business including proceeds from a subsidiary, and proceeds from an affiliated company.
    
    SaleOfBusinessCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SaleOfBusinessCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class SaleOfIntangiblesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The amount of capital inflow from the sale of all kinds of intangible assets.
    
    SaleOfIntangiblesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.SaleOfIntangiblesCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
