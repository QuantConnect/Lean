from .__Fundamental_6 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class BeginningCashPositionCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The cash and equivalents balance at the beginning of the accounting period, as indicated on the Cash Flow statement.
    
    BeginningCashPositionCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BeginningCashPositionCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BiologicalAssetsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Biological assets include plants and animals.
    
    BiologicalAssetsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BiologicalAssetsBalanceSheet:
        pass

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BookValuePerShareGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's book value per share on a percentage basis. Morningstar calculates the growth percentage based on
                the common shareholder's equity reported in the Balance Sheet divided by the diluted shares outstanding within the company
                filings or reports.
    
    BookValuePerShareGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BookValuePerShareGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeMonths: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class BuildingsAndImprovementsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Fixed assets that specifically deal with the facilities a company owns. Include the improvements associated with buildings.
    
    BuildingsAndImprovementsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.BuildingsAndImprovementsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CapExGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The growth in the company's capital expenditures on a percentage basis. Morningstar calculates the growth percentage based on
                the capital expenditures reported in the Cash Flow Statement within the company filings or reports.
    
    CapExGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CapExGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CapExReportedCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Capital expenditure, capitalized software development cost, maintenance capital expenditure, etc. as reported by the company.
    
    CapExReportedCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CapExReportedCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CapExSalesRatio(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Capital Expenditure / Revenue
    
    CapExSalesRatio(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CapExSalesRatio:
        pass

    OneYear: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CapitalExpenditureAnnual5YrGrowth(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This is the compound annual growth rate of the company's capital spending over the last 5 years. Capital Spending is the sum of
                the Capital Expenditure items found in the Statement of Cash Flows.
    
    CapitalExpenditureAnnual5YrGrowth(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CapitalExpenditureAnnual5YrGrowth:
        pass

    FiveYears: float

    OneYear: float

    ThreeYears: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CapitalExpenditureCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Funds used by a company to acquire or upgrade physical assets such as property, industrial buildings or equipment. This
                type of outlay is made by companies to maintain or increase the scope of their operations. Capital expenditures are generally
                depreciated or depleted over their useful life, as distinguished from repairs, which are subtracted from the income of the current
                year.
    
    CapitalExpenditureCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CapitalExpenditureCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CapitalExpendituretoEBITDA(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Measures the amount a company is investing in its business relative to EBITDA generated in a given PeriodAsByte.
    
    CapitalExpendituretoEBITDA(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CapitalExpendituretoEBITDA:
        pass

    OneYear: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CapitalLeaseObligationsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Current Portion of Capital Lease Obligation plus Long Term Portion of Capital Lease Obligation.
    
    CapitalLeaseObligationsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CapitalLeaseObligationsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CapitalStockBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The total amount of stock authorized for issue by a corporation, including common and preferred stock.
    
    CapitalStockBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CapitalStockBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashAdvancesandLoansMadetoOtherPartiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash outlay for cash advances and loans made to other parties.
    
    CashAdvancesandLoansMadetoOtherPartiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashAdvancesandLoansMadetoOtherPartiesCashFlowStatement:
        pass

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
