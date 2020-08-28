from .__Fundamental_26 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class ExplorationDevelopmentAndMineralPropertyLeaseExpensesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Costs incurred in identifying areas that may warrant examination and in examining specific areas that are considered to have
                prospects of containing energy or metal reserves, including costs of drilling exploratory wells. Development expense is the
                capitalized costs incurred to obtain access to proved reserves and to provide facilities for extracting, treating, gathering and storing
                the energy and metal. Mineral property includes oil and gas wells, mines, and other natural deposits (including geothermal
                deposits). The payment for leasing those properties is called mineral property lease expense. Exploration expense is included in
                operation expenses for mining industry.
    
    ExplorationDevelopmentAndMineralPropertyLeaseExpensesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.ExplorationDevelopmentAndMineralPropertyLeaseExpensesIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FCFGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's free cash flow on a percentage basis. Morningstar calculates the growth percentage based on the
                underlying cash flow from operations and capital expenditures data reported in the Cash Flow Statement within the company filings
                or reports:   Free Cash Flow = Cash flow from operations - Capital Expenditures.
    
    FCFGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FCFGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FCFNetIncomeRatio(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Free Cash Flow / Net Income
    
    FCFNetIncomeRatio(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FCFNetIncomeRatio:
        pass

    OneYear: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FCFPerShareGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's free cash flow per share on a percentage basis. Morningstar calculates the growth percentage based
                on the free cash flow divided by average diluted shares outstanding reported in the Financial Statements within the company filings
                or reports.
    
    FCFPerShareGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FCFPerShareGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeMonths: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FCFSalesRatio(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Free Cash flow / Revenue
    
    FCFSalesRatio(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FCFSalesRatio:
        pass

    OneYear: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FCFtoCFO(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Indicates the percentage of a company's operating cash flow is free to be invested in its business after capital expenditures.
    
    FCFtoCFO(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FCFtoCFO:
        pass

    OneYear: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FederalFundsPurchasedAndSecuritiesSoldUnderAgreementToRepurchaseBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This liability refers to the amount shown on the books that a bank with insufficient reserves borrows, at the federal funds rate, from
                another bank to meet its reserve requirements; and the amount of securities that an institution sells and agrees to repurchase at a
                specified date for a specified price, net of any reductions or offsets.
    
    FederalFundsPurchasedAndSecuritiesSoldUnderAgreementToRepurchaseBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FederalFundsPurchasedAndSecuritiesSoldUnderAgreementToRepurchaseBalanceSheet:
        pass

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FederalFundsPurchasedBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The amount borrowed by a bank, at the federal funds rate, from another bank to meet its reserve requirements.  This item is
                typically available for the bank industry.
    
    FederalFundsPurchasedBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FederalFundsPurchasedBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResellBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This asset refers to very-short-term loans of funds to other banks and securities dealers.
    
    FederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResellBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FederalFundsSoldAndSecuritiesPurchaseUnderAgreementsToResellBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FederalFundsSoldBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Federal funds transactions involve lending (federal funds sold) or borrowing (federal funds purchased) of immediately available
                reserve balances.  This item is typically available for the bank industry.
    
    FederalFundsSoldBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FederalFundsSoldBalanceSheet:
        pass

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FederalHomeLoanBankStockBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Federal Home Loan Bank stock represents an equity interest in a FHLB. It does not have a readily determinable fair value because
                its ownership is restricted and it lacks a market (liquidity).  This item is typically available for the bank industry.
    
    FederalHomeLoanBankStockBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FederalHomeLoanBankStockBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class FeeRevenueAndOtherIncomeIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of fees, commissions, and other income.
    
    FeeRevenueAndOtherIncomeIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.FeeRevenueAndOtherIncomeIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
