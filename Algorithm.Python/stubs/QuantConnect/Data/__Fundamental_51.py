from .__Fundamental_52 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class OtherNonCurrentLiabilitiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This item is usually not available for bank and insurance industries.
    
    OtherNonCurrentLiabilitiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherNonCurrentLiabilitiesBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherNonInterestExpenseIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    All other non interest expense that is not otherwise classified
    
    OtherNonInterestExpenseIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherNonInterestExpenseIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherNonInterestIncomeIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Usually available for the banking industry.  This is Non-Interest Income that is not otherwise classified.
    
    OtherNonInterestIncomeIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherNonInterestIncomeIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherNonOperatingExpensesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Other expenses of the company that cannot be identified by other specific items in the Non-Operating section.
    
    OtherNonOperatingExpensesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherNonOperatingExpensesIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherNonOperatingIncomeExpensesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total other income and expense of the company that cannot be identified by other specific items in the Non-Operating section.
    
    OtherNonOperatingIncomeExpensesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherNonOperatingIncomeExpensesIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherNonOperatingIncomeIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Other income of the company that cannot be identified by other specific items in the Non-Operating section.
    
    OtherNonOperatingIncomeIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherNonOperatingIncomeIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherOperatingExpensesIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of operating expenses associated with normal operations. Will not include any gain, loss, benefit, or income;
                and its value reported by the company should be <0.
    
    OtherOperatingExpensesIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherOperatingExpensesIncomeStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherOperatingIncomeTotalIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Total Other Operating Income- including interest income, dividend income and other types of operating income.
    
    OtherOperatingIncomeTotalIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherOperatingIncomeTotalIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherOperatingInflowsOutflowsofCashCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Any other cash inflows or outflows in the Operating Cash Flow section, not accounted for in the other specified items.
    
    OtherOperatingInflowsOutflowsofCashCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherOperatingInflowsOutflowsofCashCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherPayableBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Payables and Accrued Expenses that are not defined as Trade, Tax or Dividends related.
    
    OtherPayableBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherPayableBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherPropertiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Other fixed assets not otherwise classified.
    
    OtherPropertiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherPropertiesBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class OtherRealEstateOwnedBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The Carrying amount as of the balance sheet date of other real estate, which may include real estate investments, real estate loans
                that qualify as investments in real estate, and premises that are no longer used in operations may also be included in real estate
                owned. This does not include real estate assets taken in settlement of troubled loans through surrender or foreclosure.  This item is
                typically available for bank industry.
    
    OtherRealEstateOwnedBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.OtherRealEstateOwnedBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
