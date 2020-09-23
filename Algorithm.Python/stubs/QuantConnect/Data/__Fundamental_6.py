from .__Fundamental_7 import *
import typing
import System.IO
import System.Collections.Generic
import System
import QuantConnect.Data.Fundamental.MultiPeriodField
import QuantConnect.Data.Fundamental
import QuantConnect.Data
import QuantConnect
import datetime


class CashAndCashEquivalentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Includes unrestricted cash on hand, money market instruments and other debt securities which can be converted to cash
                immediately.
    
    CashAndCashEquivalentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashAndCashEquivalentsBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashAndDueFromBanksBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Includes cash on hand (currency and coin), cash items in process of collection, non-interest bearing deposits due from other
                financial institutions (including corporate credit unions), and balances with the Federal Reserve Banks, Federal Home Loan Banks
                and central banks.
    
    CashAndDueFromBanksBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashAndDueFromBanksBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash includes currency on hand as well as demand deposits with banks or financial institutions. It also includes other kinds of
                accounts that have the general characteristics of demand deposits in that the customer may deposit additional funds at any time
                and also effectively may withdraw funds at any time without prior notice or penalty.
    
    CashBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashCashEquivalentsAndFederalFundsSoldBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of cash, cash equivalents, and federal funds sold.
    
    CashCashEquivalentsAndFederalFundsSoldBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashCashEquivalentsAndFederalFundsSoldBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashCashEquivalentsAndMarketableSecuritiesBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    The aggregate amount of cash, cash equivalents, and marketable securities.
    
    CashCashEquivalentsAndMarketableSecuritiesBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashCashEquivalentsAndMarketableSecuritiesBalanceSheet:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashConversionCycle(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Days In Inventory + Days In Sales - Days In Payment
    
    CashConversionCycle(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashConversionCycle:
        pass

    OneYear: float

    SixMonths: float

    ThreeMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashDividendsForMinoritiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash Distribution of earnings to Minority Stockholders.
    
    CashDividendsForMinoritiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashDividendsForMinoritiesCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashDividendsPaidCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Payments for the cash dividends declared by an entity to shareholders during the PeriodAsByte. This element includes paid and unpaid
                dividends declared during the period for both common and preferred stock.
    
    CashDividendsPaidCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashDividendsPaidCashFlowStatement:
        pass

    NineMonths: float

    OneMonth: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashEquivalentsBalanceSheet(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash equivalents, excluding items classified as marketable securities, include short-term, highly liquid investments that are both
                readily convertible to known amounts of cash, and so near their maturity that they present insignificant risk of changes in value
                because of changes in interest rates.  Generally, only investments with original maturities of three months or less qualify under this
                definition. Original maturity means original maturity to the entity holding the investment. For example, both a three-month US
                Treasury bill and a three-year Treasury note purchased three months from maturity qualify as cash equivalents. However, a Treasury
                note purchased three years ago does not become a cash equivalent when its remaining maturity is three months.
    
    CashEquivalentsBalanceSheet(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashEquivalentsBalanceSheet:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashFlowFromContinuingFinancingActivitiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash generated by or used in financing activities of continuing operations; excludes cash flows from discontinued operations.
    
    CashFlowFromContinuingFinancingActivitiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashFlowFromContinuingFinancingActivitiesCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]


class CashFlowFromContinuingInvestingActivitiesCashFlowStatement(QuantConnect.Data.Fundamental.MultiPeriodField):
    """
    Cash generated by or used in investing activities of continuing operations; excludes cash flows from discontinued operations.
    
    CashFlowFromContinuingInvestingActivitiesCashFlowStatement(store: IDictionary[str, Decimal])
    """
    def GetPeriodValue(self, period: str) -> float:
        pass

    def SetPeriodValue(self, period: str, value: float) -> None:
        pass

    def __init__(self, store: System.Collections.Generic.IDictionary[str, float]) -> QuantConnect.Data.Fundamental.CashFlowFromContinuingInvestingActivitiesCashFlowStatement:
        pass

    NineMonths: float

    SixMonths: float

    ThreeMonths: float

    TwelveMonths: float

    TwoMonths: float

    Store: typing.List[QuantConnect.Data.Fundamental.MultiPeriodField.PeriodField]
