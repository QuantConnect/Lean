from .__Fundamental_36 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class InterestPaidDirectCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Interest paid on loans, debt or borrowings, in the direct cash flow.
    
    InterestPaidDirectCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InterestPaidDirectCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InterestPaidSupplementalDataCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The amount of cash paid during the current period for interest owed on money borrowed; including amount of interest capitalized.
    
    InterestPaidSupplementalDataCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InterestPaidSupplementalDataCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InterestPayableBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Sum of the carrying values as of the balance sheet date of interest payable on all forms of debt, including trade payable that has
                been incurred.
    
    InterestPayableBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InterestPayableBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InterestReceivedCFICashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Interest received by the company, in the Investing Cash Flow section.
    
    InterestReceivedCFICashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InterestReceivedCFICashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InterestReceivedCFOCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Interest received by the company, in the Operating Cash Flow section.
    
    InterestReceivedCFOCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InterestReceivedCFOCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InterestReceivedDirectCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Interest received by the company, in the direct cash flow.
    
    InterestReceivedDirectCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InterestReceivedDirectCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InventoriesAdjustmentsAllowancesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    This item represents certain charges made in the current period in inventory resulting from such factors as breakage, spoilage,
                employee theft and shoplifting. This item is typically available for manufacturing, mining and utility industries.
    
    InventoriesAdjustmentsAllowancesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InventoriesAdjustmentsAllowancesBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InventoryBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    A company's merchandise, raw materials, and finished and unfinished products which have not yet been sold.
    
    InventoryBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InventoryBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InventoryTurnover(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cost Of Goods Sold / Average Inventory
    
    InventoryTurnover(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InventoryTurnover:
        pass

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InvestedCapitalBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Invested capital = common shareholders' equity + long term debt + current debt
    
    InvestedCapitalBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InvestedCapitalBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InvestingCashFlowCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    An item on the cash flow statement that reports the aggregate change in a company's cash position resulting from any gains (or
                losses) from investments in the financial markets and operating subsidiaries, and changes resulting from amounts spent on
                investments in capital assets such as plant and equipment.
    
    InvestingCashFlowCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InvestingCashFlowCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class InvestmentBankingProfitIncomeStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Includes (1) underwriting revenue (the spread between the resale price received and the cost of the securities and related
                expenses) generated through the purchasing, distributing and reselling of new issues of securities (alternatively, could be a
                secondary offering of a large block of previously issued securities); and (2) fees earned for mergers, acquisitions, divestitures,
                restructurings, and other types of financial advisory services. This item is usually only available for bank industry.
    
    InvestmentBankingProfitIncomeStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.InvestmentBankingProfitIncomeStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
